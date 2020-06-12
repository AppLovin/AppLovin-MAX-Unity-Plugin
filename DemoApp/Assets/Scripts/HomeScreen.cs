using System;
using UnityEngine;
using UnityEngine.UI;

public class HomeScreen : MonoBehaviour
{
    private const string MaxSdkKey = "05TMDQ5tZabpXQ45_UTbmEGNUtVAzSTzT6KmWQc5_CuWdzccS4DCITZoL3yIWUG3bbq60QC_d4WF28tUC4gVTF";
    private const string InterstitialAdUnitId = "669b9232b93315b8";
    private const string RewardedAdUnitId = "72948f99c2065dfa";
    private const string BannerAdUnitId = "ee3f36bb0f16e195";
    private const string MRecAdUnitId = "7d4836a418a8e27d";

    public Button showInterstitialButton;
    public Button showRewardedButton;
    public Button showBannerButton;
    public Button showMRecButton;
    public Button mediationDebuggerButton;
    public Text interstitialStatusText;
    public Text rewardedStatusText;

    private bool isBannerShowing;
    private bool isMRecShowing;

    private int interstitialRetryAttempt;
    private int rewardedRetryAttempt;

    void Start()
    {
        showInterstitialButton.onClick.AddListener(ShowInterstitial);
        showRewardedButton.onClick.AddListener(ShowRewardedAd);
        showBannerButton.onClick.AddListener(ToggleBannerVisibility);
        showMRecButton.onClick.AddListener(ToggleMRecVisibility);
        mediationDebuggerButton.onClick.AddListener(MaxSdk.ShowMediationDebugger);

        MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
        {
            // AppLovin SDK is initialized, configure and start loading ads.
            Debug.Log("MAX SDK Initialized");

            InitializeInterstitialAds();
            InitializeRewardedAds();
            InitializeBannerAds();
            InitializeMRecAds();
        };

        MaxSdk.SetSdkKey(MaxSdkKey);
        MaxSdk.InitializeSdk();
    }

    #region Interstitial Ad Methods

    private void InitializeInterstitialAds()
    {
        // Attach callbacks
        MaxSdkCallbacks.OnInterstitialLoadedEvent += OnInterstitialLoadedEvent;
        MaxSdkCallbacks.OnInterstitialLoadFailedEvent += OnInterstitialFailedEvent;
        MaxSdkCallbacks.OnInterstitialAdFailedToDisplayEvent += InterstitialFailedToDisplayEvent;
        MaxSdkCallbacks.OnInterstitialHiddenEvent += OnInterstitialDismissedEvent;

        // Load the first interstitial
        LoadInterstitial();
    }

    void LoadInterstitial()
    {
        interstitialStatusText.text = "Loading...";
        MaxSdk.LoadInterstitial(InterstitialAdUnitId);
    }

    void ShowInterstitial()
    {
        if (MaxSdk.IsInterstitialReady(InterstitialAdUnitId))
        {
            interstitialStatusText.text = "Showing";
            MaxSdk.ShowInterstitial(InterstitialAdUnitId);
        }
        else
        {
            interstitialStatusText.text = "Ad not ready";
        }
    }

    private void OnInterstitialLoadedEvent(string adUnitId)
    {
        // Interstitial ad is ready to be shown. MaxSdk.IsInterstitialReady(interstitialAdUnitId) will now return 'true'
        interstitialStatusText.text = "Loaded";
        Debug.Log("Interstitial loaded");
        
        // Reset retry attempt
        interstitialRetryAttempt = 0;
    }

    private void OnInterstitialFailedEvent(string adUnitId, int errorCode)
    {
        interstitialStatusText.text = "Failed load: " + errorCode + "\nRetrying in 3s...";
        Debug.Log("Interstitial failed to load with error code: " + errorCode);
        
        // Interstitial ad failed to load. We recommend retrying with exponentially higher delays.

        interstitialRetryAttempt++;
        double retryDelay = Math.Pow(2, interstitialRetryAttempt);
        
        Invoke("LoadInterstitial", (float) retryDelay);
    }

    private void InterstitialFailedToDisplayEvent(string adUnitId, int errorCode)
    {
        // Interstitial ad failed to display. We recommend loading the next ad
        Debug.Log("Interstitial failed to display with error code: " + errorCode);
        LoadInterstitial();
    }

    private void OnInterstitialDismissedEvent(string adUnitId)
    {
        // Interstitial ad is hidden. Pre-load the next ad
        Debug.Log("Interstitial dismissed");
        LoadInterstitial();
    }

    #endregion

    #region Rewarded Ad Methods

    private void InitializeRewardedAds()
    {
        // Attach callbacks
        MaxSdkCallbacks.OnRewardedAdLoadedEvent += OnRewardedAdLoadedEvent;
        MaxSdkCallbacks.OnRewardedAdLoadFailedEvent += OnRewardedAdFailedEvent;
        MaxSdkCallbacks.OnRewardedAdFailedToDisplayEvent += OnRewardedAdFailedToDisplayEvent;
        MaxSdkCallbacks.OnRewardedAdDisplayedEvent += OnRewardedAdDisplayedEvent;
        MaxSdkCallbacks.OnRewardedAdClickedEvent += OnRewardedAdClickedEvent;
        MaxSdkCallbacks.OnRewardedAdHiddenEvent += OnRewardedAdDismissedEvent;
        MaxSdkCallbacks.OnRewardedAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;

        // Load the first RewardedAd
        LoadRewardedAd();
    }

    private void LoadRewardedAd()
    {
        rewardedStatusText.text = "Loading...";
        MaxSdk.LoadRewardedAd(RewardedAdUnitId);
    }

    private void ShowRewardedAd()
    {
        if (MaxSdk.IsRewardedAdReady(RewardedAdUnitId))
        {
            rewardedStatusText.text = "Showing";
            MaxSdk.ShowRewardedAd(RewardedAdUnitId);
        }
        else
        {
            rewardedStatusText.text = "Ad not ready";
        }
    }

    private void OnRewardedAdLoadedEvent(string adUnitId)
    {
        // Rewarded ad is ready to be shown. MaxSdk.IsRewardedAdReady(rewardedAdUnitId) will now return 'true'
        rewardedStatusText.text = "Loaded";
        Debug.Log("Rewarded ad loaded");
        
        // Reset retry attempt
        rewardedRetryAttempt = 0;
    }

    private void OnRewardedAdFailedEvent(string adUnitId, int errorCode)
    {
        rewardedStatusText.text = "Failed load: " + errorCode + "\nRetrying in 3s...";
        Debug.Log("Rewarded ad failed to load with error code: " + errorCode);
        
        // Rewarded ad failed to load. We recommend retrying with exponentially higher delays.

        rewardedRetryAttempt++;
        double retryDelay = Math.Pow(2, rewardedRetryAttempt);
        
        Invoke("LoadRewardedAd", (float) retryDelay);
    }

    private void OnRewardedAdFailedToDisplayEvent(string adUnitId, int errorCode)
    {
        // Rewarded ad failed to display. We recommend loading the next ad
        Debug.Log("Rewarded ad failed to display with error code: " + errorCode);
        LoadRewardedAd();
    }

    private void OnRewardedAdDisplayedEvent(string adUnitId)
    {
        Debug.Log("Rewarded ad displayed");
    }

    private void OnRewardedAdClickedEvent(string adUnitId)
    {
        Debug.Log("Rewarded ad clicked");
    }

    private void OnRewardedAdDismissedEvent(string adUnitId)
    {
        // Rewarded ad is hidden. Pre-load the next ad
        Debug.Log("Rewarded ad dismissed");
        LoadRewardedAd();
    }

    private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward)
    {
        // Rewarded ad was displayed and user should receive the reward
        Debug.Log("Rewarded ad received reward");
    }

    #endregion

    #region Banner Ad Methods

    private void InitializeBannerAds()
    {
        // Banners are automatically sized to 320x50 on phones and 728x90 on tablets.
        // You may use the utility method `MaxSdkUtils.isTablet()` to help with view sizing adjustments.
        MaxSdk.CreateBanner(BannerAdUnitId, MaxSdkBase.BannerPosition.TopCenter);

        // Set background or background color for banners to be fully functional.
        MaxSdk.SetBannerBackgroundColor(BannerAdUnitId, Color.black);
    }

    private void ToggleBannerVisibility()
    {
        if (!isBannerShowing)
        {
            MaxSdk.ShowBanner(BannerAdUnitId);
            showBannerButton.GetComponentInChildren<Text>().text = "Hide Banner";
        }
        else
        {
            MaxSdk.HideBanner(BannerAdUnitId);
            showBannerButton.GetComponentInChildren<Text>().text = "Show Banner";
        }

        isBannerShowing = !isBannerShowing;
    }

    #endregion

    #region MREC Ad Methods

    private void InitializeMRecAds()
    {
        // MRECs are automatically sized to 300x250.
        MaxSdk.CreateMRec(MRecAdUnitId, MaxSdkBase.AdViewPosition.BottomCenter);
    }

    private void ToggleMRecVisibility()
    {
        if (!isMRecShowing)
        {
            MaxSdk.ShowMRec(MRecAdUnitId);
            showMRecButton.GetComponentInChildren<Text>().text = "Hide MREC";
        }
        else
        {
            MaxSdk.HideMRec(MRecAdUnitId);
            showMRecButton.GetComponentInChildren<Text>().text = "Show MREC";
        }

        isMRecShowing = !isMRecShowing;
    }

    #endregion
}