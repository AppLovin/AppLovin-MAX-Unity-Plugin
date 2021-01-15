using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Android AppLovin MAX Unity Plugin implementation
/// </summary>
public class MaxSdkAndroid : MaxSdkBase
{
    private static readonly AndroidJavaClass MaxUnityPluginClass =
        new AndroidJavaClass("com.applovin.mediation.unity.MaxUnityPlugin");

    public static MaxVariableServiceAndroid VariableService
    {
        get { return MaxVariableServiceAndroid.Instance; }
    }

    public static MaxUserServiceAndroid UserService
    {
        get { return MaxUserServiceAndroid.Instance; }
    }

    static MaxSdkAndroid()
    {
        InitCallbacks();
    }

    #region Initialization

    /// <summary>
    /// Set AppLovin SDK Key.
    ///
    /// This method must be called before any other SDK operation
    /// </summary>
    public static void SetSdkKey(string sdkKey)
    {
        MaxUnityPluginClass.CallStatic("setSdkKey", sdkKey);
    }

    /// <summary>
    /// Initialize the default instance of AppLovin SDK.
    ///
    /// Please make sure that application's Android manifest or Info.plist includes the AppLovin SDK key.
    /// <param name="adUnitIds">
    /// OPTIONAL: Set the MAX ad unit ids to be used for this instance of the SDK. 3rd-party SDKs will be initialized with the credentials configured for these ad unit ids.
    /// This should only be used if you have different sets of ad unit ids / credentials for the same package name.</param>
    /// </summary>
    public static void InitializeSdk(String[] adUnitIds=null)
    {
        String serializedAdUnitIds = (adUnitIds != null) ? String.Join(",", adUnitIds) : "";
        MaxUnityPluginClass.CallStatic("initializeSdk", serializedAdUnitIds, GenerateMetaData());
    }

    /// <summary>
    /// Check if the SDK has been initialized
    /// </summary>
    /// <returns>True if SDK has been initialized</returns>
    public static bool IsInitialized()
    {
        return MaxUnityPluginClass.CallStatic<bool>("isInitialized");
    }

    #endregion

    #region User Info

    /// <summary>
    /// Set an identifier for the current user. This identifier will be tied to SDK events and our optional S2S postbacks.
    /// 
    /// If you're using reward validation, you can optionally set an identifier to be included with currency validation postbacks.
    /// For example, a username or email. We'll include this in the postback when we ping your currency endpoint from our server.
    /// </summary>
    /// 
    /// <param name="userId">The user identifier to be set.</param>
    public static void SetUserId(string userId)
    {
        MaxUnityPluginClass.CallStatic("setUserId", userId);
    }

    /// <summary>
    /// User segments allow us to serve ads using custom-defined rules based on which segment the user is in. For now, we only support a custom string 32 alphanumeric characters or less as the user segment.
    /// </summary>
    public static MaxUserSegment UserSegment
    {
        get { return SharedUserSegment; }
    }
    
    #endregion

    #region Mediation Debugger

    /// <summary>
    /// Present the mediation debugger UI.
    /// This debugger tool provides the status of your integration for each third-party ad network.
    ///
    /// Please call this method after the SDK has initialized.
    /// </summary>
    public static void ShowMediationDebugger()
    {
        MaxUnityPluginClass.CallStatic("showMediationDebugger");
    }

    /// <summary>
    /// Returns information about the last loaded ad for the given ad unit identifier. Returns null if no ad is loaded.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of an ad</param>
    /// <returns>Information about the ad, or null if no ad is loaded.</returns>
    public static MaxSdkBase.AdInfo GetAdInfo(string adUnitIdentifier)
    {
        string adInfoString = MaxUnityPluginClass.CallStatic<string>("getAdInfo", adUnitIdentifier);
        
        if (string.IsNullOrEmpty(adInfoString)) return null;

        return new MaxSdkBase.AdInfo(adInfoString);
    }

    #endregion

    #region Privacy

    /// <summary>
    /// Get the consent dialog state for this user. If no such determination could be made, <see cref="MaxSdkBase.ConsentDialogState.Unknown"/> will be returned.
    ///
    /// Note: this method should be called only after SDK has been initialized
    /// </summary>
    public static ConsentDialogState GetConsentDialogState()
    {
        if (!IsInitialized())
        {
            MaxSdkLogger.UserWarning(
                "MAX Ads SDK has not been initialized yet. GetConsentDialogState() may return ConsentDialogState.Unknown");
        }

        return (ConsentDialogState) MaxUnityPluginClass.CallStatic<int>("getConsentDialogState");
    }

    /// <summary>
    /// Set whether or not user has provided consent for information sharing with AppLovin and other providers.
    /// </summary>
    /// <param name="hasUserConsent">'true' if the user has provided consent for information sharing with AppLovin. 'false' by default.</param>
    public static void SetHasUserConsent(bool hasUserConsent)
    {
        MaxUnityPluginClass.CallStatic("setHasUserConsent", hasUserConsent);
    }

    /// <summary>
    /// Check if user has provided consent for information sharing with AppLovin and other providers.
    /// </summary>
    /// <returns></returns>
    public static bool HasUserConsent()
    {
        return MaxUnityPluginClass.CallStatic<bool>("hasUserConsent");
    }

    /// <summary>
    /// Mark user as age restricted (i.e. under 16).
    /// </summary>
    /// <param name="isAgeRestrictedUser">'true' if the user is age restricted (i.e. under 16).</param>
    public static void SetIsAgeRestrictedUser(bool isAgeRestrictedUser)
    {
        MaxUnityPluginClass.CallStatic("setIsAgeRestrictedUser", isAgeRestrictedUser);
    }

    /// <summary>
    /// Check if user is age restricted.
    /// </summary>
    /// <returns></returns>
    public static bool IsAgeRestrictedUser()
    {
        return MaxUnityPluginClass.CallStatic<bool>("isAgeRestrictedUser");
    }

    /// <summary>
    /// Set whether or not user has opted out of the sale of their personal information.
    /// </summary>
    /// <param name="doNotSell">'true' if the user has opted out of the sale of their personal information.</param>
    public static void SetDoNotSell(bool doNotSell)
    {
        MaxUnityPluginClass.CallStatic("setDoNotSell", doNotSell);
    }

    /// <summary>
    /// Check if the user has opted out of the sale of their personal information.
    /// </summary>
    public static bool IsDoNotSell()
    {
        return MaxUnityPluginClass.CallStatic<bool>("isDoNotSell");
    }

    #endregion

    #region Banners

    /// <summary>
    /// Create a new banner.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to create</param>
    /// <param name="bannerPosition">Banner position</param>
    public static void CreateBanner(string adUnitIdentifier, BannerPosition bannerPosition)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "create banner");
        MaxUnityPluginClass.CallStatic("createBanner", adUnitIdentifier, bannerPosition.ToSnakeCaseString());
    }

    /// <summary>
    /// Create a new banner with a custom position.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to create</param>
    /// <param name="x">The X coordinate (horizontal position) of the banner relative to the top left corner of the screen.</param>
    /// <param name="y">The Y coordinate (vertical position) of the banner relative to the top left corner of the screen.</param>
    /// <seealso cref="GetBannerLayout">
    /// The banner is placed within the safe area of the screen. You can use this to get the absolute position of the banner on screen.
    /// </seealso>
    public static void CreateBanner(string adUnitIdentifier, float x, float y)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "create banner");
        MaxUnityPluginClass.CallStatic("createBanner", adUnitIdentifier, x, y);
    }

    /// <summary>
    /// Set the banner placement for an ad unit identifier to tie the future ad events to.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to set the placement for</param>
    /// <param name="placement">Placement to set</param>
    public static void SetBannerPlacement(string adUnitIdentifier, string placement)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set banner placement");
        MaxUnityPluginClass.CallStatic("setBannerPlacement", adUnitIdentifier, placement);
    }

    /// <summary>
    /// Updates the position of the banner to the new position provided.
    /// </summary>
    /// <param name="adUnitIdentifier">The ad unit identifier of the banner for which to update the position</param>
    /// <param name="bannerPosition">A new position for the banner</param>
    public static void UpdateBannerPosition(string adUnitIdentifier, BannerPosition bannerPosition)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "update banner position");
        MaxUnityPluginClass.CallStatic("updateBannerPosition", adUnitIdentifier, bannerPosition.ToSnakeCaseString());
    }

    /// <summary>
    /// Updates the position of the banner to the new coordinates provided.
    /// </summary>
    /// <param name="adUnitIdentifier">The ad unit identifier of the banner for which to update the position</param>
    /// <param name="x">The X coordinate (horizontal position) of the banner relative to the top left corner of the screen.</param>
    /// <param name="y">The Y coordinate (vertical position) of the banner relative to the top left corner of the screen.</param>
    /// <seealso cref="GetBannerLayout">
    /// The banner is placed within the safe area of the screen. You can use this to get the absolute position of the banner on screen.
    /// </seealso>
    public static void UpdateBannerPosition(string adUnitIdentifier, float x, float y)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "update banner position");
        MaxUnityPluginClass.CallStatic("updateBannerPosition", adUnitIdentifier, x, y);
    }

    /// <summary>
    /// Overrides the width of the banner in dp.
    /// </summary>
    /// <param name="adUnitIdentifier">The ad unit identifier of the banner for which to override the width for</param>
    /// <param name="width">The desired width of the banner in dp</param>
    public static void SetBannerWidth(string adUnitIdentifier, float width)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set banner width");
        MaxUnityPluginClass.CallStatic("setBannerWidth", adUnitIdentifier, width);
    }
    
    /// <summary>
    /// Show banner at a position determined by the 'CreateBanner' call.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to show</param>
    public static void ShowBanner(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "show banner");
        MaxUnityPluginClass.CallStatic("showBanner", adUnitIdentifier);
    }

    /// <summary>
    /// Remove banner from the ad view and destroy it.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to destroy</param>
    public static void DestroyBanner(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "destroy banner");
        MaxUnityPluginClass.CallStatic("destroyBanner", adUnitIdentifier);
    }

    /// <summary>
    /// Hide banner.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to hide</param>
    /// <returns></returns>
    public static void HideBanner(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "hide banner");
        MaxUnityPluginClass.CallStatic("hideBanner", adUnitIdentifier);
    }

    /// <summary>
    /// Set non-transparent background color for banners to be fully functional.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to set background color for</param>
    /// <param name="color">A background color to set for the ad</param>
    /// <returns></returns>
    public static void SetBannerBackgroundColor(string adUnitIdentifier, Color color)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set background color");
        MaxUnityPluginClass.CallStatic("setBannerBackgroundColor", adUnitIdentifier, MaxSdkUtils.ParseColor(color));
    }

    /// <summary>
    /// Set an extra parameter for the ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to set the extra parameter for.</param>
    /// <param name="key">The key for the extra parameter.</param>
    /// <param name="value">The value for the extra parameter.</param>
    public static void SetBannerExtraParameter(string adUnitIdentifier, string key, string value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set banner extra parameter");
        MaxUnityPluginClass.CallStatic("setBannerExtraParameter", adUnitIdentifier, key, value);
    }

    /// <summary>
    /// The banner position on the screen. When setting the banner position via <see cref="CreateBanner(string, float, float)"/> or <see cref="UpdateBannerPosition(string, float, float)"/>,
    /// the banner is placed within the safe area of the screen. This returns the absolute position of the banner on screen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner for which to get the position on screen.</param>
    /// <returns>A <see cref="Rect"/> representing the banner position on screen.</returns>
    public static Rect GetBannerLayout(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "get banner layout");
        var positionRect = MaxUnityPluginClass.CallStatic<string>("getBannerLayout", adUnitIdentifier);
        return GetRectFromString(positionRect);
    }

    #endregion

    #region MRECs

    /// <summary>
    /// Create a new MREC.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to create</param>
    /// <param name="mrecPosition">MREC position</param>
    public static void CreateMRec(string adUnitIdentifier, AdViewPosition mrecPosition)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "create MREC");
        MaxUnityPluginClass.CallStatic("createMRec", adUnitIdentifier, mrecPosition.ToSnakeCaseString());
    }

    /// <summary>
    /// Create a new MREC with a custom position.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to create</param>
    /// <param name="x">The X coordinate (horizontal position) of the MREC relative to the top left corner of the screen.</param>
    /// <param name="y">The Y coordinate (vertical position) of the MREC relative to the top left corner of the screen.</param>
    /// <seealso cref="GetMRecLayout">
    /// The MREC is placed within the safe area of the screen. You can use this to get the absolute position Rect of the MREC on screen.
    /// </seealso>
    public static void CreateMRec(string adUnitIdentifier, float x, float y)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "create MREC");
        MaxUnityPluginClass.CallStatic("createMRec", adUnitIdentifier, x, y);
    }

    /// <summary>
    /// Set the MREC placement for an ad unit identifier to tie the future ad events to.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to set the placement for</param>
    /// <param name="placement">Placement to set</param>
    public static void SetMRecPlacement(string adUnitIdentifier, string placement)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set MREC placement");
        MaxUnityPluginClass.CallStatic("setMRecPlacement", adUnitIdentifier, placement);
    }

    /// <summary>
    /// Updates the position of the MREC to the new position provided.
    /// </summary>
    /// <param name="adUnitIdentifier">The ad unit identifier of the MREC for which to update the position</param>
    /// <param name="mrecPosition">A new position for the MREC</param>
    public static void UpdateMRecPosition(string adUnitIdentifier, AdViewPosition mrecPosition)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "update MREC position");
        MaxUnityPluginClass.CallStatic("updateMRecPosition", adUnitIdentifier, mrecPosition.ToSnakeCaseString());
    }

    /// <summary>
    /// Updates the position of the MREC to the new coordinates provided.
    /// </summary>
    /// <param name="adUnitIdentifier">The ad unit identifier of the MREC for which to update the position</param>
    /// <param name="x">The X coordinate (horizontal position) of the MREC relative to the top left corner of the screen.</param>
    /// <param name="y">The Y coordinate (vertical position) of the MREC relative to the top left corner of the screen.</param>
    /// <seealso cref="GetMRecLayout">
    /// The MREC is placed within the safe area of the screen. You can use this to get the absolute position Rect of the MREC on screen.
    /// </seealso>
    public static void UpdateMRecPosition(string adUnitIdentifier, float x, float y)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "update MREC position");
        MaxUnityPluginClass.CallStatic("updateMRecPosition", adUnitIdentifier, x, y);
    }

    /// <summary>
    /// Show MREC at a position determined by the 'CreateMRec' call.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to show</param>
    public static void ShowMRec(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "show MREC");
        MaxUnityPluginClass.CallStatic("showMRec", adUnitIdentifier);
    }

    /// <summary>
    /// Remove MREC from the ad view and destroy it.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to destroy</param>
    public static void DestroyMRec(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "destroy MREC");
        MaxUnityPluginClass.CallStatic("destroyMRec", adUnitIdentifier);
    }

    /// <summary>
    /// Hide MREC.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to hide</param>
    public static void HideMRec(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "hide MREC");
        MaxUnityPluginClass.CallStatic("hideMRec", adUnitIdentifier);
    }

    /// <summary>
    /// The MREC position on the screen. When setting the banner position via <see cref="CreateMRec(string, float, float)"/> or <see cref="UpdateMRecPosition(string, float, float)"/>,
    /// the banner is placed within the safe area of the screen. This returns the absolute position of the MREC on screen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC for which to get the position on screen.</param>
    /// <returns>A <see cref="Rect"/> representing the banner position on screen.</returns>
    public static Rect GetMRecLayout(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "get MREC layout");
        var positionRect = MaxUnityPluginClass.CallStatic<string>("getMRecLayout", adUnitIdentifier);
        return GetRectFromString(positionRect);
    }

    #endregion

    #region Interstitials

    /// <summary>
    /// Start loading an interstitial.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the interstitial to load</param>
    public static void LoadInterstitial(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "load interstitial");
        MaxUnityPluginClass.CallStatic("loadInterstitial", adUnitIdentifier);
    }

    /// <summary>
    /// Check if interstitial ad is loaded and ready to be displayed.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the interstitial to load</param>
    /// <returns>True if the ad is ready to be displayed</returns>
    public static bool IsInterstitialReady(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "check interstitial loaded");
        return MaxUnityPluginClass.CallStatic<bool>("isInterstitialReady", adUnitIdentifier);
    }

    /// <summary>
    /// Present loaded interstitial. Note: if the interstitial is not ready to be displayed nothing will happen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the interstitial to load</param>
    public static void ShowInterstitial(string adUnitIdentifier)
    {
        ShowInterstitial(adUnitIdentifier, null);
    }

    /// <summary>
    /// Present loaded interstitial for a given placement to tie ad events to. Note: if the interstitial is not ready to be displayed nothing will happen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the interstitial to load</param>
    /// <param name="placement">The placement to tie the showing ad's events to</param>
    public static void ShowInterstitial(string adUnitIdentifier, string placement)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "show interstitial");

        if (IsInterstitialReady(adUnitIdentifier))
        {
            MaxUnityPluginClass.CallStatic("showInterstitial", adUnitIdentifier, placement);
        }
        else
        {
            MaxSdkLogger.UserWarning("Not showing MAX Ads interstitial: ad not ready");
        }
    }

    /// <summary>
    /// Set an extra parameter for the ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the interstitial to set the extra parameter for.</param>
    /// <param name="key">The key for the extra parameter.</param>
    /// <param name="value">The value for the extra parameter.</param>
    public static void SetInterstitialExtraParameter(string adUnitIdentifier, string key, string value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set interstitial extra parameter");
        MaxUnityPluginClass.CallStatic("setInterstitialExtraParameter", adUnitIdentifier, key, value);
    }

    #endregion

    #region Rewarded

    /// <summary>
    /// Start loading an rewarded ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded ad to load</param>
    public static void LoadRewardedAd(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "load rewarded ad");
        MaxUnityPluginClass.CallStatic("loadRewardedAd", adUnitIdentifier);
    }

    /// <summary>
    /// Check if rewarded ad ad is loaded and ready to be displayed.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded ad to load</param>
    /// <returns>True if the ad is ready to be displayed</returns>
    public static bool IsRewardedAdReady(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "check rewarded ad loaded");
        return MaxUnityPluginClass.CallStatic<bool>("isRewardedAdReady", adUnitIdentifier);
    }

    /// <summary>
    /// Present loaded rewarded ad. Note: if the rewarded ad is not ready to be displayed nothing will happen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded ad to show</param>
    public static void ShowRewardedAd(string adUnitIdentifier)
    {
        ShowRewardedAd(adUnitIdentifier, null);
    }

    /// <summary> ready to be
    /// Present loaded rewarded ad for a given placement to tie ad events to. Note: if the rewarded ad is not ready to be displayed nothing will happen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the interstitial to load</param>
    /// <param name="placement">The placement to tie the showing ad's events to</param>
    public static void ShowRewardedAd(string adUnitIdentifier, string placement)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "show rewarded ad");

        if (IsRewardedAdReady(adUnitIdentifier))
        {
            MaxUnityPluginClass.CallStatic("showRewardedAd", adUnitIdentifier, placement);
        }
        else
        {
            MaxSdkLogger.UserWarning("Not showing MAX Ads rewarded ad: ad not ready");
        }
    }

    /// <summary>
    /// Set an extra parameter for the ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded to set the extra parameter for.</param>
    /// <param name="key">The key for the extra parameter.</param>
    /// <param name="value">The value for the extra parameter.</param>
    public static void SetRewardedAdExtraParameter(string adUnitIdentifier, string key, string value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set rewarded ad extra parameter");
        MaxUnityPluginClass.CallStatic("setRewardedAdExtraParameter", adUnitIdentifier, key, value);
    }

    #endregion

    #region Rewarded Interstitial

    /// <summary>
    /// Start loading an rewarded interstitial ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded interstitial ad to load</param>
    public static void LoadRewardedInterstitialAd(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "load rewarded interstitial ad");
        MaxUnityPluginClass.CallStatic("loadRewardedInterstitialAd", adUnitIdentifier);
    }

    /// <summary>
    /// Check if rewarded interstitial ad ad is loaded and ready to be displayed.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded interstitial ad to load</param>
    /// <returns>True if the ad is ready to be displayed</returns>
    public static bool IsRewardedInterstitialAdReady(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "check rewarded interstitial ad loaded");
        return MaxUnityPluginClass.CallStatic<bool>("isRewardedInterstitialAdReady", adUnitIdentifier);
    }

    /// <summary>
    /// Present loaded rewarded interstitial ad. Note: if the rewarded interstitial ad is not ready to be displayed nothing will happen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded interstitial ad to show</param>
    public static void ShowRewardedInterstitialAd(string adUnitIdentifier)
    {
        ShowRewardedInterstitialAd(adUnitIdentifier, null);
    }

    /// <summary>
    /// Present loaded rewarded interstitial ad for a given placement to tie ad events to. Note: if the rewarded interstitial ad is not ready to be displayed nothing will happen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded interstitial to show</param>
    /// <param name="placement">The placement to tie the showing ad's events to</param>
    public static void ShowRewardedInterstitialAd(string adUnitIdentifier, string placement)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "show rewarded interstitial ad");

        if (IsRewardedInterstitialAdReady(adUnitIdentifier))
        {
            MaxUnityPluginClass.CallStatic("showRewardedInterstitialAd", adUnitIdentifier, placement);
        }
        else
        {
            MaxSdkLogger.UserWarning("Not showing MAX Ads rewarded interstitial ad: ad not ready");
        }
    }

    /// <summary>
    /// Set an extra parameter for the ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded interstitial to set the extra parameter for.</param>
    /// <param name="key">The key for the extra parameter.</param>
    /// <param name="value">The value for the extra parameter.</param>
    public static void SetRewardedInterstitialAdExtraParameter(string adUnitIdentifier, string key, string value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set rewarded interstitial ad extra parameter");
        MaxUnityPluginClass.CallStatic("setRewardedInterstitialAdExtraParameter", adUnitIdentifier, key, value);
    }

    #endregion
    
    #region Event Tracking

    /// <summary>
    /// Track an event using AppLovin.
    /// </summary>
    /// <param name="name">An event from the list of pre-defined events may be found in MaxEvents.cs as part of the AppLovin SDK framework.</param>
    /// <param name="parameters">A dictionary containing key-value pairs further describing this event.</param>
    public static void TrackEvent(string name, IDictionary<string, string> parameters = null)
    {
        MaxUnityPluginClass.CallStatic("trackEvent", name, MaxSdkUtils.DictToPropsString(parameters));
    }

    #endregion

    #region Settings

    /// <summary>
    /// Set whether to begin video ads in a muted state or not.
    ///
    /// Please call this method after the SDK has initialized.
    /// </summary>
    /// <param name="muted"><c>true</c> if video ads should being in muted state.</param>
    public static void SetMuted(bool muted)
    {
        MaxUnityPluginClass.CallStatic("setMuted", muted);
    }

    /// <summary>
    /// Whether video ads begin in a muted state or not. Defaults to <c>false</c>.
    ///
    /// Note: Returns <c>false</c> if the SDK is not initialized.
    /// </summary>
    /// <returns><c>true</c> if video ads begin in muted state.</returns>
    public static bool IsMuted()
    {
        return MaxUnityPluginClass.CallStatic<bool>("isMuted");
    }

    /// <summary>
    /// Toggle verbose logging of AppLovin SDK. If enabled AppLovin messages will appear in standard application log accessible via logcat. All log messages will have "AppLovinSdk" tag.
    /// </summary>
    /// <param name="enabled"><c>true</c> if verbose logging should be enabled.</param>
    public static void SetVerboseLogging(bool enabled)
    {
        MaxUnityPluginClass.CallStatic("setVerboseLogging", enabled);
    }

    /// <summary>
    /// Whether or not verbose logging is enabled.
    /// </summary>
    /// <returns><c>true</c> if verbose logging is enabled.</returns>
    public static bool IsVerboseLoggingEnabled()
    {
        return MaxUnityPluginClass.CallStatic<bool>("isVerboseLoggingEnabled");
    }

    /// <summary>
    /// If enabled, a button will appear over fullscreen ads in development builds. This button will display information about the current ad when pressed.
    /// </summary>
    /// <param name="enabled"><c>true</c> if the ad info button should be enabled.</param>
    public static void SetAdInfoButtonEnabled(bool enabled)
    {
        MaxUnityPluginClass.CallStatic("setAdInfoButtonEnabled", enabled);
    }

    /// <summary>
    /// Enable devices to receive test ads, by passing in the advertising identifier (IDFA/GAID) of each test device.
    /// Refer to AppLovin logs for the IDFA/GAID of your current device.
    /// </summary>
    /// <param name="advertisingIdentifiers">String list of advertising identifiers from devices to receive test ads.</param>
    public static void SetTestDeviceAdvertisingIdentifiers(string[] advertisingIdentifiers)
    {
        // Wrap the string array in an object array, so the compiler does not split into multiple strings.
        object[] arguments = { advertisingIdentifiers };
        MaxUnityPluginClass.CallStatic("setTestDeviceAdvertisingIds", arguments);
    }
    
    /// <summary>
    /// Whether or not the native AppLovin SDKs listen to exceptions. Defaults to <c>true</c>.
    /// </summary>
    /// <param name="enabled"><c>true</c> if the native AppLovin SDKs should not listen to exceptions.</param>
    public static void SetExceptionHandlerEnabled(bool enabled)
    {
        MaxUnityPluginClass.CallStatic("setExceptionHandlerEnabled", enabled);
    }

    #endregion
    
    #region Private
    
    internal static void SetUserSegmentField(string name, string value)
    {
        MaxUnityPluginClass.CallStatic("setUserSegmentField", name, value);
    }
    
    #endregion
}
