//
//  AppLovinAutoUpdater.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 1/27/20.
//  Copyright © 2020 AppLovin. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    /// <summary>
    /// Handles auto updates for AppLovin MAX plugin.
    /// </summary>
    public class AppLovinAutoUpdater
    {
        public const string KeyAutoUpdateEnabled = "com.applovin.auto_update_enabled";
        private const string KeyLastUpdateCheckTime = "com.applovin.last_update_check_time_v2"; // Updated to v2 to force adapter version checks in plugin version 3.1.10.
        private static readonly DateTime EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly int SecondsInADay = (int) TimeSpan.FromDays(1).TotalSeconds;

        // TODO: Make this list dynamic.
        public static readonly Dictionary<string, string> MinAdapterVersions = new Dictionary<string, string>()
        {
            {"ADMOB_NETWORK", "android_23.3.0.1_ios_11.9.0.1"},
            {"BIDMACHINE_NETWORK", "android_3.0.1.1_ios_3.0.0.0.1"},
            {"CHARTBOOST_NETWORK", "android_9.7.0.3_ios_9.7.0.2"},
            {"FACEBOOK_MEDIATE", "android_6.17.0.1_ios_6.15.2.1"},
            {"FYBER_NETWORK", "android_8.3.1.1_ios_8.3.2.1"},
            {"GOOGLE_AD_MANAGER_NETWORK", "android_23.3.0.1_ios_11.9.0.1"},
            {"HYPRMX_NETWORK", "android_6.4.2.1_ios_6.4.1.0.1"},
            {"INMOBI_NETWORK", "android_10.7.7.1_ios_10.7.5.1"},
            {"IRONSOURCE_NETWORK", "android_8.3.0.0.2_ios_8.3.0.0.1"},
            {"LINE_NETWORK", "android_2024.8.27.1_ios_2.8.20240827.1"},
            {"MINTEGRAL_NETWORK", "android_16.8.51.1_ios_7.7.2.0.1"},
            {"MOBILEFUSE_NETWORK", "android_1.7.6.1_ios_1.7.6.1"},
            {"MOLOCO_NETWORK", "android_3.1.0.1_ios_3.1.3.1"},
            {"MYTARGET_NETWORK", "android_5.22.1.1_ios_5.21.7.1"},
            {"PUBMATIC_NETWORK", "android_3.9.0.2_ios_3.9.0.2"},
            {"SMAATO_NETWORK", "android_22.7.0.1_ios_22.8.4.1"},
            {"TIKTOK_NETWORK", "android_6.2.0.5.2_ios_6.2.0.7.2"},
            {"UNITY_NETWORK", "android_4.12.2.1_ios_4.12.2.1"},
            {"VERVE_NETWORK", "android_3.0.4.1_ios_3.0.4.1"},
            {"VUNGLE_NETWORK", "android_7.4.1.1_ios_7.4.1.1"},
            {"YANDEX_NETWORK", "android_7.4.0.1_ios_2.18.0.1"},
        };

        /// <summary>
        /// Checks if a new version of the plugin is available and prompts the user to update if one is available.
        /// </summary>
        public static void Update()
        {
            var now = (int) (DateTime.UtcNow - EpochTime).TotalSeconds;
            if (EditorPrefs.HasKey(KeyLastUpdateCheckTime))
            {
                var elapsedTime = now - EditorPrefs.GetInt(KeyLastUpdateCheckTime);

                // Check if we have checked for a new version in the last 24 hrs and skip update if we have.
                if (elapsedTime < SecondsInADay) return;
            }

            // Update last checked time.
            EditorPrefs.SetInt(KeyLastUpdateCheckTime, now);

            // Load the plugin data
            AppLovinEditorCoroutine.StartCoroutine(AppLovinIntegrationManager.Instance.LoadPluginData(data =>
            {
                if (data == null) return;

                ShowPluginUpdateDialogIfNeeded(data);
                ShowNetworkAdaptersUpdateDialogIfNeeded(data.MediatedNetworks);
                ShowGoogleNetworkAdaptersUpdateDialogIfNeeded(data.MediatedNetworks);
            }));
        }

        private static void ShowPluginUpdateDialogIfNeeded(PluginData data)
        {
            // Check if publisher has disabled auto update.
            if (!EditorPrefs.GetBool(KeyAutoUpdateEnabled, true)) return;

            // Check if the current and latest version are the same or if the publisher is on a newer version (on beta). If so, skip update.
            var comparison = data.AppLovinMax.CurrentToLatestVersionComparisonResult;
            if (comparison == MaxSdkUtils.VersionComparisonResult.Equal || comparison == MaxSdkUtils.VersionComparisonResult.Greater) return;

            // A new version of the plugin is available. Show a dialog to the publisher.
            var option = EditorUtility.DisplayDialogComplex(
                "AppLovin MAX Plugin Update",
                "A new version of AppLovin MAX plugin is available for download. Update now?",
                "Download",
                "Not Now",
                "Don't Ask Again");

            if (option == 0) // Download
            {
                MaxSdkLogger.UserDebug("Downloading plugin...");
                AppLovinIntegrationManager.downloadPluginProgressCallback = AppLovinIntegrationManagerWindow.OnDownloadPluginProgress;
                AppLovinEditorCoroutine.StartCoroutine(AppLovinIntegrationManager.Instance.DownloadPlugin(data.AppLovinMax));
            }
            else if (option == 1) // Not Now
            {
                // Do nothing
                MaxSdkLogger.UserDebug("Update postponed.");
            }
            else if (option == 2) // Don't Ask Again
            {
                MaxSdkLogger.UserDebug("Auto Update disabled. You can enable it again from the AppLovin Integration Manager");
                EditorPrefs.SetBool(KeyAutoUpdateEnabled, false);
            }
        }

        private static void ShowNetworkAdaptersUpdateDialogIfNeeded(Network[] networks)
        {
            var networksToUpdate = networks.Where(network => network.RequiresUpdate).ToList();

            // If all networks are above the required version, do nothing.
            if (networksToUpdate.Count <= 0) return;

            // We found a few adapters that are not compatible with the current SDK, show alert.
            var message = "The following network adapters are not compatible with the current version of AppLovin MAX Plugin:\n";
            foreach (var networkName in networksToUpdate)
            {
                message += "\n- ";
                message += networkName.DisplayName + " (Requires " + MinAdapterVersions[networkName.Name] + " or newer)";
            }

            message += "\n\nPlease update them to the latest versions to avoid any issues.";

            AppLovinIntegrationManager.ShowBuildFailureDialog(message);
        }

        private static void ShowGoogleNetworkAdaptersUpdateDialogIfNeeded(Network[] networks)
        {
            // AdMob and GAM use the same SDKs so their adapters should use the same underlying SDK version.
            var googleNetwork = networks.FirstOrDefault(network => network.Name.Equals("ADMOB_NETWORK"));
            var googleAdManagerNetwork = networks.FirstOrDefault(network => network.Name.Equals("GOOGLE_AD_MANAGER_NETWORK"));

            // If both AdMob and GAM are not integrated, do nothing.
            if (googleNetwork == null || string.IsNullOrEmpty(googleNetwork.CurrentVersions.Unity) ||
                googleAdManagerNetwork == null || string.IsNullOrEmpty(googleAdManagerNetwork.CurrentVersions.Unity)) return;

            var isAndroidVersionCompatible = GoogleNetworkAdaptersCompatible(googleNetwork.CurrentVersions.Android, googleAdManagerNetwork.CurrentVersions.Android, "19.8.0.0");
            var isIosVersionCompatible = GoogleNetworkAdaptersCompatible(googleNetwork.CurrentVersions.Ios, googleAdManagerNetwork.CurrentVersions.Ios, "8.0.0.0");

            if (isAndroidVersionCompatible && isIosVersionCompatible) return;

            var message = "You may see unexpected errors if you use different versions of the AdMob and Google Ad Manager adapter SDKs. " +
                          "AdMob and Google Ad Manager share the same SDKs.\n\n" +
                          "You can be sure that you are using the same SDK for both if the first three numbers in each adapter version match.";

            AppLovinIntegrationManager.ShowBuildFailureDialog(message);
        }

        private static bool GoogleNetworkAdaptersCompatible(string googleVersion, string googleAdManagerVersion, string breakingVersion)
        {
            var googleResult = MaxSdkUtils.CompareVersions(googleVersion, breakingVersion);
            var googleAdManagerResult = MaxSdkUtils.CompareVersions(googleAdManagerVersion, breakingVersion);

            // If one is less than the breaking version and the other is not, they are not compatible.
            if (googleResult == MaxSdkUtils.VersionComparisonResult.Lesser &&
                googleAdManagerResult != MaxSdkUtils.VersionComparisonResult.Lesser) return false;

            if (googleAdManagerResult == MaxSdkUtils.VersionComparisonResult.Lesser &&
                googleResult != MaxSdkUtils.VersionComparisonResult.Lesser) return false;

            return true;
        }
    }
}
