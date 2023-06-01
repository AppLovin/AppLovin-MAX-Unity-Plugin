using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public abstract class MaxSdkBase
{
    // Shared Properties
    protected static readonly MaxUserSegment SharedUserSegment = new MaxUserSegment();

    /// <summary>
    /// This enum represents whether or not the consent dialog should be shown for this user.
    /// The state where no such determination could be made is represented by <see cref="ConsentDialogState.Unknown"/>.
    ///
    /// NOTE: This version of the iOS consent flow has been deprecated and is only available on UNITY_ANDROID as of MAX Unity Plugin v4.0.0 + iOS SDK v7.0.0, please refer to our documentation for enabling the new consent flow.
    /// </summary>
    public enum ConsentDialogState
    {
        /// <summary>
        /// The consent dialog state could not be determined. This is likely due to SDK failing to initialize.
        /// </summary>
        Unknown,

        /// <summary>
        /// This user should be shown a consent dialog.
        /// </summary>
        Applies,

        /// <summary>
        /// This user should not be shown a consent dialog.
        /// </summary>
        DoesNotApply
    }

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
        /// Get the consent dialog state for this user. If no such determination could be made, `ALConsentDialogStateUnknown` will be returned.
        /// </summary>
        public ConsentDialogState ConsentDialogState;

        /// <summary>
        /// Get the country code for this user.
        /// </summary>
        public string CountryCode;

#if UNITY_EDITOR || UNITY_IPHONE || UNITY_IOS
        /// <summary>
        /// App tracking status values. Primarily used in conjunction with iOS14's AppTrackingTransparency.framework.
        /// </summary>
        public AppTrackingStatus AppTrackingStatus;
#endif

        public static SdkConfiguration Create(IDictionary<string, string> eventProps)
        {
            var sdkConfiguration = new SdkConfiguration();

            string countryCode = eventProps.TryGetValue("countryCode", out countryCode) ? countryCode : "";
            sdkConfiguration.CountryCode = countryCode;

            string consentDialogStateStr = eventProps.TryGetValue("consentDialogState", out consentDialogStateStr) ? consentDialogStateStr : "";
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

#if UNITY_IPHONE || UNITY_IOS
            string appTrackingStatusStr = eventProps.TryGetValue("appTrackingStatus", out appTrackingStatusStr) ? appTrackingStatusStr : "-1";
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

    public class AdInfo
    {
        public string AdUnitIdentifier { get; private set; }
        public string NetworkName { get; private set; }
        public string NetworkPlacement { get; private set; }
        public string Placement { get; private set; }
        public string CreativeIdentifier { get; private set; }
        public double Revenue { get; private set; }

        public AdInfo(IDictionary<string, string> adInfoDictionary)
        {
            string adUnitIdentifier;
            string networkName;
            string networkPlacement;
            string creativeIdentifier;
            string placement;
            string revenue;

            // NOTE: Unity Editor creates empty string
            AdUnitIdentifier = adInfoDictionary.TryGetValue("adUnitId", out adUnitIdentifier) ? adUnitIdentifier : "";
            NetworkName = adInfoDictionary.TryGetValue("networkName", out networkName) ? networkName : "";
            NetworkPlacement = adInfoDictionary.TryGetValue("networkPlacement", out networkPlacement) ? networkPlacement : "";
            CreativeIdentifier = adInfoDictionary.TryGetValue("creativeId", out creativeIdentifier) ? creativeIdentifier : "";
            Placement = adInfoDictionary.TryGetValue("placement", out placement) ? placement : "";

            if (adInfoDictionary.TryGetValue("revenue", out revenue))
            {
                try
                {
                    // InvariantCulture guarantees the decimal is used for the separator even in regions that use commas as the separator
                    Revenue = double.Parse(revenue, NumberStyles.Any, CultureInfo.InvariantCulture);
                }
                catch (Exception exception)
                {
                    MaxSdkLogger.E("Failed to parse double (" + revenue + ") with exception: " + exception);
                    Revenue = -1;
                }
            }
            else
            {
                Revenue = -1;
            }
        }

        public override string ToString()
        {
            return "[AdInfo adUnitIdentifier: " + AdUnitIdentifier +
                   ", networkName: " + NetworkName +
                   ", networkPlacement: " + NetworkPlacement +
                   ", creativeIdentifier: " + CreativeIdentifier +
                   ", placement: " + Placement +
                   ", revenue: " + Revenue + "]";
        }
    }

    public class MediatedNetworkInfo
    {
        public string Name { get; private set; }
        public string AdapterClassName { get; private set; }
        public string AdapterVersion { get; private set; }
        public string SdkVersion { get; private set; }

        public MediatedNetworkInfo(string networkInfoString)
        {
            string name;
            string adapterClassName;
            string adapterVersion;
            string sdkVersion;

            // NOTE: Unity Editor creates empty string
            var mediatedNetworkObject = MaxSdkUtils.PropsStringToDict(networkInfoString);
            Name = mediatedNetworkObject.TryGetValue("name", out name) ? name : "";
            AdapterClassName = mediatedNetworkObject.TryGetValue("adapterClassName", out adapterClassName) ? adapterClassName : "";
            AdapterVersion = mediatedNetworkObject.TryGetValue("adapterVersion", out adapterVersion) ? adapterVersion : "";
            SdkVersion = mediatedNetworkObject.TryGetValue("sdkVersion", out sdkVersion) ? sdkVersion : "";
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
        public string AdLoadFailureInfo { get; private set; }

        public ErrorInfo(IDictionary<string, string> errorInfoDictionary)
        {
            string code;
            string message;
            string adLoadFailureInfo;

            Message = errorInfoDictionary.TryGetValue("errorMessage", out message) ? message : "";
            AdLoadFailureInfo = errorInfoDictionary.TryGetValue("adLoadFailureInfo", out adLoadFailureInfo) ? adLoadFailureInfo : "";

            if (errorInfoDictionary.TryGetValue("errorCode", out code))
            {
                try
                {
                    Code = (ErrorCode) int.Parse(code);
                }
                catch (Exception exception)
                {
                    MaxSdkLogger.E("Failed to parse int (" + code + ") with exception: " + exception);
                    Code = ErrorCode.Unspecified;
                }
            }
            else
            {
                Code = ErrorCode.Unspecified;
            }
        }

        public override string ToString()
        {
            return "[ErrorInfo code: " + Code +
                   ", message: " + Message +
                   ", adLoadFailureInfo: " + AdLoadFailureInfo + "]";
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

        return MaxSdkUtils.DictToPropsString(metaData);
    }

    /// <summary>
    /// Parses the prop string provided to a <see cref="Rect"/>.
    /// </summary>
    /// <param name="rectPropString">A prop string representing a Rect</param>
    /// <returns>A <see cref="Rect"/> the prop string represents.</returns>
    protected static Rect GetRectFromString(string rectPropString)
    {
        var rectDict = MaxSdkUtils.PropsStringToDict(rectPropString);
        float originX;
        float originY;
        float width;
        float height;
        string output;

        rectDict.TryGetValue("origin_x", out output);
        float.TryParse(output, out originX);

        rectDict.TryGetValue("origin_y", out output);
        float.TryParse(output, out originY);

        rectDict.TryGetValue("width", out output);
        float.TryParse(output, out width);

        rectDict.TryGetValue("height", out output);
        float.TryParse(output, out height);

        return new Rect(originX, originY, width, height);
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
