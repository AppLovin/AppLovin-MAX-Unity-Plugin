/**
 * This is is a global Unity object that is used to forward callbacks from native iOS / Android Max code to the application.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using AppLovinMax.ThirdParty.MiniJson;
using AppLovinMax.Internal;

public static class MaxSdkCallbacks
{
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

    private static Action<bool> _onApplicationStateChangedEvent;
    /// <summary>
    /// Fired when the application is paused or resumed.
    /// </summary>
    public static event Action<bool> OnApplicationStateChangedEvent
    {
        add
        {
            LogSubscribedToEvent("OnApplicationStateChangedEvent");
            _onApplicationStateChangedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnApplicationStateChangedEvent");
            _onApplicationStateChangedEvent -= value;
        }
    }

    private static Action<string, MaxSdkBase.AdInfo> _onInterstitialAdLoadedEventV2;
    private static Action<string, MaxSdkBase.ErrorInfo> _onInterstitialAdLoadFailedEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onInterstitialAdDisplayedEventV2;
    private static Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> _onInterstitialAdFailedToDisplayEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onInterstitialAdClickedEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onInterstitialAdRevenuePaidEvent;
    private static Action<string, string, MaxSdkBase.AdInfo> _onInterstitialAdReviewCreativeIdGeneratedEvent;
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
         * Fired when an interstitial ad is displayed (may not be received by Unity until the interstitial ad closes).
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

        /// <summary>
        /// Fired when an interstitial ad impression was validated and revenue will be paid.
        /// Executed on a background thread to avoid any delays in execution.
        /// </summary>
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

        /// <summary>
        /// Fired when an Ad Review Creative ID has been generated.
        /// The parameters returned are the adUnitIdentifier, adReviewCreativeId, and adInfo in that respective order.
        /// Executed on a background thread to avoid any delays in execution.
        /// </summary>
        public static event Action<string, string, MaxSdkBase.AdInfo> OnAdReviewCreativeIdGeneratedEvent
        {
            add
            {
                LogSubscribedToEvent("OnInterstitialAdReviewCreativeIdGeneratedEvent");
                _onInterstitialAdReviewCreativeIdGeneratedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnInterstitialAdReviewCreativeIdGeneratedEvent");
                _onInterstitialAdReviewCreativeIdGeneratedEvent -= value;
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

    private static Action<string, MaxSdkBase.AdInfo> _onAppOpenAdLoadedEvent;
    private static Action<string, MaxSdkBase.ErrorInfo> _onAppOpenAdLoadFailedEvent;
    private static Action<string, MaxSdkBase.AdInfo> _onAppOpenAdDisplayedEvent;
    private static Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> _onAppOpenAdFailedToDisplayEvent;
    private static Action<string, MaxSdkBase.AdInfo> _onAppOpenAdClickedEvent;
    private static Action<string, MaxSdkBase.AdInfo> _onAppOpenAdRevenuePaidEvent;
    private static Action<string, MaxSdkBase.AdInfo> _onAppOpenAdHiddenEvent;

    public class AppOpen
    {
        public static event Action<string, MaxSdkBase.AdInfo> OnAdLoadedEvent
        {
            add
            {
                LogSubscribedToEvent("OnAppOpenAdLoadedEvent");
                _onAppOpenAdLoadedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnAppOpenAdLoadedEvent");
                _onAppOpenAdLoadedEvent -= value;
            }
        }

        public static event Action<string, MaxSdkBase.ErrorInfo> OnAdLoadFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnAppOpenAdLoadFailedEvent");
                _onAppOpenAdLoadFailedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnAppOpenAdLoadFailedEvent");
                _onAppOpenAdLoadFailedEvent -= value;
            }
        }

        /**
         * Fired when an app open ad is displayed (may not be received by Unity until the app open ad closes).
         */
        public static event Action<string, MaxSdkBase.AdInfo> OnAdDisplayedEvent
        {
            add
            {
                LogSubscribedToEvent("OnAppOpenAdDisplayedEvent");
                _onAppOpenAdDisplayedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnAppOpenAdDisplayedEvent");
                _onAppOpenAdDisplayedEvent -= value;
            }
        }

        public static event Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> OnAdDisplayFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnAppOpenAdDisplayFailedEvent");
                _onAppOpenAdFailedToDisplayEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnAppOpenAdDisplayFailedEvent");
                _onAppOpenAdFailedToDisplayEvent -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdClickedEvent
        {
            add
            {
                LogSubscribedToEvent("OnAppOpenAdClickedEvent");
                _onAppOpenAdClickedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnAppOpenAdClickedEvent");
                _onAppOpenAdClickedEvent -= value;
            }
        }

        /// <summary>
        /// Fired when an app open ad impression was validated and revenue will be paid.
        /// Executed on a background thread to avoid any delays in execution.
        /// </summary>
        public static event Action<string, MaxSdkBase.AdInfo> OnAdRevenuePaidEvent
        {
            add
            {
                LogSubscribedToEvent("OnAppOpenAdRevenuePaidEvent");
                _onAppOpenAdRevenuePaidEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnAppOpenAdRevenuePaidEvent");
                _onAppOpenAdRevenuePaidEvent -= value;
            }
        }

        public static event Action<string, MaxSdkBase.AdInfo> OnAdHiddenEvent
        {
            add
            {
                LogSubscribedToEvent("OnAppOpenAdHiddenEvent");
                _onAppOpenAdHiddenEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnAppOpenAdHiddenEvent");
                _onAppOpenAdHiddenEvent -= value;
            }
        }
    }

    private static Action<string, MaxSdkBase.AdInfo> _onRewardedAdLoadedEventV2;
    private static Action<string, MaxSdkBase.ErrorInfo> _onRewardedAdLoadFailedEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onRewardedAdDisplayedEventV2;
    private static Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> _onRewardedAdFailedToDisplayEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onRewardedAdClickedEventV2;
    private static Action<string, MaxSdkBase.AdInfo> _onRewardedAdRevenuePaidEvent;
    private static Action<string, string, MaxSdkBase.AdInfo> _onRewardedAdReviewCreativeIdGeneratedEvent;
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
         * Fired when a rewarded ad is displayed (may not be received by Unity until the rewarded ad closes).
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

        /// <summary>
        /// Fired when a rewarded ad impression was validated and revenue will be paid.
        /// Executed on a background thread to avoid any delays in execution.
        /// </summary>
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

        /// <summary>
        /// Fired when an Ad Review Creative ID has been generated.
        /// The parameters returned are the adUnitIdentifier, adReviewCreativeId, and adInfo in that respective order.
        /// Executed on a background thread to avoid any delays in execution.
        /// </summary>
        public static event Action<string, string, MaxSdkBase.AdInfo> OnAdReviewCreativeIdGeneratedEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdReviewCreativeIdGeneratedEvent");
                _onRewardedAdReviewCreativeIdGeneratedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdReviewCreativeIdGeneratedEvent");
                _onRewardedAdReviewCreativeIdGeneratedEvent -= value;
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
    private static Action<string, string, MaxSdkBase.AdInfo> _onRewardedInterstitialAdReviewCreativeIdGeneratedEvent;
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
         * Fired when a rewarded interstitial ad is displayed (may not be received by Unity until
         * the rewarded interstitial ad closes).
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

        /// <summary>
        /// Fired when a rewarded interstitial ad impression was validated and revenue will be paid.
        /// Executed on a background thread to avoid any delays in execution.
        /// </summary>
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

        /// <summary>
        /// Fired when an Ad Review Creative ID has been generated.
        /// The parameters returned are the adUnitIdentifier, adReviewCreativeId, and adInfo in that respective order.
        /// Executed on a background thread to avoid any delays in execution.
        /// </summary>
        public static event Action<string, string, MaxSdkBase.AdInfo> OnAdReviewCreativeIdGeneratedEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedInterstitialAdReviewCreativeIdGeneratedEvent");
                _onRewardedInterstitialAdReviewCreativeIdGeneratedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedInterstitialAdReviewCreativeIdGeneratedEvent");
                _onRewardedInterstitialAdReviewCreativeIdGeneratedEvent -= value;
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
    private static Action<string, string, MaxSdkBase.AdInfo> _onBannerAdReviewCreativeIdGeneratedEvent;
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

        /// <summary>
        /// Fired when an Ad Review Creative ID has been generated.
        /// The parameters returned are the adUnitIdentifier, adReviewCreativeId, and adInfo in that respective order.
        /// </summary>
        public static event Action<string, string, MaxSdkBase.AdInfo> OnAdReviewCreativeIdGeneratedEvent
        {
            add
            {
                LogSubscribedToEvent("OnBannerAdReviewCreativeIdGeneratedEvent");
                _onBannerAdReviewCreativeIdGeneratedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnBannerAdReviewCreativeIdGeneratedEvent");
                _onBannerAdReviewCreativeIdGeneratedEvent -= value;
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
    private static Action<string, string, MaxSdkBase.AdInfo> _onMRecAdReviewCreativeIdGeneratedEvent;
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

        /// <summary>
        /// Fired when an Ad Review Creative ID has been generated.
        /// The parameters returned are the adUnitIdentifier, adReviewCreativeId, and adInfo in that respective order.
        /// </summary>
        public static event Action<string, string, MaxSdkBase.AdInfo> OnAdReviewCreativeIdGeneratedEvent
        {
            add
            {
                LogSubscribedToEvent("OnMRecAdReviewCreativeIdGeneratedEvent");
                _onMRecAdReviewCreativeIdGeneratedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnMRecAdReviewCreativeIdGeneratedEvent");
                _onMRecAdReviewCreativeIdGeneratedEvent -= value;
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

    private static Action<string> _onBannerAdLoadedEvent;
    private static Action<string, int> _onBannerAdLoadFailedEvent;
    private static Action<string> _onBannerAdClickedEvent;
    private static Action<string> _onBannerAdExpandedEvent;
    private static Action<string> _onBannerAdCollapsedEvent;

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Banner.OnAdLoadedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Banner.OnAdLoadFailedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Banner.OnAdClickedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Banner.OnAdExpandedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Banner.OnAdCollapsedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.MRec.OnAdLoadedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.MRec.OnAdLoadFailedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.MRec.OnAdClickedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.MRec.OnAdExpandedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.MRec.OnAdCollapsedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Interstitial.OnAdLoadedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Interstitial.OnAdHiddenEvent` instead.")]
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
    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Interstitial.OnAdClickedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Rewarded.OnAdLoadedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent` instead.")]
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
    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Rewarded.OnAdHiddenEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Rewarded.OnAdClickedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent` instead.")]
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

    [Obsolete("This callback has been deprecated. Please use `MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent` instead.")]
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

    public static void ForwardEvent(string eventPropsStr)
    {
        var eventProps = Json.Deserialize(eventPropsStr) as Dictionary<string, object>;
        if (eventProps == null)
        {
            MaxSdkLogger.E("Failed to forward event due to invalid event data");
            return;
        }

        var keepInBackground = MaxSdkUtils.GetBoolFromDictionary(eventProps, "keepInBackground", false);
        var eventName = MaxSdkUtils.GetStringFromDictionary(eventProps, "name", "");
        if (eventName == "OnInitialCallbackEvent")
        {
            MaxSdkLogger.D("Initial background callback.");
        }
        else if (eventName == "OnSdkInitializedEvent")
        {
            var sdkConfiguration = MaxSdkBase.SdkConfiguration.Create(eventProps);
            InvokeEvent(_onSdkInitializedEvent, sdkConfiguration, eventName, keepInBackground);
        }
        else if (eventName == "OnSdkConsentDialogDismissedEvent")
        {
            InvokeEvent(_onSdkConsentDialogDismissedEvent, eventName, keepInBackground);
        }
        else if (eventName == "OnCmpCompletedEvent")
        {
            var errorProps = MaxSdkUtils.GetDictionaryFromDictionary(eventProps, "error");
            MaxCmpService.NotifyCompletedIfNeeded(errorProps);
        }
        else if (eventName == "OnApplicationStateChanged")
        {
            var isPaused = MaxSdkUtils.GetBoolFromDictionary(eventProps, "isPaused");
            InvokeEvent(_onApplicationStateChangedEvent, isPaused, eventName, keepInBackground);
        }
        // Ad Events
        else
        {
            var adInfo = new MaxSdkBase.AdInfo(eventProps);
            var adUnitIdentifier = MaxSdkUtils.GetStringFromDictionary(eventProps, "adUnitId", "");
            if (eventName == "OnBannerAdLoadedEvent")
            {
                InvokeEvent(_onBannerAdLoadedEvent, adUnitIdentifier, eventName, keepInBackground);
                InvokeEvent(_onBannerAdLoadedEventV2, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnBannerAdLoadFailedEvent")
            {
                var errorCode = MaxSdkUtils.GetIntFromDictionary(eventProps, "errorCode", -1);
                InvokeEvent(_onBannerAdLoadFailedEvent, adUnitIdentifier, errorCode, eventName, keepInBackground);

                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(_onBannerAdLoadFailedEventV2, adUnitIdentifier, errorInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnBannerAdClickedEvent")
            {
                InvokeEvent(_onBannerAdClickedEvent, adUnitIdentifier, eventName, keepInBackground);
                InvokeEvent(_onBannerAdClickedEventV2, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnBannerAdRevenuePaidEvent")
            {
                InvokeEvent(_onBannerAdRevenuePaidEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnBannerAdReviewCreativeIdGeneratedEvent")
            {
                var adReviewCreativeId = MaxSdkUtils.GetStringFromDictionary(eventProps, "adReviewCreativeId", "");
                InvokeEvent(_onBannerAdReviewCreativeIdGeneratedEvent, adUnitIdentifier, adReviewCreativeId, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnBannerAdExpandedEvent")
            {
                InvokeEvent(_onBannerAdExpandedEvent, adUnitIdentifier, eventName, keepInBackground);
                InvokeEvent(_onBannerAdExpandedEventV2, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnBannerAdCollapsedEvent")
            {
                InvokeEvent(_onBannerAdCollapsedEvent, adUnitIdentifier, eventName, keepInBackground);
                InvokeEvent(_onBannerAdCollapsedEventV2, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnMRecAdLoadedEvent")
            {
                InvokeEvent(_onMRecAdLoadedEvent, adUnitIdentifier, eventName, keepInBackground);
                InvokeEvent(_onMRecAdLoadedEventV2, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnMRecAdLoadFailedEvent")
            {
                var errorCode = MaxSdkUtils.GetIntFromDictionary(eventProps, "errorCode", -1);
                InvokeEvent(_onMRecAdLoadFailedEvent, adUnitIdentifier, errorCode, eventName, keepInBackground);

                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(_onMRecAdLoadFailedEventV2, adUnitIdentifier, errorInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnMRecAdClickedEvent")
            {
                InvokeEvent(_onMRecAdClickedEvent, adUnitIdentifier, eventName, keepInBackground);
                InvokeEvent(_onMRecAdClickedEventV2, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnMRecAdRevenuePaidEvent")
            {
                InvokeEvent(_onMRecAdRevenuePaidEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnMRecAdReviewCreativeIdGeneratedEvent")
            {
                var adReviewCreativeId = MaxSdkUtils.GetStringFromDictionary(eventProps, "adReviewCreativeId", "");
                InvokeEvent(_onMRecAdReviewCreativeIdGeneratedEvent, adUnitIdentifier, adReviewCreativeId, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnMRecAdExpandedEvent")
            {
                InvokeEvent(_onMRecAdExpandedEvent, adUnitIdentifier, eventName, keepInBackground);
                InvokeEvent(_onMRecAdExpandedEventV2, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnMRecAdCollapsedEvent")
            {
                InvokeEvent(_onMRecAdCollapsedEvent, adUnitIdentifier, eventName, keepInBackground);
                InvokeEvent(_onMRecAdCollapsedEventV2, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnInterstitialLoadedEvent")
            {
                InvokeEvent(_onInterstitialAdLoadedEvent, adUnitIdentifier, eventName, keepInBackground);
                InvokeEvent(_onInterstitialAdLoadedEventV2, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnInterstitialLoadFailedEvent")
            {
                var errorCode = MaxSdkUtils.GetIntFromDictionary(eventProps, "errorCode", -1);
                InvokeEvent(_onInterstitialLoadFailedEvent, adUnitIdentifier, errorCode, eventName, keepInBackground);

                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(_onInterstitialAdLoadFailedEventV2, adUnitIdentifier, errorInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnInterstitialHiddenEvent")
            {
                InvokeEvent(_onInterstitialAdHiddenEvent, adUnitIdentifier, eventName, keepInBackground);
                InvokeEvent(_onInterstitialAdHiddenEventV2, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnInterstitialDisplayedEvent")
            {
                InvokeEvent(_onInterstitialAdDisplayedEvent, adUnitIdentifier, eventName, keepInBackground);
                InvokeEvent(_onInterstitialAdDisplayedEventV2, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnInterstitialAdFailedToDisplayEvent")
            {
                var errorCode = MaxSdkUtils.GetIntFromDictionary(eventProps, "errorCode", -1);
                InvokeEvent(_onInterstitialAdFailedToDisplayEvent, adUnitIdentifier, errorCode, eventName, keepInBackground);

                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(_onInterstitialAdFailedToDisplayEventV2, adUnitIdentifier, errorInfo, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnInterstitialClickedEvent")
            {
                InvokeEvent(_onInterstitialAdClickedEvent, adUnitIdentifier, eventName, keepInBackground);
                InvokeEvent(_onInterstitialAdClickedEventV2, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnInterstitialAdRevenuePaidEvent")
            {
                InvokeEvent(_onInterstitialAdRevenuePaidEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnInterstitialAdReviewCreativeIdGeneratedEvent")
            {
                var adReviewCreativeId = MaxSdkUtils.GetStringFromDictionary(eventProps, "adReviewCreativeId", "");
                InvokeEvent(_onInterstitialAdReviewCreativeIdGeneratedEvent, adUnitIdentifier, adReviewCreativeId, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnAppOpenAdLoadedEvent")
            {
                InvokeEvent(_onAppOpenAdLoadedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnAppOpenAdLoadFailedEvent")
            {
                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(_onAppOpenAdLoadFailedEvent, adUnitIdentifier, errorInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnAppOpenAdHiddenEvent")
            {
                InvokeEvent(_onAppOpenAdHiddenEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnAppOpenAdDisplayedEvent")
            {
                InvokeEvent(_onAppOpenAdDisplayedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnAppOpenAdFailedToDisplayEvent")
            {
                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(_onAppOpenAdFailedToDisplayEvent, adUnitIdentifier, errorInfo, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnAppOpenAdClickedEvent")
            {
                InvokeEvent(_onAppOpenAdClickedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnAppOpenAdRevenuePaidEvent")
            {
                InvokeEvent(_onAppOpenAdRevenuePaidEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdLoadedEvent")
            {
                InvokeEvent(_onRewardedAdLoadedEvent, adUnitIdentifier, eventName, keepInBackground);
                InvokeEvent(_onRewardedAdLoadedEventV2, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdLoadFailedEvent")
            {
                var errorCode = MaxSdkUtils.GetIntFromDictionary(eventProps, "errorCode", -1);
                InvokeEvent(_onRewardedAdLoadFailedEvent, adUnitIdentifier, errorCode, eventName, keepInBackground);

                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(_onRewardedAdLoadFailedEventV2, adUnitIdentifier, errorInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdDisplayedEvent")
            {
                InvokeEvent(_onRewardedAdDisplayedEvent, adUnitIdentifier, eventName, keepInBackground);
                InvokeEvent(_onRewardedAdDisplayedEventV2, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdHiddenEvent")
            {
                InvokeEvent(_onRewardedAdHiddenEvent, adUnitIdentifier, eventName, keepInBackground);
                InvokeEvent(_onRewardedAdHiddenEventV2, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdClickedEvent")
            {
                InvokeEvent(_onRewardedAdClickedEvent, adUnitIdentifier, eventName, keepInBackground);
                InvokeEvent(_onRewardedAdClickedEventV2, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdRevenuePaidEvent")
            {
                InvokeEvent(_onRewardedAdRevenuePaidEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdReviewCreativeIdGeneratedEvent")
            {
                var adReviewCreativeId = MaxSdkUtils.GetStringFromDictionary(eventProps, "adReviewCreativeId", "");
                InvokeEvent(_onRewardedAdReviewCreativeIdGeneratedEvent, adUnitIdentifier, adReviewCreativeId, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdFailedToDisplayEvent")
            {
                var errorCode = MaxSdkUtils.GetIntFromDictionary(eventProps, "errorCode", -1);
                InvokeEvent(_onRewardedAdFailedToDisplayEvent, adUnitIdentifier, errorCode, eventName, keepInBackground);

                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(_onRewardedAdFailedToDisplayEventV2, adUnitIdentifier, errorInfo, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdReceivedRewardEvent")
            {
                var reward = new MaxSdkBase.Reward
                {
                    Label = MaxSdkUtils.GetStringFromDictionary(eventProps, "rewardLabel", ""),
                    Amount = MaxSdkUtils.GetIntFromDictionary(eventProps, "rewardAmount", 0)
                };

                InvokeEvent(_onRewardedAdReceivedRewardEvent, adUnitIdentifier, reward, eventName, keepInBackground);
                InvokeEvent(_onRewardedAdReceivedRewardEventV2, adUnitIdentifier, reward, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedInterstitialAdLoadedEvent")
            {
                InvokeEvent(_onRewardedInterstitialAdLoadedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedInterstitialAdLoadFailedEvent")
            {
                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);

                InvokeEvent(_onRewardedInterstitialAdLoadFailedEvent, adUnitIdentifier, errorInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedInterstitialAdDisplayedEvent")
            {
                InvokeEvent(_onRewardedInterstitialAdDisplayedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedInterstitialAdHiddenEvent")
            {
                InvokeEvent(_onRewardedInterstitialAdHiddenEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedInterstitialAdClickedEvent")
            {
                InvokeEvent(_onRewardedInterstitialAdClickedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedInterstitialAdRevenuePaidEvent")
            {
                InvokeEvent(_onRewardedInterstitialAdRevenuePaidEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedInterstitialAdReviewCreativeIdGeneratedEvent")
            {
                var adReviewCreativeId = MaxSdkUtils.GetStringFromDictionary(eventProps, "adReviewCreativeId", "");
                InvokeEvent(_onRewardedInterstitialAdReviewCreativeIdGeneratedEvent, adUnitIdentifier, adReviewCreativeId, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedInterstitialAdFailedToDisplayEvent")
            {
                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);

                InvokeEvent(_onRewardedInterstitialAdFailedToDisplayEvent, adUnitIdentifier, errorInfo, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedInterstitialAdReceivedRewardEvent")
            {
                var reward = new MaxSdkBase.Reward
                {
                    Label = MaxSdkUtils.GetStringFromDictionary(eventProps, "rewardLabel", ""),
                    Amount = MaxSdkUtils.GetIntFromDictionary(eventProps, "rewardAmount", 0)
                };

                InvokeEvent(_onRewardedInterstitialAdReceivedRewardEvent, adUnitIdentifier, reward, adInfo, eventName, keepInBackground);
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
        if (_onSdkInitializedEvent == null) return;

        _onSdkInitializedEvent(MaxSdkBase.SdkConfiguration.CreateEmpty());
    }
#endif

    private static void InvokeEvent(Action evt, string eventName, bool keepInBackground)
    {
        if (!CanInvokeEvent(evt)) return;

        MaxSdkLogger.UserDebug("Invoking event: " + eventName);
        if (ShouldInvokeInBackground(keepInBackground))
        {
            try
            {
                evt();
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Caught exception in publisher event: " + eventName + ", exception: " + exception);
                Debug.LogException(exception);
            }
        }
        else
        {
            MaxEventExecutor.ExecuteOnMainThread(evt, eventName);
        }
    }

    private static void InvokeEvent<T>(Action<T> evt, T param, string eventName, bool keepInBackground)
    {
        if (!CanInvokeEvent(evt)) return;

        MaxSdkLogger.UserDebug("Invoking event: " + eventName + ". Param: " + param);
        if (ShouldInvokeInBackground(keepInBackground))
        {
            try
            {
                evt(param);
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Caught exception in publisher event: " + eventName + ", exception: " + exception);
                Debug.LogException(exception);
            }
        }
        else
        {
            MaxEventExecutor.ExecuteOnMainThread(() => evt(param), eventName);
        }
    }

    private static void InvokeEvent<T1, T2>(Action<T1, T2> evt, T1 param1, T2 param2, string eventName, bool keepInBackground)
    {
        if (!CanInvokeEvent(evt)) return;

        MaxSdkLogger.UserDebug("Invoking event: " + eventName + ". Params: " + param1 + ", " + param2);
        if (ShouldInvokeInBackground(keepInBackground))
        {
            try
            {
                evt(param1, param2);
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Caught exception in publisher event: " + eventName + ", exception: " + exception);
                Debug.LogException(exception);
            }
        }
        else
        {
            MaxEventExecutor.ExecuteOnMainThread(() => evt(param1, param2), eventName);
        }
    }

    private static void InvokeEvent<T1, T2, T3>(Action<T1, T2, T3> evt, T1 param1, T2 param2, T3 param3, string eventName, bool keepInBackground)
    {
        if (!CanInvokeEvent(evt)) return;

        MaxSdkLogger.UserDebug("Invoking event: " + eventName + ". Params: " + param1 + ", " + param2 + ", " + param3);
        if (ShouldInvokeInBackground(keepInBackground))
        {
            try
            {
                evt(param1, param2, param3);
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Caught exception in publisher event: " + eventName + ", exception: " + exception);
                Debug.LogException(exception);
            }
        }
        else
        {
            MaxEventExecutor.ExecuteOnMainThread(() => evt(param1, param2, param3), eventName);
        }
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

    private static bool ShouldInvokeInBackground(bool keepInBackground)
    {
        return MaxSdkBase.InvokeEventsOnUnityMainThread == null ? keepInBackground : !MaxSdkBase.InvokeEventsOnUnityMainThread.Value;
    }

    private static void LogSubscribedToEvent(string eventName)
    {
        MaxSdkLogger.D("Listener has been added to callback: " + eventName);
    }

    private static void LogUnsubscribedToEvent(string eventName)
    {
        MaxSdkLogger.D("Listener has been removed from callback: " + eventName);
    }

#if UNITY_EDITOR && UNITY_2019_2_OR_NEWER
    /// <summary>
    /// Resets static event handlers so they still get reset even if Domain Reloading is disabled
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnDomainReload()
    {
        _onSdkInitializedEvent = null;
        _onSdkConsentDialogDismissedEvent = null;

        _onInterstitialAdLoadedEventV2 = null;
        _onInterstitialAdLoadFailedEventV2 = null;
        _onInterstitialAdDisplayedEventV2 = null;
        _onInterstitialAdFailedToDisplayEventV2 = null;
        _onInterstitialAdClickedEventV2 = null;
        _onInterstitialAdRevenuePaidEvent = null;
        _onInterstitialAdReviewCreativeIdGeneratedEvent = null;
        _onInterstitialAdHiddenEventV2 = null;

        _onAppOpenAdLoadedEvent = null;
        _onAppOpenAdLoadFailedEvent = null;
        _onAppOpenAdDisplayedEvent = null;
        _onAppOpenAdFailedToDisplayEvent = null;
        _onAppOpenAdClickedEvent = null;
        _onAppOpenAdRevenuePaidEvent = null;
        _onAppOpenAdHiddenEvent = null;

        _onRewardedAdLoadedEventV2 = null;
        _onRewardedAdLoadFailedEventV2 = null;
        _onRewardedAdDisplayedEventV2 = null;
        _onRewardedAdFailedToDisplayEventV2 = null;
        _onRewardedAdClickedEventV2 = null;
        _onRewardedAdRevenuePaidEvent = null;
        _onRewardedAdReviewCreativeIdGeneratedEvent = null;
        _onRewardedAdReceivedRewardEventV2 = null;
        _onRewardedAdHiddenEventV2 = null;

        _onRewardedInterstitialAdLoadedEvent = null;
        _onRewardedInterstitialAdLoadFailedEvent = null;
        _onRewardedInterstitialAdDisplayedEvent = null;
        _onRewardedInterstitialAdFailedToDisplayEvent = null;
        _onRewardedInterstitialAdClickedEvent = null;
        _onRewardedInterstitialAdRevenuePaidEvent = null;
        _onRewardedInterstitialAdReviewCreativeIdGeneratedEvent = null;
        _onRewardedInterstitialAdReceivedRewardEvent = null;
        _onRewardedInterstitialAdHiddenEvent = null;

        _onBannerAdLoadedEventV2 = null;
        _onBannerAdLoadFailedEventV2 = null;
        _onBannerAdClickedEventV2 = null;
        _onBannerAdRevenuePaidEvent = null;
        _onBannerAdReviewCreativeIdGeneratedEvent = null;
        _onBannerAdExpandedEventV2 = null;
        _onBannerAdCollapsedEventV2 = null;

        _onMRecAdLoadedEventV2 = null;
        _onMRecAdLoadFailedEventV2 = null;
        _onMRecAdClickedEventV2 = null;
        _onMRecAdRevenuePaidEvent = null;
        _onMRecAdReviewCreativeIdGeneratedEvent = null;
        _onMRecAdExpandedEventV2 = null;
        _onMRecAdCollapsedEventV2 = null;

        _onBannerAdLoadedEvent = null;
        _onBannerAdLoadFailedEvent = null;
        _onBannerAdClickedEvent = null;
        _onBannerAdExpandedEvent = null;
        _onBannerAdCollapsedEvent = null;

        _onMRecAdLoadedEvent = null;
        _onMRecAdLoadFailedEvent = null;
        _onMRecAdClickedEvent = null;
        _onMRecAdExpandedEvent = null;
        _onMRecAdCollapsedEvent = null;

        _onInterstitialAdLoadedEvent = null;
        _onInterstitialLoadFailedEvent = null;
        _onInterstitialAdDisplayedEvent = null;
        _onInterstitialAdFailedToDisplayEvent = null;
        _onInterstitialAdClickedEvent = null;
        _onInterstitialAdHiddenEvent = null;

        _onRewardedAdLoadedEvent = null;
        _onRewardedAdLoadFailedEvent = null;
        _onRewardedAdDisplayedEvent = null;
        _onRewardedAdFailedToDisplayEvent = null;
        _onRewardedAdClickedEvent = null;
        _onRewardedAdReceivedRewardEvent = null;
        _onRewardedAdHiddenEvent = null;
    }
#endif
}
