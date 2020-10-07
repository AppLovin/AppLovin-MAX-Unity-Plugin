//
//  AppLovinAutoUpdater.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 1/27/20.
//  Copyright © 2020 AppLovin. All rights reserved.
//

using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Handles auto updates for AppLovin MAX plugin.
/// </summary>
public class AppLovinAutoUpdater
{
    public const string KeyAutoUpdateEnabled = "com.applovin.auto_update_enabled";
    private const string KeyLastUpdateCheckTime = "com.applovin.last_update_check_time";
    private static readonly DateTime EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly int SecondsInADay = (int) TimeSpan.FromDays(1).TotalSeconds;

    /// <summary>
    /// Checks if a new version of the plugin is available and prompts the user to update if one is available.
    /// </summary>
    public static void Update()
    {
        // Check if publisher has disabled auto update.
        if (!EditorPrefs.GetBool(KeyAutoUpdateEnabled, true)) return;

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
            // Check if the current and latest version are the same. If so, skip update.
            if (data == null || data.AppLovinMax.LatestVersions.Equals(data.AppLovinMax.CurrentVersions)) return;

            // A new version of the plugin is available. Show a dialog to the publisher.
            var option = EditorUtility.DisplayDialogComplex(
                "AppLovin MAX Plugin Update",
                "A new version of AppLovin MAX plugin is available for download. Update now?",
                "Download",
                "Not Now",
                "Don't Ask Again");

            if (option == 0) // Download
            {
                Debug.Log("[AppLovin MAX] Downloading plugin...");
                AppLovinIntegrationManager.downloadPluginProgressCallback = AppLovinIntegrationManagerWindow.OnDownloadPluginProgress;
                AppLovinEditorCoroutine.StartCoroutine(AppLovinIntegrationManager.Instance.DownloadPlugin(data.AppLovinMax));
            }
            else if (option == 1) // Not Now
            {
                // Do nothing
                Debug.Log("[AppLovin MAX] Update postponed.");
            }
            else if (option == 2) // Don't Ask Again
            {
                Debug.Log("[AppLovin MAX] Auto Update disabled. You can enable it again from the AppLovin Integration Manager");
                EditorPrefs.SetBool(KeyAutoUpdateEnabled, false);
            }
        }));
    }
}
