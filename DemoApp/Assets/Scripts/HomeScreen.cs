using UnityEngine;
using UnityEngine.UI;

public class HomeScreen : MonoBehaviour
{
    private const string MaxSdkKey = "ENTER_MAX_SDK_KEY_HERE";
    private const string InterstitialAdUnitId = "ENTER_INTERSTITIAL_AD_UNIT_ID_HERE";
    private const string RewardedAdUnitId = "ENTER_REWARD_AD_UNIT_ID_HERE";
    private const string BannerAdUnitId = "ENTER_BANNER_AD_UNIT_ID_HERE";

    public Button showInterstitialButton;
    public Button showRewardedButton;
    public Button showBannerButton;
    public Button mediationDebuggerButton;
    public Text interstitialStatusText;
    public Text rewardedStatusText;

    private bool isBannerShowing = false;

    void Start()
    {
        showInterstitialButton.onClick.AddListener(ShowInterstitial);
        showRewardedButton.onClick.AddListener(ShowRewardedAd);
        showBannerButton.onClick.AddListener(ToggleBannerVisibility);
        mediationDebuggerButton.onClick.AddListener(MaxSdk.ShowMediationDebugger);

        MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration sdkConfiguration) =>
        {
            // AppLovin SDK is initialized, configure and start loading ads
            Debug.Log("MAX SDK Initialized");

            if (sdkConfiguration.ConsentDialogState == MaxSdkBase.ConsentDialogState.Applies)
            {
                // Show user consent dialog
            }
            else if (sdkConfiguration.ConsentDialogState == MaxSdkBase.ConsentDialogState.DoesNotApply)
            {
                // No need to show consent dialog, proceed with initialization
            }
            else
            {
                // Consent dialog state is unknown. Proceed with initialization, but check if the consent
                // dialog should be shown on the next application initialization
            }

            MaxSdk.SetHasUserConsent(true);
            MaxSdk.SetIsAgeRestrictedUser(false);

            InitializeInterstitialAds();
            InitializeRewardedAds();
            InitializeBannerAds();
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
        MaxSdk.LoadInterstitial(InterstitialAdUnitId);
        interstitialStatusText.text = "Loading...";
    }

    void ShowInterstitial()
    {
        if (MaxSdk.IsInterstitialReady(InterstitialAdUnitId))
        {
            MaxSdk.ShowInterstitial(InterstitialAdUnitId);
            interstitialStatusText.text = "Showing";
        }
    }

    private void OnInterstitialLoadedEvent(string adUnitId)
    {
        // Interstitial ad is ready to be shown. MaxSdk.IsInterstitialReady(interstitialAdUnitId) will now return 'true'
        Debug.Log("Interstitial loaded");
        interstitialStatusText.text = "Loaded";
    }

    private void OnInterstitialFailedEvent(string adUnitId, int errorCode)
    {
        // Interstitial ad failed to load. We recommend re-trying in 3 seconds.
        Invoke("LoadInterstitial", 3);
        Debug.Log("Interstitial failed to load with error code: " + errorCode);
        interstitialStatusText.text = "Failed load: " + errorCode + "\nRetrying in 3s...";
    }

    private void InterstitialFailedToDisplayEvent(string adUnitId, int errorCode)
    {
        Debug.Log("Interstitial failed to display with error code: " + errorCode);
        // Interstitial ad failed to display. We recommend loading the next ad
        LoadInterstitial();
    }

    private void OnInterstitialDismissedEvent(string adUnitId)
    {
        Debug.Log("Interstitial dismissed");
        // Interstitial ad is hidden. Pre-load the next ad
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
        MaxSdk.LoadRewardedAd(RewardedAdUnitId);
        rewardedStatusText.text = "Loading...";
    }

    private void ShowRewardedAd()
    {
        if (MaxSdk.IsRewardedAdReady(RewardedAdUnitId))
        {
            MaxSdk.ShowRewardedAd(RewardedAdUnitId);
            rewardedStatusText.text = "Showing";
        }
    }

    private void OnRewardedAdLoadedEvent(string adUnitId)
    {
        // Rewarded ad is ready to be shown. MaxSdk.IsRewardedAdReady(rewardedAdUnitId) will now return 'true'
        Debug.Log("Rewarded ad loaded");
        rewardedStatusText.text = "Loaded";
    }

    private void OnRewardedAdFailedEvent(string adUnitId, int errorCode)
    {
        // Rewarded ad failed to load. We recommend re-trying in 3 seconds.
        Invoke("LoadRewardedAd", 3);
        Debug.Log("Rewarded ad failed to load with error code: " + errorCode);
        rewardedStatusText.text = "Failed load: " + errorCode + "\nRetrying in 3s...";
    }

    private void OnRewardedAdFailedToDisplayEvent(string adUnitId, int errorCode)
    {
        Debug.Log("Rewarded ad failed to display with error code: " + errorCode);
        // Rewarded ad failed to display. We recommend loading the next ad
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
        Debug.Log("Rewarded ad dismissed");
        // Rewarded ad is hidden. Pre-load the next ad
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
        // Banners are automatically sized to 320x50 on phones and 728x90 on tablets
        // You may use the utility method `MaxSdkUtils.isTablet()` to help with view sizing adjustments
        MaxSdk.CreateBanner(BannerAdUnitId, MaxSdkBase.BannerPosition.BottomCenter);

        // Set background or background color for banners to be fully functional
        MaxSdk.SetBannerBackgroundColor(BannerAdUnitId, Color.black);
    }

    private void ToggleBannerVisibility()
    {
        if (!isBannerShowing)
        {
            MaxSdk.ShowBanner(BannerAdUnitId);
            isBannerShowing = true;
            showBannerButton.GetComponentInChildren<Text>().text = "Hide Banner";
        }
        else
        {
            MaxSdk.HideBanner(BannerAdUnitId);
            isBannerShowing = false;
            showBannerButton.GetComponentInChildren<Text>().text = "Show Banner";
        }
    }

    #endregion
}