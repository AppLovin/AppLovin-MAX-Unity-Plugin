/**
 * This is is a global Unity object that is used to forward callbacks from native iOS / Android Max code to the application.
 */

using System;
using System.Globalization;
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

    private static Action<string, MaxSdkBase.AdInfo> _onInterstitialAdLoadedEventV2;
    private static Action<string, MaxSdkBase.ErrorInfo> _onInterstitialAdLoadFailedEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onInterstitialAdDisplayedEventV2;
    private static Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> _onInterstitialAdFailedToDisplayEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onInterstitialAdClickedEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onInterstitialAdRevenuePaidEvent;
    private static Action<string, MaxSdkBase.AdInfo> _onInterstitialAdHiddenEventV2;

    public class Interstitial
    {
        public static event Action<string, MaxSdkBase.AdInfo> OnAdLoadedEvent
        {
            add
            {
                LogSubscribedToEvent("OnInterstitialAdLoadedEvent");
                _onInterstitialAdLoadedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnInterstitialAdLoadedEvent");
                _onInterstitialAdLoadedEventV2 -= value;
            }
        }

        public static event Action<string, MaxSdkBase.ErrorInfo> OnAdLoadFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnInterstitialAdLoadFailedEvent");
                _onInterstitialAdLoadFailedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnInterstitialAdLoadFailedEvent");
                _onInterstitialAdLoadFailedEventV2 -= value;
            }
        }

        /**
         * Fired when an rewarded ad is displayed (may not be received by Unity until the rewarded ad closes).
         */
        public static event Action<string, MaxSdkBase.AdInfo> OnAdDisplayedEvent
        {
            add
            {
                LogSubscribedToEvent("OnInterstitialAdDisplayedEvent");
                _onInterstitialAdDisplayedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnInterstitialAdDisplayedEvent");
                _onInterstitialAdDisplayedEventV2 -= value;
            }
        }

        public static event Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> OnAdDisplayFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnInterstitialAdDisplayFailedEvent");
                _onInterstitialAdFailedToDisplayEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnInterstitialAdDisplayFailedEvent");
                _onInterstitialAdFailedToDisplayEventV2 -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdClickedEvent
        {
            add
            {
                LogSubscribedToEvent("OnInterstitialAdClickedEvent");
                _onInterstitialAdClickedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnInterstitialAdClickedEvent");
                _onInterstitialAdClickedEventV2 -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdRevenuePaidEvent
        {
            add
            {
                LogSubscribedToEvent("OnInterstitialAdRevenuePaidEvent");
                _onInterstitialAdRevenuePaidEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnInterstitialAdRevenuePaidEvent");
                _onInterstitialAdRevenuePaidEvent -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdHiddenEvent
        {
            add
            {
                LogSubscribedToEvent("OnInterstitialAdHiddenEvent");
                _onInterstitialAdHiddenEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnInterstitialAdHiddenEvent");
                _onInterstitialAdHiddenEventV2 -= value;
            }
        }
    }

    private static Action<string, MaxSdkBase.AdInfo> _onRewardedAdLoadedEventV2;
    private static Action<string, MaxSdkBase.ErrorInfo> _onRewardedAdLoadFailedEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onRewardedAdDisplayedEventV2;
    private static Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> _onRewardedAdFailedToDisplayEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onRewardedAdClickedEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onRewardedAdRevenuePaidEvent;
    private static Action<string, MaxSdkBase.Reward, MaxSdkBase.AdInfo> _onRewardedAdReceivedRewardEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onRewardedAdHiddenEventV2;

    public class Rewarded
    {
        public static event Action<string, MaxSdkBase.AdInfo> OnAdLoadedEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdLoadedEvent");
                _onRewardedAdLoadedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdLoadedEvent");
                _onRewardedAdLoadedEventV2 -= value;
            }
        }

        public static event Action<string, MaxSdkBase.ErrorInfo> OnAdLoadFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdLoadFailedEvent");
                _onRewardedAdLoadFailedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdLoadFailedEvent");
                _onRewardedAdLoadFailedEventV2 -= value;
            }
        }

        /**
         * Fired when an rewarded ad is displayed (may not be received by Unity until the rewarded ad closes).
         */
        public static event Action<string, MaxSdkBase.AdInfo> OnAdDisplayedEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdDisplayedEvent");
                _onRewardedAdDisplayedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdDisplayedEvent");
                _onRewardedAdDisplayedEventV2 -= value;
            }
        }

        public static event Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> OnAdDisplayFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdDisplayFailedEvent");
                _onRewardedAdFailedToDisplayEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdDisplayFailedEvent");
                _onRewardedAdFailedToDisplayEventV2 -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdClickedEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdClickedEvent");
                _onRewardedAdClickedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdClickedEvent");
                _onRewardedAdClickedEventV2 -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdRevenuePaidEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdRevenuePaidEvent");
                _onRewardedAdRevenuePaidEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdRevenuePaidEvent");
                _onRewardedAdRevenuePaidEvent -= value;
            }
        }

        public static event Action<string, MaxSdkBase.Reward, MaxSdkBase.AdInfo> OnAdReceivedRewardEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdReceivedRewardEvent");
                _onRewardedAdReceivedRewardEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdReceivedRewardEvent");
                _onRewardedAdReceivedRewardEventV2 -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdHiddenEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdHiddenEvent");
                _onRewardedAdHiddenEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdHiddenEvent");
                _onRewardedAdHiddenEventV2 -= value;
            }
        }
    }

    private static Action<string, MaxSdkBase.AdInfo> _onRewardedInterstitialAdLoadedEvent;
    private static Action<string, MaxSdkBase.ErrorInfo> _onRewardedInterstitialAdLoadFailedEvent;
    private static Action<string, MaxSdkBase.AdInfo> _onRewardedInterstitialAdDisplayedEvent;
    private static Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> _onRewardedInterstitialAdFailedToDisplayEvent;
    private static Action<string, MaxSdkBase.AdInfo> _onRewardedInterstitialAdClickedEvent;
    private static Action<string, MaxSdkBase.AdInfo> _onRewardedInterstitialAdRevenuePaidEvent;
    private static Action<string, MaxSdkBase.Reward, MaxSdkBase.AdInfo> _onRewardedInterstitialAdReceivedRewardEvent;
    private static Action<string, MaxSdkBase.AdInfo> _onRewardedInterstitialAdHiddenEvent;

    public class RewardedInterstitial
    {
        public static event Action<string, MaxSdkBase.AdInfo> OnAdLoadedEvent
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

        public static event Action<string, MaxSdkBase.ErrorInfo> OnAdLoadFailedEvent
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

        /**
         * Fired when an rewarded ad is displayed (may not be received by Unity until the rewarded ad closes).
         */
        public static event Action<string, MaxSdkBase.AdInfo> OnAdDisplayedEvent
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

        public static event Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> OnAdDisplayFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedInterstitialAdDisplayFailedEvent");
                _onRewardedInterstitialAdFailedToDisplayEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedInterstitialAdDisplayFailedEvent");
                _onRewardedInterstitialAdFailedToDisplayEvent -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdClickedEvent
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

        public static event Action<string, MaxSdkBase.AdInfo> OnAdRevenuePaidEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedInterstitialAdRevenuePaidEvent");
                _onRewardedInterstitialAdRevenuePaidEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedInterstitialAdRevenuePaidEvent");
                _onRewardedInterstitialAdRevenuePaidEvent -= value;
            }
        }

        public static event Action<string, MaxSdkBase.Reward, MaxSdkBase.AdInfo> OnAdReceivedRewardEvent
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

        public static event Action<string, MaxSdkBase.AdInfo> OnAdHiddenEvent
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
    }

    private static Action<string, MaxSdkBase.AdInfo> _onBannerAdLoadedEventV2;
    private static Action<string, MaxSdkBase.ErrorInfo> _onBannerAdLoadFailedEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onBannerAdClickedEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onBannerAdRevenuePaidEvent;
    private static Action<string, MaxSdkBase.AdInfo> _onBannerAdExpandedEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onBannerAdCollapsedEventV2;

    public class Banner
    {
        public static event Action<string, MaxSdkBase.AdInfo> OnAdLoadedEvent
        {
            add
            {
                LogSubscribedToEvent("OnBannerAdLoadedEvent");
                _onBannerAdLoadedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnBannerAdLoadedEvent");
                _onBannerAdLoadedEventV2 -= value;
            }
        }

        public static event Action<string, MaxSdkBase.ErrorInfo> OnAdLoadFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnBannerAdLoadFailedEvent");
                _onBannerAdLoadFailedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnBannerAdLoadFailedEvent");
                _onBannerAdLoadFailedEventV2 -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdClickedEvent
        {
            add
            {
                LogSubscribedToEvent("OnBannerAdClickedEvent");
                _onBannerAdClickedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnBannerAdClickedEvent");
                _onBannerAdClickedEventV2 -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdRevenuePaidEvent
        {
            add
            {
                LogSubscribedToEvent("OnBannerAdRevenuePaidEvent");
                _onBannerAdRevenuePaidEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnBannerAdRevenuePaidEvent");
                _onBannerAdRevenuePaidEvent -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdExpandedEvent
        {
            add
            {
                LogSubscribedToEvent("OnBannerAdExpandedEvent");
                _onBannerAdExpandedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnBannerAdExpandedEvent");
                _onBannerAdExpandedEventV2 -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdCollapsedEvent
        {
            add
            {
                LogSubscribedToEvent("OnBannerAdCollapsedEvent");
                _onBannerAdCollapsedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnBannerAdCollapsedEvent");
                _onBannerAdCollapsedEventV2 -= value;
            }
        }
    }

    private static Action<string, MaxSdkBase.AdInfo> _onMRecAdLoadedEventV2;
    private static Action<string, MaxSdkBase.ErrorInfo> _onMRecAdLoadFailedEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onMRecAdClickedEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onMRecAdRevenuePaidEvent;
    private static Action<string, MaxSdkBase.AdInfo> _onMRecAdExpandedEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onMRecAdCollapsedEventV2;

    public class MRec
    {
        public static event Action<string, MaxSdkBase.AdInfo> OnAdLoadedEvent
        {
            add
            {
                LogSubscribedToEvent("OnMRecAdLoadedEvent");
                _onMRecAdLoadedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnMRecAdLoadedEvent");
                _onMRecAdLoadedEventV2 -= value;
            }
        }

        public static event Action<string, MaxSdkBase.ErrorInfo> OnAdLoadFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnMRecAdLoadFailedEvent");
                _onMRecAdLoadFailedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnMRecAdLoadFailedEvent");
                _onMRecAdLoadFailedEventV2 -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdClickedEvent
        {
            add
            {
                LogSubscribedToEvent("OnMRecAdClickedEvent");
                _onMRecAdClickedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnMRecAdClickedEvent");
                _onMRecAdClickedEventV2 -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdRevenuePaidEvent
        {
            add
            {
                LogSubscribedToEvent("OnMRecAdRevenuePaidEvent");
                _onMRecAdRevenuePaidEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnMRecAdRevenuePaidEvent");
                _onMRecAdRevenuePaidEvent -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdExpandedEvent
        {
            add
            {
                LogSubscribedToEvent("OnMRecAdExpandedEvent");
                _onMRecAdExpandedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnMRecAdExpandedEvent");
                _onMRecAdExpandedEventV2 -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdCollapsedEvent
        {
            add
            {
                LogSubscribedToEvent("OnMRecAdCollapsedEvent");
                _onMRecAdCollapsedEventV2 += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnMRecAdCollapsedEvent");
                _onMRecAdCollapsedEventV2 -= value;
            }
        }
    }

    private static Action<string, MaxSdkBase.AdInfo> _onCrossPromoAdLoadedEvent;
    private static Action<string, MaxSdkBase.ErrorInfo> _onCrossPromoAdLoadFailedEvent;
    private static Action<string, MaxSdkBase.AdInfo> _onCrossPromoAdClickedEvent;
    private static Action<string, MaxSdkBase.AdInfo> _onCrossPromoAdRevenuePaidEvent;
    private static Action<string, MaxSdkBase.AdInfo> _onCrossPromoAdExpandedEvent;
    private static Action<string, MaxSdkBase.AdInfo> _onCrossPromoAdCollapsedEvent;

    public class CrossPromo
    {
        public static event Action<string, MaxSdkBase.AdInfo> OnAdLoadedEvent
        {
            add
            {
                LogSubscribedToEvent("OnCrossPromoAdLoadedEvent");
                _onCrossPromoAdLoadedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnCrossPromoAdLoadedEvent");
                _onCrossPromoAdLoadedEvent -= value;
            }
        }

        public static event Action<string, MaxSdkBase.ErrorInfo> OnAdLoadFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnCrossPromoAdLoadFailedEvent");
                _onCrossPromoAdLoadFailedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnCrossPromoAdLoadFailedEvent");
                _onCrossPromoAdLoadFailedEvent -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdClickedEvent
        {
            add
            {
                LogSubscribedToEvent("OnCrossPromoAdClickedEvent");
                _onCrossPromoAdClickedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnCrossPromoAdClickedEvent");
                _onCrossPromoAdClickedEvent -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdRevenuePaidEvent
        {
            add
            {
                LogSubscribedToEvent("OnCrossPromoAdRevenuePaidEvent");
                _onCrossPromoAdRevenuePaidEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnCrossPromoAdRevenuePaidEvent");
                _onCrossPromoAdRevenuePaidEvent -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdExpandedEvent
        {
            add
            {
                LogSubscribedToEvent("OnCrossPromoAdExpandedEvent");
                _onCrossPromoAdExpandedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnCrossPromoAdExpandedEvent");
                _onCrossPromoAdExpandedEvent -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdCollapsedEvent
        {
            add
            {
                LogSubscribedToEvent("OnCrossPromoAdCollapsedEvent");
                _onCrossPromoAdCollapsedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnCrossPromoAdCollapsedEvent");
                _onCrossPromoAdCollapsedEvent -= value;
            }
        }
    }

    private static Action<string> _onBannerAdLoadedEvent;
    private static Action<string, int> _onBannerAdLoadFailedEvent;
    private static Action<string> _onBannerAdClickedEvent;
    private static Action<string> _onBannerAdExpandedEvent;
    private static Action<string> _onBannerAdCollapsedEvent;

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Banner.OnAdLoadedEvent` instead.")]
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

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Banner.OnAdLoadFailedEvent` instead.")]
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

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Banner.OnAdClickedEvent` instead.")]
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

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Banner.OnAdExpandedEvent` instead.")]
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

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Banner.OnAdCollapsedEvent` instead.")]
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

    private static Action<string> _onMRecAdLoadedEvent;
    private static Action<string, int> _onMRecAdLoadFailedEvent;
    private static Action<string> _onMRecAdClickedEvent;
    private static Action<string> _onMRecAdExpandedEvent;
    private static Action<string> _onMRecAdCollapsedEvent;

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.MRec.OnAdLoadedEvent` instead.")]
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

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.MRec.OnAdLoadFailedEvent` instead.")]
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

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.MRec.OnAdClickedEvent` instead.")]
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

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.MRec.OnAdExpandedEvent` instead.")]
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

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.MRec.OnAdCollapsedEvent` instead.")]
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

    private static Action<string> _onInterstitialAdLoadedEvent;
    private static Action<string, int> _onInterstitialLoadFailedEvent;
    private static Action<string> _onInterstitialAdDisplayedEvent;
    private static Action<string, int> _onInterstitialAdFailedToDisplayEvent;
    private static Action<string> _onInterstitialAdClickedEvent;
    private static Action<string> _onInterstitialAdHiddenEvent;

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Interstitial.OnAdLoadedEvent` instead.")]
    public static event Action<string> OnInterstitialLoadedEvent
    {
        add
        {
            LogSubscribedToEvent("OnInterstitialLoadedEvent");
            _onInterstitialAdLoadedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnInterstitialLoadedEvent");
            _onInterstitialAdLoadedEvent -= value;
        }
    }

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent` instead.")]
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

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Interstitial.OnAdHiddenEvent` instead.")]
    public static event Action<string> OnInterstitialHiddenEvent
    {
        add
        {
            LogSubscribedToEvent("OnInterstitialHiddenEvent");
            _onInterstitialAdHiddenEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnInterstitialHiddenEvent");
            _onInterstitialAdHiddenEvent -= value;
        }
    }

    // Fired when an interstitial ad is displayed (may not be received by Unity until the interstitial closes)
    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent` instead.")]
    public static event Action<string> OnInterstitialDisplayedEvent
    {
        add
        {
            LogSubscribedToEvent("OnInterstitialDisplayedEvent");
            _onInterstitialAdDisplayedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnInterstitialDisplayedEvent");
            _onInterstitialAdDisplayedEvent -= value;
        }
    }

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent` instead.")]
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

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Interstitial.OnAdClickedEvent` instead.")]
    public static event Action<string> OnInterstitialClickedEvent
    {
        add
        {
            LogSubscribedToEvent("OnInterstitialClickedEvent");
            _onInterstitialAdClickedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnInterstitialClickedEvent");
            _onInterstitialAdClickedEvent -= value;
        }
    }

    private static Action<string> _onRewardedAdLoadedEvent;
    private static Action<string, int> _onRewardedAdLoadFailedEvent;
    private static Action<string> _onRewardedAdDisplayedEvent;
    private static Action<string, int> _onRewardedAdFailedToDisplayEvent;
    private static Action<string> _onRewardedAdClickedEvent;
    private static Action<string, MaxSdkBase.Reward> _onRewardedAdReceivedRewardEvent;
    private static Action<string> _onRewardedAdHiddenEvent;

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Rewarded.OnAdLoadedEvent` instead.")]
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

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent` instead.")]
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
    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent` instead.")]
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

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Rewarded.OnAdHiddenEvent` instead.")]
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

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Rewarded.OnAdClickedEvent` instead.")]
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

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent` instead.")]
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

    [System.Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent` instead.")]
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
            var sdkConfiguration = MaxSdkBase.SdkConfiguration.Create(eventProps);
            InvokeEvent(_onSdkInitializedEvent, sdkConfiguration);
        }
        else if (eventName == "OnVariablesUpdatedEvent")
        {
            InvokeEvent(_onVariablesUpdatedEvent);
        }
        else if (eventName == "OnSdkConsentDialogDismissedEvent")
        {
            InvokeEvent(_onSdkConsentDialogDismissedEvent);
        }
        // Ad Events
        else
        {
            var adInfo = new MaxSdkBase.AdInfo(eventProps);
            var adUnitIdentifier = eventProps["adUnitId"];
            if (eventName == "OnBannerAdLoadedEvent")
            {
                InvokeEvent(_onBannerAdLoadedEvent, adUnitIdentifier);
                InvokeEvent(_onBannerAdLoadedEventV2, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnBannerAdLoadFailedEvent")
            {
                var errorCode = -1;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(_onBannerAdLoadFailedEvent, adUnitIdentifier, errorCode);

                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(_onBannerAdLoadFailedEventV2, adUnitIdentifier, errorInfo);
            }
            else if (eventName == "OnBannerAdClickedEvent")
            {
                InvokeEvent(_onBannerAdClickedEvent, adUnitIdentifier);
                InvokeEvent(_onBannerAdClickedEventV2, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnBannerAdRevenuePaidEvent")
            {
                InvokeEvent(_onBannerAdRevenuePaidEvent, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnBannerAdExpandedEvent")
            {
                InvokeEvent(_onBannerAdExpandedEvent, adUnitIdentifier);
                InvokeEvent(_onBannerAdExpandedEventV2, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnBannerAdCollapsedEvent")
            {
                InvokeEvent(_onBannerAdCollapsedEvent, adUnitIdentifier);
                InvokeEvent(_onBannerAdCollapsedEventV2, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnMRecAdLoadedEvent")
            {
                InvokeEvent(_onMRecAdLoadedEvent, adUnitIdentifier);
                InvokeEvent(_onMRecAdLoadedEventV2, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnMRecAdLoadFailedEvent")
            {
                var errorCode = -1;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(_onMRecAdLoadFailedEvent, adUnitIdentifier, errorCode);

                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(_onMRecAdLoadFailedEventV2, adUnitIdentifier, errorInfo);
            }
            else if (eventName == "OnMRecAdClickedEvent")
            {
                InvokeEvent(_onMRecAdClickedEvent, adUnitIdentifier);
                InvokeEvent(_onMRecAdClickedEventV2, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnMRecAdRevenuePaidEvent")
            {
                InvokeEvent(_onMRecAdRevenuePaidEvent, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnMRecAdExpandedEvent")
            {
                InvokeEvent(_onMRecAdExpandedEvent, adUnitIdentifier);
                InvokeEvent(_onMRecAdExpandedEventV2, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnMRecAdCollapsedEvent")
            {
                InvokeEvent(_onMRecAdCollapsedEvent, adUnitIdentifier);
                InvokeEvent(_onMRecAdCollapsedEventV2, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnCrossPromoAdLoadedEvent")
            {
                InvokeEvent(_onCrossPromoAdLoadedEvent, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnCrossPromoAdLoadFailedEvent")
            {
                var errorCode = -1;
                int.TryParse(eventProps["errorCode"], out errorCode);
                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);

                InvokeEvent(_onCrossPromoAdLoadFailedEvent, adUnitIdentifier, errorInfo);
            }
            else if (eventName == "OnCrossPromoAdClickedEvent")
            {
                InvokeEvent(_onCrossPromoAdClickedEvent, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnCrossPromoAdRevenuePaidEvent")
            {
                InvokeEvent(_onCrossPromoAdRevenuePaidEvent, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnCrossPromoAdExpandedEvent")
            {
                InvokeEvent(_onCrossPromoAdExpandedEvent, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnCrossPromoAdCollapsedEvent")
            {
                InvokeEvent(_onCrossPromoAdCollapsedEvent, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnInterstitialLoadedEvent")
            {
                InvokeEvent(_onInterstitialAdLoadedEvent, adUnitIdentifier);
                InvokeEvent(_onInterstitialAdLoadedEventV2, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnInterstitialLoadFailedEvent")
            {
                var errorCode = -1;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(_onInterstitialLoadFailedEvent, adUnitIdentifier, errorCode);

                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(_onInterstitialAdLoadFailedEventV2, adUnitIdentifier, errorInfo);
            }
            else if (eventName == "OnInterstitialHiddenEvent")
            {
                InvokeEvent(_onInterstitialAdHiddenEvent, adUnitIdentifier);
                InvokeEvent(_onInterstitialAdHiddenEventV2, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnInterstitialDisplayedEvent")
            {
                InvokeEvent(_onInterstitialAdDisplayedEvent, adUnitIdentifier);
                InvokeEvent(_onInterstitialAdDisplayedEventV2, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnInterstitialAdFailedToDisplayEvent")
            {
                var errorCode = -1;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(_onInterstitialAdFailedToDisplayEvent, adUnitIdentifier, errorCode);

                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(_onInterstitialAdFailedToDisplayEventV2, adUnitIdentifier, errorInfo, adInfo);
            }
            else if (eventName == "OnInterstitialClickedEvent")
            {
                InvokeEvent(_onInterstitialAdClickedEvent, adUnitIdentifier);
                InvokeEvent(_onInterstitialAdClickedEventV2, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnInterstitialAdRevenuePaidEvent")
            {
                InvokeEvent(_onInterstitialAdRevenuePaidEvent, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnRewardedAdLoadedEvent")
            {
                InvokeEvent(_onRewardedAdLoadedEvent, adUnitIdentifier);
                InvokeEvent(_onRewardedAdLoadedEventV2, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnRewardedAdLoadFailedEvent")
            {
                var errorCode = -1;
                int.TryParse(eventProps["errorCode"], out errorCode);

                InvokeEvent(_onRewardedAdLoadFailedEvent, adUnitIdentifier, errorCode);

                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(_onRewardedAdLoadFailedEventV2, adUnitIdentifier, errorInfo);
            }
            else if (eventName == "OnRewardedAdDisplayedEvent")
            {
                InvokeEvent(_onRewardedAdDisplayedEvent, adUnitIdentifier);
                InvokeEvent(_onRewardedAdDisplayedEventV2, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnRewardedAdHiddenEvent")
            {
                InvokeEvent(_onRewardedAdHiddenEvent, adUnitIdentifier);
                InvokeEvent(_onRewardedAdHiddenEventV2, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnRewardedAdClickedEvent")
            {
                InvokeEvent(_onRewardedAdClickedEvent, adUnitIdentifier);
                InvokeEvent(_onRewardedAdClickedEventV2, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnRewardedAdRevenuePaidEvent")
            {
                InvokeEvent(_onRewardedAdRevenuePaidEvent, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnRewardedAdFailedToDisplayEvent")
            {
                var errorCode = -1;
                int.TryParse(eventProps["errorCode"], out errorCode);
                InvokeEvent(_onRewardedAdFailedToDisplayEvent, adUnitIdentifier, errorCode);

                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(_onRewardedAdFailedToDisplayEventV2, adUnitIdentifier, errorInfo, adInfo);
            }
            else if (eventName == "OnRewardedAdReceivedRewardEvent")
            {
                var reward = new MaxSdkBase.Reward {Label = eventProps["rewardLabel"]};

                int.TryParse(eventProps["rewardAmount"], out reward.Amount);

                InvokeEvent(_onRewardedAdReceivedRewardEvent, adUnitIdentifier, reward);
                InvokeEvent(_onRewardedAdReceivedRewardEventV2, adUnitIdentifier, reward, adInfo);
            }
            else if (eventName == "OnRewardedInterstitialAdLoadedEvent")
            {
                InvokeEvent(_onRewardedInterstitialAdLoadedEvent, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnRewardedInterstitialAdLoadFailedEvent")
            {
                var errorCode = -1;
                int.TryParse(eventProps["errorCode"], out errorCode);
                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);

                InvokeEvent(_onRewardedInterstitialAdLoadFailedEvent, adUnitIdentifier, errorInfo);
            }
            else if (eventName == "OnRewardedInterstitialAdDisplayedEvent")
            {
                InvokeEvent(_onRewardedInterstitialAdDisplayedEvent, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnRewardedInterstitialAdHiddenEvent")
            {
                InvokeEvent(_onRewardedInterstitialAdHiddenEvent, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnRewardedInterstitialAdClickedEvent")
            {
                InvokeEvent(_onRewardedInterstitialAdClickedEvent, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnRewardedInterstitialAdRevenuePaidEvent")
            {
                InvokeEvent(_onRewardedInterstitialAdRevenuePaidEvent, adUnitIdentifier, adInfo);
            }
            else if (eventName == "OnRewardedInterstitialAdFailedToDisplayEvent")
            {
                var errorCode = -1;
                int.TryParse(eventProps["errorCode"], out errorCode);
                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);

                InvokeEvent(_onRewardedInterstitialAdFailedToDisplayEvent, adUnitIdentifier, errorInfo, adInfo);
            }
            else if (eventName == "OnRewardedInterstitialAdReceivedRewardEvent")
            {
                var reward = new MaxSdkBase.Reward {Label = eventProps["rewardLabel"]};

                int.TryParse(eventProps["rewardAmount"], out reward.Amount);

                InvokeEvent(_onRewardedInterstitialAdReceivedRewardEvent, adUnitIdentifier, reward, adInfo);
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
        sdkConfiguration.AppTrackingStatus = MaxSdkBase.AppTrackingStatus.Authorized;
        var currentRegion = RegionInfo.CurrentRegion;
        sdkConfiguration.CountryCode = currentRegion != null ? currentRegion.TwoLetterISORegionName : "US";

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

    private static void InvokeEvent<T1, T2, T3>(Action<T1, T2, T3> evt, T1 param1, T2 param2, T3 param3)
    {
        if (!CanInvokeEvent(evt)) return;

        MaxSdkLogger.UserDebug("Invoking event: " + evt + ". Params: " + param1 + ", " + param2 + ", " + param3);
        evt(param1, param2, param3);
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
