//
//  MaxWebRequest.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Jonathan Liu on 6/10/2025.
//  Copyright © 2025 AppLovin. All rights reserved.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using AppLovinMax.ThirdParty.MiniJson;

namespace AppLovinMax.Internal
{
    public enum WebRequestType
    {
        Get,
        Post
    }

    public class WebRequestConfig
    {
        /// <summary>
        /// Request endpoint. Task will not execute if one is not set.
        /// </summary>
        public string EndPoint { get; set; }

        /// <summary>
        /// Request method. GET is used by default.
        /// </summary>
        public WebRequestType RequestType { get; set; } = WebRequestType.Get;

        /// <summary>
        /// The download handler for the web request.
        /// </summary>
        public DownloadHandler DownloadHandler { get; set; } = new DownloadHandlerBuffer();

        /// <summary>
        /// Parameters that will be attached to the request.
        /// </summary>
        public Dictionary<string, string> QueryParams { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Headers that will be added to the request.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Request message data that will be sent with the request.
        /// If both <see cref="Data"/> and <see cref="JsonString"/> are set, <see cref="Data"/> takes precedence and will be serialized to JSON.
        /// </summary>
        public object Data { get; set; } = null;

        /// <summary>
        /// Request message data in JSON format that will be sent with the request.
        /// If both <see cref="Data"/> and <see cref="JsonString"/> are set, <see cref="Data"/> takes precedence and will be serialized to JSON.
        /// </summary>
        public string JsonString { get; set; } = "";

        /// <summary>
        /// The max number of attempts to make the web request before stopping.
        /// </summary>
        public int MaxRequestAttempts { get; set; } = 3;

        /// <summary>
        /// Timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 60;
    }

    public class WebResponse
    {
        /// <summary>
        /// Whether the request succeeded.
        /// </summary>
        public bool IsSuccess { get; } = false;

        /// <summary>
        /// The completed UnityWebRequest.
        /// </summary>
        public string ResponseMessage { get; } = "";

        /// <summary>
        /// The error message if the request failed.
        /// </summary>
        public string ErrorMessage { get; } = "";

        public WebResponse(UnityWebRequest request)
        {
            if (request == null) return;

#if UNITY_2020_1_OR_NEWER
            IsSuccess = request.result == UnityWebRequest.Result.Success;
#else
            IsSuccess = !(request.isNetworkError || request.isHttpError);
#endif
            // Only DownloadHandlerBuffer should try to access the text
            if (request.downloadHandler is DownloadHandlerBuffer)
            {
                ResponseMessage = request.downloadHandler.text;
            }

            ErrorMessage = request.error;
        }
    }

    public class MaxWebRequest
    {
        private const int WaitBetweenRetriesSeconds = 1;
        private readonly WebRequestConfig webRequestConfig;
        private UnityWebRequest webRequest;
        private bool isSending;

        public MaxWebRequest(WebRequestConfig config)
        {
            if (config == null)
            {
                MaxSdkLogger.E("WebRequestConfig cannot be null. Please provide a valid configuration.");
                return;
            }

            webRequestConfig = config;
        }

        /// <summary>
        /// Sends a web request using coroutines.
        /// </summary>
        /// <param name="callback">
        /// A callback invoked with the resulting <see cref="WebResponse"/> object.
        /// </param>
        public IEnumerator Send(Action<WebResponse> callback)
        {
            yield return SendInternal(
                request => request.SendAndWait(),
                response => { callback?.Invoke(response); });
        }

        /// <summary>
        /// Sends a web request synchronously and returns the response.
        /// </summary>
        /// <returns>Returns a <see cref="WebResponse"/> object.</returns>
        public WebResponse SendSync()
        {
            var finalResponse = new WebResponse(null);

            SendInternal(
                waitFunc: request =>
                {
                    request.SendWebRequest();
                    while (!request.isDone) { } // Block until the request is done

                    return null; // We don't use IEnumerator for sync version
                },
                onComplete: response => finalResponse = response
            ).MoveNext(); // Needed to start the loop (since it's still an IEnumerator)

            return finalResponse;
        }

        public void Abort()
        {
            if (webRequest != null && !webRequest.isDone)
            {
                webRequest.Abort();
            }
        }

        /// <summary>
        /// Sends a web request using the current WebRequestConfig with automatic retries on failure.
        /// </summary>
        /// <param name="waitFunc">
        /// The function to use for waiting on the web request to complete.
        /// </param>
        /// /// <param name="onComplete">
        /// The callback invoked when the web request completes, with a <see cref="WebResponse"/> object containing the result.
        /// </param>
        private IEnumerator SendInternal(Func<UnityWebRequest, IEnumerator> waitFunc, Action<WebResponse> onComplete)
        {
            if (isSending || string.IsNullOrEmpty(webRequestConfig.EndPoint))
            {
                var errorString = isSending ? "Web Request currently being sent. Please send another request after the current one has finished." : "Web request endpoint is null or empty.";
                MaxSdkLogger.E(errorString);
                onComplete(new WebResponse(null));
            }

            isSending = true;
            try
            {
                for (var attempt = 1; attempt <= webRequestConfig.MaxRequestAttempts; attempt++)
                {
                    using (var request = CreateWebRequest())
                    {
                        // Hold a reference to the request so we can Abort the request if needed.
                        webRequest = request;

                        var wait = waitFunc(request);
                        if (wait != null)
                            yield return wait;

                        var webResponse = new WebResponse(request);

                        if (webResponse.IsSuccess)
                        {
                            onComplete(webResponse);
                            yield break;
                        }

                        if (attempt < webRequestConfig.MaxRequestAttempts)
                        {
                            MaxSdkLogger.UserWarning($"Error: {request.error}, Attempt {attempt} failed... Retrying request");
                        }
                        else
                        {
                            // All attempts have failed. Send error callback.
                            MaxSdkLogger.UserError($"Failed to make web request after {webRequestConfig.MaxRequestAttempts} attempts.");
                            onComplete(webResponse);
                        }
                    }

                    yield return new WaitForSeconds(WaitBetweenRetriesSeconds);
                }
            }
            finally
            {
                webRequest = null;
                isSending = false;
            }
        }

        /// <summary>
        /// Creates and returns a web request using the given configuration.
        /// </summary>
        /// <returns>Returns the web request that was created using the instance's WebRequestConfiguration.</returns>
        private UnityWebRequest CreateWebRequest()
        {
            var url = BuildURL();
            var request = new UnityWebRequest(url, webRequestConfig.RequestType.ToHttpMethodString())
            {
                downloadHandler = webRequestConfig.DownloadHandler,
                timeout = webRequestConfig.TimeoutSeconds
            };

            // Set request upload data if needed
            if (webRequestConfig.Data != null || MaxSdkUtils.IsValidString(webRequestConfig.JsonString))
            {
                var jsonString = webRequestConfig.Data != null ? Json.Serialize(webRequestConfig.Data) : webRequestConfig.JsonString;
                var rawData = Encoding.UTF8.GetBytes(jsonString);
                request.uploadHandler = new UploadHandlerRaw(rawData);
            }

            // Set request headers
            foreach (var header in webRequestConfig.Headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }

            return request;
        }

        /// <summary>
        /// Builds a URL with the endpoint and query parameters from the instance's WebRequestConfiguration.
        /// </summary>
        /// <returns>Returns a formatted URL built using the endpoint and query parameters.</returns>
        private string BuildURL()
        {
            if (webRequestConfig.QueryParams.Count == 0) return webRequestConfig.EndPoint;

            var uriBuilder = new UriBuilder(webRequestConfig.EndPoint);
            uriBuilder.Query = webRequestConfig.QueryParams.ToQueryString();
            return uriBuilder.ToString();
        }
    }

    public static class MaxWebRequestExtension
    {
        internal static IEnumerator SendAndWait(this UnityWebRequest request)
        {
#if UNITY_EDITOR
            var operation = request.SendWebRequest();

            // In the Unity Editor, `yield return request.SendWebRequest()` fails, so we manually poll `isDone` in a loop.
            while (!operation.isDone) yield return new WaitForSeconds(0.1f);
#else
            yield return request.SendWebRequest();
#endif
        }

        internal static string ToHttpMethodString(this WebRequestType type)
        {
            switch (type)
            {
                case WebRequestType.Get:
                    return "GET";
                case WebRequestType.Post:
                    return "POST";
                default:
                    return "GET";
            }
        }

        internal static string ToQueryString(this Dictionary<string, string> queries)
        {
            var queryBuilder = new StringBuilder();
            foreach (var query in queries)
            {
                if (query.Key == null || query.Value == null) continue;

                queryBuilder.Append(queryBuilder.Length == 0 ? "?" : "&");
                queryBuilder.AppendFormat("{0}={1}", Uri.EscapeDataString(query.Key), Uri.EscapeDataString(query.Value));
            }

            return queryBuilder.ToString();
        }
    }
}
