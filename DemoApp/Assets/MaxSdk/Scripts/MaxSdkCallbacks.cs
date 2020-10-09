/**
 * This is is a global Unity object that is used to forward callbacks from native iOS / Android Max code to the application.
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class MaxSdkCallbacks : MonoBehaviour
{
    public static MaxSdkCallbacks Instance { get; private set; }

    // Fired when the SDK has finished initializing
    public static event Action<MaxSdkBase.SdkConfiguration> OnSdkInitializedEvent;


    // Fire when the MaxVariableService has finished loading the latest set of variables.
    public static event Action OnVariablesUpdatedEvent;


    // Fired when a banner is loaded
    public static event Action<string> OnBannerAdLoadedEvent;

    // Fired when a banner has failed to load
    public static event Action<string, int> OnBannerAdLoadFailedEvent;

    // Fired when a banner ad is clicked
    public static event Action<string> OnBannerAdClickedEvent;

    // Fired when a banner ad expands to encompass a greater portion of the screen
    public static event Action<string> OnBannerAdExpandedEvent;

    // Fired when a banner ad collapses back to its initial size
    public static event Action<string> OnBannerAdCollapsedEvent;


    // Fired when a MREC is loaded
    public static event Action<string> OnMRecAdLoadedEvent;

    // Fired when a MREC has failed to load
    public static event Action<string, int> OnMRecAdLoadFailedEvent;

    // Fired when a MREC ad is clicked
    public static event Action<string> OnMRecAdClickedEvent;

    // Fired when a MREC ad expands to encompass a greater portion of the screen
    public static event Action<string> OnMRecAdExpandedEvent;

    // Fired when a MREC ad collapses back to its initial size
    public static event Action<string> OnMRecAdCollapsedEvent;


    // Fired when an interstitial ad is loaded and ready to be shown
    public static event Action<string> OnInterstitialLoadedEvent;

    // Fired when an interstitial ad fails to load
    public static event Action<string, int> OnInterstitialLoadFailedEvent;

    // Fired when an interstitial ad is dismissed
    public static event Action<string> OnInterstitialHiddenEvent;

    // Fired when an interstitial ad is displayed (may not be received by Unity until the interstitial closes)
    public static event Action<string> OnInterstitialDisplayedEvent;

    // Fired when a interstitial video fails to display
    public static event Action<string, int> OnInterstitialAdFailedToDisplayEvent;

    // Fired when an interstitial ad is clicked (may not be received by Unity until the interstitial closes)
    public static event Action<string> OnInterstitialClickedEvent;


    // Fired when a rewarded ad finishes loading and is ready to be displayed
    public static event Action<string> OnRewardedAdLoadedEvent;

    // Fired when a rewarded ad fails to load. Includes the error message.
    public static event Action<string, int> OnRewardedAdLoadFailedEvent;

    // Fired when an rewarded ad is displayed (may not be received by Unity until the rewarded ad closes)
    public static event Action<string> OnRewardedAdDisplayedEvent;

    // Fired when an rewarded ad is hidden
    public static event Action<string> OnRewardedAdHiddenEvent;

    // Fired when an rewarded video is clicked (may not be received by Unity until the rewarded ad closes)
    public static event Action<string> OnRewardedAdClickedEvent;

    // Fired when a rewarded video fails to play. Includes the error message.
    public static event Action<string, int> OnRewardedAdFailedToDisplayEvent;

    // Fired when a rewarded video completes. Includes information about the reward
    public static event Action<string, MaxSdkBase.Reward> OnRewardedAdReceivedRewardEvent;
    

    // Fired when a rewarded interstitial ad finishes loading and is ready to be displayed
    public static event Action<string> OnRewardedInterstitialAdLoadedEvent;

    // Fired when a rewarded interstitial ad fails to load. Includes the error message.
    public static event Action<string, int> OnRewardedInterstitialAdLoadFailedEvent;

    // Fired when a rewarded interstitial ad is displayed (may not be received by Unity until the rewarded ad closes)
    public static event Action<string> OnRewardedInterstitialAdDisplayedEvent;

    // Fired when a rewarded interstitial ad is hidden
    public static event Action<string> OnRewardedInterstitialAdHiddenEvent;

    // Fired when a rewarded interstitial ad is clicked (may not be received by Unity until the rewarded ad closes)
    public static event Action<string> OnRewardedInterstitialAdClickedEvent;

    // Fired when a rewarded interstitial ad fails to play. Includes the error message.
    public static event Action<string, int> OnRewardedInterstitialAdFailedToDisplayEvent;

    // Fired when a rewarded interstitial ad completes. Includes information about the reward
    public static event Action<string, MaxSdkBase.Reward> OnRewardedInterstitialAdReceivedRewardEvent;
    
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void ForwardEvent(string eventPropsStr)
    {
        var eventProps = MaxSdkUtils.PropsStringToDict(eventPropsStr);

        var eventName = eventProps["name"];
        if (eventName == "OnSdkInitializedEvent")
        {
            var consentDialogStateStr = eventProps["consentDialogState"];
            var sdkConfiguration = new MaxSdkBase.SdkConfiguration();

            if ("1".Equals(consentDialogStateStr))
            {
                sdkConfiguration.ConsentDialogState = MaxSdkBase.ConsentDialogState.Applies;
            }
            else if ("2".Equals(consentDialogStateStr))
            {
                sdkConfiguration.ConsentDialogState = MaxSdkBase.ConsentDialogState.DoesNotApply;
            }
            else
            {
                sdkConfiguration.ConsentDialogState = MaxSdkBase.ConsentDialogState.Unknown;
            }

            InvokeEvent(OnSdkInitializedEvent, sdkConfiguration);
        }
        else if (eventName == "OnVariablesUpdatedEvent")
        {
            InvokeEvent(OnVariablesUpdatedEvent);
        }
        // Ad Events
        else
        {
            var adUnitIdentifier = eventProps["adUnitId"];
            if (eventName == "OnBannerAdLoadedEvent")
            {
                InvokeEvent(OnBannerAdLoadedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnBannerAdLoadFailedEvent")
            {
                var errorCode = 0;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(OnBannerAdLoadFailedEvent, adUnitIdentifier, errorCode);
            }
            else if (eventName == "OnBannerAdClickedEvent")
            {
                InvokeEvent(OnBannerAdClickedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnBannerAdExpandedEvent")
            {
                InvokeEvent(OnBannerAdExpandedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnBannerAdCollapsedEvent")
            {
                InvokeEvent(OnBannerAdCollapsedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnMRecAdLoadedEvent")
            {
                InvokeEvent(OnMRecAdLoadedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnMRecAdLoadFailedEvent")
            {
                var errorCode = 0;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(OnMRecAdLoadFailedEvent, adUnitIdentifier, errorCode);
            }
            else if (eventName == "OnMRecAdClickedEvent")
            {
                InvokeEvent(OnMRecAdClickedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnMRecAdExpandedEvent")
            {
                InvokeEvent(OnMRecAdExpandedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnMRecAdCollapsedEvent")
            {
                InvokeEvent(OnMRecAdCollapsedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnInterstitialLoadedEvent")
            {
                InvokeEvent(OnInterstitialLoadedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnInterstitialLoadFailedEvent")
            {
                var errorCode = 0;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(OnInterstitialLoadFailedEvent, adUnitIdentifier, errorCode);
            }
            else if (eventName == "OnInterstitialHiddenEvent")
            {
                InvokeEvent(OnInterstitialHiddenEvent, adUnitIdentifier);
            }
            else if (eventName == "OnInterstitialDisplayedEvent")
            {
                InvokeEvent(OnInterstitialDisplayedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnInterstitialAdFailedToDisplayEvent")
            {
                var errorCode = 0;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(OnInterstitialAdFailedToDisplayEvent, adUnitIdentifier, errorCode);
            }
            else if (eventName == "OnInterstitialClickedEvent")
            {
                InvokeEvent(OnInterstitialClickedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedAdLoadedEvent")
            {
                InvokeEvent(OnRewardedAdLoadedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedAdLoadFailedEvent")
            {
                var errorCode = 0;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(OnRewardedAdLoadFailedEvent, adUnitIdentifier, errorCode);
            }
            else if (eventName == "OnRewardedAdDisplayedEvent")
            {
                InvokeEvent(OnRewardedAdDisplayedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedAdHiddenEvent")
            {
                InvokeEvent(OnRewardedAdHiddenEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedAdClickedEvent")
            {
                InvokeEvent(OnRewardedAdClickedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedAdFailedToDisplayEvent")
            {
                var errorCode = 0;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(OnRewardedAdFailedToDisplayEvent, adUnitIdentifier, errorCode);
            }
            else if (eventName == "OnRewardedAdReceivedRewardEvent")
            {
                var reward = new MaxSdkBase.Reward {Label = eventProps["rewardLabel"]};

                int.TryParse(eventProps["rewardAmount"], out reward.Amount);

                InvokeEvent(OnRewardedAdReceivedRewardEvent, adUnitIdentifier, reward);
            }
            else if (eventName == "OnRewardedInterstitialAdLoadedEvent")
            {
                InvokeEvent(OnRewardedInterstitialAdLoadedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedInterstitialAdLoadFailedEvent")
            {
                var errorCode = 0;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(OnRewardedInterstitialAdLoadFailedEvent, adUnitIdentifier, errorCode);
            }
            else if (eventName == "OnRewardedInterstitialAdDisplayedEvent")
            {
                InvokeEvent(OnRewardedInterstitialAdDisplayedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedInterstitialAdHiddenEvent")
            {
                InvokeEvent(OnRewardedInterstitialAdHiddenEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedInterstitialAdClickedEvent")
            {
                InvokeEvent(OnRewardedInterstitialAdClickedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedInterstitialAdFailedToDisplayEvent")
            {
                var errorCode = 0;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(OnRewardedInterstitialAdFailedToDisplayEvent, adUnitIdentifier, errorCode);
            }
            else if (eventName == "OnRewardedInterstitialAdReceivedRewardEvent")
            {
                var reward = new MaxSdkBase.Reward {Label = eventProps["rewardLabel"]};

                int.TryParse(eventProps["rewardAmount"], out reward.Amount);

                InvokeEvent(OnRewardedInterstitialAdReceivedRewardEvent, adUnitIdentifier, reward);
            }
            else
            {
                Debug.LogWarning("[AppLovin MAX] Unknown MAX Ads event fired: " + eventName);
            }
        }
    }

#if UNITY_EDITOR
    public static void EmitSdkInitializedEvent()
    {
        var sdkConfiguration = new MaxSdkBase.SdkConfiguration();
        sdkConfiguration.ConsentDialogState = MaxSdkBase.ConsentDialogState.Unknown;
        
        OnSdkInitializedEvent(sdkConfiguration);
    }
#endif

    private static void InvokeEvent(Action evt)
    {
        if (!CanInvokeEvent(evt)) return;

        Debug.Log("[AppLovin MAX] Invoking event: " + evt);
        evt();
    }

    private static void InvokeEvent<T>(Action<T> evt, T param)
    {
        if (!CanInvokeEvent(evt)) return;

        Debug.Log("[AppLovin MAX] Invoking event: " + evt + ". Param: " + param);
        evt(param);
    }

    private static void InvokeEvent<T1, T2>(Action<T1, T2> evt, T1 param1, T2 param2)
    {
        if (!CanInvokeEvent(evt)) return;

        Debug.Log("[AppLovin MAX] Invoking event: " + evt + ". Params: " + param1 + ", " + param2);
        evt(param1, param2);
    }

    private static bool CanInvokeEvent(Delegate evt)
    {
        if (evt == null) return false;

        // Check that publisher is not over-subscribing
        if (evt.GetInvocationList().Length > 5)
        {
            Debug.LogWarning("[AppLovin MAX] Ads Event (" + evt + ") has over 5 subscribers. Please make sure you are properly un-subscribing to actions!!!");
        }

        return true;
    }
}