using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AppLovinMax.ThirdParty.MiniJson;
using UnityEngine;

#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public abstract class MaxSdkBase
{
    // Shared Properties
    protected static readonly MaxUserSegment SharedUserSegment = new MaxUserSegment();
    protected static readonly MaxTargetingData SharedTargetingData = new MaxTargetingData();


#if UNITY_EDITOR || UNITY_IPHONE || UNITY_IOS
    /// <summary>
    /// App tracking status values. Primarily used in conjunction with iOS14's AppTrackingTransparency.framework.
    /// </summary>
    public enum AppTrackingStatus
    {
        /// <summary>
        /// Device is on < iOS14, AppTrackingTransparency.framework is not available.
        /// </summary>
        Unavailable,

        /// <summary>
        /// The value returned if a user has not yet received an authorization request to authorize access to app-related data that can be used for tracking the user or the device.
        /// </summary>
        NotDetermined,

        /// <summary>
        /// The value returned if authorization to access app-related data that can be used for tracking the user or the device is restricted.
        /// </summary>
        Restricted,

        /// <summary>
        /// The value returned if the user denies authorization to access app-related data that can be used for tracking the user or the device.
        /// </summary>
        Denied,

        /// <summary>
        /// The value returned if the user authorizes access to app-related data that can be used for tracking the user or the device.
        /// </summary>
        Authorized,
    }
#endif

    public enum AdViewPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        Centered,
        CenterLeft,
        CenterRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    public enum BannerPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        Centered,
        CenterLeft,
        CenterRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    public class SdkConfiguration
    {
        /// <summary>
        /// Whether or not the SDK has been initialized successfully.
        /// </summary>
        public bool IsSuccessfullyInitialized { get; private set; }

        /// <summary>
        /// Get the country code for this user.
        /// </summary>
        public string CountryCode { get; private set; }

#if UNITY_EDITOR || UNITY_IPHONE || UNITY_IOS
        /// <summary>
        /// App tracking status values. Primarily used in conjunction with iOS14's AppTrackingTransparency.framework.
        /// </summary>
        public AppTrackingStatus AppTrackingStatus { get; private set; }
#endif

        public bool IsTestModeEnabled { get; private set; }

        [Obsolete("This API has been deprecated and will be removed in a future release.")]
        public ConsentDialogState ConsentDialogState { get; private set; }

#if UNITY_EDITOR || !(UNITY_ANDROID || UNITY_IPHONE || UNITY_IOS)
        public static SdkConfiguration CreateEmpty()
        {
            var sdkConfiguration = new SdkConfiguration();
            sdkConfiguration.IsSuccessfullyInitialized = true;
#pragma warning disable 0618
            sdkConfiguration.ConsentDialogState = ConsentDialogState.Unknown;
#pragma warning restore 0618
#if UNITY_EDITOR
            sdkConfiguration.AppTrackingStatus = AppTrackingStatus.Authorized;
#endif
            var currentRegion = RegionInfo.CurrentRegion;
            sdkConfiguration.CountryCode = currentRegion != null ? currentRegion.TwoLetterISORegionName : "US";
            sdkConfiguration.IsTestModeEnabled = false;

            return sdkConfiguration;
        }
#endif

        public static SdkConfiguration Create(IDictionary<string, object> eventProps)
        {
            var sdkConfiguration = new SdkConfiguration();

            sdkConfiguration.IsSuccessfullyInitialized = MaxSdkUtils.GetBoolFromDictionary(eventProps, "isSuccessfullyInitialized");
            sdkConfiguration.CountryCode = MaxSdkUtils.GetStringFromDictionary(eventProps, "countryCode", "");
            sdkConfiguration.IsTestModeEnabled = MaxSdkUtils.GetBoolFromDictionary(eventProps, "isTestModeEnabled");

#pragma warning disable 0618
            var consentDialogStateStr = MaxSdkUtils.GetStringFromDictionary(eventProps, "consentDialogState", "");
            if ("1".Equals(consentDialogStateStr))
            {
                sdkConfiguration.ConsentDialogState = ConsentDialogState.Applies;
            }
            else if ("2".Equals(consentDialogStateStr))
            {
                sdkConfiguration.ConsentDialogState = ConsentDialogState.DoesNotApply;
            }
            else
            {
                sdkConfiguration.ConsentDialogState = ConsentDialogState.Unknown;
            }
#pragma warning restore 0618

#if UNITY_IPHONE || UNITY_IOS
            var appTrackingStatusStr = MaxSdkUtils.GetStringFromDictionary(eventProps, "appTrackingStatus", "-1");
            if ("-1".Equals(appTrackingStatusStr))
            {
                sdkConfiguration.AppTrackingStatus = AppTrackingStatus.Unavailable;
            }
            else if ("0".Equals(appTrackingStatusStr))
            {
                sdkConfiguration.AppTrackingStatus = AppTrackingStatus.NotDetermined;
            }
            else if ("1".Equals(appTrackingStatusStr))
            {
                sdkConfiguration.AppTrackingStatus = AppTrackingStatus.Restricted;
            }
            else if ("2".Equals(appTrackingStatusStr))
            {
                sdkConfiguration.AppTrackingStatus = AppTrackingStatus.Denied;
            }
            else // "3" is authorized
            {
                sdkConfiguration.AppTrackingStatus = AppTrackingStatus.Authorized;
            }
#endif

            return sdkConfiguration;
        }
    }

    public struct Reward
    {
        public string Label;
        public int Amount;

        public override string ToString()
        {
            return "Reward: " + Amount + " " + Label;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Label) && Amount > 0;
        }
    }

    /**
     *  This enum contains various error codes that the SDK can return when a MAX ad fails to load or display.
     */
    public enum ErrorCode
    {
        /// <summary>
        /// This error code represents an error that could not be categorized into one of the other defined errors. See the message field in the error object for more details.
        /// </summary>
        Unspecified = -1,

        /// <summary>
        /// This error code indicates that MAX returned no eligible ads from any mediated networks for this app/device.
        /// </summary>
        NoFill = 204,

        /// <summary>
        /// This error code indicates that MAX returned eligible ads from mediated networks, but all ads failed to load. See the adLoadFailureInfo field in the error object for more details.
        /// </summary>
        AdLoadFailed = -5001,

        /// <summary>
        /// This error code represents an error that was encountered when showing an ad.
        /// </summary>
        AdDisplayFailed = -4205,

        /// <summary>
        /// This error code indicates that the ad request failed due to a generic network error. See the message field in the error object for more details.
        /// </summary>
        NetworkError = -1000,

        /// <summary>
        /// This error code indicates that the ad request timed out due to a slow internet connection.
        /// </summary>
        NetworkTimeout = -1001,

        /// <summary>
        /// This error code indicates that the ad request failed because the device is not connected to the internet.
        /// </summary>
        NoNetwork = -1009,

        /// <summary>
        /// This error code indicates that you attempted to show a fullscreen ad while another fullscreen ad is still showing.
        /// </summary>
        FullscreenAdAlreadyShowing = -23,

        /// <summary>
        /// This error code indicates you are attempting to show a fullscreen ad before the one has been loaded.
        /// </summary>
        FullscreenAdNotReady = -24,

#if UNITY_ANDROID
        /// <summary>
        /// This error code indicates that the SDK failed to load an ad because it could not find the top Activity.
        /// </summary>
        NoActivity = -5601,

        /// <summary>
        /// This error code indicates that the SDK failed to display an ad because the user has the "Don't Keep Activities" developer setting enabled.
        /// </summary>
        DontKeepActivitiesEnabled = -5602,
#endif
    }

    /**
     * This enum contains possible states of an ad in the waterfall the adapter response info could represent.
     */
    public enum MaxAdLoadState
    {
        /// <summary>
        /// The AppLovin Max SDK did not attempt to load an ad from this network in the waterfall because an ad higher
        /// in the waterfall loaded successfully.
        /// </summary>
        AdLoadNotAttempted,

        /// <summary>
        /// An ad successfully loaded from this network.
        /// </summary>
        AdLoaded,

        /// <summary>
        /// An ad failed to load from this network.
        /// </summary>
        FailedToLoad
    }

    public class AdInfo
    {
        public string AdUnitIdentifier { get; private set; }
        public string AdFormat { get; private set; }
        public string NetworkName { get; private set; }
        public string NetworkPlacement { get; private set; }
        public string Placement { get; private set; }
        public string CreativeIdentifier { get; private set; }
        public double Revenue { get; private set; }
        public string RevenuePrecision { get; private set; }
        public WaterfallInfo WaterfallInfo { get; private set; }
        public string DspName { get; private set; }

        public AdInfo(IDictionary<string, object> adInfoDictionary)
        {
            AdUnitIdentifier = MaxSdkUtils.GetStringFromDictionary(adInfoDictionary, "adUnitId");
            AdFormat = MaxSdkUtils.GetStringFromDictionary(adInfoDictionary, "adFormat");
            NetworkName = MaxSdkUtils.GetStringFromDictionary(adInfoDictionary, "networkName");
            NetworkPlacement = MaxSdkUtils.GetStringFromDictionary(adInfoDictionary, "networkPlacement");
            CreativeIdentifier = MaxSdkUtils.GetStringFromDictionary(adInfoDictionary, "creativeId");
            Placement = MaxSdkUtils.GetStringFromDictionary(adInfoDictionary, "placement");
            Revenue = MaxSdkUtils.GetDoubleFromDictionary(adInfoDictionary, "revenue", -1);
            RevenuePrecision = MaxSdkUtils.GetStringFromDictionary(adInfoDictionary, "revenuePrecision");
            WaterfallInfo = new WaterfallInfo(MaxSdkUtils.GetDictionaryFromDictionary(adInfoDictionary, "waterfallInfo", new Dictionary<string, object>()));
            DspName = MaxSdkUtils.GetStringFromDictionary(adInfoDictionary, "dspName");
        }

        public override string ToString()
        {
            return "[AdInfo adUnitIdentifier: " + AdUnitIdentifier +
                   ", adFormat: " + AdFormat +
                   ", networkName: " + NetworkName +
                   ", networkPlacement: " + NetworkPlacement +
                   ", creativeIdentifier: " + CreativeIdentifier +
                   ", placement: " + Placement +
                   ", revenue: " + Revenue +
                   ", revenuePrecision: " + RevenuePrecision +
                   ", dspName: " + DspName + "]";
        }
    }

    /// <summary>
    /// Returns information about the ad response in a waterfall.
    /// </summary>
    public class WaterfallInfo
    {
        public String Name { get; private set; }
        public String TestName { get; private set; }
        public List<NetworkResponseInfo> NetworkResponses { get; private set; }
        public long LatencyMillis { get; private set; }

        public WaterfallInfo(IDictionary<string, object> waterfallInfoDict)
        {
            Name = MaxSdkUtils.GetStringFromDictionary(waterfallInfoDict, "name");
            TestName = MaxSdkUtils.GetStringFromDictionary(waterfallInfoDict, "testName");

            var networkResponsesList = MaxSdkUtils.GetListFromDictionary(waterfallInfoDict, "networkResponses", new List<object>());
            NetworkResponses = new List<NetworkResponseInfo>();
            foreach (var networkResponseObject in networkResponsesList)
            {
                var networkResponseDict = networkResponseObject as Dictionary<string, object>;
                if (networkResponseDict == null) continue;

                var networkResponse = new NetworkResponseInfo(networkResponseDict);
                NetworkResponses.Add(networkResponse);
            }

            LatencyMillis = MaxSdkUtils.GetLongFromDictionary(waterfallInfoDict, "latencyMillis");
        }

        public override string ToString()
        {
            return "[MediatedNetworkInfo: name = " + Name +
                   ", testName = " + TestName +
                   ", latency = " + LatencyMillis +
                   ", networkResponse = " + string.Join(", ", NetworkResponses.Select(networkResponseInfo => networkResponseInfo.ToString()).ToArray()) + "]";
        }
    }

    public class NetworkResponseInfo
    {
        public MaxAdLoadState AdLoadState { get; private set; }
        public MediatedNetworkInfo MediatedNetwork { get; private set; }
        public Dictionary<string, object> Credentials { get; private set; }
        public bool IsBidding { get; private set; }
        public long LatencyMillis { get; private set; }
        public ErrorInfo Error { get; private set; }

        public NetworkResponseInfo(IDictionary<string, object> networkResponseInfoDict)
        {
            var mediatedNetworkInfoDict = MaxSdkUtils.GetDictionaryFromDictionary(networkResponseInfoDict, "mediatedNetwork");
            MediatedNetwork = mediatedNetworkInfoDict != null ? new MediatedNetworkInfo(mediatedNetworkInfoDict) : null;

            Credentials = MaxSdkUtils.GetDictionaryFromDictionary(networkResponseInfoDict, "credentials", new Dictionary<string, object>());
            IsBidding = MaxSdkUtils.GetBoolFromDictionary(networkResponseInfoDict, "isBidding");
            LatencyMillis = MaxSdkUtils.GetLongFromDictionary(networkResponseInfoDict, "latencyMillis");
            AdLoadState = (MaxAdLoadState) MaxSdkUtils.GetIntFromDictionary(networkResponseInfoDict, "adLoadState");

            var errorInfoDict = MaxSdkUtils.GetDictionaryFromDictionary(networkResponseInfoDict, "error");
            Error = errorInfoDict != null ? new ErrorInfo(errorInfoDict) : null;
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder("[NetworkResponseInfo: adLoadState = ").Append(AdLoadState);
            stringBuilder.Append(", mediatedNetwork = ").Append(MediatedNetwork);
            stringBuilder.Append(", credentials = ").Append(string.Join(", ", Credentials.Select(keyValuePair => keyValuePair.ToString()).ToArray()));

            switch (AdLoadState)
            {
                case MaxAdLoadState.FailedToLoad:
                    stringBuilder.Append(", error = ").Append(Error);
                    break;
                case MaxAdLoadState.AdLoaded:
                    stringBuilder.Append(", latency = ").Append(LatencyMillis);
                    break;
            }

            return stringBuilder.Append("]").ToString();
        }
    }

    public class MediatedNetworkInfo
    {
        public string Name { get; private set; }
        public string AdapterClassName { get; private set; }
        public string AdapterVersion { get; private set; }
        public string SdkVersion { get; private set; }

        public MediatedNetworkInfo(IDictionary<string, object> mediatedNetworkDictionary)
        {
            // NOTE: Unity Editor creates empty string
            Name = MaxSdkUtils.GetStringFromDictionary(mediatedNetworkDictionary, "name", "");
            AdapterClassName = MaxSdkUtils.GetStringFromDictionary(mediatedNetworkDictionary, "adapterClassName", "");
            AdapterVersion = MaxSdkUtils.GetStringFromDictionary(mediatedNetworkDictionary, "adapterVersion", "");
            SdkVersion = MaxSdkUtils.GetStringFromDictionary(mediatedNetworkDictionary, "sdkVersion", "");
        }

        public override string ToString()
        {
            return "[MediatedNetworkInfo name: " + Name +
                   ", adapterClassName: " + AdapterClassName +
                   ", adapterVersion: " + AdapterVersion +
                   ", sdkVersion: " + SdkVersion + "]";
        }
    }

    public class ErrorInfo
    {
        public ErrorCode Code { get; private set; }
        public string Message { get; private set; }
        public int MediatedNetworkErrorCode { get; private set; }
        public string MediatedNetworkErrorMessage { get; private set; }
        public string AdLoadFailureInfo { get; private set; }
        public WaterfallInfo WaterfallInfo { get; private set; }

        public ErrorInfo(IDictionary<string, object> errorInfoDictionary)
        {
            Code = (ErrorCode) MaxSdkUtils.GetIntFromDictionary(errorInfoDictionary, "errorCode", -1);
            Message = MaxSdkUtils.GetStringFromDictionary(errorInfoDictionary, "errorMessage", "");
            MediatedNetworkErrorCode = MaxSdkUtils.GetIntFromDictionary(errorInfoDictionary, "mediatedNetworkErrorCode", (int) ErrorCode.Unspecified);
            MediatedNetworkErrorMessage = MaxSdkUtils.GetStringFromDictionary(errorInfoDictionary, "mediatedNetworkErrorMessage", "");
            AdLoadFailureInfo = MaxSdkUtils.GetStringFromDictionary(errorInfoDictionary, "adLoadFailureInfo", "");
            WaterfallInfo = new WaterfallInfo(MaxSdkUtils.GetDictionaryFromDictionary(errorInfoDictionary, "waterfallInfo", new Dictionary<string, object>()));
        }

        public override string ToString()
        {
            var stringbuilder = new StringBuilder("[ErrorInfo code: ").Append(Code);
            stringbuilder.Append(", message: ").Append(Message);

            if (Code == ErrorCode.AdDisplayFailed)
            {
                stringbuilder.Append(", mediatedNetworkCode: ").Append(MediatedNetworkErrorCode);
                stringbuilder.Append(", mediatedNetworkMessage: ").Append(MediatedNetworkErrorMessage);
            }

            return stringbuilder.Append(", adLoadFailureInfo: ").Append(AdLoadFailureInfo).Append("]").ToString();
        }
    }

    protected static void ValidateAdUnitIdentifier(string adUnitIdentifier, string debugPurpose)
    {
        if (string.IsNullOrEmpty(adUnitIdentifier))
        {
            MaxSdkLogger.UserError("No MAX Ads Ad Unit ID specified for: " + debugPurpose);
        }
    }

    // Allocate the MaxSdkCallbacks singleton, which receives all callback events from the native SDKs.
    protected static void InitCallbacks()
    {
        var type = typeof(MaxSdkCallbacks);
        var mgr = new GameObject("MaxSdkCallbacks", type)
            .GetComponent<MaxSdkCallbacks>(); // Its Awake() method sets Instance.
        if (MaxSdkCallbacks.Instance != mgr)
        {
            MaxSdkLogger.UserWarning("It looks like you have the " + type.Name + " on a GameObject in your scene. Please remove the script from your scene.");
        }
    }

    /// <summary>
    /// Generates serialized Unity meta data to be passed to the SDK.
    /// </summary>
    /// <returns>Serialized Unity meta data.</returns>
    protected static string GenerateMetaData()
    {
        var metaData = new Dictionary<string, string>(2);
        metaData.Add("UnityVersion", Application.unityVersion);

        var graphicsMemorySize = SystemInfo.graphicsMemorySize;
        metaData.Add("GraphicsMemorySizeMegabytes", graphicsMemorySize.ToString());

        return Json.Serialize(metaData);
    }

    /// <summary>
    /// Parses the prop string provided to a <see cref="Rect"/>.
    /// </summary>
    /// <param name="rectPropString">A prop string representing a Rect</param>
    /// <returns>A <see cref="Rect"/> the prop string represents.</returns>
    protected static Rect GetRectFromString(string rectPropString)
    {
        var rectDict = Json.Deserialize(rectPropString) as Dictionary<string, object>;
        var originX = MaxSdkUtils.GetFloatFromDictionary(rectDict, "origin_x", 0);
        var originY = MaxSdkUtils.GetFloatFromDictionary(rectDict, "origin_y", 0);
        var width = MaxSdkUtils.GetFloatFromDictionary(rectDict, "width", 0);
        var height = MaxSdkUtils.GetFloatFromDictionary(rectDict, "height", 0);

        return new Rect(originX, originY, width, height);
    }

    [Obsolete("This API has been deprecated and will be removed in a future release.")]
    public enum ConsentDialogState
    {
        Unknown,
        Applies,
        DoesNotApply
    }
}

/// <summary>
/// An extension class for <see cref="MaxSdkBase.BannerPosition"/> and <see cref="MaxSdkBase.AdViewPosition"/> enums.
/// </summary>
internal static class AdPositionExtenstion
{
    public static string ToSnakeCaseString(this MaxSdkBase.BannerPosition position)
    {
        if (position == MaxSdkBase.BannerPosition.TopLeft)
        {
            return "top_left";
        }
        else if (position == MaxSdkBase.BannerPosition.TopCenter)
        {
            return "top_center";
        }
        else if (position == MaxSdkBase.BannerPosition.TopRight)
        {
            return "top_right";
        }
        else if (position == MaxSdkBase.BannerPosition.Centered)
        {
            return "centered";
        }
        else if (position == MaxSdkBase.BannerPosition.CenterLeft)
        {
            return "center_left";
        }
        else if (position == MaxSdkBase.BannerPosition.CenterRight)
        {
            return "center_right";
        }
        else if (position == MaxSdkBase.BannerPosition.BottomLeft)
        {
            return "bottom_left";
        }
        else if (position == MaxSdkBase.BannerPosition.BottomCenter)
        {
            return "bottom_center";
        }
        else // position == MaxSdkBase.BannerPosition.BottomRight
        {
            return "bottom_right";
        }
    }

    public static string ToSnakeCaseString(this MaxSdkBase.AdViewPosition position)
    {
        if (position == MaxSdkBase.AdViewPosition.TopLeft)
        {
            return "top_left";
        }
        else if (position == MaxSdkBase.AdViewPosition.TopCenter)
        {
            return "top_center";
        }
        else if (position == MaxSdkBase.AdViewPosition.TopRight)
        {
            return "top_right";
        }
        else if (position == MaxSdkBase.AdViewPosition.Centered)
        {
            return "centered";
        }
        else if (position == MaxSdkBase.AdViewPosition.CenterLeft)
        {
            return "center_left";
        }
        else if (position == MaxSdkBase.AdViewPosition.CenterRight)
        {
            return "center_right";
        }
        else if (position == MaxSdkBase.AdViewPosition.BottomLeft)
        {
            return "bottom_left";
        }
        else if (position == MaxSdkBase.AdViewPosition.BottomCenter)
        {
            return "bottom_center";
        }
        else // position == MaxSdkBase.AdViewPosition.BottomRight
        {
            return "bottom_right";
        }
    }
}

namespace AppLovinMax.Internal.API
{
    public class CFError
    {
        /// <summary>
        /// Indicates that the flow ended in an unexpected state.
        /// </summary>
        public const int ErrorCodeUnspecified = -1;

        /// <summary>
        /// Indicates that the consent flow has not been integrated correctly.
        /// </summary>
        public const int ErrorCodeInvalidIntegration = -100;

        /// <summary>
        /// Indicates that the consent flow is already being shown.
        /// </summary>
        public const int ErrorCodeFlowAlreadyInProgress = -200;

        /// <summary>
        /// Indicates that the user is not in a GDPR region.
        /// </summary>
        public const int ErrorCodeNotInGdprRegion = -300;

        /// <summary>
        /// The error code for this error. Will be one of the error codes listed in this file.
        /// </summary>
        public int Code { get; private set; }

        /// <summary>
        /// The error message for this error.
        /// </summary>
        public string Message { get; private set; }

        public static CFError Create(IDictionary<string, object> errorObject)
        {
            if (!errorObject.ContainsKey("code") && !errorObject.ContainsKey("message")) return null;

            var code = MaxSdkUtils.GetIntFromDictionary(errorObject, "code", ErrorCodeUnspecified);
            var message = MaxSdkUtils.GetStringFromDictionary(errorObject, "message");
            return new CFError(code, message);
        }

        private CFError(int code, string message)
        {
            Code = code;
            Message = message;
        }

        public override string ToString()
        {
            return "[CFError Code: " + Code +
                   ", Message: " + Message + "]";
        }
    }

    public enum CFType
    {
        /// <summary>
        /// The flow type is not known.
        /// </summary>
        Unknown,

        /// <summary>
        /// A standard flow where a TOS/PP alert is shown.
        /// </summary>
        Standard,

        /// <summary>
        /// A detailed modal shown to users in GDPR region.
        /// </summary>
        Detailed
    }

    public class CFService
    {
        private static Action<CFError> OnConsentFlowCompletedAction;

#if UNITY_EDITOR
#elif UNITY_ANDROID
        private static readonly AndroidJavaClass MaxUnityPluginClass = new AndroidJavaClass("com.applovin.mediation.unity.MaxUnityPlugin");
#elif UNITY_IOS
        [DllImport("__Internal")]
        private static extern string _MaxGetCFType();

        [DllImport("__Internal")]
        private static extern void _MaxStartConsentFlow();
#endif

        /// <summary>
        /// The consent flow type that will be displayed.
        /// </summary>
        public static CFType CFType
        {
            get
            {
                var cfType = "0";
#if UNITY_EDITOR
#elif UNITY_ANDROID
                cfType = MaxUnityPluginClass.CallStatic<string>("getCFType");
#elif UNITY_IOS
                cfType = _MaxGetCFType();
#endif

                if ("1".Equals(cfType))
                {
                    return CFType.Standard;
                }
                else if ("2".Equals(cfType))
                {
                    return CFType.Detailed;
                }

                return CFType.Unknown;
            }
        }

        /// <summary>
        /// Starts the consent flow. Call this method to re-show the consent flow for a user in GDPR region.
        ///
        /// Note: The flow will only be shown to users in GDPR regions.
        /// </summary>
        /// <param name="onFlowCompletedAction">Called when we finish showing the consent flow. Error object will be <c>null</c> if the flow completed successfully.</param>
        public static void SCF(Action<CFError> onFlowCompletedAction)
        {
            OnConsentFlowCompletedAction = onFlowCompletedAction;

#if UNITY_EDITOR
            var errorDict = new Dictionary<string, object>()
            {
                {"code", CFError.ErrorCodeUnspecified},
                {"message", "Consent flow is not supported in Unity Editor."}
            };

            NotifyConsentFlowCompletedIfNeeded(errorDict);
#elif UNITY_ANDROID
            MaxUnityPluginClass.CallStatic("startConsentFlow");
#elif UNITY_IOS
            _MaxStartConsentFlow();
#endif
        }

        public static void NotifyConsentFlowCompletedIfNeeded(IDictionary<string, object> error)
        {
            if (OnConsentFlowCompletedAction == null) return;

            OnConsentFlowCompletedAction(CFError.Create(error));
        }
    }
}
