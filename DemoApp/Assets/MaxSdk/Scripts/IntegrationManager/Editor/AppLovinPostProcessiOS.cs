//
//  MaxIntegrationManager.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 8/29/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

#if UNITY_IOS || UNITY_IPHONE

using System.Text;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    /// <summary>
    /// Adds AppLovin Quality Service to the iOS project once the project has been exported.
    ///
    /// 1. Downloads the Quality Service ruby script.
    /// 2. Runs the script using Ruby which integrates AppLovin Quality Service to the project.
    /// </summary>
    public class AppLovinPostProcessiOS
    {
        private const string OutputFileName = "AppLovinQualityServiceSetup.rb";

        [PostProcessBuild(int.MaxValue)] // We want to run Quality Service script last.
        public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (!AppLovinSettings.Instance.QualityServiceEnabled) return;

            var sdkKey = AppLovinSettings.Instance.SdkKey;
            if (string.IsNullOrEmpty(sdkKey))
            {
                MaxSdkLogger.UserError("Failed to install AppLovin Quality Service plugin. SDK Key is empty. Please enter the AppLovin SDK Key in the Integration Manager.");
                return;
            }

            var outputFilePath = Path.Combine(buildPath, OutputFileName);

            // Check if Quality Service is already installed.
            if (File.Exists(outputFilePath) && Directory.Exists(Path.Combine(buildPath, "AppLovinQualityService")))
            {
                // TODO: Check if there is a way to validate if the SDK key matches the script. Else the pub can't use append when/if they change the SDK Key.
                return;
            }

            // Download the ruby script needed to install Quality Service
            var downloadHandler = new DownloadHandlerFile(outputFilePath);
            var postJson = string.Format("{{\"sdk_key\" : \"{0}\"}}", sdkKey);
            var bodyRaw = Encoding.UTF8.GetBytes(postJson);
            var uploadHandler = new UploadHandlerRaw(bodyRaw);
            uploadHandler.contentType = "application/json";

            using (var unityWebRequest = new UnityWebRequest("https://api2.safedk.com/v1/build/ios_setup2"))
            {
                unityWebRequest.method = UnityWebRequest.kHttpVerbPOST;
                unityWebRequest.downloadHandler = downloadHandler;
                unityWebRequest.uploadHandler = uploadHandler;
                var operation = unityWebRequest.SendWebRequest();

                // Wait for the download to complete or the request to timeout.
                while (!operation.isDone) { }

#if UNITY_2020_1_OR_NEWER
                if (unityWebRequest.result != UnityWebRequest.Result.Success)
#else
                if (unityWebRequest.isNetworkError || unityWebRequest.isHttpError)
#endif
                {
                    MaxSdkLogger.UserError("AppLovin Quality Service installation failed. Failed to download script with error: " + unityWebRequest.error);
                    return;
                }

                // Check if Ruby is installed
                var rubyVersion = AppLovinCommandLine.Run("ruby", "--version", buildPath);
                if (rubyVersion.ExitCode != 0)
                {
                    MaxSdkLogger.UserError("AppLovin Quality Service installation requires Ruby. Please install Ruby, export it to your system PATH and re-export the project.");
                    return;
                }

                // Ruby is installed, run `ruby AppLovinQualityServiceSetup.rb`
                var result = AppLovinCommandLine.Run("ruby", OutputFileName, buildPath);

                // Check if we have an error.
                if (result.ExitCode != 0) MaxSdkLogger.UserError("Failed to set up AppLovin Quality Service");

                MaxSdkLogger.UserDebug(result.Message);
            }
        }
    }
}

#endif
