//
//  AppLovinBuildPreProcessor.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 8/27/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

#if UNITY_ANDROID

using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using AppLovinMax.ThirdParty.MiniJson;
using UnityEditor;
using UnityEditor.Build;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
using UnityEngine;

#endif

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    /// <summary>
    /// Adds the AppLovin Quality Service plugin to the gradle template file. See <see cref="AppLovinProcessGradleBuildFile"/> for more details.
    /// </summary>
    public class AppLovinPreProcessAndroid : AppLovinProcessGradleBuildFile,
#if UNITY_2018_1_OR_NEWER
        IPreprocessBuildWithReport
#else
        IPreprocessBuild
#endif
    {
        private const string AppLovinSettingsFileName = "applovin_settings.json";

        private const string KeyTermsFlowSettings = "terms_flow_settings";
        private const string KeyTermsFlowEnabled = "terms_flow_enabled";
        private const string KeyTermsFlowTermsOfService = "terms_flow_terms_of_service";
        private const string KeyTermsFlowPrivacyPolicy = "terms_flow_privacy_policy";

        private const string KeyConsentFlowSettings = "consent_flow_settings";
        private const string KeyConsentFlowEnabled = "consent_flow_enabled";
        private const string KeyConsentFlowTermsOfService = "consent_flow_terms_of_service";
        private const string KeyConsentFlowPrivacyPolicy = "consent_flow_privacy_policy";
        private const string KeyConsentFlowAdvertisingPartners = "consent_flow_advertising_partners";
        private const string KeyConsentFlowIncludeDefaultAdvertisingPartners = "consent_flow_should_include_default_advertising_partners";
        private const string KeyConsentFlowAnalyticsPartners = "consent_flow_analytics_partners";
        private const string KeyConsentFlowIncludeDefaultAnalyticsPartners = "consent_flow_should_include_default_analytics_partners";

#if UNITY_2018_1_OR_NEWER
        public void OnPreprocessBuild(BuildReport report)
#else
        public void OnPreprocessBuild(BuildTarget target, string path)
#endif
        {
            // We can only process gradle template file here. If it is not available, we will try again in post build on Unity IDEs newer than 2018_2 (see AppLovinPostProcessGradleProject).
            if (!AppLovinIntegrationManager.GradleTemplateEnabled) return;

#if UNITY_2019_3_OR_NEWER
            // The publisher could be migrating from older Unity versions to 2019_3 or newer.
            // If so, we should delete the plugin from the template. The plugin will be added to the project's application module in the post processing script (AppLovinPostProcessGradleProject).
            RemoveAppLovinQualityServiceOrSafeDkPlugin(AppLovinIntegrationManager.GradleTemplatePath);
#else
            AddAppLovinQualityServicePlugin(AppLovinIntegrationManager.GradleTemplatePath);
#endif

            // For Unity 2018.2 or newer, the consent flow is enabled in MaxPostProcessBuildAndroid.
#if !UNITY_2018_2_OR_NEWER
            if (AppLovinSettings.Instance.ShowInternalSettingsInIntegrationManager)
            {
                var consentFlowSettingsFilePath = Path.Combine("Assets", "Plugin/Android/res/raw/");
                EnableConsentFlowIfNeeded(consentFlowSettingsFilePath);
            }
#endif
        }

        public static void EnableConsentFlowIfNeeded(string rawResourceDirectory)
        {
            // Check if consent flow is enabled. No need to create the applovin_consent_flow_settings.json if consent flow is disabled.
            var consentFlowEnabled = AppLovinInternalSettings.Instance.ConsentFlowEnabled;
            if (!consentFlowEnabled) return;

            var privacyPolicyUrl = AppLovinInternalSettings.Instance.ConsentFlowPrivacyPolicyUrl;
            if (string.IsNullOrEmpty(privacyPolicyUrl))
            {
                AppLovinIntegrationManager.ShowBuildFailureDialog("You cannot use the AppLovin SDK's consent flow without defining a Privacy Policy URL in the AppLovin Integration Manager.");

                // No need to update the applovin_consent_flow_settings.json here. Default consent flow state will be determined on the SDK side.
                return;
            }

            var consentFlowSettings = new Dictionary<string, object>();
            consentFlowSettings[KeyConsentFlowEnabled] = consentFlowEnabled;
            consentFlowSettings[KeyConsentFlowPrivacyPolicy] = privacyPolicyUrl;

            var termsOfServiceUrl = AppLovinInternalSettings.Instance.ConsentFlowTermsOfServiceUrl;
            if (MaxSdkUtils.IsValidString(termsOfServiceUrl))
            {
                consentFlowSettings[KeyConsentFlowTermsOfService] = termsOfServiceUrl;
            }

            consentFlowSettings[KeyConsentFlowIncludeDefaultAdvertisingPartners] = AppLovinInternalSettings.Instance.ConsentFlowIncludeDefaultAdvertisingPartnerUrls;
            var advertisingPartnerUrls = AppLovinInternalSettings.Instance.ConsentFlowAdvertisingPartnerUrls;
            if (MaxSdkUtils.IsValidString(advertisingPartnerUrls))
            {
                var advertisingPartnerUrlsList = advertisingPartnerUrls.Split(',').ToList();
                consentFlowSettings[KeyConsentFlowAdvertisingPartners] = advertisingPartnerUrlsList;
            }

            consentFlowSettings[KeyConsentFlowIncludeDefaultAnalyticsPartners] = AppLovinInternalSettings.Instance.ConsentFlowIncludeDefaultAnalyticsPartnerUrls;
            var analyticsPartnerUrls = AppLovinInternalSettings.Instance.ConsentFlowAnalyticsPartnerUrls;
            if (MaxSdkUtils.IsValidString(analyticsPartnerUrls))
            {
                var analyticsPartnerUrlsList = analyticsPartnerUrls.Split(',').ToList();
                consentFlowSettings[KeyConsentFlowAnalyticsPartners] = analyticsPartnerUrlsList;
            }

            var applovinSdkSettings = new Dictionary<string, object>();
            applovinSdkSettings[KeyConsentFlowSettings] = consentFlowSettings;

            var applovinSdkSettingsJson = Json.Serialize(applovinSdkSettings);
            WriteAppLovinSettingsRawResourceFile(applovinSdkSettingsJson, rawResourceDirectory);
        }

        public static void EnableTermsFlowIfNeeded(string rawResourceDirectory)
        {
            if (AppLovinSettings.Instance.ShowInternalSettingsInIntegrationManager) return;

            // Check if terms flow is enabled. No need to create the applovin_consent_flow_settings.json if consent flow is disabled.
            var consentFlowEnabled = AppLovinSettings.Instance.ConsentFlowEnabled;
            if (!consentFlowEnabled) return;

            // Check if terms flow is enabled for this format.
            var consentFlowPlatform = AppLovinSettings.Instance.ConsentFlowPlatform;
            if (consentFlowPlatform != Platform.All && consentFlowPlatform != Platform.Android) return;

            var privacyPolicyUrl = AppLovinSettings.Instance.ConsentFlowPrivacyPolicyUrl;
            if (string.IsNullOrEmpty(privacyPolicyUrl))
            {
                AppLovinIntegrationManager.ShowBuildFailureDialog("You cannot use the AppLovin SDK's consent flow without defining a Privacy Policy URL in the AppLovin Integration Manager.");

                // No need to update the applovin_consent_flow_settings.json here. Default consent flow state will be determined on the SDK side.
                return;
            }

            var consentFlowSettings = new Dictionary<string, object>();
            consentFlowSettings[KeyTermsFlowEnabled] = consentFlowEnabled;
            consentFlowSettings[KeyTermsFlowPrivacyPolicy] = privacyPolicyUrl;

            var termsOfServiceUrl = AppLovinSettings.Instance.ConsentFlowTermsOfServiceUrl;
            if (MaxSdkUtils.IsValidString(termsOfServiceUrl))
            {
                consentFlowSettings[KeyTermsFlowTermsOfService] = termsOfServiceUrl;
            }

            var applovinSdkSettings = new Dictionary<string, object>();
            applovinSdkSettings[KeyTermsFlowSettings] = consentFlowSettings;

            var applovinSdkSettingsJson = Json.Serialize(applovinSdkSettings);
            WriteAppLovinSettingsRawResourceFile(applovinSdkSettingsJson, rawResourceDirectory);
        }

        private static void WriteAppLovinSettingsRawResourceFile(string applovinSdkSettingsJson, string rawResourceDirectory)
        {
            if (!Directory.Exists(rawResourceDirectory))
            {
                Directory.CreateDirectory(rawResourceDirectory);
            }

            var consentFlowSettingsFilePath = Path.Combine(rawResourceDirectory, AppLovinSettingsFileName);
            try
            {
                File.WriteAllText(consentFlowSettingsFilePath, applovinSdkSettingsJson + "\n");
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("applovin_settings.json file write failed due to: " + exception.Message);
                Console.WriteLine(exception);
            }
        }

        public int callbackOrder
        {
            get { return int.MaxValue; }
        }
    }
}

#endif
