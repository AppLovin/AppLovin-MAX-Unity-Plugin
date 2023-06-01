//
//  MaxInitialization.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Thomas So on 5/24/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

using AppLovinMax.Scripts.IntegrationManager.Editor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace AppLovinMax.Scripts.Editor
{
    [InitializeOnLoad]
    public class MaxInitialize
    {
        private const string AndroidChangelog = "ANDROID_CHANGELOG.md";
        private const string IosChangelog = "IOS_CHANGELOG.md";

        private static readonly List<string> Networks = new List<string>
        {
            "AdColony",
            "Amazon",
            "ByteDance",
            "Chartboost",
            "Facebook",
            "Fyber",
            "Google",
            "InMobi",
            "IronSource",
            "Maio",
            "Mintegral",
            "MyTarget",
            "MoPub",
            "Nend",
            "Ogury",
            "Smaato",
            "Tapjoy",
            "TencentGDT",
            "UnityAds",
            "VerizonAds",
            "Vungle",
            "Yandex"
        };

        private static readonly List<string> ObsoleteNetworks = new List<string>
        {
            "VoodooAds"
        };

        static MaxInitialize()
        {
#if UNITY_IOS
            // Check that the publisher is targeting iOS 9.0+
            if (!PlayerSettings.iOS.targetOSVersionString.StartsWith("9.") && !PlayerSettings.iOS.targetOSVersionString.StartsWith("1"))
            {
                MaxSdkLogger.UserError("Detected iOS project version less than iOS 9 - The AppLovin MAX SDK WILL NOT WORK ON < iOS9!!!");
            }
#endif

            var pluginParentDir = AppLovinIntegrationManager.PluginParentDirectory;
            var isPluginOutsideAssetsDir = AppLovinIntegrationManager.IsPluginOutsideAssetsDirectory;
            var changesMade = AppLovinIntegrationManager.MovePluginFilesIfNeeded(pluginParentDir, isPluginOutsideAssetsDir);
            if (isPluginOutsideAssetsDir)
            {
                // If the plugin is not under the assets folder, delete the MaxSdk/Mediation folder in the plugin, so that the adapters are not imported at that location and imported to the default location.
                var mediationDir = Path.Combine(pluginParentDir, "MaxSdk/Mediation");
                if (Directory.Exists(mediationDir))
                {
                    FileUtil.DeleteFileOrDirectory(mediationDir);
                    FileUtil.DeleteFileOrDirectory(mediationDir + ".meta");
                    changesMade = true;
                }
            }

            AppLovinIntegrationManager.AddLabelsToAssetsIfNeeded(pluginParentDir, isPluginOutsideAssetsDir);

            // Check if we have legacy adapter CHANGELOGs.
            foreach (var network in Networks)
            {
                var mediationAdapterDir = Path.Combine(pluginParentDir, "MaxSdk/Mediation/" + network);

                // If new directory exists
                if (CheckExistence(mediationAdapterDir))
                {
                    var androidChangelogFile = Path.Combine(mediationAdapterDir, AndroidChangelog);
                    if (CheckExistence(androidChangelogFile))
                    {
                        FileUtil.DeleteFileOrDirectory(androidChangelogFile);
                        changesMade = true;
                    }

                    var iosChangelogFile = Path.Combine(mediationAdapterDir, IosChangelog);
                    if (CheckExistence(iosChangelogFile))
                    {
                        FileUtil.DeleteFileOrDirectory(iosChangelogFile);
                        changesMade = true;
                    }
                }
            }

            // Check if any obsolete networks are installed
            foreach (var obsoleteNetwork in ObsoleteNetworks)
            {
                var networkDir = Path.Combine(pluginParentDir, "MaxSdk/Mediation/" + obsoleteNetwork);
                if (CheckExistence(networkDir))
                {
                    MaxSdkLogger.UserDebug("Deleting obsolete network " + obsoleteNetwork + " from path " + networkDir + "...");
                    FileUtil.DeleteFileOrDirectory(networkDir);
                    changesMade = true;
                }
            }

            // Refresh UI
            if (changesMade)
            {
                AssetDatabase.Refresh();
                MaxSdkLogger.UserDebug("AppLovin MAX Migration completed");
            }

            AppLovinAutoUpdater.Update();
        }

        private static bool CheckExistence(string location)
        {
            return File.Exists(location) ||
                   Directory.Exists(location) ||
                   (location.EndsWith("/*") && Directory.Exists(Path.GetDirectoryName(location)));
        }
    }
}
