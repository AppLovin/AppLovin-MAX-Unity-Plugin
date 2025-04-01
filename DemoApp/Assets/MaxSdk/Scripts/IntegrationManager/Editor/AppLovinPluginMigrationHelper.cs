using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if UNITY_2019_2_OR_NEWER
namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    /// <summary>
    /// Moves our SDK Unity Plugin from under the Assets folder to the Unity Package Manager.
    /// </summary>
    public static class AppLovinPluginMigrationHelper
    {
        private const string ApplovinRegistryName = "AppLovin MAX Unity";
        private const string ApplovinRegistryUrl = "https://unity.packages.applovin.com/";
        private static readonly List<string> AppLovinRegistryScopes = new List<string>() {"com.applovin.mediation.ads", "com.applovin.mediation.adapters", "com.applovin.mediation.dsp"};

        private const string OpenUpmRegistryName = "package.openupm.com";
        private const string OpenUpmRegistryUrl = "https://package.openupm.com";
        private static readonly List<string> OpenUpmRegistryScopes = new List<string>() {"com.google.external-dependency-manager"};

        private static List<string> betaNetworkPluginFilePaths = new List<string>();

        /// <summary>
        /// Attempts to move the Unity plugin to UPM by adding the AppLovin scoped registry and dependencies to the manifest.
        /// </summary>
        /// <param name="pluginData">The Unity Plugin data for our sdk and mediation adapters.</param>
        /// <param name="deleteExternalDependencyManager">Whether to delete the EDM folder under "Assets"</param>
        internal static void MigrateToUnityPackageManager(PluginData pluginData, bool deleteExternalDependencyManager)
        {
            MaxSdkLogger.UserDebug("Moving AppLovin Unity Plugin to package manager");

            if (deleteExternalDependencyManager)
            {
                DeleteExternalDependencyManager();
            }

            var appLovinManifest = AppLovinUpmManifest.Load();

            MigrateAdapters(pluginData, appLovinManifest);
            MigratePlugin(pluginData, appLovinManifest);

            appLovinManifest.Save();
            AppLovinUpmPackageManager.ResolvePackageManager();
            DeletePluginFiles();
        }

        /// <summary>
        /// Add all currently installed networks to the manifest.
        /// </summary>
        internal static void MigrateAdapters(PluginData pluginData, AppLovinUpmManifest appLovinManifest)
        {
            var allNetworks = pluginData.MediatedNetworks.Concat(pluginData.PartnerMicroSdks).ToArray();
            betaNetworkPluginFilePaths.Clear();

            // Add every currently installed network and separate it by android and iOS.
            foreach (var network in allNetworks)
            {
                var currentVersion = network.CurrentVersions != null ? network.CurrentVersions.Unity : "";
                if (string.IsNullOrEmpty(currentVersion)) continue;

                if (currentVersion.Contains("beta"))
                {
                    betaNetworkPluginFilePaths.AddRange(network.PluginFilePaths);
                    continue;
                }

                AppLovinUpmPackageManager.AddPackages(network, appLovinManifest);
            }
        }

        /// <summary>
        /// Add the AppLovin scoped registry to the manifest if it doesn't exist. Otherwise update it.
        /// </summary>
        private static void MigratePlugin(PluginData pluginData, AppLovinUpmManifest appLovinManifest)
        {
            appLovinManifest.AddOrUpdateRegistry(ApplovinRegistryName, ApplovinRegistryUrl, AppLovinRegistryScopes);
            appLovinManifest.AddOrUpdateRegistry(OpenUpmRegistryName, OpenUpmRegistryUrl, OpenUpmRegistryScopes);

            var appLovinVersion = pluginData.AppLovinMax.LatestVersions.Unity;
            appLovinManifest.AddPackageDependency(AppLovinUpmPackageManager.PackageNamePrefixAppLovin, appLovinVersion);
        }

        #region Utility

        /// <summary>
        /// Delete the external dependency manager folder from the project.
        /// </summary>
        private static void DeleteExternalDependencyManager()
        {
            var externalDependencyManagerPath = Path.Combine(Application.dataPath, "ExternalDependencyManager");
            FileUtil.DeleteFileOrDirectory(externalDependencyManagerPath);
            FileUtil.DeleteFileOrDirectory(externalDependencyManagerPath + ".meta");
        }

        /// <summary>
        /// Deletes all the files in the plugin directory except the AppLovinSettings.asset file.
        /// </summary>
        private static void DeletePluginFiles()
        {
            if (AppLovinIntegrationManager.IsPluginInPackageManager) return;

            var pluginPath = Path.Combine(AppLovinIntegrationManager.PluginParentDirectory, "MaxSdk");
            var appLovinSettingsPath = Path.Combine(pluginPath, "Resources/AppLovinSettings.asset");

            var appLovinResourcesDirectory = Path.Combine(pluginPath, "Resources");
            var appLovinSettingsTempPath = Path.Combine(Path.GetTempPath(), "AppLovinSettings.asset");
            var appLovinMediationTempPath = Path.Combine(Path.GetTempPath(), "Mediation");

            // Ensure there aren't any errors when moving the files due to the directories/files already existing.
            FileUtil.DeleteFileOrDirectory(appLovinSettingsTempPath);
            FileUtil.DeleteFileOrDirectory(appLovinMediationTempPath);

            var mediationAssetsDir = Path.Combine(AppLovinIntegrationManager.PluginParentDirectory, "MaxSdk/Mediation");

            // Move the AppLovinSettings.asset file and any beta adapters to a temporary directory.
            File.Move(appLovinSettingsPath, appLovinSettingsTempPath);
            var adapterSaved = MoveBetaAdaptersIfNeeded(mediationAssetsDir, appLovinMediationTempPath);

            // Move the meta file if the adapter was saved to save the asset labels
            if (adapterSaved)
            {
                File.Move(mediationAssetsDir, appLovinMediationTempPath + ".meta");
            }

            // Delete the plugin directory and then move the AppLovinSettings.asset and beta adapters back.
            FileUtil.DeleteFileOrDirectory(pluginPath);
            Directory.CreateDirectory(appLovinResourcesDirectory);
            File.Move(appLovinSettingsTempPath, appLovinSettingsPath);
            MoveBetaAdaptersIfNeeded(appLovinMediationTempPath, mediationAssetsDir);
            if (adapterSaved)
            {
                File.Move(appLovinMediationTempPath + ".meta", mediationAssetsDir);
            }

            FileUtil.DeleteFileOrDirectory(appLovinMediationTempPath);
        }

        /// <summary>
        /// Moves the beta adapters from a source mediation directory to a destination mediation directory.
        /// </summary>
        /// <param name="sourceMediationDirectory">The directory containing the beta adapters to be moved.</param>
        /// <param name="destinationMediationDirectory">The target directory where the beta adapters should be moved.</param>
        private static bool MoveBetaAdaptersIfNeeded(string sourceMediationDirectory, string destinationMediationDirectory)
        {
            if (betaNetworkPluginFilePaths.Count == 0 || !Directory.Exists(sourceMediationDirectory)) return false;

            var movedAdapter = false;
            Directory.CreateDirectory(destinationMediationDirectory);
            foreach (var pluginFilePath in betaNetworkPluginFilePaths)
            {
                var sourceDirectory = Path.Combine(sourceMediationDirectory, Path.GetFileName(pluginFilePath));
                var destinationDirectory = Path.Combine(destinationMediationDirectory, Path.GetFileName(pluginFilePath));
                if (Directory.Exists(sourceDirectory))
                {
                    Directory.Move(sourceDirectory, destinationDirectory);
                    movedAdapter = true;
                }
            }

            return movedAdapter;
        }

        #endregion
    }
}
#endif
