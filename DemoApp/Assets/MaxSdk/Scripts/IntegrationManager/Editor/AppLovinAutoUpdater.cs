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
using UnityEngine;

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
        {"ADCOLONY_NETWORK", "android_4.2.3.1_ios_4.3.1.1"},
        {"ADMOB_NETWORK", "android_19.3.0.3_ios_7.65.0.0"},
        {"CHARTBOOST_NETWORK", "android_8.1.0.7_ios_8.2.1.3"},
        {"FACEBOOK_MEDIATE", "android_6.0.0.1_ios_6.0.0.3"},
        {"FYBER_NETWORK", "android_7.7.0.1_ios_7.6.4.1"},
        {"GOOGLE_AD_MANAGER_NETWORK", "android_19.3.0.3_ios_7.65.0.0"},
        {"INMOBI_NETWORK", "android_9.0.9.2_ios_9.0.7.9"},
        {"IRONSOURCE_NETWORK", "android_7.0.1.1.1_ios_7.0.1.0.1"},
        {"MYTARGET_NETWORK", "android_5.9.1.2_ios_5.7.5.1"},
        {"SMAATO_NETWORK", "android_21.5.2.5_ios_21.5.2.3"},
        {"TAPJOY_NETWORK", "android_12.6.1.5_ios_12.6.1.6"},
        {"TIKTOK_NETWORK", "android_3.1.0.1.6_ios_3.2.5.1.1"},
        {"UNITY_NETWORK", "android_3.4.8.2_ios_3.4.8.2"},
        {"VERIZON_NETWORK", "android_1.6.0.5_ios_1.7.1.1"},
        {"VUNGLE_NETWORK", "android_6.7.1.2_ios_6.7.1.3"},
        {"YANDEX_NETWORK", "android_2.170.2_ios_2.18.0.1"}
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
            ShowNetworkAdaptersUpdateDialogIfNeeded(data);
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

    private static void ShowNetworkAdaptersUpdateDialogIfNeeded(PluginData data)
    {
        var networks = data.MediatedNetworks;
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
}
