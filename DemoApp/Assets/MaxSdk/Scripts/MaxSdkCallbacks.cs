// ReSharper disable RedundantArgumentDefaultValue

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using AppLovinMax.ThirdParty.MiniJson;
using AppLovinMax.Internal;

/// <summary>
/// This is is a global Unity object that is used to forward callbacks from native iOS / Android Max code to the application.
/// </summary>
public static class MaxSdkCallbacks
{
    /// <summary>
    /// Fired when the SDK has finished initializing
    /// </summary>
    private static Action<MaxSdkBase.SdkConfiguration> onSdkInitializedEvent;
    public static event Action<MaxSdkBase.SdkConfiguration> OnSdkInitializedEvent
    {
        add
        {
            LogSubscribedToEvent("OnSdkInitializedEvent");
            onSdkInitializedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnSdkInitializedEvent");
            onSdkInitializedEvent -= value;
        }
    }

    /// <summary>
    /// Fired when the application is paused or resumed.
    /// </summary>
    private static Action<bool> onApplicationStateChangedEvent;
    public static event Action<bool> OnApplicationStateChangedEvent
    {
        add
        {
            LogSubscribedToEvent("OnApplicationStateChangedEvent");
            onApplicationStateChangedEvent += value;
        }
        remove
        {
            LogUnsubscribedToEvent("OnApplicationStateChangedEvent");
            onApplicationStateChangedEvent -= value;
        }
    }

    public static class Interstitial
    {
        internal static Action<string, MaxSdkBase.AdInfo> onAdLoadedEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdLoadedEvent
        {
            add
            {
                LogSubscribedToEvent("OnInterstitialAdLoadedEvent");
                onAdLoadedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnInterstitialAdLoadedEvent");
                onAdLoadedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.ErrorInfo> onAdLoadFailedEvent;
        public static event Action<string, MaxSdkBase.ErrorInfo> OnAdLoadFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnInterstitialAdLoadFailedEvent");
                onAdLoadFailedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnInterstitialAdLoadFailedEvent");
                onAdLoadFailedEvent -= value;
            }
        }

        /// <summary>
        /// Fired when an interstitial ad is displayed (may not be received by Unity until the interstitial ad closes).
        /// </summary>
        internal static Action<string, MaxSdkBase.AdInfo> onAdDisplayedEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdDisplayedEvent
        {
            add
            {
                LogSubscribedToEvent("OnInterstitialAdDisplayedEvent");
                onAdDisplayedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnInterstitialAdDisplayedEvent");
                onAdDisplayedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> onAdDisplayFailedEvent;
        public static event Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> OnAdDisplayFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnInterstitialAdDisplayFailedEvent");
                onAdDisplayFailedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnInterstitialAdDisplayFailedEvent");
                onAdDisplayFailedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.AdInfo> onAdClickedEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdClickedEvent
        {
            add
            {
                LogSubscribedToEvent("OnInterstitialAdClickedEvent");
                onAdClickedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnInterstitialAdClickedEvent");
                onAdClickedEvent -= value;
            }
        }

        /// <summary>
        /// Fired when an interstitial ad impression was validated and revenue will be paid.
        /// Executed on a background thread to avoid any delays in execution.
        /// </summary>
        internal static Action<string, MaxSdkBase.AdInfo> onAdRevenuePaidEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdRevenuePaidEvent
        {
            add
            {
                LogSubscribedToEvent("OnInterstitialAdRevenuePaidEvent");
                onAdRevenuePaidEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnInterstitialAdRevenuePaidEvent");
                onAdRevenuePaidEvent -= value;
            }
        }

        /// <summary>
        /// Fired when an expired interstitial ad is reloaded.
        /// </summary>
        internal static Action<string, MaxSdkBase.AdInfo, MaxSdkBase.AdInfo> onExpiredAdReloadedEvent;
        public static event Action<string, MaxSdkBase.AdInfo, MaxSdkBase.AdInfo> OnExpiredAdReloadedEvent
        {
            add
            {
                LogSubscribedToEvent("OnExpiredInterstitialAdReloadedEvent");
                onExpiredAdReloadedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnExpiredInterstitialAdReloadedEvent");
                onExpiredAdReloadedEvent -= value;
            }
        }

        /// <summary>
        /// Fired when an Ad Review Creative ID has been generated.
        /// The parameters returned are the adUnitIdentifier, adReviewCreativeId, and adInfo in that respective order.
        /// Executed on a background thread to avoid any delays in execution.
        /// </summary>
        internal static Action<string, string, MaxSdkBase.AdInfo> onAdReviewCreativeIdGeneratedEvent;
        public static event Action<string, string, MaxSdkBase.AdInfo> OnAdReviewCreativeIdGeneratedEvent
        {
            add
            {
                LogSubscribedToEvent("OnInterstitialAdReviewCreativeIdGeneratedEvent");
                onAdReviewCreativeIdGeneratedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnInterstitialAdReviewCreativeIdGeneratedEvent");
                onAdReviewCreativeIdGeneratedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.AdInfo> onAdHiddenEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdHiddenEvent
        {
            add
            {
                LogSubscribedToEvent("OnInterstitialAdHiddenEvent");
                onAdHiddenEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnInterstitialAdHiddenEvent");
                onAdHiddenEvent -= value;
            }
        }
    }

    public static class AppOpen
    {
        internal static Action<string, MaxSdkBase.AdInfo> onAdLoadedEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdLoadedEvent
        {
            add
            {
                LogSubscribedToEvent("OnAppOpenAdLoadedEvent");
                onAdLoadedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnAppOpenAdLoadedEvent");
                onAdLoadedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.ErrorInfo> onAdLoadFailedEvent;
        public static event Action<string, MaxSdkBase.ErrorInfo> OnAdLoadFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnAppOpenAdLoadFailedEvent");
                onAdLoadFailedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnAppOpenAdLoadFailedEvent");
                onAdLoadFailedEvent -= value;
            }
        }

        /// <summary>
        /// Fired when an app open ad is displayed (may not be received by Unity until the app open ad closes).
        /// </summary>
        internal static Action<string, MaxSdkBase.AdInfo> onAdDisplayedEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdDisplayedEvent
        {
            add
            {
                LogSubscribedToEvent("OnAppOpenAdDisplayedEvent");
                onAdDisplayedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnAppOpenAdDisplayedEvent");
                onAdDisplayedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> onAdDisplayFailedEvent;
        public static event Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> OnAdDisplayFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnAppOpenAdDisplayFailedEvent");
                onAdDisplayFailedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnAppOpenAdDisplayFailedEvent");
                onAdDisplayFailedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.AdInfo> onAdClickedEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdClickedEvent
        {
            add
            {
                LogSubscribedToEvent("OnAppOpenAdClickedEvent");
                onAdClickedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnAppOpenAdClickedEvent");
                onAdClickedEvent -= value;
            }
        }

        /// <summary>
        /// Fired when an app open ad impression was validated and revenue will be paid.
        /// Executed on a background thread to avoid any delays in execution.
        /// </summary>
        internal static Action<string, MaxSdkBase.AdInfo> onAdRevenuePaidEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdRevenuePaidEvent
        {
            add
            {
                LogSubscribedToEvent("OnAppOpenAdRevenuePaidEvent");
                onAdRevenuePaidEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnAppOpenAdRevenuePaidEvent");
                onAdRevenuePaidEvent -= value;
            }
        }

        /// <summary>
        /// Fired when an expired app open ad is reloaded.
        /// </summary>
        internal static Action<string, MaxSdkBase.AdInfo, MaxSdkBase.AdInfo> onExpiredAdReloadedEvent;
        public static event Action<string, MaxSdkBase.AdInfo, MaxSdkBase.AdInfo> OnExpiredAdReloadedEvent
        {
            add
            {
                LogSubscribedToEvent("OnExpiredAppOpenAdReloadedEvent");
                onExpiredAdReloadedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnExpiredAppOpenAdReloadedEvent");
                onExpiredAdReloadedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.AdInfo> onAdHiddenEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdHiddenEvent
        {
            add
            {
                LogSubscribedToEvent("OnAppOpenAdHiddenEvent");
                onAdHiddenEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnAppOpenAdHiddenEvent");
                onAdHiddenEvent -= value;
            }
        }
    }
    public static class Rewarded
    {
        internal static Action<string, MaxSdkBase.AdInfo> onAdLoadedEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdLoadedEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdLoadedEvent");
                onAdLoadedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdLoadedEvent");
                onAdLoadedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.ErrorInfo> onAdLoadFailedEvent;
        public static event Action<string, MaxSdkBase.ErrorInfo> OnAdLoadFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdLoadFailedEvent");
                onAdLoadFailedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdLoadFailedEvent");
                onAdLoadFailedEvent -= value;
            }
        }

        /// <summary>
        ///Fired when a rewarded ad is displayed (may not be received by Unity until the rewarded ad closes).
        /// </summary>
        internal static Action<string, MaxSdkBase.AdInfo> onAdDisplayedEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdDisplayedEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdDisplayedEvent");
                onAdDisplayedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdDisplayedEvent");
                onAdDisplayedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> onAdDisplayFailedEvent;
        public static event Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> OnAdDisplayFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdDisplayFailedEvent");
                onAdDisplayFailedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdDisplayFailedEvent");
                onAdDisplayFailedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.AdInfo> onAdClickedEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdClickedEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdClickedEvent");
                onAdClickedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdClickedEvent");
                onAdClickedEvent -= value;
            }
        }

        /// <summary>
        /// Fired when a rewarded ad impression was validated and revenue will be paid.
        /// Executed on a background thread to avoid any delays in execution.
        /// </summary>
        internal static Action<string, MaxSdkBase.AdInfo> onAdRevenuePaidEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdRevenuePaidEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdRevenuePaidEvent");
                onAdRevenuePaidEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdRevenuePaidEvent");
                onAdRevenuePaidEvent -= value;
            }
        }

        /// <summary>
        /// Fired when an expired rewarded ad is reloaded.
        /// </summary>
        internal static Action<string, MaxSdkBase.AdInfo, MaxSdkBase.AdInfo> onExpiredAdReloadedEvent;
        public static event Action<string, MaxSdkBase.AdInfo, MaxSdkBase.AdInfo> OnExpiredAdReloadedEvent
        {
            add
            {
                LogSubscribedToEvent("OnExpiredRewardedAdReloadedEvent");
                onExpiredAdReloadedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnExpiredRewardedAdReloadedEvent");
                onExpiredAdReloadedEvent -= value;
            }
        }

        /// <summary>
        /// Fired when an Ad Review Creative ID has been generated.
        /// </summary>
        internal static Action<string, string, MaxSdkBase.AdInfo> onAdReviewCreativeIdGeneratedEvent;
        public static event Action<string, string, MaxSdkBase.AdInfo> OnAdReviewCreativeIdGeneratedEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdReviewCreativeIdGeneratedEvent");
                onAdReviewCreativeIdGeneratedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdReviewCreativeIdGeneratedEvent");
                onAdReviewCreativeIdGeneratedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.Reward, MaxSdkBase.AdInfo> onAdReceivedRewardEvent;
        public static event Action<string, MaxSdkBase.Reward, MaxSdkBase.AdInfo> OnAdReceivedRewardEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdReceivedRewardEvent");
                onAdReceivedRewardEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdReceivedRewardEvent");
                onAdReceivedRewardEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.AdInfo> onAdHiddenEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdHiddenEvent
        {
            add
            {
                LogSubscribedToEvent("OnRewardedAdHiddenEvent");
                onAdHiddenEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnRewardedAdHiddenEvent");
                onAdHiddenEvent -= value;
            }
        }
    }
    public static class Banner
    {
        internal static Action<string, MaxSdkBase.AdInfo> onAdLoadedEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdLoadedEvent
        {
            add
            {
                LogSubscribedToEvent("OnBannerAdLoadedEvent");
                onAdLoadedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnBannerAdLoadedEvent");
                onAdLoadedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.ErrorInfo> onAdLoadFailedEvent;
        public static event Action<string, MaxSdkBase.ErrorInfo> OnAdLoadFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnBannerAdLoadFailedEvent");
                onAdLoadFailedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnBannerAdLoadFailedEvent");
                onAdLoadFailedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.AdInfo> onAdClickedEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdClickedEvent
        {
            add
            {
                LogSubscribedToEvent("OnBannerAdClickedEvent");
                onAdClickedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnBannerAdClickedEvent");
                onAdClickedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.AdInfo> onAdRevenuePaidEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdRevenuePaidEvent
        {
            add
            {
                LogSubscribedToEvent("OnBannerAdRevenuePaidEvent");
                onAdRevenuePaidEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnBannerAdRevenuePaidEvent");
                onAdRevenuePaidEvent -= value;
            }
        }

        internal static Action<string, string, MaxSdkBase.AdInfo> onAdReviewCreativeIdGeneratedEvent;
        public static event Action<string, string, MaxSdkBase.AdInfo> OnAdReviewCreativeIdGeneratedEvent
        {
            add
            {
                LogSubscribedToEvent("OnBannerAdReviewCreativeIdGeneratedEvent");
                onAdReviewCreativeIdGeneratedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnBannerAdReviewCreativeIdGeneratedEvent");
                onAdReviewCreativeIdGeneratedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.AdInfo> onAdExpandedEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdExpandedEvent
        {
            add
            {
                LogSubscribedToEvent("OnBannerAdExpandedEvent");
                onAdExpandedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnBannerAdExpandedEvent");
                onAdExpandedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.AdInfo> onAdCollapsedEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdCollapsedEvent
        {
            add
            {
                LogSubscribedToEvent("OnBannerAdCollapsedEvent");
                onAdCollapsedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnBannerAdCollapsedEvent");
                onAdCollapsedEvent -= value;
            }
        }
    }

    public static class MRec
    {
        internal static Action<string, MaxSdkBase.AdInfo> onAdLoadedEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdLoadedEvent
        {
            add
            {
                LogSubscribedToEvent("OnMRecAdLoadedEvent");
                onAdLoadedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnMRecAdLoadedEvent");
                onAdLoadedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.ErrorInfo> onAdLoadFailedEvent;
        public static event Action<string, MaxSdkBase.ErrorInfo> OnAdLoadFailedEvent
        {
            add
            {
                LogSubscribedToEvent("OnMRecAdLoadFailedEvent");
                onAdLoadFailedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnMRecAdLoadFailedEvent");
                onAdLoadFailedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.AdInfo> onAdClickedEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdClickedEvent
        {
            add
            {
                LogSubscribedToEvent("OnMRecAdClickedEvent");
                onAdClickedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnMRecAdClickedEvent");
                onAdClickedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.AdInfo> onAdRevenuePaidEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdRevenuePaidEvent
        {
            add
            {
                LogSubscribedToEvent("OnMRecAdRevenuePaidEvent");
                onAdRevenuePaidEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnMRecAdRevenuePaidEvent");
                onAdRevenuePaidEvent -= value;
            }
        }

        /// <summary>
        /// Fired when an Ad Review Creative ID has been generated.
        /// </summary>
        internal static Action<string, string, MaxSdkBase.AdInfo> onAdReviewCreativeIdGeneratedEvent;
        public static event Action<string, string, MaxSdkBase.AdInfo> OnAdReviewCreativeIdGeneratedEvent
        {
            add
            {
                LogSubscribedToEvent("OnMRecAdReviewCreativeIdGeneratedEvent");
                onAdReviewCreativeIdGeneratedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnMRecAdReviewCreativeIdGeneratedEvent");
                onAdReviewCreativeIdGeneratedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.AdInfo> onAdExpandedEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdExpandedEvent
        {
            add
            {
                LogSubscribedToEvent("OnMRecAdExpandedEvent");
                onAdExpandedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnMRecAdExpandedEvent");
                onAdExpandedEvent -= value;
            }
        }

        internal static Action<string, MaxSdkBase.AdInfo> onAdCollapsedEvent;
        public static event Action<string, MaxSdkBase.AdInfo> OnAdCollapsedEvent
        {
            add
            {
                LogSubscribedToEvent("OnMRecAdCollapsedEvent");
                onAdCollapsedEvent += value;
            }
            remove
            {
                LogUnsubscribedToEvent("OnMRecAdCollapsedEvent");
                onAdCollapsedEvent -= value;
            }
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
            InvokeEvent(onSdkInitializedEvent, sdkConfiguration, eventName, keepInBackground);
        }
        else if (eventName == "OnCmpCompletedEvent")
        {
            var errorProps = MaxSdkUtils.GetDictionaryFromDictionary(eventProps, "error");
            MaxCmpService.NotifyCompletedIfNeeded(errorProps);
        }
        else if (eventName == "OnApplicationStateChanged")
        {
            var isPaused = MaxSdkUtils.GetBoolFromDictionary(eventProps, "isPaused");
            InvokeEvent(onApplicationStateChangedEvent, isPaused, eventName, keepInBackground);
        }
        // Ad Events
        else
        {
            var isExpiredAdReloadedEvent = Regex.IsMatch(eventName, @"^OnExpired\w+AdReloadedEvent$");
            var adInfoEventProps = isExpiredAdReloadedEvent ? MaxSdkUtils.GetDictionaryFromDictionary(eventProps, "newAdInfo") : eventProps;
            var adInfo = new MaxSdkBase.AdInfo(adInfoEventProps);
            var adUnitIdentifier = MaxSdkUtils.GetStringFromDictionary(adInfoEventProps, "adUnitId", "");

            // Expired ad reloaded callbacks pass down multiple adInfo objects
            if (isExpiredAdReloadedEvent)
            {
                var expiredAdInfo = new MaxSdkBase.AdInfo(MaxSdkUtils.GetDictionaryFromDictionary(eventProps, "expiredAdInfo"));
                if (eventName == "OnExpiredInterstitialAdReloadedEvent")
                {
                    InvokeEvent(Interstitial.onExpiredAdReloadedEvent, adUnitIdentifier, expiredAdInfo, adInfo, eventName, keepInBackground);
                }
                else if (eventName == "OnExpiredAppOpenAdReloadedEvent")
                {
                    InvokeEvent(AppOpen.onExpiredAdReloadedEvent, adUnitIdentifier, expiredAdInfo, adInfo, eventName, keepInBackground);
                }
                else if (eventName == "OnExpiredRewardedAdReloadedEvent")
                {
                    InvokeEvent(Rewarded.onExpiredAdReloadedEvent, adUnitIdentifier, expiredAdInfo, adInfo, eventName, keepInBackground);
                }
                else
                {
                    MaxSdkLogger.UserWarning("Unknown MAX Ads event fired: " + eventName);
                }
            }
            else if (eventName == "OnBannerAdLoadedEvent")
            {
                InvokeEvent(Banner.onAdLoadedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnBannerAdLoadFailedEvent")
            {
                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(Banner.onAdLoadFailedEvent, adUnitIdentifier, errorInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnBannerAdClickedEvent")
            {
                InvokeEvent(Banner.onAdClickedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnBannerAdRevenuePaidEvent")
            {
                InvokeEvent(Banner.onAdRevenuePaidEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnBannerAdReviewCreativeIdGeneratedEvent")
            {
                var adReviewCreativeId = MaxSdkUtils.GetStringFromDictionary(eventProps, "adReviewCreativeId", "");
                InvokeEvent(Banner.onAdReviewCreativeIdGeneratedEvent, adUnitIdentifier, adReviewCreativeId, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnBannerAdExpandedEvent")
            {
                InvokeEvent(Banner.onAdExpandedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnBannerAdCollapsedEvent")
            {
                InvokeEvent(Banner.onAdCollapsedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnMRecAdLoadedEvent")
            {
                InvokeEvent(MRec.onAdLoadedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnMRecAdLoadFailedEvent")
            {
                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(MRec.onAdLoadFailedEvent, adUnitIdentifier, errorInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnMRecAdClickedEvent")
            {
                InvokeEvent(MRec.onAdClickedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnMRecAdRevenuePaidEvent")
            {
                InvokeEvent(MRec.onAdRevenuePaidEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnMRecAdReviewCreativeIdGeneratedEvent")
            {
                var adReviewCreativeId = MaxSdkUtils.GetStringFromDictionary(eventProps, "adReviewCreativeId", "");
                InvokeEvent(MRec.onAdReviewCreativeIdGeneratedEvent, adUnitIdentifier, adReviewCreativeId, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnMRecAdExpandedEvent")
            {
                InvokeEvent(MRec.onAdExpandedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnMRecAdCollapsedEvent")
            {
                InvokeEvent(MRec.onAdCollapsedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnInterstitialLoadedEvent")
            {
                InvokeEvent(Interstitial.onAdLoadedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnInterstitialLoadFailedEvent")
            {
                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(Interstitial.onAdLoadFailedEvent, adUnitIdentifier, errorInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnInterstitialHiddenEvent")
            {
                InvokeEvent(Interstitial.onAdHiddenEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnInterstitialDisplayedEvent")
            {
                InvokeEvent(Interstitial.onAdDisplayedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnInterstitialAdFailedToDisplayEvent")
            {
                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(Interstitial.onAdDisplayFailedEvent, adUnitIdentifier, errorInfo, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnInterstitialClickedEvent")
            {
                InvokeEvent(Interstitial.onAdClickedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnInterstitialAdRevenuePaidEvent")
            {
                InvokeEvent(Interstitial.onAdRevenuePaidEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnInterstitialAdReviewCreativeIdGeneratedEvent")
            {
                var adReviewCreativeId = MaxSdkUtils.GetStringFromDictionary(eventProps, "adReviewCreativeId", "");
                InvokeEvent(Interstitial.onAdReviewCreativeIdGeneratedEvent, adUnitIdentifier, adReviewCreativeId, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnAppOpenAdLoadedEvent")
            {
                InvokeEvent(AppOpen.onAdLoadedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnAppOpenAdLoadFailedEvent")
            {
                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(AppOpen.onAdLoadFailedEvent, adUnitIdentifier, errorInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnAppOpenAdHiddenEvent")
            {
                InvokeEvent(AppOpen.onAdHiddenEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnAppOpenAdDisplayedEvent")
            {
                InvokeEvent(AppOpen.onAdDisplayedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnAppOpenAdFailedToDisplayEvent")
            {
                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(AppOpen.onAdDisplayFailedEvent, adUnitIdentifier, errorInfo, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnAppOpenAdClickedEvent")
            {
                InvokeEvent(AppOpen.onAdClickedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnAppOpenAdRevenuePaidEvent")
            {
                InvokeEvent(AppOpen.onAdRevenuePaidEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdLoadedEvent")
            {
                InvokeEvent(Rewarded.onAdLoadedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdLoadFailedEvent")
            {
                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(Rewarded.onAdLoadFailedEvent, adUnitIdentifier, errorInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdDisplayedEvent")
            {
                InvokeEvent(Rewarded.onAdDisplayedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdHiddenEvent")
            {
                InvokeEvent(Rewarded.onAdHiddenEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdClickedEvent")
            {
                InvokeEvent(Rewarded.onAdClickedEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdRevenuePaidEvent")
            {
                InvokeEvent(Rewarded.onAdRevenuePaidEvent, adUnitIdentifier, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdReviewCreativeIdGeneratedEvent")
            {
                var adReviewCreativeId = MaxSdkUtils.GetStringFromDictionary(eventProps, "adReviewCreativeId", "");
                InvokeEvent(Rewarded.onAdReviewCreativeIdGeneratedEvent, adUnitIdentifier, adReviewCreativeId, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdFailedToDisplayEvent")
            {
                var errorInfo = new MaxSdkBase.ErrorInfo(eventProps);
                InvokeEvent(Rewarded.onAdDisplayFailedEvent, adUnitIdentifier, errorInfo, adInfo, eventName, keepInBackground);
            }
            else if (eventName == "OnRewardedAdReceivedRewardEvent")
            {
                var reward = new MaxSdkBase.Reward
                {
                    Label = MaxSdkUtils.GetStringFromDictionary(eventProps, "rewardLabel", ""),
                    Amount = MaxSdkUtils.GetIntFromDictionary(eventProps, "rewardAmount", 0)
                };

                InvokeEvent(Rewarded.onAdReceivedRewardEvent, adUnitIdentifier, reward, adInfo, eventName, keepInBackground);
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
        if (onSdkInitializedEvent == null) return;

        onSdkInitializedEvent(MaxSdkBase.SdkConfiguration.CreateEmpty());
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
                MaxSdkLogger.LogException(exception);
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
                MaxSdkLogger.LogException(exception);
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
                MaxSdkLogger.LogException(exception);
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
                MaxSdkLogger.LogException(exception);
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
        onSdkInitializedEvent = null;

        Interstitial.onAdLoadedEvent = null;
        Interstitial.onAdLoadFailedEvent = null;
        Interstitial.onAdDisplayedEvent = null;
        Interstitial.onAdDisplayFailedEvent = null;
        Interstitial.onAdClickedEvent = null;
        Interstitial.onAdRevenuePaidEvent = null;
        Interstitial.onAdReviewCreativeIdGeneratedEvent = null;
        Interstitial.onAdHiddenEvent = null;

        AppOpen.onAdLoadedEvent = null;
        AppOpen.onAdLoadFailedEvent = null;
        AppOpen.onAdDisplayedEvent = null;
        AppOpen.onAdDisplayFailedEvent = null;
        AppOpen.onAdClickedEvent = null;
        AppOpen.onAdRevenuePaidEvent = null;
        AppOpen.onAdHiddenEvent = null;

        Rewarded.onAdLoadedEvent = null;
        Rewarded.onAdLoadFailedEvent = null;
        Rewarded.onAdDisplayedEvent = null;
        Rewarded.onAdDisplayFailedEvent = null;
        Rewarded.onAdClickedEvent = null;
        Rewarded.onAdRevenuePaidEvent = null;
        Rewarded.onAdReviewCreativeIdGeneratedEvent = null;
        Rewarded.onAdReceivedRewardEvent = null;
        Rewarded.onAdHiddenEvent = null;

        Banner.onAdLoadedEvent = null;
        Banner.onAdLoadFailedEvent = null;
        Banner.onAdClickedEvent = null;
        Banner.onAdRevenuePaidEvent = null;
        Banner.onAdReviewCreativeIdGeneratedEvent = null;
        Banner.onAdExpandedEvent = null;
        Banner.onAdCollapsedEvent = null;

        MRec.onAdLoadedEvent = null;
        MRec.onAdLoadFailedEvent = null;
        MRec.onAdClickedEvent = null;
        MRec.onAdRevenuePaidEvent = null;
        MRec.onAdReviewCreativeIdGeneratedEvent = null;
        MRec.onAdExpandedEvent = null;
        MRec.onAdCollapsedEvent = null;

    }
#endif
}
