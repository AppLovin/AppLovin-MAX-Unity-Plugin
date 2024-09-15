using System;
using System.Collections.Generic;
using UnityEngine;
using AppLovinMax.ThirdParty.MiniJson;

/// <summary>
/// Android AppLovin MAX Unity Plugin implementation
/// </summary>
public class MaxSdkAndroid : MaxSdkBase
{
    private static readonly AndroidJavaClass MaxUnityPluginClass =
        new AndroidJavaClass("com.applovin.mediation.unity.MaxUnityPlugin");

    private static BackgroundCallbackProxy BackgroundCallback = new BackgroundCallbackProxy();

    public static MaxUserServiceAndroid UserService
    {
        get { return MaxUserServiceAndroid.Instance; }
    }

    static MaxSdkAndroid()
    {
        InitializeEventExecutor();
        
        MaxUnityPluginClass.CallStatic("setBackgroundCallback", BackgroundCallback);
    }

    #region Initialization

    /// <summary>
    /// Initialize the default instance of AppLovin SDK.
    ///
    /// Please make sure that application's Android manifest or Info.plist includes the AppLovin SDK key.
    /// <param name="adUnitIds">
    /// OPTIONAL: Set the MAX ad unit ids to be used for this instance of the SDK. 3rd-party SDKs will be initialized with the credentials configured for these ad unit ids.
    /// This should only be used if you have different sets of ad unit ids / credentials for the same package name.</param>
    /// </summary>
    public static void InitializeSdk(string[] adUnitIds = null)
    {
        var serializedAdUnitIds = (adUnitIds != null) ? string.Join(",", adUnitIds) : "";
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
    /// <param name="userId">The user identifier to be set. Must not be null.</param>
    public static void SetUserId(string userId)
    {
        MaxUnityPluginClass.CallStatic("setUserId", userId);
    }

    /// <summary>
    /// Set the <see cref="MaxSegmentCollection"/>.
    /// </summary>
    /// <param name="segmentCollection"> The segment collection to be set. Must not be {@code null}</param>
    public static void SetSegmentCollection(MaxSegmentCollection segmentCollection)
    {
        MaxUnityPluginClass.CallStatic("setSegmentCollection", JsonUtility.ToJson(segmentCollection));
    }

    #endregion

    #region MAX

    /// <summary>
    /// Returns the list of available mediation networks.
    ///
    /// Please call this method after the SDK has initialized.
    /// </summary>
    public static List<MaxSdkBase.MediatedNetworkInfo> GetAvailableMediatedNetworks()
    {
        var serializedNetworks = MaxUnityPluginClass.CallStatic<string>("getAvailableMediatedNetworks");
        return MaxSdkUtils.PropsStringsToList<MaxSdkBase.MediatedNetworkInfo>(serializedNetworks);
    }

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
    /// Present the creative debugger UI.
    /// This debugger tool provides information for recently displayed ads.
    ///
    /// Please call this method after the SDK has initialized.
    /// </summary>
    public static void ShowCreativeDebugger()
    {
        MaxUnityPluginClass.CallStatic("showCreativeDebugger");
    }

    /// <summary>
    /// Returns the arbitrary ad value for a given ad unit identifier with key. Returns null if no ad is loaded.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier for which to get the ad value for. Must not be null.</param>
    /// <param name="key">Ad value key. Must not be null.</param>
    /// <returns>Arbitrary ad value for a given key, or null if no ad is loaded.</returns>
    public static string GetAdValue(string adUnitIdentifier, string key)
    {
        var value = MaxUnityPluginClass.CallStatic<string>("getAdValue", adUnitIdentifier, key);

        if (string.IsNullOrEmpty(value)) return null;

        return value;
    }

    #endregion

    #region Privacy

    /// <summary>
    /// Get the SDK configuration for this user.
    ///
    /// Note: This method should be called only after SDK has been initialized.
    /// </summary>
    public static SdkConfiguration GetSdkConfiguration()
    {
        var sdkConfigurationStr = MaxUnityPluginClass.CallStatic<string>("getSdkConfiguration");
        var sdkConfigurationDict = Json.Deserialize(sdkConfigurationStr) as Dictionary<string, object>;
        return SdkConfiguration.Create(sdkConfigurationDict);
    }

    /// <summary>
    /// Set whether or not user has provided consent for information sharing with AppLovin and other providers.
    /// </summary>
    /// <param name="hasUserConsent"><c>true</c> if the user has provided consent for information sharing with AppLovin. <c>false</c> by default.</param>
    public static void SetHasUserConsent(bool hasUserConsent)
    {
        MaxUnityPluginClass.CallStatic("setHasUserConsent", hasUserConsent);
    }

    /// <summary>
    /// Check if user has provided consent for information sharing with AppLovin and other providers.
    /// </summary>
    /// <returns><c>true</c> if user has provided consent for information sharing. <c>false</c> if the user declined to share information or the consent value has not been set <see cref="IsUserConsentSet">.</returns>
    public static bool HasUserConsent()
    {
        return MaxUnityPluginClass.CallStatic<bool>("hasUserConsent");
    }

    /// <summary>
    /// Check if user has set consent for information sharing. 
    /// </summary>
    /// <returns><c>true</c> if user has set a value of consent for information sharing.</returns>
    public static bool IsUserConsentSet()
    {
        return MaxUnityPluginClass.CallStatic<bool>("isUserConsentSet");
    }

    /// <summary>
    /// Set whether or not user has opted out of the sale of their personal information.
    /// </summary>
    /// <param name="doNotSell"><c>true</c> if the user has opted out of the sale of their personal information.</param>
    public static void SetDoNotSell(bool doNotSell)
    {
        MaxUnityPluginClass.CallStatic("setDoNotSell", doNotSell);
    }

    /// <summary>
    /// Check if the user has opted out of the sale of their personal information.
    /// </summary>
    /// <returns><c>true</c> if the user has opted out of the sale of their personal information. <c>false</c> if the user opted in to the sell of their personal information or the value has not been set <see cref="IsDoNotSellSet">.</returns>
    public static bool IsDoNotSell()
    {
        return MaxUnityPluginClass.CallStatic<bool>("isDoNotSell");
    }

    /// <summary>
    /// Check if the user has set the option to sell their personal information.
    /// </summary>
    /// <returns><c>true</c> if user has chosen an option to sell their personal information.</returns>
    public static bool IsDoNotSellSet()
    {
        return MaxUnityPluginClass.CallStatic<bool>("isDoNotSellSet");
    }

    #endregion

    #region Banners

    /// <summary>
    /// Create a new banner.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to create. Must not be null.</param>
    /// <param name="bannerPosition">Banner position. Must not be null.</param>
    public static void CreateBanner(string adUnitIdentifier, BannerPosition bannerPosition)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "create banner");
        MaxUnityPluginClass.CallStatic("createBanner", adUnitIdentifier, bannerPosition.ToSnakeCaseString());
    }

    /// <summary>
    /// Create a new banner with a custom position.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to create. Must not be null.</param>
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
    /// Load a new banner ad.
    /// NOTE: The <see cref="CreateBanner()"/> method loads the first banner ad and initiates an automated banner refresh process.
    /// You only need to call this method if you pause banner refresh.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to load. Must not be null.</param>
    public static void LoadBanner(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "load banner");
        MaxUnityPluginClass.CallStatic("loadBanner", adUnitIdentifier);
    }

    /// <summary>
    /// Set the banner placement for an ad unit identifier to tie the future ad events to.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to set the placement for. Must not be null.</param>
    /// <param name="placement">Placement to set</param>
    public static void SetBannerPlacement(string adUnitIdentifier, string placement)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set banner placement");
        MaxUnityPluginClass.CallStatic("setBannerPlacement", adUnitIdentifier, placement);
    }

    /// <summary>
    /// Starts or resumes auto-refreshing of the banner for the given ad unit identifier.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner for which to start auto-refresh. Must not be null.</param>
    public static void StartBannerAutoRefresh(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "start banner auto-refresh");
        MaxUnityPluginClass.CallStatic("startBannerAutoRefresh", adUnitIdentifier);
    }

    /// <summary>
    /// Pauses auto-refreshing of the banner for the given ad unit identifier.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner for which to stop auto-refresh. Must not be null.</param>
    public static void StopBannerAutoRefresh(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "stop banner auto-refresh");
        MaxUnityPluginClass.CallStatic("stopBannerAutoRefresh", adUnitIdentifier);
    }

    /// <summary>
    /// Updates the position of the banner to the new position provided.
    /// </summary>
    /// <param name="adUnitIdentifier">The ad unit identifier of the banner for which to update the position. Must not be null.</param>
    /// <param name="bannerPosition">A new position for the banner. Must not be null.</param>
    public static void UpdateBannerPosition(string adUnitIdentifier, BannerPosition bannerPosition)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "update banner position");
        MaxUnityPluginClass.CallStatic("updateBannerPosition", adUnitIdentifier, bannerPosition.ToSnakeCaseString());
    }

    /// <summary>
    /// Updates the position of the banner to the new coordinates provided.
    /// </summary>
    /// <param name="adUnitIdentifier">The ad unit identifier of the banner for which to update the position. Must not be null.</param>
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
    /// <param name="adUnitIdentifier">The ad unit identifier of the banner for which to override the width for. Must not be null.</param>
    /// <param name="width">The desired width of the banner in dp</param>
    public static void SetBannerWidth(string adUnitIdentifier, float width)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set banner width");
        MaxUnityPluginClass.CallStatic("setBannerWidth", adUnitIdentifier, width);
    }

    /// <summary>
    /// Show banner at a position determined by the 'CreateBanner' call.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to show. Must not be null.</param>
    public static void ShowBanner(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "show banner");
        MaxUnityPluginClass.CallStatic("showBanner", adUnitIdentifier);
    }

    /// <summary>
    /// Remove banner from the ad view and destroy it.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to destroy. Must not be null.</param>
    public static void DestroyBanner(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "destroy banner");
        MaxUnityPluginClass.CallStatic("destroyBanner", adUnitIdentifier);
    }

    /// <summary>
    /// Hide banner.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to hide. Must not be null.</param>
    public static void HideBanner(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "hide banner");
        MaxUnityPluginClass.CallStatic("hideBanner", adUnitIdentifier);
    }

    /// <summary>
    /// Set non-transparent background color for banners to be fully functional.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to set background color for. Must not be null.</param>
    /// <param name="color">A background color to set for the ad. Must not be null.</param>
    public static void SetBannerBackgroundColor(string adUnitIdentifier, Color color)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set background color");
        MaxUnityPluginClass.CallStatic("setBannerBackgroundColor", adUnitIdentifier, MaxSdkUtils.ParseColor(color));
    }

    /// <summary>
    /// Set an extra parameter for the banner ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to set the extra parameter for. Must not be null.</param>
    /// <param name="key">The key for the extra parameter. Must not be null.</param>
    /// <param name="value">The value for the extra parameter.</param>
    public static void SetBannerExtraParameter(string adUnitIdentifier, string key, string value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set banner extra parameter");
        MaxUnityPluginClass.CallStatic("setBannerExtraParameter", adUnitIdentifier, key, value);
    }

    /// <summary>
    /// Set a local extra parameter for the banner ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to set the local extra parameter for. Must not be null.</param>
    /// <param name="key">The key for the local extra parameter. Must not be null.</param>
    /// <param name="value">The value for the extra parameter. Accepts the following types: <see cref="AndroidJavaObject"/>, <c>null</c>, <c>IList</c>, <c>IDictionary</c>, <c>string</c>, primitive types</param>
    public static void SetBannerLocalExtraParameter(string adUnitIdentifier, string key, object value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set banner local extra parameter");

        if (value == null || value is AndroidJavaObject)
        {
            MaxUnityPluginClass.CallStatic("setBannerLocalExtraParameter", adUnitIdentifier, key, (AndroidJavaObject) value);
        }
        else
        {
            MaxUnityPluginClass.CallStatic("setBannerLocalExtraParameterJson", adUnitIdentifier, key, SerializeLocalExtraParameterValue(value));
        }
    }

    /// <summary>
    /// The custom data to tie the showing banner ad to, for ILRD and rewarded postbacks via the <c>{CUSTOM_DATA}</c> macro. Maximum size is 8KB.
    /// </summary>
    /// <param name="adUnitIdentifier">Banner ad unit identifier of the banner to set the custom data for. Must not be null.</param>
    /// <param name="customData">The custom data to be set.</param>
    public static void SetBannerCustomData(string adUnitIdentifier, string customData)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set banner custom data");
        MaxUnityPluginClass.CallStatic("setBannerCustomData", adUnitIdentifier, customData);
    }

    /// <summary>
    /// The banner position on the screen. When setting the banner position via <see cref="CreateBanner(string, float, float)"/> or <see cref="UpdateBannerPosition(string, float, float)"/>,
    /// the banner is placed within the safe area of the screen. This returns the absolute position of the banner on screen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner for which to get the position on screen. Must not be null.</param>
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
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to create. Must not be null.</param>
    /// <param name="mrecPosition">MREC position. Must not be null.</param>
    public static void CreateMRec(string adUnitIdentifier, AdViewPosition mrecPosition)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "create MREC");
        MaxUnityPluginClass.CallStatic("createMRec", adUnitIdentifier, mrecPosition.ToSnakeCaseString());
    }

    /// <summary>
    /// Create a new MREC with a custom position.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to create. Must not be null.</param>
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
    /// Load a new MREC ad.
    /// NOTE: The <see cref="CreateMRec()"/> method loads the first MREC ad and initiates an automated MREC refresh process.
    /// You only need to call this method if you pause MREC refresh.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to load. Must not be null.</param>
    public static void LoadMRec(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "load MREC");
        MaxUnityPluginClass.CallStatic("loadMRec", adUnitIdentifier);
    }

    /// <summary>
    /// Set the MREC placement for an ad unit identifier to tie the future ad events to.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to set the placement for. Must not be null.</param>
    /// <param name="placement">Placement to set</param>
    public static void SetMRecPlacement(string adUnitIdentifier, string placement)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set MREC placement");
        MaxUnityPluginClass.CallStatic("setMRecPlacement", adUnitIdentifier, placement);
    }

    /// <summary>
    /// Starts or resumes auto-refreshing of the MREC for the given ad unit identifier.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC for which to start auto-refresh. Must not be null.</param>
    public static void StartMRecAutoRefresh(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "start MREC auto-refresh");
        MaxUnityPluginClass.CallStatic("startMRecAutoRefresh", adUnitIdentifier);
    }

    /// <summary>
    /// Pauses auto-refreshing of the MREC for the given ad unit identifier.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC for which to stop auto-refresh. Must not be null.</param>
    public static void StopMRecAutoRefresh(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "stop MREC auto-refresh");
        MaxUnityPluginClass.CallStatic("stopMRecAutoRefresh", adUnitIdentifier);
    }

    /// <summary>
    /// Updates the position of the MREC to the new position provided.
    /// </summary>
    /// <param name="adUnitIdentifier">The ad unit identifier of the MREC for which to update the position. Must not be null.</param>
    /// <param name="mrecPosition">A new position for the MREC. Must not be null.</param>
    public static void UpdateMRecPosition(string adUnitIdentifier, AdViewPosition mrecPosition)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "update MREC position");
        MaxUnityPluginClass.CallStatic("updateMRecPosition", adUnitIdentifier, mrecPosition.ToSnakeCaseString());
    }

    /// <summary>
    /// Updates the position of the MREC to the new coordinates provided.
    /// </summary>
    /// <param name="adUnitIdentifier">The ad unit identifier of the MREC for which to update the position. Must not be null.</param>
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
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to show. Must not be null.</param>
    public static void ShowMRec(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "show MREC");
        MaxUnityPluginClass.CallStatic("showMRec", adUnitIdentifier);
    }

    /// <summary>
    /// Remove MREC from the ad view and destroy it.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to destroy. Must not be null.</param>
    public static void DestroyMRec(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "destroy MREC");
        MaxUnityPluginClass.CallStatic("destroyMRec", adUnitIdentifier);
    }

    /// <summary>
    /// Hide MREC.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to hide. Must not be null.</param>
    public static void HideMRec(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "hide MREC");
        MaxUnityPluginClass.CallStatic("hideMRec", adUnitIdentifier);
    }

    /// <summary>
    /// Set an extra parameter for the MREC ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to set the extra parameter for. Must not be null.</param>
    /// <param name="key">The key for the extra parameter. Must not be null.</param>
    /// <param name="value">The value for the extra parameter.</param>
    public static void SetMRecExtraParameter(string adUnitIdentifier, string key, string value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set MREC extra parameter");
        MaxUnityPluginClass.CallStatic("setMRecExtraParameter", adUnitIdentifier, key, value);
    }

    /// <summary>
    /// Set a local extra parameter for the MREC ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to set the local extra parameter for. Must not be null.</param>
    /// <param name="key">The key for the local extra parameter. Must not be null.</param>
    /// <param name="value">The value for the extra parameter. Accepts the following types: <see cref="AndroidJavaObject"/>, <c>null</c>, <c>IList</c>, <c>IDictionary</c>, <c>string</c>, primitive types</param>
    public static void SetMRecLocalExtraParameter(string adUnitIdentifier, string key, object value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set MREC local extra parameter");

        if (value == null || value is AndroidJavaObject)
        {
            MaxUnityPluginClass.CallStatic("setMRecLocalExtraParameter", adUnitIdentifier, key, (AndroidJavaObject) value);
        }
        else
        {
            MaxUnityPluginClass.CallStatic("setMRecLocalExtraParameterJson", adUnitIdentifier, key, SerializeLocalExtraParameterValue(value));
        }
    }

    /// <summary>
    /// The custom data to tie the showing MREC ad to, for ILRD and rewarded postbacks via the <c>{CUSTOM_DATA}</c> macro. Maximum size is 8KB.
    /// </summary>
    /// <param name="adUnitIdentifier">MREC Ad unit identifier of the banner to set the custom data for. Must not be null.</param>
    /// <param name="customData">The custom data to be set.</param>
    public static void SetMRecCustomData(string adUnitIdentifier, string customData)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set MREC custom data");
        MaxUnityPluginClass.CallStatic("setMRecCustomData", adUnitIdentifier, customData);
    }

    /// <summary>
    /// The MREC position on the screen. When setting the banner position via <see cref="CreateMRec(string, float, float)"/> or <see cref="UpdateMRecPosition(string, float, float)"/>,
    /// the banner is placed within the safe area of the screen. This returns the absolute position of the MREC on screen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC for which to get the position on screen. Must not be null.</param>
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
    /// <param name="adUnitIdentifier">Ad unit identifier of the interstitial to load. Must not be null.</param>
    public static void LoadInterstitial(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "load interstitial");
        MaxUnityPluginClass.CallStatic("loadInterstitial", adUnitIdentifier);
    }

    /// <summary>
    /// Check if interstitial ad is loaded and ready to be displayed.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the interstitial to load. Must not be null.</param>
    /// <returns>True if the ad is ready to be displayed</returns>
    public static bool IsInterstitialReady(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "check interstitial loaded");
        return MaxUnityPluginClass.CallStatic<bool>("isInterstitialReady", adUnitIdentifier);
    }

    /// <summary>
    /// Present loaded interstitial for a given placement to tie ad events to. Note: if the interstitial is not ready to be displayed nothing will happen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the interstitial to load. Must not be null.</param>
    /// <param name="placement">The placement to tie the showing ad's events to</param>
    /// <param name="customData">The custom data to tie the showing ad's events to. Maximum size is 8KB.</param>
    public static void ShowInterstitial(string adUnitIdentifier, string placement = null, string customData = null)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "show interstitial");

        if (IsInterstitialReady(adUnitIdentifier))
        {
            MaxUnityPluginClass.CallStatic("showInterstitial", adUnitIdentifier, placement, customData);
        }
        else
        {
            MaxSdkLogger.UserWarning("Not showing MAX Ads interstitial: ad not ready");
        }
    }

    /// <summary>
    /// Set an extra parameter for the ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the interstitial to set the extra parameter for. Must not be null.</param>
    /// <param name="key">The key for the extra parameter. Must not be null.</param>
    /// <param name="value">The value for the extra parameter.</param>
    public static void SetInterstitialExtraParameter(string adUnitIdentifier, string key, string value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set interstitial extra parameter");
        MaxUnityPluginClass.CallStatic("setInterstitialExtraParameter", adUnitIdentifier, key, value);
    }

    /// <summary>
    /// Set a local extra parameter for the ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the interstitial to set the local extra parameter for. Must not be null.</param>
    /// <param name="key">The key for the local extra parameter. Must not be null.</param>
    /// <param name="value">The value for the extra parameter. Accepts the following types: <see cref="AndroidJavaObject"/>, <c>null</c>, <c>IList</c>, <c>IDictionary</c>, <c>string</c>, primitive types</param>
    public static void SetInterstitialLocalExtraParameter(string adUnitIdentifier, string key, object value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set interstitial local extra parameter");

        if (value == null || value is AndroidJavaObject)
        {
            MaxUnityPluginClass.CallStatic("setInterstitialLocalExtraParameter", adUnitIdentifier, key, (AndroidJavaObject) value);
        }
        else
        {
            MaxUnityPluginClass.CallStatic("setInterstitialLocalExtraParameterJson", adUnitIdentifier, key, SerializeLocalExtraParameterValue(value));
        }
    }

    #endregion

    #region App Open

    /// <summary>
    /// Start loading an app open ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the app open ad to load. Must not be null.</param>
    public static void LoadAppOpenAd(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "load app open ad");
        MaxUnityPluginClass.CallStatic("loadAppOpenAd", adUnitIdentifier);
    }

    /// <summary>
    /// Check if app open ad ad is loaded and ready to be displayed.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the app open ad to load. Must not be null.</param>
    /// <returns>True if the ad is ready to be displayed</returns>
    public static bool IsAppOpenAdReady(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "check app open ad loaded");
        return MaxUnityPluginClass.CallStatic<bool>("isAppOpenAdReady", adUnitIdentifier);
    }

    /// <summary>
    /// Present loaded app open ad for a given placement to tie ad events to. Note: if the app open ad is not ready to be displayed nothing will happen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the app open ad to load. Must not be null.</param>
    /// <param name="placement">The placement to tie the showing ad's events to</param>
    /// <param name="customData">The custom data to tie the showing ad's events to. Maximum size is 8KB.</param>
    public static void ShowAppOpenAd(string adUnitIdentifier, string placement = null, string customData = null)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "show app open ad");

        if (IsAppOpenAdReady(adUnitIdentifier))
        {
            MaxUnityPluginClass.CallStatic("showAppOpenAd", adUnitIdentifier, placement, customData);
        }
        else
        {
            MaxSdkLogger.UserWarning("Not showing MAX Ads app open ad: ad not ready");
        }
    }

    /// <summary>
    /// Set an extra parameter for the ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the app open ad to set the extra parameter for. Must not be null.</param>
    /// <param name="key">The key for the extra parameter. Must not be null.</param>
    /// <param name="value">The value for the extra parameter.</param>
    public static void SetAppOpenAdExtraParameter(string adUnitIdentifier, string key, string value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set app open ad extra parameter");
        MaxUnityPluginClass.CallStatic("setAppOpenAdExtraParameter", adUnitIdentifier, key, value);
    }

    /// <summary>
    /// Set a local extra parameter for the ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the app open ad to set the local extra parameter for. Must not be null.</param>
    /// <param name="key">The key for the local extra parameter. Must not be null.</param>
    /// <param name="value">The value for the extra parameter. Accepts the following types: <see cref="AndroidJavaObject"/>, <c>null</c>, <c>IList</c>, <c>IDictionary</c>, <c>string</c>, primitive types</param>
    public static void SetAppOpenAdLocalExtraParameter(string adUnitIdentifier, string key, object value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set app open ad local extra parameter");

        if (value == null || value is AndroidJavaObject)
        {
            MaxUnityPluginClass.CallStatic("setAppOpenAdLocalExtraParameter", adUnitIdentifier, key, (AndroidJavaObject) value);
        }
        else
        {
            MaxUnityPluginClass.CallStatic("setAppOpenAdLocalExtraParameterJson", adUnitIdentifier, key, SerializeLocalExtraParameterValue(value));
        }
    }

    #endregion

    #region Rewarded

    /// <summary>
    /// Start loading an rewarded ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded ad to load. Must not be null.</param>
    public static void LoadRewardedAd(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "load rewarded ad");
        MaxUnityPluginClass.CallStatic("loadRewardedAd", adUnitIdentifier);
    }

    /// <summary>
    /// Check if rewarded ad ad is loaded and ready to be displayed.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded ad to load. Must not be null.</param>
    /// <returns>True if the ad is ready to be displayed</returns>
    public static bool IsRewardedAdReady(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "check rewarded ad loaded");
        return MaxUnityPluginClass.CallStatic<bool>("isRewardedAdReady", adUnitIdentifier);
    }

    /// <summary> ready to be
    /// Present loaded rewarded ad for a given placement to tie ad events to. Note: if the rewarded ad is not ready to be displayed nothing will happen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the interstitial to load. Must not be null.</param>
    /// <param name="placement">The placement to tie the showing ad's events to</param>
    /// <param name="customData">The custom data to tie the showing ad's events to. Maximum size is 8KB.</param>
    public static void ShowRewardedAd(string adUnitIdentifier, string placement = null, string customData = null)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "show rewarded ad");

        if (IsRewardedAdReady(adUnitIdentifier))
        {
            MaxUnityPluginClass.CallStatic("showRewardedAd", adUnitIdentifier, placement, customData);
        }
        else
        {
            MaxSdkLogger.UserWarning("Not showing MAX Ads rewarded ad: ad not ready");
        }
    }

    /// <summary>
    /// Set an extra parameter for the ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded to set the extra parameter for. Must not be null.</param>
    /// <param name="key">The key for the extra parameter. Must not be null.</param>
    /// <param name="value">The value for the extra parameter.</param>
    public static void SetRewardedAdExtraParameter(string adUnitIdentifier, string key, string value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set rewarded ad extra parameter");
        MaxUnityPluginClass.CallStatic("setRewardedAdExtraParameter", adUnitIdentifier, key, value);
    }

    /// <summary>
    /// Set a local extra parameter for the ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded to set the local extra parameter for. Must not be null.</param>
    /// <param name="key">The key for the local extra parameter. Must not be null.</param>
    /// <param name="value">The value for the extra parameter. Accepts the following types: <see cref="AndroidJavaObject"/>, <c>null</c>, <c>IList</c>, <c>IDictionary</c>, <c>string</c>, primitive types</param>
    public static void SetRewardedAdLocalExtraParameter(string adUnitIdentifier, string key, object value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set rewarded ad local extra parameter");

        if (value == null || value is AndroidJavaObject)
        {
            MaxUnityPluginClass.CallStatic("setRewardedAdLocalExtraParameter", adUnitIdentifier, key, (AndroidJavaObject) value);
        }
        else
        {
            MaxUnityPluginClass.CallStatic("setRewardedAdLocalExtraParameterJson", adUnitIdentifier, key, SerializeLocalExtraParameterValue(value));
        }
    }

    #endregion

    #region Rewarded Interstitial

    /// <summary>
    /// Start loading an rewarded interstitial ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded interstitial ad to load. Must not be null.</param>
    public static void LoadRewardedInterstitialAd(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "load rewarded interstitial ad");
        MaxUnityPluginClass.CallStatic("loadRewardedInterstitialAd", adUnitIdentifier);
    }

    /// <summary>
    /// Check if rewarded interstitial ad ad is loaded and ready to be displayed.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded interstitial ad to load. Must not be null.</param>
    /// <returns>True if the ad is ready to be displayed</returns>
    public static bool IsRewardedInterstitialAdReady(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "check rewarded interstitial ad loaded");
        return MaxUnityPluginClass.CallStatic<bool>("isRewardedInterstitialAdReady", adUnitIdentifier);
    }

    /// <summary>
    /// Present loaded rewarded interstitial ad for a given placement to tie ad events to. Note: if the rewarded interstitial ad is not ready to be displayed nothing will happen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded interstitial to show. Must not be null.</param>
    /// <param name="placement">The placement to tie the showing ad's events to</param>
    /// <param name="customData">The custom data to tie the showing ad's events to. Maximum size is 8KB.</param>
    public static void ShowRewardedInterstitialAd(string adUnitIdentifier, string placement = null, string customData = null)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "show rewarded interstitial ad");

        if (IsRewardedInterstitialAdReady(adUnitIdentifier))
        {
            MaxUnityPluginClass.CallStatic("showRewardedInterstitialAd", adUnitIdentifier, placement, customData);
        }
        else
        {
            MaxSdkLogger.UserWarning("Not showing MAX Ads rewarded interstitial ad: ad not ready");
        }
    }

    /// <summary>
    /// Set an extra parameter for the ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded interstitial to set the extra parameter for. Must not be null.</param>
    /// <param name="key">The key for the extra parameter. Must not be null.</param>
    /// <param name="value">The value for the extra parameter.</param>
    public static void SetRewardedInterstitialAdExtraParameter(string adUnitIdentifier, string key, string value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set rewarded interstitial ad extra parameter");
        MaxUnityPluginClass.CallStatic("setRewardedInterstitialAdExtraParameter", adUnitIdentifier, key, value);
    }

    /// <summary>
    /// Set a local extra parameter for the ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded interstitial to set the local extra parameter for. Must not be null.</param>
    /// <param name="key">The key for the local extra parameter. Must not be null.</param>
    /// <param name="value">The value for the extra parameter. Accepts the following types: <see cref="AndroidJavaObject"/>, <c>null</c>, <c>IList</c>, <c>IDictionary</c>, <c>string</c>, primitive types</param>
    public static void SetRewardedInterstitialAdLocalExtraParameter(string adUnitIdentifier, string key, object value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set rewarded interstitial ad local extra parameter");

        if (value == null || value is AndroidJavaObject)
        {
            MaxUnityPluginClass.CallStatic("setRewardedInterstitialAdLocalExtraParameter", adUnitIdentifier, key, (AndroidJavaObject) value);
        }
        else
        {
            MaxUnityPluginClass.CallStatic("setRewardedInterstitialAdLocalExtraParameterJson", adUnitIdentifier, key, SerializeLocalExtraParameterValue(value));
        }
    }

    #endregion

    #region Event Tracking

    /// <summary>
    /// Track an event using AppLovin.
    /// </summary>
    /// <param name="name">An event from the list of pre-defined events may be found in MaxEvents.cs as part of the AppLovin SDK framework. Must not be null.</param>
    /// <param name="parameters">A dictionary containing key-value pairs further describing this event.</param>
    public static void TrackEvent(string name, IDictionary<string, string> parameters = null)
    {
        MaxUnityPluginClass.CallStatic("trackEvent", name, Json.Serialize(parameters));
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
    /// Whether the creative debugger will be displayed on fullscreen ads after flipping the device screen down twice. Defaults to true.
    /// </summary>
    /// <param name="enabled"><c>true</c> if the creative debugger should be enabled.</param>
    public static void SetCreativeDebuggerEnabled(bool enabled)
    {
        MaxUnityPluginClass.CallStatic("setCreativeDebuggerEnabled", enabled);
    }

    /// <summary>
    /// Enable devices to receive test ads, by passing in the advertising identifier (IDFA/GAID) of each test device.
    /// Refer to AppLovin logs for the IDFA/GAID of your current device.
    /// </summary>
    /// <param name="advertisingIdentifiers">String list of advertising identifiers from devices to receive test ads.</param>
    public static void SetTestDeviceAdvertisingIdentifiers(string[] advertisingIdentifiers)
    {
        if (IsInitialized())
        {
            MaxSdkLogger.UserError("Test Device Advertising Identifiers must be set before SDK initialization.");
            return;
        }

        // Wrap the string array in an object array, so the compiler does not split into multiple strings.
        object[] arguments = {advertisingIdentifiers};
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

    /// <summary>
    /// Set an extra parameter to pass to the AppLovin server.
    /// </summary>
    /// <param name="key">The key for the extra parameter. Must not be null.</param>
    /// <param name="value">The value for the extra parameter. May be null.</param>
    public static void SetExtraParameter(string key, string value)
    {
        MaxUnityPluginClass.CallStatic("setExtraParameter", key, value);
    }

    /// <summary>
    /// Get the native insets in pixels for the safe area.
    /// These insets are used to position ads within the safe area of the screen.
    /// </summary>
    public static SafeAreaInsets GetSafeAreaInsets()
    {
        // Use an int array instead of json serialization for performance
        var insets = MaxUnityPluginClass.CallStatic<int[]>("getSafeAreaInsets");

        // Convert from points to pixels
        var screenDensity = MaxSdkUtils.GetScreenDensity();
        for (var i = 0; i < insets.Length; i++)
        {
            insets[i] *= (int) screenDensity;
        }

        return new SafeAreaInsets(insets);
    }

    #endregion

    #region Obsolete

    [Obsolete("This API has been deprecated and will be removed in a future release. Please set your SDK key in the AppLovin Integration Manager.")]
    public static void SetSdkKey(string sdkKey)
    {
        MaxUnityPluginClass.CallStatic("setSdkKey", sdkKey);
        Debug.LogWarning("MaxSdk.SetSdkKey() has been deprecated and will be removed in a future release. Please set your SDK key in the AppLovin Integration Manager.");
    }

    [Obsolete("This method has been deprecated. Please use `GetSdkConfiguration().ConsentDialogState`")]
    public static ConsentDialogState GetConsentDialogState()
    {
        if (!IsInitialized())
        {
            MaxSdkLogger.UserWarning(
                "MAX Ads SDK has not been initialized yet. GetConsentDialogState() may return ConsentDialogState.Unknown");
        }

        return (ConsentDialogState) MaxUnityPluginClass.CallStatic<int>("getConsentDialogState");
    }

    [Obsolete("This method has been deprecated. The AdInfo object is returned with ad callbacks.")]
    public static AdInfo GetAdInfo(string adUnitIdentifier)
    {
        var adInfoString = MaxUnityPluginClass.CallStatic<string>("getAdInfo", adUnitIdentifier);

        if (string.IsNullOrEmpty(adInfoString)) return null;

        var adInfoDictionary = Json.Deserialize(adInfoString) as Dictionary<string, object>;
        return new AdInfo(adInfoDictionary);
    }

    #endregion

    internal class BackgroundCallbackProxy : AndroidJavaProxy
    {
        public BackgroundCallbackProxy() : base("com.applovin.mediation.unity.MaxUnityAdManager$BackgroundCallback") { }

        public void onEvent(string propsStr)
        {
            HandleBackgroundCallback(propsStr);
        }
    }
}
