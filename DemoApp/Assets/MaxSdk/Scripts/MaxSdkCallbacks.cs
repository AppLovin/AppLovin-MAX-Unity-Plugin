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
    private static Action<MaxSdkBase.SdkConfiguration> _onSdkInitializedEvent;
    public static event Action<MaxSdkBase.SdkConfiguration> OnSdkInitializedEvent
    {
        add
        {
            LogSubscribedToEvent("OnSdkInitializedEvent");
            _onSdkInitializedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnSdkInitializedEvent");
            _onSdkInitializedEvent -= value;
        }
    }


    // Fire when the MaxVariableService has finished loading the latest set of variables.
    private static Action _onVariablesUpdatedEvent;
    public static event Action OnVariablesUpdatedEvent
    {
        add
        {
            LogSubscribedToEvent("OnVariablesUpdatedEvent");
            _onVariablesUpdatedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnVariablesUpdatedEvent");
            _onVariablesUpdatedEvent -= value;
        }
    }

    // Fire when the Consent Dialog has been dismissed.
    private static Action _onSdkConsentDialogDismissedEvent;
    public static event Action OnSdkConsentDialogDismissedEvent
    {
        add
        {
            LogSubscribedToEvent("OnSdkConsentDialogDismissedEvent");
            _onSdkConsentDialogDismissedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnSdkConsentDialogDismissedEvent");
            _onSdkConsentDialogDismissedEvent -= value;
        }
    }


    // Fired when a banner is loaded
    private static Action<string> _onBannerAdLoadedEvent;
    public static event Action<string> OnBannerAdLoadedEvent
    {
        add
        {
            LogSubscribedToEvent("OnBannerAdLoadedEvent");
            _onBannerAdLoadedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnBannerAdLoadedEvent");
            _onBannerAdLoadedEvent -= value;
        }
    }

    // Fired when a banner has failed to load
    private static Action<string, int> _onBannerAdLoadFailedEvent;
    public static event Action<string, int> OnBannerAdLoadFailedEvent
    {
        add
        {
            LogSubscribedToEvent("OnBannerAdLoadFailedEvent");
            _onBannerAdLoadFailedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnBannerAdLoadFailedEvent");
            _onBannerAdLoadFailedEvent -= value;
        }
    }

    // Fired when a banner ad is clicked
    private static Action<string> _onBannerAdClickedEvent;
    public static event Action<string> OnBannerAdClickedEvent
    {
        add
        {
            LogSubscribedToEvent("OnBannerAdClickedEvent");
            _onBannerAdClickedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnBannerAdClickedEvent");
            _onBannerAdClickedEvent -= value;
        }
    }

    // Fired when a banner ad expands to encompass a greater portion of the screen
    private static Action<string> _onBannerAdExpandedEvent;
    public static event Action<string> OnBannerAdExpandedEvent
    {
        add
        {
            LogSubscribedToEvent("OnBannerAdExpandedEvent");
            _onBannerAdExpandedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnBannerAdExpandedEvent");
            _onBannerAdExpandedEvent -= value;
        }
    }

    // Fired when a banner ad collapses back to its initial size
    private static Action<string> _onBannerAdCollapsedEvent;
    public static event Action<string> OnBannerAdCollapsedEvent
    {
        add
        {
            LogSubscribedToEvent("OnBannerAdCollapsedEvent");
            _onBannerAdCollapsedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnBannerAdCollapsedEvent");
            _onBannerAdCollapsedEvent -= value;
        }
    }


    // Fired when a MREC is loaded
    private static Action<string> _onMRecAdLoadedEvent;
    public static event Action<string> OnMRecAdLoadedEvent
    {
        add
        {
            LogSubscribedToEvent("OnMRecAdLoadedEvent");
            _onMRecAdLoadedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnMRecAdLoadedEvent");
            _onMRecAdLoadedEvent -= value;
        }
    }

    // Fired when a MREC has failed to load
    private static Action<string, int> _onMRecAdLoadFailedEvent;
    public static event Action<string, int> OnMRecAdLoadFailedEvent
    {
        add
        {
            LogSubscribedToEvent("OnMRecAdLoadFailedEvent");
            _onMRecAdLoadFailedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnMRecAdLoadFailedEvent");
            _onMRecAdLoadFailedEvent -= value;
        }
    }

    // Fired when a MREC ad is clicked
    private static Action<string> _onMRecAdClickedEvent;
    public static event Action<string> OnMRecAdClickedEvent
    {
        add
        {
            LogSubscribedToEvent("OnMRecAdClickedEvent");
            _onMRecAdClickedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnMRecAdClickedEvent");
            _onMRecAdClickedEvent -= value;
        }
    }

    // Fired when a MREC ad expands to encompass a greater portion of the screen
    private static Action<string> _onMRecAdExpandedEvent;
    public static event Action<string> OnMRecAdExpandedEvent
    {
        add
        {
            LogSubscribedToEvent("OnMRecAdExpandedEvent");
            _onMRecAdExpandedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnMRecAdExpandedEvent");
            _onMRecAdExpandedEvent -= value;
        }
    }

    // Fired when a MREC ad collapses back to its initial size
    private static Action<string> _onMRecAdCollapsedEvent;
    public static event Action<string> OnMRecAdCollapsedEvent
    {
        add
        {
            LogSubscribedToEvent("OnMRecAdCollapsedEvent");
            _onMRecAdCollapsedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnMRecAdCollapsedEvent");
            _onMRecAdCollapsedEvent -= value;
        }
    }


    // Fired when an interstitial ad is loaded and ready to be shown
    private static Action<string> _onInterstitialLoadedEvent;
    public static event Action<string> OnInterstitialLoadedEvent
    {
        add
        {
            LogSubscribedToEvent("OnInterstitialLoadedEvent");
            _onInterstitialLoadedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnInterstitialLoadedEvent");
            _onInterstitialLoadedEvent -= value;
        }
    }

    // Fired when an interstitial ad fails to load
    private static Action<string, int> _onInterstitialLoadFailedEvent;
    public static event Action<string, int> OnInterstitialLoadFailedEvent
    {
        add
        {
            LogSubscribedToEvent("OnInterstitialLoadFailedEvent");
            _onInterstitialLoadFailedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnInterstitialLoadFailedEvent");
            _onInterstitialLoadFailedEvent -= value;
        }
    }

    // Fired when an interstitial ad is dismissed
    private static Action<string> _onInterstitialHiddenEvent;
    public static event Action<string> OnInterstitialHiddenEvent
    {
        add
        {
            LogSubscribedToEvent("OnInterstitialHiddenEvent");
            _onInterstitialHiddenEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnInterstitialHiddenEvent");
            _onInterstitialHiddenEvent -= value;
        }
    }

    // Fired when an interstitial ad is displayed (may not be received by Unity until the interstitial closes)
    private static Action<string> _onInterstitialDisplayedEvent;
    public static event Action<string> OnInterstitialDisplayedEvent
    {
        add
        {
            LogSubscribedToEvent("OnInterstitialDisplayedEvent");
            _onInterstitialDisplayedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnInterstitialDisplayedEvent");
            _onInterstitialDisplayedEvent -= value;
        }
    }

    // Fired when a interstitial video fails to display
    private static Action<string, int> _onInterstitialAdFailedToDisplayEvent;
    public static event Action<string, int> OnInterstitialAdFailedToDisplayEvent
    {
        add
        {
            LogSubscribedToEvent("OnInterstitialAdFailedToDisplayEvent");
            _onInterstitialAdFailedToDisplayEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnInterstitialAdFailedToDisplayEvent");
            _onInterstitialAdFailedToDisplayEvent -= value;
        }
    }

    // Fired when an interstitial ad is clicked (may not be received by Unity until the interstitial closes)
    private static Action<string> _onInterstitialClickedEvent;
    public static event Action<string> OnInterstitialClickedEvent
    {
        add
        {
            LogSubscribedToEvent("OnInterstitialClickedEvent");
            _onInterstitialClickedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnInterstitialClickedEvent");
            _onInterstitialClickedEvent -= value;
        }
    }


    // Fired when a rewarded ad finishes loading and is ready to be displayed
    private static Action<string> _onRewardedAdLoadedEvent;
    public static event Action<string> OnRewardedAdLoadedEvent
    {
        add
        {
            LogSubscribedToEvent("OnRewardedAdLoadedEvent");
            _onRewardedAdLoadedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnRewardedAdLoadedEvent");
            _onRewardedAdLoadedEvent -= value;
        }
    }

    // Fired when a rewarded ad fails to load. Includes the error message.
    private static Action<string, int> _onRewardedAdLoadFailedEvent;
    public static event Action<string, int> OnRewardedAdLoadFailedEvent
    {
        add
        {
            LogSubscribedToEvent("OnRewardedAdLoadFailedEvent");
            _onRewardedAdLoadFailedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnRewardedAdLoadFailedEvent");
            _onRewardedAdLoadFailedEvent -= value;
        }
    }

    // Fired when an rewarded ad is displayed (may not be received by Unity until the rewarded ad closes)
    private static Action<string> _onRewardedAdDisplayedEvent;
    public static event Action<string> OnRewardedAdDisplayedEvent
    {
        add
        {
            LogSubscribedToEvent("OnRewardedAdDisplayedEvent");
            _onRewardedAdDisplayedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnRewardedAdDisplayedEvent");
            _onRewardedAdDisplayedEvent -= value;
        }
    }

    // Fired when an rewarded ad is hidden
    private static Action<string> _onRewardedAdHiddenEvent;
    public static event Action<string> OnRewardedAdHiddenEvent
    {
        add
        {
            LogSubscribedToEvent("OnRewardedAdHiddenEvent");
            _onRewardedAdHiddenEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnRewardedAdHiddenEvent");
            _onRewardedAdHiddenEvent -= value;
        }
    }

    // Fired when an rewarded video is clicked (may not be received by Unity until the rewarded ad closes)
    private static Action<string> _onRewardedAdClickedEvent;
    public static event Action<string> OnRewardedAdClickedEvent
    {
        add
        {
            LogSubscribedToEvent("OnRewardedAdClickedEvent");
            _onRewardedAdClickedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnRewardedAdClickedEvent");
            _onRewardedAdClickedEvent -= value;
        }
    }

    // Fired when a rewarded video fails to play. Includes the error message.
    private static Action<string, int> _onRewardedAdFailedToDisplayEvent;
    public static event Action<string, int> OnRewardedAdFailedToDisplayEvent
    {
        add
        {
            LogSubscribedToEvent("OnRewardedAdFailedToDisplayEvent");
            _onRewardedAdFailedToDisplayEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnRewardedAdFailedToDisplayEvent");
            _onRewardedAdFailedToDisplayEvent -= value;
        }
    }

    // Fired when a rewarded video completes. Includes information about the reward
    private static Action<string, MaxSdkBase.Reward> _onRewardedAdReceivedRewardEvent;
    public static event Action<string, MaxSdkBase.Reward> OnRewardedAdReceivedRewardEvent
    {
        add
        {
            LogSubscribedToEvent("OnRewardedAdReceivedRewardEvent");
            _onRewardedAdReceivedRewardEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnRewardedAdReceivedRewardEvent");
            _onRewardedAdReceivedRewardEvent -= value;
        }
    }
    

    // Fired when a rewarded interstitial ad finishes loading and is ready to be displayed
    private static Action<string> _onRewardedInterstitialAdLoadedEvent;
    public static event Action<string> OnRewardedInterstitialAdLoadedEvent
    {
        add
        {
            LogSubscribedToEvent("OnRewardedInterstitialAdLoadedEvent");
            _onRewardedInterstitialAdLoadedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnRewardedInterstitialAdLoadedEvent");
            _onRewardedInterstitialAdLoadedEvent -= value;
        }
    }

    // Fired when a rewarded interstitial ad fails to load. Includes the error message.
    private static Action<string, int> _onRewardedInterstitialAdLoadFailedEvent;
    public static event Action<string, int> OnRewardedInterstitialAdLoadFailedEvent
    {
        add
        {
            LogSubscribedToEvent("OnRewardedInterstitialAdLoadFailedEvent");
            _onRewardedInterstitialAdLoadFailedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnRewardedInterstitialAdLoadFailedEvent");
            _onRewardedInterstitialAdLoadFailedEvent -= value;
        }
    }

    // Fired when a rewarded interstitial ad is displayed (may not be received by Unity until the rewarded ad closes)
    private static Action<string> _onRewardedInterstitialAdDisplayedEvent;
    public static event Action<string> OnRewardedInterstitialAdDisplayedEvent
    {
        add
        {
            LogSubscribedToEvent("OnRewardedInterstitialAdDisplayedEvent");
            _onRewardedInterstitialAdDisplayedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnRewardedInterstitialAdDisplayedEvent");
            _onRewardedInterstitialAdDisplayedEvent -= value;
        }
    }

    // Fired when a rewarded interstitial ad is hidden
    private static Action<string> _onRewardedInterstitialAdHiddenEvent;
    public static event Action<string> OnRewardedInterstitialAdHiddenEvent
    {
        add
        {
            LogSubscribedToEvent("OnRewardedInterstitialAdHiddenEvent");
            _onRewardedInterstitialAdHiddenEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnRewardedInterstitialAdHiddenEvent");
            _onRewardedInterstitialAdHiddenEvent -= value;
        }
    }

    // Fired when a rewarded interstitial ad is clicked (may not be received by Unity until the rewarded ad closes)
    private static Action<string> _onRewardedInterstitialAdClickedEvent;
    public static event Action<string> OnRewardedInterstitialAdClickedEvent
    {
        add
        {
            LogSubscribedToEvent("OnRewardedInterstitialAdClickedEvent");
            _onRewardedInterstitialAdClickedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnRewardedInterstitialAdClickedEvent");
            _onRewardedInterstitialAdClickedEvent -= value;
        }
    }

    // Fired when a rewarded interstitial ad fails to play. Includes the error message.
    private static Action<string, int> _onRewardedInterstitialAdFailedToDisplayEvent;
    public static event Action<string, int> OnRewardedInterstitialAdFailedToDisplayEvent
    {
        add
        {
            LogSubscribedToEvent("OnRewardedInterstitialAdFailedToDisplayEvent");
            _onRewardedInterstitialAdFailedToDisplayEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnRewardedInterstitialAdFailedToDisplayEvent");
            _onRewardedInterstitialAdFailedToDisplayEvent -= value;
        }
    }

    // Fired when a rewarded interstitial ad completes. Includes information about the reward
    private static Action<string, MaxSdkBase.Reward> _onRewardedInterstitialAdReceivedRewardEvent;
    public static event Action<string, MaxSdkBase.Reward> OnRewardedInterstitialAdReceivedRewardEvent
    {
        add
        {
            LogSubscribedToEvent("OnRewardedInterstitialAdReceivedRewardEvent");
            _onRewardedInterstitialAdReceivedRewardEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnRewardedInterstitialAdReceivedRewardEvent");
            _onRewardedInterstitialAdReceivedRewardEvent -= value;
        }
    }

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
            InvokeEvent(_onSdkInitializedEvent, sdkConfiguration);
        }
        else if (eventName == "OnVariablesUpdatedEvent")
        {
            InvokeEvent(_onVariablesUpdatedEvent);
        }
        else if ( eventName == "OnSdkConsentDialogDismissedEvent" )
        {
            InvokeEvent(_onSdkConsentDialogDismissedEvent);
        }
        // Ad Events
        else
        {
            var adUnitIdentifier = eventProps["adUnitId"];
            if (eventName == "OnBannerAdLoadedEvent")
            {
                InvokeEvent(_onBannerAdLoadedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnBannerAdLoadFailedEvent")
            {
                var errorCode = 0;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(_onBannerAdLoadFailedEvent, adUnitIdentifier, errorCode);
            }
            else if (eventName == "OnBannerAdClickedEvent")
            {
                InvokeEvent(_onBannerAdClickedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnBannerAdExpandedEvent")
            {
                InvokeEvent(_onBannerAdExpandedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnBannerAdCollapsedEvent")
            {
                InvokeEvent(_onBannerAdCollapsedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnMRecAdLoadedEvent")
            {
                InvokeEvent(_onMRecAdLoadedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnMRecAdLoadFailedEvent")
            {
                var errorCode = 0;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(_onMRecAdLoadFailedEvent, adUnitIdentifier, errorCode);
            }
            else if (eventName == "OnMRecAdClickedEvent")
            {
                InvokeEvent(_onMRecAdClickedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnMRecAdExpandedEvent")
            {
                InvokeEvent(_onMRecAdExpandedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnMRecAdCollapsedEvent")
            {
                InvokeEvent(_onMRecAdCollapsedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnInterstitialLoadedEvent")
            {
                InvokeEvent(_onInterstitialLoadedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnInterstitialLoadFailedEvent")
            {
                var errorCode = 0;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(_onInterstitialLoadFailedEvent, adUnitIdentifier, errorCode);
            }
            else if (eventName == "OnInterstitialHiddenEvent")
            {
                InvokeEvent(_onInterstitialHiddenEvent, adUnitIdentifier);
            }
            else if (eventName == "OnInterstitialDisplayedEvent")
            {
                InvokeEvent(_onInterstitialDisplayedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnInterstitialAdFailedToDisplayEvent")
            {
                var errorCode = 0;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(_onInterstitialAdFailedToDisplayEvent, adUnitIdentifier, errorCode);
            }
            else if (eventName == "OnInterstitialClickedEvent")
            {
                InvokeEvent(_onInterstitialClickedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedAdLoadedEvent")
            {
                InvokeEvent(_onRewardedAdLoadedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedAdLoadFailedEvent")
            {
                var errorCode = 0;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(_onRewardedAdLoadFailedEvent, adUnitIdentifier, errorCode);
            }
            else if (eventName == "OnRewardedAdDisplayedEvent")
            {
                InvokeEvent(_onRewardedAdDisplayedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedAdHiddenEvent")
            {
                InvokeEvent(_onRewardedAdHiddenEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedAdClickedEvent")
            {
                InvokeEvent(_onRewardedAdClickedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedAdFailedToDisplayEvent")
            {
                var errorCode = 0;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(_onRewardedAdFailedToDisplayEvent, adUnitIdentifier, errorCode);
            }
            else if (eventName == "OnRewardedAdReceivedRewardEvent")
            {
                var reward = new MaxSdkBase.Reward {Label = eventProps["rewardLabel"]};

                int.TryParse(eventProps["rewardAmount"], out reward.Amount);

                InvokeEvent(_onRewardedAdReceivedRewardEvent, adUnitIdentifier, reward);
            }
            else if (eventName == "OnRewardedInterstitialAdLoadedEvent")
            {
                InvokeEvent(_onRewardedInterstitialAdLoadedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedInterstitialAdLoadFailedEvent")
            {
                var errorCode = 0;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(_onRewardedInterstitialAdLoadFailedEvent, adUnitIdentifier, errorCode);
            }
            else if (eventName == "OnRewardedInterstitialAdDisplayedEvent")
            {
                InvokeEvent(_onRewardedInterstitialAdDisplayedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedInterstitialAdHiddenEvent")
            {
                InvokeEvent(_onRewardedInterstitialAdHiddenEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedInterstitialAdClickedEvent")
            {
                InvokeEvent(_onRewardedInterstitialAdClickedEvent, adUnitIdentifier);
            }
            else if (eventName == "OnRewardedInterstitialAdFailedToDisplayEvent")
            {
                var errorCode = 0;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(_onRewardedInterstitialAdFailedToDisplayEvent, adUnitIdentifier, errorCode);
            }
            else if (eventName == "OnRewardedInterstitialAdReceivedRewardEvent")
            {
                var reward = new MaxSdkBase.Reward {Label = eventProps["rewardLabel"]};

                int.TryParse(eventProps["rewardAmount"], out reward.Amount);

                InvokeEvent(_onRewardedInterstitialAdReceivedRewardEvent, adUnitIdentifier, reward);
            }
            else
            {
                MaxSdkLogger.UserWarning("Unknown MAX Ads event fired: " + eventName);
            }
        }
    }

#if UNITY_EDITOR
    public static void EmitSdkInitializedEvent()
    {
        var sdkConfiguration = new MaxSdkBase.SdkConfiguration();
        sdkConfiguration.ConsentDialogState = MaxSdkBase.ConsentDialogState.Unknown;
        
        _onSdkInitializedEvent(sdkConfiguration);
    }
#endif

    private static void InvokeEvent(Action evt)
    {
        if (!CanInvokeEvent(evt)) return;

        MaxSdkLogger.UserDebug("Invoking event: " + evt);
        evt();
    }

    private static void InvokeEvent<T>(Action<T> evt, T param)
    {
        if (!CanInvokeEvent(evt)) return;

        MaxSdkLogger.UserDebug("Invoking event: " + evt + ". Param: " + param);
        evt(param);
    }

    private static void InvokeEvent<T1, T2>(Action<T1, T2> evt, T1 param1, T2 param2)
    {
        if (!CanInvokeEvent(evt)) return;

        MaxSdkLogger.UserDebug("Invoking event: " + evt + ". Params: " + param1 + ", " + param2);
        evt(param1, param2);
    }

    private static bool CanInvokeEvent(Delegate evt)
    {
        if (evt == null) return false;

        // Check that publisher is not over-subscribing
        if (evt.GetInvocationList().Length > 5)
        {
            MaxSdkLogger.UserWarning("Ads Event (" + evt + ") has over 5 subscribers. Please make sure you are properly un-subscribing to actions!!!");
        }

        return true;
    }

    private static void LogSubscribedToEvent(string eventName)
    {
        MaxSdkLogger.D("Listener has been added to callback: " + eventName);
    }

    private static void LogUnsubscribedToEvent(string eventName)
    {
        MaxSdkLogger.D("Listener has been removed from callback: " + eventName);
    }
}
