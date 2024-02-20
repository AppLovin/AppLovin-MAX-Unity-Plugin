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
        private static readonly List<string> ObsoleteNetworks = new List<string>
        {
            "AdColony",
            "Criteo",
            "Snap",
            "Tapjoy",
            "VerizonAds",
            "VoodooAds"
        };

        private static readonly List<string> ObsoleteFileExportPathsToDelete = new List<string>
        {
            // The `EventSystemChecker` has been renamed to `MaxEventSystemChecker`.
            "MaxSdk/Scripts/EventSystemChecker.cs",
            "MaxSdk/Scripts/EventSystemChecker.cs.meta",

            // Google AdMob adapter pre/post process scripts. The logic has been migrated to the main plugin.
            "MaxSdk/Mediation/Google/Editor/MaxGoogleInitialize.cs",
            "MaxSdk/Mediation/Google/Editor/MaxGoogleInitialize.cs.meta",
            "MaxSdk/Mediation/Google/Editor/MaxMediationGoogleUtils.cs",
            "MaxSdk/Mediation/Google/Editor/MaxMediationGoogleUtils.cs.meta",
            "MaxSdk/Mediation/Google/Editor/PostProcessor.cs",
            "MaxSdk/Mediation/Google/Editor/PostProcessor.cs.meta",
            "MaxSdk/Mediation/Google/Editor/PreProcessor.cs",
            "MaxSdk/Mediation/Google/Editor/PreProcessor.cs.meta",
            "MaxSdk/Mediation/Google/Editor/MaxSdk.Mediation.Google.Editor.asmdef",
            "MaxSdk/Mediation/Google/MaxSdk.Mediation.Google.Editor.asmdef.meta",
            "Plugins/Android/MaxMediationGoogle.androidlib",
            "Plugins/Android/MaxMediationGoogle.androidlib.meta",

            // Google Ad Manager adapter pre/post process scripts. The logic has been migrated to the main plugin.
            "MaxSdk/Mediation/GoogleAdManager/Editor/MaxGoogleAdManagerInitialize.cs",
            "MaxSdk/Mediation/GoogleAdManager/Editor/MaxGoogleAdManagerInitialize.cs.meta",
            "MaxSdk/Mediation/GoogleAdManager/Editor/PostProcessor.cs",
            "MaxSdk/Mediation/GoogleAdManager/Editor/PostProcessor.cs.meta",
            "MaxSdk/Mediation/GoogleAdManager/Editor/MaxSdk.Mediation.GoogleAdManager.Editor.asmdef",
            "MaxSdk/Mediation/GoogleAdManager/Editor/MaxSdk.Mediation.GoogleAdManager.Editor.asmdef.meta",
            "Plugins/Android/MaxMediationGoogleAdManager.androidlib",
            "Plugins/Android/MaxMediationGoogleAdManager.androidlib.meta",
                
            // The `VariableService` has been removed.
            "MaxSdk/Scripts/MaxVariableServiceAndroid.cs",
            "MaxSdk/Scripts/MaxVariableServiceAndroid.cs.meta",
            "MaxSdk/Scripts/MaxVariableServiceiOS.cs",
            "MaxSdk/Scripts/MaxVariableServiceiOS.cs.meta",
            "MaxSdk/Scripts/MaxVariableServiceUnityEditor.cs",
            "MaxSdk/Scripts/MaxVariableServiceUnityEditor.cs.meta"
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

            foreach (var obsoleteFileExportPathToDelete in ObsoleteFileExportPathsToDelete)
            {
                var pathToDelete = MaxSdkUtils.GetAssetPathForExportPath(obsoleteFileExportPathToDelete);
                if (CheckExistence(pathToDelete))
                {
                    MaxSdkLogger.UserDebug("Deleting obsolete file '" + pathToDelete + "' that are no longer needed.");
                    FileUtil.DeleteFileOrDirectory(pathToDelete);
                    changesMade = true;
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
