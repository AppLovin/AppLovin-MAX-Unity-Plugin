// This script is used for Unity Editor and non Android or iOS platforms.

#if UNITY_EDITOR || !(UNITY_ANDROID || UNITY_IPHONE || UNITY_IOS)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Unity Editor AppLovin MAX Unity Plugin implementation
/// </summary>
public class MaxSdkUnityEditor : MaxSdkBase
{
    private static bool _isInitialized;
    private static bool _hasSdkKey;
    private static bool _hasUserConsent = true;
    private static bool _isAgeRestrictedUser = false;
    private static bool _showStubAds = true;
    private static readonly HashSet<string> RequestedAdUnits = new HashSet<string>();
    private static readonly HashSet<string> ReadyAdUnits = new HashSet<string>();
    private static readonly Dictionary<string, GameObject> StubBanners = new Dictionary<string, GameObject>();

    public static MaxVariableServiceUnityEditor VariableService
    {
        get { return MaxVariableServiceUnityEditor.Instance; }
    }

    public static MaxUserServiceUnityEditor UserService
    {
        get { return MaxUserServiceUnityEditor.Instance; }
    }

    static MaxSdkUnityEditor()
    {
        InitCallbacks();
    }

    /// <summary>
    /// Set AppLovin SDK Key.
    ///
    /// This method must be called before any other SDK operation
    /// </summary>
    public static void SetSdkKey(string sdkKey)
    {
        _hasSdkKey = true;
    }

    #region Initialization

    /// <summary>
    /// Initialize the default instance of AppLovin SDK.
    ///
    /// Please make sure that application's Android manifest or Info.plist includes the AppLovin SDK key.
    ///
    /// <param name="adUnitIds">
    /// OPTIONAL: Set the MAX ad unit ids to be used for this instance of the SDK. 3rd-party SDKs will be initialized with the credentials configured for these ad unit ids.
    /// This should only be used if you have different sets of ad unit ids / credentials for the same package name.</param>
    /// </summary>
    public static void InitializeSdk(string[] adUnitIds = null)
    {
        _ensureHaveSdkKey();

        _isInitialized = true;
        _hasSdkKey = true;

        // Slight delay to emulate the SDK initializing
        ExecuteWithDelay(0.1f, () =>
        {
            _isInitialized = true;

#if UNITY_EDITOR
            MaxSdkCallbacks.EmitSdkInitializedEvent();
#endif
        });
    }

    /// <summary>
    /// Check if the SDK has been initialized.
    /// </summary>
    /// <returns>True if SDK has been initialized</returns>
    public static bool IsInitialized()
    {
        return _isInitialized;
    }

    /// <summary>
    /// Prevent stub ads from showing in the Unity Editor
    /// </summary>
    public static void DisableStubAds()
    {
        _showStubAds = false;
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
    public static void SetUserId(string userId) { }

    /// <summary>
    /// User segments allow us to serve ads using custom-defined rules based on which segment the user is in. For now, we only support a custom string 32 alphanumeric characters or less as the user segment.
    /// </summary>
    public static MaxUserSegment UserSegment
    {
        get { return SharedUserSegment; }
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
        return new List<MaxSdkBase.MediatedNetworkInfo>();
    }

    /// <summary>
    /// Present the mediation debugger UI.
    /// This debugger tool provides the status of your integration for each third-party ad network.
    ///
    /// Please call this method after the SDK has initialized.
    /// </summary>
    public static void ShowMediationDebugger()
    {
        if (!_isInitialized)
        {
            MaxSdkLogger.UserWarning("The mediation debugger cannot be shown before the MAX SDK has been initialized."
                                     + "\nCall 'MaxSdk.InitializeSdk();' and listen for 'MaxSdkCallbacks.OnSdkInitializedEvent' before showing the mediation debugger.");
        }
        else
        {
            MaxSdkLogger.UserWarning("The mediation debugger cannot be shown in the Unity Editor. Please export the project to Android or iOS first.");
        }
    }

    /// <summary>
    /// Returns the arbitrary ad value for a given ad unit identifier with key. Returns null if no ad is loaded.
    /// </summary>
    /// <param name="adUnitIdentifier"></param>
    /// <param name="key">Ad value key</param>
    /// <returns>Arbitrary ad value for a given key, or null if no ad is loaded.</returns>
    public static string GetAdValue(string adUnitIdentifier, string key)
    {
        return "";
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
        var sdkConfiguration = new SdkConfiguration();
        sdkConfiguration.ConsentDialogState = ConsentDialogState.Unknown;
#if UNITY_EDITOR
        sdkConfiguration.AppTrackingStatus = AppTrackingStatus.Authorized;
#endif
        sdkConfiguration.CountryCode = RegionInfo.CurrentRegion.TwoLetterISORegionName;

        return sdkConfiguration;
    }

    /// <summary>
    /// Set whether or not user has provided consent for information sharing with AppLovin and other providers.
    /// </summary>
    /// <param name="hasUserConsent">'true' if the user has provided consent for information sharing with AppLovin. 'false' by default.</param>
    public static void SetHasUserConsent(bool hasUserConsent)
    {
        _hasUserConsent = hasUserConsent;
    }

    /// <summary>
    /// Check if user has provided consent for information sharing with AppLovin and other providers.
    /// </summary>
    /// <returns></returns>
    public static bool HasUserConsent()
    {
        return _hasUserConsent;
    }

    /// <summary>
    /// Mark user as age restricted (i.e. under 16).
    /// </summary>
    /// <param name="isAgeRestrictedUser">'true' if the user is age restricted (i.e. under 16).</param>
    public static void SetIsAgeRestrictedUser(bool isAgeRestrictedUser)
    {
        _isAgeRestrictedUser = isAgeRestrictedUser;
    }

    /// <summary>
    /// Check if user is age restricted.
    /// </summary>
    /// <returns></returns>
    public static bool IsAgeRestrictedUser()
    {
        return _isAgeRestrictedUser;
    }

    private static bool _doNotSell = false;

    /// <summary>
    /// Set whether or not user has opted out of the sale of their personal information.
    /// </summary>
    /// <param name="doNotSell">'true' if the user has opted out of the sale of their personal information.</param>
    public static void SetDoNotSell(bool doNotSell)
    {
        _doNotSell = doNotSell;
    }

    /// <summary>
    /// Check if the user has opted out of the sale of their personal information.
    /// </summary>
    public static bool IsDoNotSell()
    {
        return _doNotSell;
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
        RequestAdUnit(adUnitIdentifier);

        if (_showStubAds && !StubBanners.ContainsKey(adUnitIdentifier))
        {
            CreateStubBanner(adUnitIdentifier, bannerPosition);
        }
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
        RequestAdUnit(adUnitIdentifier);

        // TODO: Add stub ads support
    }

    private static void CreateStubBanner(string adUnitIdentifier, BannerPosition bannerPosition)
    {
#if UNITY_EDITOR
        // Only support BottomCenter and TopCenter for now
        var bannerPrefabName = bannerPosition == BannerPosition.BottomCenter ? "BannerBottom" : "BannerTop";
        var prefabPath = MaxSdkUtils.GetAssetPathForExportPath("MaxSdk/Prefabs/" + bannerPrefabName + ".prefab");
        var bannerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var stubBanner = Object.Instantiate(bannerPrefab, Vector3.zero, Quaternion.identity);
        stubBanner.SetActive(false); // Hidden by default
        Object.DontDestroyOnLoad(stubBanner);

        var bannerText = stubBanner.GetComponentInChildren<Text>();
        bannerText.text += ":\n" + adUnitIdentifier;

        StubBanners.Add(adUnitIdentifier, stubBanner);
#endif
    }

    /// <summary>
    /// Set the banner placement for an ad unit identifier to tie the future ad events to.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to set the placement for</param>
    /// <param name="placement">Placement to set</param>
    public static void SetBannerPlacement(string adUnitIdentifier, string placement)
    {
        MaxSdkLogger.UserDebug("Setting banner placement to '" + placement + "' for ad unit id '" + adUnitIdentifier + "'");
    }

    /// <summary>
    /// Updates the position of the banner to the new position provided.
    /// </summary>
    /// <param name="adUnitIdentifier">The ad unit identifier of the banner for which to update the position</param>
    /// <param name="bannerPosition">A new position for the banner</param>
    public static void UpdateBannerPosition(string adUnitIdentifier, BannerPosition bannerPosition)
    {
        Debug.Log("[AppLovin MAX] Updating banner position to '" + bannerPosition + "' for ad unit id '" + adUnitIdentifier + "'");
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
        MaxSdkLogger.UserDebug("Updating banner position to '(" + x + "," + y + ")");
    }

    /// <summary>
    /// Overrides the width of the banner in points/dp.
    /// </summary>
    /// <param name="adUnitIdentifier">The ad unit identifier of the banner for which to override the width for</param>
    /// <param name="width">The desired width of the banner in points/dp</param>
    public static void SetBannerWidth(string adUnitIdentifier, float width)
    {
        // NOTE: Will implement in a future release
        Debug.Log("[AppLovin MAX] Set banner width to '" + width + "' for ad unit id '" + adUnitIdentifier + "'");
    }

    /// <summary>
    /// Show banner at a position determined by the 'CreateBanner' call.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to show</param>
    public static void ShowBanner(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "show banner");

        if (!IsAdUnitRequested(adUnitIdentifier))
        {
            MaxSdkLogger.UserWarning("Banner '" + adUnitIdentifier + "' was not created, can not show it");
        }
        else
        {
            GameObject stubBanner;
            if (StubBanners.TryGetValue(adUnitIdentifier, out stubBanner))
            {
                stubBanner.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Remove banner from the ad view and destroy it.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to destroy</param>
    public static void DestroyBanner(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "destroy banner");

        GameObject stubBanner;
        if (StubBanners.TryGetValue(adUnitIdentifier, out stubBanner))
        {
            Object.Destroy(stubBanner);
            StubBanners.Remove(adUnitIdentifier);
        }
    }

    /// <summary>
    /// Hide banner.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to hide</param>
    /// <returns></returns>
    public static void HideBanner(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "hide banner");

        GameObject stubBanner;
        if (StubBanners.TryGetValue(adUnitIdentifier, out stubBanner))
        {
            stubBanner.SetActive(false);
        }
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

        GameObject stubBanner;
        if (StubBanners.TryGetValue(adUnitIdentifier, out stubBanner))
        {
            stubBanner.GetComponentInChildren<Image>().color = color;
        }
    }

    /// <summary>
    /// Set an extra parameter for the banner ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the banner to set the extra parameter for.</param>
    /// <param name="key">The key for the extra parameter.</param>
    /// <param name="value">The value for the extra parameter.</param>
    public static void SetBannerExtraParameter(string adUnitIdentifier, string key, string value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set banner extra parameter");
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
        return Rect.zero;
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
        RequestAdUnit(adUnitIdentifier);
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
        RequestAdUnit(adUnitIdentifier);
    }

    /// <summary>
    /// Set the MREC placement for an ad unit identifier to tie the future ad events to.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to set the placement for</param>
    /// <param name="placement">Placement to set</param>
    public static void SetMRecPlacement(string adUnitIdentifier, string placement)
    {
        MaxSdkLogger.UserDebug("Setting MREC placement to '" + placement + "' for ad unit id '" + adUnitIdentifier + "'");
    }

    /// <summary>
    /// Updates the position of the MREC to the new position provided.
    /// </summary>
    /// <param name="adUnitIdentifier">The ad unit identifier of the MREC for which to update the position</param>
    /// <param name="mrecPosition">A new position for the MREC</param>
    public static void UpdateMRecPosition(string adUnitIdentifier, AdViewPosition mrecPosition)
    {
        MaxSdkLogger.UserDebug("Updating MREC position to '" + mrecPosition + "' for ad unit id '" + adUnitIdentifier + "'");
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
        MaxSdkLogger.UserDebug("Updating MREC position to '(" + x + "," + y + ")");
    }

    /// <summary>
    /// Show MREC at a position determined by the 'CreateMRec' call.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to show</param>
    public static void ShowMRec(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "show MREC");

        if (!IsAdUnitRequested(adUnitIdentifier))
        {
            MaxSdkLogger.UserWarning("MREC '" + adUnitIdentifier + "' was not created, can not show it");
        }
    }

    /// <summary>
    /// Remove MREC from the ad view and destroy it.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to destroy</param>
    public static void DestroyMRec(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "destroy MREC");
    }

    /// <summary>
    /// Hide MREC.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to hide</param>
    public static void HideMRec(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "hide MREC");
    }

    /// <summary>
    /// Set an extra parameter for the MREC ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC to set the extra parameter for.</param>
    /// <param name="key">The key for the extra parameter.</param>
    /// <param name="value">The value for the extra parameter.</param>
    public static void SetMRecExtraParameter(string adUnitIdentifier, string key, string value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set MREC extra parameter");
    }

    /// <summary>
    /// The MREC position on the screen. When setting the MREC position via <see cref="CreateMRec(string, float, float)"/> or <see cref="UpdateMRecPosition(string, float, float)"/>,
    /// the MREC is placed within the safe area of the screen. This returns the absolute position of the MREC on screen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the MREC for which to get the position on screen.</param>
    /// <returns>A <see cref="Rect"/> representing the banner position on screen.</returns>
    public static Rect GetMRecLayout(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "get MREC layout");
        return Rect.zero;
    }

    #endregion

    #region Cross Promo Ads

    /// <summary>
    /// Create a new cross promo ad with a custom position.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the cross promo ad to create</param>
    /// <param name="x">The X coordinate (horizontal position) of the cross promo ad relative to the top left corner of the screen.</param>
    /// <param name="y">The Y coordinate (vertical position) of the cross promo ad relative to the top left corner of the screen.</param>
    /// <param name="width">The width of the cross promo ad.</param>
    /// <param name="height">The height of the cross promo ad.</param>
    /// <param name="rotation">The rotation of the cross promo ad in degrees.</param>
    /// <seealso cref="GetCrossPromoAdLayout">
    /// The cross promo is placed within the safe area of the screen. You can use this to get the absolute position Rect of the cross promo ad on screen.
    /// </seealso>
    public static void CreateCrossPromoAd(string adUnitIdentifier, float x, float y, float width, float height, float rotation)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "create cross promo ad");
        RequestAdUnit(adUnitIdentifier);
    }

    /// <summary>
    /// Set the cross promo ad placement for an ad unit identifier to tie the future ad events to.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the cross promo ad to set the placement for</param>
    /// <param name="placement">Placement to set</param>
    public static void SetCrossPromoAdPlacement(string adUnitIdentifier, string placement)
    {
        MaxSdkLogger.UserDebug("Setting cross promo ad placement to '" + placement + "' for ad unit id '" + adUnitIdentifier + "'");
    }

    /// <summary>
    /// Updates the position of the cross promo ad to the new coordinates provided.
    /// </summary>
    /// <param name="adUnitIdentifier">The ad unit identifier of the cross promo ad for which to update the position</param>
    /// <param name="x">The X coordinate (horizontal position) of the cross promo ad relative to the top left corner of the screen.</param>
    /// <param name="y">The Y coordinate (vertical position) of the cross promo ad relative to the top left corner of the screen.</param>
    /// <param name="width">The width of the cross promo ad.</param>
    /// <param name="height">The height of the cross promo ad.</param>
    /// <param name="rotation">The rotation of the cross promo ad in degrees.</param>
    /// <seealso cref="GetCrossPromoAdLayout">
    /// The cross promo ad is placed within the safe area of the screen. You can use this to get the absolute position Rect of the cross promo ad on screen.
    /// </seealso>
    public static void UpdateCrossPromoAdPosition(string adUnitIdentifier, float x, float y, float width, float height, float rotation)
    {
        MaxSdkLogger.UserDebug("Updating cross promo ad position to (" + x + "," + y + ") with size " + width + " x " + height + " and rotation of " + rotation + " degrees");
    }

    /// <summary>
    /// Show cross promo ad at a position determined by the 'CreateCrossPromoAd' call.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the cross promo ad to show</param>
    public static void ShowCrossPromoAd(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "show cross promo ad");

        if (!IsAdUnitRequested(adUnitIdentifier))
        {
            MaxSdkLogger.UserWarning("Cross promo ad '" + adUnitIdentifier + "' was not created, can not show it");
        }
    }

    /// <summary>
    /// Remove cross promo ad from the ad view and destroy it.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the cross promo ad to destroy</param>
    public static void DestroyCrossPromoAd(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "destroy cross promo ad");
    }

    /// <summary>
    /// Hide cross promo ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the cross promo ad to hide</param>
    public static void HideCrossPromoAd(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "hide cross promo ad");
    }

    /// <summary>
    /// The cross promo ad position on the screen. When setting the cross promo ad position via <see cref="CreateCrossPromoAd(string, float, float, float, float, float)"/> or <see cref="UpdateCrossPromoAdPosition(string, float, float, float, float, float)"/>,
    /// the cross promo ad is placed within the safe area of the screen. This returns the absolute position of the cross promo ad on screen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the cross promo ad for which to get the position on screen.</param>
    /// <returns>A <see cref="Rect"/> representing the banner position on screen.</returns>
    public static Rect GetCrossPromoAdLayout(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "get cross promo ad layout");
        return Rect.zero;
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
        RequestAdUnit(adUnitIdentifier);

        ExecuteWithDelay(1f, () =>
        {
            AddReadyAdUnit(adUnitIdentifier);
            MaxSdkCallbacks.Instance.ForwardEvent("name=OnInterstitialLoadedEvent\nadUnitId=" + adUnitIdentifier);
        });
    }

    /// <summary>
    /// Check if interstitial ad is loaded and ready to be displayed.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the interstitial to load</param>
    /// <returns>True if the ad is ready to be displayed</returns>
    public static bool IsInterstitialReady(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "check interstitial loaded");

        if (!IsAdUnitRequested(adUnitIdentifier))
        {
            MaxSdkLogger.UserWarning("Interstitial '" + adUnitIdentifier +
                                     "' was not requested, can not check if it is loaded");
            return false;
        }

        return IsAdUnitReady(adUnitIdentifier);
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

        if (!IsAdUnitRequested(adUnitIdentifier))
        {
            MaxSdkLogger.UserWarning(
                "Interstitial '" + adUnitIdentifier + "' was not requested, can not show it");
            return;
        }

        if (!IsInterstitialReady(adUnitIdentifier))
        {
            MaxSdkLogger.UserWarning("Interstitial '" + adUnitIdentifier + "' is not ready, please check IsInterstitialReady() before showing.");
            return;
        }

        RemoveReadyAdUnit(adUnitIdentifier);

        if (_showStubAds)
        {
            ShowStubInterstitial(adUnitIdentifier);
        }
    }

    private static void ShowStubInterstitial(string adUnitIdentifier)
    {
#if UNITY_EDITOR
        var prefabPath = MaxSdkUtils.GetAssetPathForExportPath("MaxSdk/Prefabs/Interstitial.prefab");
        var interstitialPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var stubInterstitial = Object.Instantiate(interstitialPrefab, Vector3.zero, Quaternion.identity);
        var interstitialText = GameObject.Find("MaxInterstitialTitle").GetComponent<Text>();
        var closeButton = GameObject.Find("MaxInterstitialCloseButton").GetComponent<Button>();
        Object.DontDestroyOnLoad(stubInterstitial);

        interstitialText.text += ":\n" + adUnitIdentifier;
        closeButton.onClick.AddListener(() =>
        {
            MaxSdkCallbacks.Instance.ForwardEvent("name=OnInterstitialHiddenEvent\nadUnitId=" + adUnitIdentifier);
            Object.Destroy(stubInterstitial);
        });

        MaxSdkCallbacks.Instance.ForwardEvent("name=OnInterstitialDisplayedEvent\nadUnitId=" + adUnitIdentifier);
#endif
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
        RequestAdUnit(adUnitIdentifier);

        ExecuteWithDelay(1f, () =>
        {
            AddReadyAdUnit(adUnitIdentifier);
            MaxSdkCallbacks.Instance.ForwardEvent("name=OnRewardedAdLoadedEvent\nadUnitId=" + adUnitIdentifier);
        });
    }

    /// <summary>
    /// Check if rewarded ad ad is loaded and ready to be displayed.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded ad to load</param>
    /// <returns>True if the ad is ready to be displayed</returns>
    public static bool IsRewardedAdReady(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "check rewarded ad loaded");

        if (!IsAdUnitRequested(adUnitIdentifier))
        {
            MaxSdkLogger.UserWarning("Rewarded ad '" + adUnitIdentifier +
                                     "' was not requested, can not check if it is loaded");
            return false;
        }

        return IsAdUnitReady(adUnitIdentifier);
    }

    /// <summary>
    /// Present loaded rewarded ad. Note: if the rewarded ad is not ready to be displayed nothing will happen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded ad to show</param>
    public static void ShowRewardedAd(string adUnitIdentifier)
    {
        ShowRewardedAd(adUnitIdentifier, null);
    }

    /// <summary>
    /// Present loaded rewarded ad for a given placement to tie ad events to. Note: if the rewarded ad is not ready to be displayed nothing will happen.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the interstitial to load</param>
    /// <param name="placement">The placement to tie the showing ad's events to</param>
    public static void ShowRewardedAd(string adUnitIdentifier, string placement)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "show rewarded ad");

        if (!IsAdUnitRequested(adUnitIdentifier))
        {
            MaxSdkLogger.UserWarning("Rewarded ad '" + adUnitIdentifier +
                                     "' was not requested, can not show it");
            return;
        }

        if (!IsRewardedAdReady(adUnitIdentifier))
        {
            MaxSdkLogger.UserWarning("Rewarded ad '" + adUnitIdentifier + "' is not ready, please check IsRewardedAdReady() before showing.");
            return;
        }

        RemoveReadyAdUnit(adUnitIdentifier);

        if (_showStubAds)
        {
            ShowStubRewardedAd(adUnitIdentifier);
        }
    }

    private static void ShowStubRewardedAd(string adUnitIdentifier)
    {
#if UNITY_EDITOR
        var prefabPath = MaxSdkUtils.GetAssetPathForExportPath("MaxSdk/Prefabs/Rewarded.prefab");
        var rewardedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var stubRewardedAd = Object.Instantiate(rewardedPrefab, Vector3.zero, Quaternion.identity);
        var grantedReward = false;
        var rewardedTitle = GameObject.Find("MaxRewardTitle").GetComponent<Text>();
        var rewardStatus = GameObject.Find("MaxRewardStatus").GetComponent<Text>();
        var closeButton = GameObject.Find("MaxRewardedCloseButton").GetComponent<Button>();
        var rewardButton = GameObject.Find("MaxRewardButton").GetComponent<Button>();
        Object.DontDestroyOnLoad(stubRewardedAd);

        rewardedTitle.text += ":\n" + adUnitIdentifier;
        closeButton.onClick.AddListener(() =>
        {
            if (grantedReward)
            {
                MaxSdkCallbacks.Instance.ForwardEvent("name=OnRewardedAdReceivedRewardEvent\nadUnitId=" + adUnitIdentifier + "\nrewardLabel=coins\nrewardAmount=5");
            }

            MaxSdkCallbacks.Instance.ForwardEvent("name=OnRewardedAdHiddenEvent\nadUnitId=" + adUnitIdentifier);
            Object.Destroy(stubRewardedAd);
        });
        rewardButton.onClick.AddListener(() =>
        {
            grantedReward = true;
            rewardStatus.text = "Reward granted. Will send reward callback on ad close.";
        });

        MaxSdkCallbacks.Instance.ForwardEvent("name=OnRewardedAdDisplayedEvent\nadUnitId=" + adUnitIdentifier);
#endif
    }

    /// <summary>
    /// Set an extra parameter for the ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded ad to set the extra parameter for.</param>
    /// <param name="key">The key for the extra parameter.</param>
    /// <param name="value">The value for the extra parameter.</param>
    public static void SetRewardedAdExtraParameter(string adUnitIdentifier, string key, string value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set rewarded extra parameter");
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
        RequestAdUnit(adUnitIdentifier);

        ExecuteWithDelay(1f, () =>
        {
            AddReadyAdUnit(adUnitIdentifier);
            MaxSdkCallbacks.Instance.ForwardEvent("name=OnRewardedInterstitialAdLoadedEvent\nadUnitId=" + adUnitIdentifier);
        });
    }

    /// <summary>
    /// Check if rewarded interstitial ad ad is loaded and ready to be displayed.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded ad to load</param>
    /// <returns>True if the ad is ready to be displayed</returns>
    public static bool IsRewardedInterstitialAdReady(string adUnitIdentifier)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "check rewarded interstitial ad loaded");

        if (!IsAdUnitRequested(adUnitIdentifier))
        {
            MaxSdkLogger.UserWarning("Rewarded interstitial ad '" + adUnitIdentifier +
                                     "' was not requested, can not check if it is loaded");
            return false;
        }

        return IsAdUnitReady(adUnitIdentifier);
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

        if (!IsAdUnitRequested(adUnitIdentifier))
        {
            MaxSdkLogger.UserWarning("Rewarded interstitial ad '" + adUnitIdentifier +
                                     "' was not requested, can not show it");
            return;
        }

        if (!IsRewardedInterstitialAdReady(adUnitIdentifier))
        {
            MaxSdkLogger.UserWarning("Rewarded interstitial ad '" + adUnitIdentifier + "' is not ready, please check IsRewardedInterstitialAdReady() before showing.");
            return;
        }

        RemoveReadyAdUnit(adUnitIdentifier);

        if (_showStubAds)
        {
            ShowStubRewardedInterstitialAd(adUnitIdentifier);
        }
    }

    private static void ShowStubRewardedInterstitialAd(string adUnitIdentifier)
    {
#if UNITY_EDITOR
        var prefabPath = MaxSdkUtils.GetAssetPathForExportPath("MaxSdk/Prefabs/Rewarded.prefab");
        var rewardedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var stubRewardedAd = Object.Instantiate(rewardedPrefab, Vector3.zero, Quaternion.identity);
        var grantedReward = false;
        var rewardedTitle = GameObject.Find("MaxRewardTitle").GetComponent<Text>();
        var rewardStatus = GameObject.Find("MaxRewardStatus").GetComponent<Text>();
        var closeButton = GameObject.Find("MaxRewardedCloseButton").GetComponent<Button>();
        var rewardButton = GameObject.Find("MaxRewardButton").GetComponent<Button>();
        Object.DontDestroyOnLoad(stubRewardedAd);

        rewardedTitle.text = "MAX Rewarded Interstitial Ad:\n" + adUnitIdentifier;
        closeButton.onClick.AddListener(() =>
        {
            if (grantedReward)
            {
                MaxSdkCallbacks.Instance.ForwardEvent("name=OnRewardedInterstitialAdReceivedRewardEvent\nadUnitId=" + adUnitIdentifier + "\nrewardLabel=coins\nrewardAmount=5");
            }

            MaxSdkCallbacks.Instance.ForwardEvent("name=OnRewardedInterstitialAdHiddenEvent\nadUnitId=" + adUnitIdentifier);
            Object.Destroy(stubRewardedAd);
        });
        rewardButton.onClick.AddListener(() =>
        {
            grantedReward = true;
            rewardStatus.text = "Reward granted. Will send reward callback on ad close.";
        });

        MaxSdkCallbacks.Instance.ForwardEvent("name=OnRewardedInterstitialAdDisplayedEvent\nadUnitId=" + adUnitIdentifier);
#endif
    }

    /// <summary>
    /// Set an extra parameter for the ad.
    /// </summary>
    /// <param name="adUnitIdentifier">Ad unit identifier of the rewarded interstitial ad to set the extra parameter for.</param>
    /// <param name="key">The key for the extra parameter.</param>
    /// <param name="value">The value for the extra parameter.</param>
    public static void SetRewardedInterstitialAdExtraParameter(string adUnitIdentifier, string key, string value)
    {
        ValidateAdUnitIdentifier(adUnitIdentifier, "set rewarded interstitial extra parameter");
    }

    #endregion

    #region Event Tracking

    /// <summary>
    /// Track an event using AppLovin.
    /// </summary>
    /// <param name="name">An event from the list of pre-defined events may be found in MaxEvents.cs as part of the AppLovin SDK framework.</param>
    /// <param name="parameters">A dictionary containing key-value pairs further describing this event.</param>
    public static void TrackEvent(string name, IDictionary<string, string> parameters = null) { }

    #endregion

    #region Settings

    private static bool _isMuted;

    /// <summary>
    /// Set whether to begin video ads in a muted state or not.
    ///
    /// Please call this method after the SDK has initialized.
    /// </summary>
    /// <param name="muted"><c>true</c> if video ads should being in muted state.</param>
    public static void SetMuted(bool muted)
    {
        _isMuted = muted;
    }

    /// <summary>
    /// Whether video ads begin in a muted state or not. Defaults to <c>false</c>.
    ///
    /// Note: Returns <c>false</c> if the SDK is not initialized yet.
    /// </summary>
    /// <returns><c>true</c> if video ads begin in muted state.</returns>
    public static bool IsMuted()
    {
        return _isMuted;
    }

    /// <summary>
    /// Toggle verbose logging of AppLovin SDK. If enabled AppLovin messages will appear in standard application log. All log messages will have "AppLovinSdk" tag.
    /// </summary>
    /// <param name="enabled"><c>true</c> if verbose logging should be enabled.</param>
    public static void SetVerboseLogging(bool enabled)
    {
#if UNITY_EDITOR
        EditorPrefs.SetBool(MaxSdkLogger.KeyVerboseLoggingEnabled, enabled);
#endif
    }

    /// <summary>
    /// Whether or not verbose logging is enabled.
    /// </summary>
    /// <returns><c>true</c> if verbose logging is enabled.</returns>
    public static bool IsVerboseLoggingEnabled()
    {
#if UNITY_EDITOR
        return EditorPrefs.GetBool(MaxSdkLogger.KeyVerboseLoggingEnabled, false);
#else
        return false;
#endif
    }

    /// <summary>
    /// Whether the creative debugger will be displayed on fullscreen ads after flipping the device screen down twice. Defaults to true.
    /// </summary>
    /// <param name="enabled"><c>true</c> if the creative debugger should be enabled.</param>
    public static void SetCreativeDebuggerEnabled(bool enabled) { }

    /// <summary>
    /// Enable devices to receive test ads, by passing in the advertising identifier (IDFA/GAID) of each test device.
    /// Refer to AppLovin logs for the IDFA/GAID of your current device.
    /// </summary>
    /// <param name="advertisingIdentifiers">String list of advertising identifiers from devices to receive test ads.</param>
    public static void SetTestDeviceAdvertisingIdentifiers(string[] advertisingIdentifiers) { }

    /// <summary>
    /// Whether or not the native AppLovin SDKs listen to exceptions. Defaults to <c>true</c>.
    /// </summary>
    /// <param name="enabled"><c>true</c> if the native AppLovin SDKs should not listen to exceptions.</param>
    public static void SetExceptionHandlerEnabled(bool enabled) { }

    #endregion

    #region Internal

    private static void RequestAdUnit(string adUnitId)
    {
        _ensureInitialized();
        RequestedAdUnits.Add(adUnitId);
    }

    private static bool IsAdUnitRequested(string adUnitId)
    {
        _ensureInitialized();
        return RequestedAdUnits.Contains(adUnitId);
    }

    private static void AddReadyAdUnit(string adUnitId)
    {
        _ensureInitialized();
        ReadyAdUnits.Add(adUnitId);
    }

    private static bool IsAdUnitReady(string adUnitId)
    {
        _ensureInitialized();
        return ReadyAdUnits.Contains(adUnitId);
    }

    private static void RemoveReadyAdUnit(string adUnitId)
    {
        ReadyAdUnits.Remove(adUnitId);
    }

    private static void _ensureHaveSdkKey()
    {
        if (_hasSdkKey) return;
        MaxSdkLogger.UserWarning(
            "MAX Ads SDK did not receive SDK key. Please call Max.SetSdkKey() to assign it");
    }

    private static void _ensureInitialized()
    {
        _ensureHaveSdkKey();

        if (_isInitialized) return;
        MaxSdkLogger.UserWarning(
            "MAX Ads SDK is not initialized by the time ad is requested. Please call Max.InitializeSdk() in your first scene");
    }

    private static void ExecuteWithDelay(float seconds, Action action)
    {
        MaxSdkCallbacks.Instance.StartCoroutine(ExecuteAction(seconds, action));
    }

    private static IEnumerator ExecuteAction(float seconds, Action action)
    {
        yield return new WaitForSeconds(seconds);

        action();
    }

    internal static void SetUserSegmentField(string key, string value) { }

    #endregion

    #region Obsolete

    [Obsolete("This method has been deprecated. Please use `GetSdkConfiguration().ConsentDialogState`")]
    public static ConsentDialogState GetConsentDialogState()
    {
        return ConsentDialogState.Unknown;
    }

    [Obsolete("This method has been deprecated. The AdInfo object is returned with ad callbacks.")]
    public static AdInfo GetAdInfo(string adUnitIdentifier)
    {
        return new AdInfo(new Dictionary<string, string>());
    }

    #endregion
}

#endif
