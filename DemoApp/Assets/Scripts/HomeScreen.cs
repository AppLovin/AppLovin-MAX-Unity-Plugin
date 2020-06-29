using System;
using UnityEngine;
using UnityEngine.UI;

public class HomeScreen : MonoBehaviour
{
    private const string MaxSdkKey = "ENTER_MAX_SDK_KEY_HERE";
    private const string InterstitialAdUnitId = "ENTER_INTERSTITIAL_AD_UNIT_ID_HERE";
    private const string RewardedAdUnitId = "ENTER_REWARD_AD_UNIT_ID_HERE";
    private const string RewardedInterstitialAdUnitId = "ENTER_REWARD_INTER_AD_UNIT_ID_HERE";
    private const string BannerAdUnitId = "ENTER_BANNER_AD_UNIT_ID_HERE";
    private const string MRecAdUnitId = "ENTER_MREC_AD_UNIT_ID_HERE";

    public Button showInterstitialButton;
    public Button showRewardedButton;
    public Button showRewardedInterstitialButton;
    public Button showBannerButton;
    public Button showMRecButton;
    public Button mediationDebuggerButton;
    public Text interstitialStatusText;
    public Text rewardedStatusText;
    public Text rewardedInterstitialStatusText;

    private bool isBannerShowing;
    private bool isMRecShowing;

    private int interstitialRetryAttempt;
    private int rewardedRetryAttempt;
    private int rewardedInterstitialRetryAttempt;

    void Start()
    {
        showInterstitialButton.onClick.AddListener(ShowInterstitial);
        showRewardedButton.onClick.AddListener(ShowRewardedAd);
        showRewardedInterstitialButton.onClick.AddListener(ShowRewardedInterstitialAd);
        showBannerButton.onClick.AddListener(ToggleBannerVisibility);
        showMRecButton.onClick.AddListener(ToggleMRecVisibility);
        mediationDebuggerButton.onClick.AddListener(MaxSdk.ShowMediationDebugger);

        MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
        {
            // AppLovin SDK is initialized, configure and start loading ads.
            Debug.Log("MAX SDK Initialized");

            InitializeInterstitialAds();
            InitializeRewardedAds();
            InitializeRewardedInterstitialAds();
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
        // Interstitial ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
        interstitialRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, interstitialRetryAttempt));
        
        interstitialStatusText.text = "Load failed: " + errorCode + "\nRetrying in " + retryDelay + "s...";
        Debug.Log("Interstitial failed to load with error code: " + errorCode);
        
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
        // Rewarded ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
        rewardedRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, rewardedRetryAttempt));
        
        rewardedStatusText.text = "Load failed: " + errorCode + "\nRetrying in " + retryDelay + "s...";
        Debug.Log("Rewarded ad failed to load with error code: " + errorCode);
        
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
    
    #region Rewarded Interstitial Ad Methods

    private void InitializeRewardedInterstitialAds()
    {
        // Attach callbacks
        MaxSdkCallbacks.OnRewardedInterstitialAdLoadedEvent += OnRewardedInterstitialAdLoadedEvent;
        MaxSdkCallbacks.OnRewardedInterstitialAdLoadFailedEvent += OnRewardedInterstitialAdFailedEvent;
        MaxSdkCallbacks.OnRewardedInterstitialAdFailedToDisplayEvent += OnRewardedInterstitialAdFailedToDisplayEvent;
        MaxSdkCallbacks.OnRewardedInterstitialAdDisplayedEvent += OnRewardedInterstitialAdDisplayedEvent;
        MaxSdkCallbacks.OnRewardedInterstitialAdClickedEvent += OnRewardedInterstitialAdClickedEvent;
        MaxSdkCallbacks.OnRewardedInterstitialAdHiddenEvent += OnRewardedInterstitialAdDismissedEvent;
        MaxSdkCallbacks.OnRewardedInterstitialAdReceivedRewardEvent += OnRewardedInterstitialAdReceivedRewardEvent;

        // Load the first RewardedInterstitialAd
        LoadRewardedInterstitialAd();
    }

    private void LoadRewardedInterstitialAd()
    {
        rewardedInterstitialStatusText.text = "Loading...";
        MaxSdk.LoadRewardedInterstitialAd(RewardedInterstitialAdUnitId);
    }

    private void ShowRewardedInterstitialAd()
    {
        if (MaxSdk.IsRewardedInterstitialAdReady(RewardedInterstitialAdUnitId))
        {
            rewardedInterstitialStatusText.text = "Showing";
            MaxSdk.ShowRewardedInterstitialAd(RewardedInterstitialAdUnitId);
        }
        else
        {
            rewardedInterstitialStatusText.text = "Ad not ready";
        }
    }

    private void OnRewardedInterstitialAdLoadedEvent(string adUnitId)
    {
        // Rewarded interstitial ad is ready to be shown. MaxSdk.IsRewardedInterstitialAdReady(rewardedInterstitialAdUnitId) will now return 'true'
        rewardedInterstitialStatusText.text = "Loaded";
        Debug.Log("Rewarded interstitial ad loaded");
        
        // Reset retry attempt
        rewardedInterstitialRetryAttempt = 0;
    }

    private void OnRewardedInterstitialAdFailedEvent(string adUnitId, int errorCode)
    {
        // Rewarded interstitial ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
        rewardedInterstitialRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, rewardedInterstitialRetryAttempt));
        
        rewardedInterstitialStatusText.text = "Load failed: " + errorCode + "\nRetrying in " + retryDelay + "s...";
        Debug.Log("Rewarded interstitial ad failed to load with error code: " + errorCode);
        
        Invoke("LoadRewardedInterstitialAd", (float) retryDelay);
    }

    private void OnRewardedInterstitialAdFailedToDisplayEvent(string adUnitId, int errorCode)
    {
        // Rewarded interstitial ad failed to display. We recommend loading the next ad
        Debug.Log("Rewarded interstitial ad failed to display with error code: " + errorCode);
        LoadRewardedInterstitialAd();
    }

    private void OnRewardedInterstitialAdDisplayedEvent(string adUnitId)
    {
        Debug.Log("Rewarded interstitial ad displayed");
    }

    private void OnRewardedInterstitialAdClickedEvent(string adUnitId)
    {
        Debug.Log("Rewarded interstitial ad clicked");
    }

    private void OnRewardedInterstitialAdDismissedEvent(string adUnitId)
    {
        // Rewarded interstitial ad is hidden. Pre-load the next ad
        Debug.Log("Rewarded interstitial ad dismissed");
        LoadRewardedInterstitialAd();
    }

    private void OnRewardedInterstitialAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward)
    {
        // Rewarded interstitial ad was displayed and user should receive the reward
        Debug.Log("Rewarded interstitial ad received reward");
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