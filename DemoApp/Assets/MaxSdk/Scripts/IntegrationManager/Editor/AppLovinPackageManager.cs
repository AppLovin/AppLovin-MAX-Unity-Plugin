using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if !UNITY_2020_1_OR_NEWER
using System.Reflection;
#endif
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    [Serializable]
    public class PackageInfo
    {
        // ReSharper disable InconsistentNaming - For JSON Deserialization
        public string Name;
        public string Version;
    }

    public interface IPackageManagerClient
    {
        List<string> GetInstalledMediationNetworks();
        IEnumerator AddNetwork(Network network, bool showImport);
        void RemoveNetwork(Network network);
    }

    public static class AppLovinPackageManager
    {
#if UNITY_2019_2_OR_NEWER
        private static readonly IPackageManagerClient _upmPackageManager = new AppLovinUpmPackageManager();
#endif
        private static readonly IPackageManagerClient _assetsPackageManager = new AppLovinAssetsPackageManager();

        private static bool _migrationPromptShown;

        private static IPackageManagerClient PackageManagerClient
        {
            get
            {
#if UNITY_2019_2_OR_NEWER
                return AppLovinIntegrationManager.IsPluginInPackageManager ? _upmPackageManager : _assetsPackageManager;
#else
                return _assetsPackageManager;
#endif
            }
        }

        internal static PluginData PluginData { get; set; }

        /// <summary>
        /// Checks whether or not an adapter with the given version or newer exists.
        /// </summary>
        /// <param name="adapterName">The name of the network (the root adapter folder name in "MaxSdk/Mediation/" folder.</param>
        /// <param name="iosVersion">The min iOS adapter version to check for. Can be <c>null</c> if we want to check for any version.</param>
        /// <param name="androidVersion">The min android adapter version to check for. Can be <c>null</c> if we want to check for any version.</param>
        /// <returns><c>true</c> if an adapter with the min version is installed.</returns>
        internal static bool IsAdapterInstalled(string adapterName, string iosVersion = null, string androidVersion = null)
        {
            var dependencyFilePathList = GetAssetPathListForExportPath("MaxSdk/Mediation/" + adapterName + "/Editor/Dependencies.xml");
            if (dependencyFilePathList.Count <= 0) return false;

            var currentVersion = GetCurrentVersions(dependencyFilePathList);
            if (iosVersion != null)
            {
                var iosVersionComparison = MaxSdkUtils.CompareVersions(currentVersion.Ios, iosVersion);
                if (iosVersionComparison == MaxSdkUtils.VersionComparisonResult.Lesser)
                {
                    return false;
                }
            }

            if (androidVersion != null)
            {
                var androidVersionComparison = MaxSdkUtils.CompareVersions(currentVersion.Android, androidVersion);
                if (androidVersionComparison == MaxSdkUtils.VersionComparisonResult.Lesser)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether an adapter is installed using the plugin data.
        /// </summary>
        /// <param name="pluginData">The plugin data to check for the adapter</param>
        /// <param name="adapterName">The name of the network.</param>
        /// <returns>Whether an adapter is installed in the plugin data</returns>
        internal static bool IsAdapterInstalled(PluginData pluginData, string adapterName)
        {
            var network = pluginData.MediatedNetworks.Where(mediatedNetwork => mediatedNetwork.Name.Equals(adapterName)).ToList().FirstOrDefault();
            var networkVersion = network != null ? network.CurrentVersions : null;
            var currentVersion = networkVersion != null ? networkVersion.Unity : "";

            return MaxSdkUtils.IsValidString(currentVersion);
        }

        /// <summary>
        /// Gets the mediation networks that are currently installed in the project. If using UPM, checks
        /// for networks in Packages folder and Mediation folder in case a custom adapter was added to the project.
        /// </summary>
        /// <returns>A list of the installed mediation network names.</returns>
        internal static List<string> GetInstalledMediationNetworks()
        {
            var installedNetworks = PackageManagerClient.GetInstalledMediationNetworks();
            if (AppLovinSettings.Instance.AddApsSkAdNetworkIds)
            {
                installedNetworks.Add("AmazonAdMarketplace");
            }

            return installedNetworks;
        }

        /// <summary>
        /// Adds a network to the project.
        /// </summary>
        /// <param name="network">The network to add.</param>
        /// <param name="showImport">Whether to show the import window (only for non UPM)</param>
        internal static IEnumerator AddNetwork(Network network, bool showImport)
        {
            yield return PackageManagerClient.AddNetwork(network, showImport);

            AppLovinEditorCoroutine.StartCoroutine(RefreshAssetsAtEndOfFrame(network));
        }

        /// <summary>
        /// Removes a network from the project.
        /// </summary>
        /// <param name="network">The network to remove.</param>
        internal static void RemoveNetwork(Network network)
        {
            PackageManagerClient.RemoveNetwork(network);

            AppLovinEditorCoroutine.StartCoroutine(RefreshAssetsAtEndOfFrame(network));
        }

        #region Utility

        /// <summary>
        /// Gets the list of all asset paths for a given MAX plugin export path.
        /// </summary>
        /// <param name="exportPath">The actual exported path of the asset.</param>
        /// <returns>The exported path of the MAX plugin asset or an empty list if the asset is not found.</returns>
        private static List<string> GetAssetPathListForExportPath(string exportPath)
        {
            var assetLabelToFind = "l:al_max_export_path-" + exportPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var assetGuids = AssetDatabase.FindAssets(assetLabelToFind);

            var assetPaths = new List<string>();
            foreach (var assetGuid in assetGuids)
            {
                assetPaths.Add(AssetDatabase.GUIDToAssetPath(assetGuid));
            }

            return assetPaths.Count <= 0 ? new List<string>() : assetPaths;
        }

        /// <summary>
        /// Updates the CurrentVersion fields for a given network data object.
        /// </summary>
        /// <param name="network">Network for which to update the current versions.</param>
        internal static void UpdateCurrentVersions(Network network)
        {
            var assetPaths = GetAssetPathListForExportPath(network.DependenciesFilePath);
            var currentVersions = GetCurrentVersions(assetPaths);
            network.CurrentVersions = currentVersions;

            // If AppLovin mediation plugin, get the version from MaxSdk and the latest and current version comparison.
            if (network.Name.Equals("APPLOVIN_NETWORK"))
            {
                network.CurrentVersions.Unity = MaxSdk.Version;

                var unityVersionComparison = MaxSdkUtils.CompareVersions(network.CurrentVersions.Unity, network.LatestVersions.Unity);
                var androidVersionComparison = MaxSdkUtils.CompareVersions(network.CurrentVersions.Android, network.LatestVersions.Android);
                var iosVersionComparison = MaxSdkUtils.CompareVersions(network.CurrentVersions.Ios, network.LatestVersions.Ios);

                // Overall version is same if all the current and latest (from db) versions are same.
                if (unityVersionComparison == MaxSdkUtils.VersionComparisonResult.Equal &&
                    androidVersionComparison == MaxSdkUtils.VersionComparisonResult.Equal &&
                    iosVersionComparison == MaxSdkUtils.VersionComparisonResult.Equal)
                {
                    network.CurrentToLatestVersionComparisonResult = MaxSdkUtils.VersionComparisonResult.Equal;
                }
                // One of the installed versions is newer than the latest versions which means that the publisher is on a beta version.
                else if (unityVersionComparison == MaxSdkUtils.VersionComparisonResult.Greater ||
                         androidVersionComparison == MaxSdkUtils.VersionComparisonResult.Greater ||
                         iosVersionComparison == MaxSdkUtils.VersionComparisonResult.Greater)
                {
                    network.CurrentToLatestVersionComparisonResult = MaxSdkUtils.VersionComparisonResult.Greater;
                }
                // We have a new version available if all Android, iOS and Unity has a newer version available in db.
                else
                {
                    network.CurrentToLatestVersionComparisonResult = MaxSdkUtils.VersionComparisonResult.Lesser;
                }
            }
            // For all other mediation adapters, get the version comparison using their Unity versions.
            else
            {
                // If adapter is indeed installed, compare the current (installed) and the latest (from db) versions, so that we can determine if the publisher is on an older, current or a newer version of the adapter.
                // If the publisher is on a newer version of the adapter than the db version, that means they are on a beta version.
                if (!string.IsNullOrEmpty(currentVersions.Unity))
                {
                    network.CurrentToLatestVersionComparisonResult = AppLovinIntegrationManagerUtils.CompareUnityMediationVersions(currentVersions.Unity, network.LatestVersions.Unity);
                }

                if (!string.IsNullOrEmpty(network.CurrentVersions.Unity) && AppLovinAutoUpdater.MinAdapterVersions.ContainsKey(network.Name))
                {
                    var comparisonResult = AppLovinIntegrationManagerUtils.CompareUnityMediationVersions(network.CurrentVersions.Unity, AppLovinAutoUpdater.MinAdapterVersions[network.Name]);
                    // Requires update if current version is lower than the min required version.
                    network.RequiresUpdate = comparisonResult < 0;
                }
                else
                {
                    // Reset value so that the Integration manager can hide the alert icon once adapter is updated.
                    network.RequiresUpdate = false;
                }
            }
        }

        /// <summary>
        /// Gets the current versions for a given network's dependency file paths. UPM will have multiple paths
        /// for each network - one each for iOS and Android.
        /// </summary>
        /// <param name="dependencyPaths">A list of dependency file paths to extract current versions from.</param>
        /// <returns>Current versions of a given network's dependency files.</returns>
        private static Versions GetCurrentVersions(List<string> dependencyPaths)
        {
            var currentVersions = new Versions();
            foreach (var dependencyPath in dependencyPaths)
            {
                GetCurrentVersion(currentVersions, dependencyPath);
            }

            if (currentVersions.Android != null && currentVersions.Ios != null)
            {
                currentVersions.Unity = "android_" + currentVersions.Android + "_ios_" + currentVersions.Ios;
            }
            else if (currentVersions.Android != null)
            {
                currentVersions.Unity = "android_" + currentVersions.Android;
            }
            else if (currentVersions.Ios != null)
            {
                currentVersions.Unity = "ios_" + currentVersions.Ios;
            }

            return currentVersions;
        }

        /// <summary>
        /// Extracts the current version of a network from its dependency.xml file.
        /// </summary>
        /// <param name="currentVersions">The Versions object we are using.</param>
        /// <param name="dependencyPath">The path to the dependency.xml file.</param>
        private static void GetCurrentVersion(Versions currentVersions, string dependencyPath)
        {
            XDocument dependency;
            try
            {
                dependency = XDocument.Load(dependencyPath);
            }
#pragma warning disable 0168
            catch (IOException exception)
#pragma warning restore 0168
            {
                // Couldn't find the dependencies file. The plugin is not installed.
                return;
            }

            // <dependencies>
            //  <androidPackages>
            //      <androidPackage spec="com.applovin.mediation:network_name-adapter:1.2.3.4" />
            //  </androidPackages>
            //  <iosPods>
            //      <iosPod name="AppLovinMediationNetworkNameAdapter" version="2.3.4.5" />
            //  </iosPods>
            // </dependencies>
            string androidVersion = null;
            string iosVersion = null;
            var dependenciesElement = dependency.Element("dependencies");
            if (dependenciesElement != null)
            {
                var androidPackages = dependenciesElement.Element("androidPackages");
                if (androidPackages != null)
                {
                    var adapterPackage = androidPackages.Descendants().FirstOrDefault(element => element.Name.LocalName.Equals("androidPackage")
                                                                                                 && element.FirstAttribute.Name.LocalName.Equals("spec")
                                                                                                 && element.FirstAttribute.Value.StartsWith("com.applovin"));
                    if (adapterPackage != null)
                    {
                        androidVersion = adapterPackage.FirstAttribute.Value.Split(':').Last();
                        // Hack alert: Some Android versions might have square brackets to force a specific version. Remove them if they are detected.
                        if (androidVersion.StartsWith("["))
                        {
                            androidVersion = androidVersion.Trim('[', ']');
                        }
                    }
                }

                var iosPods = dependenciesElement.Element("iosPods");
                if (iosPods != null)
                {
                    var adapterPod = iosPods.Descendants().FirstOrDefault(element => element.Name.LocalName.Equals("iosPod")
                                                                                     && element.FirstAttribute.Name.LocalName.Equals("name")
                                                                                     && element.FirstAttribute.Value.StartsWith("AppLovin"));
                    if (adapterPod != null)
                    {
                        iosVersion = adapterPod.Attributes().First(attribute => attribute.Name.LocalName.Equals("version")).Value;
                    }
                }
            }

            if (androidVersion != null)
            {
                currentVersions.Android = androidVersion;
            }

            if (iosVersion != null)
            {
                currentVersions.Ios = iosVersion;
            }
        }

#if UNITY_2019_2_OR_NEWER
        /// <summary>
        /// Show the adapter migration prompt if it hasn't been shown yet.
        /// </summary>
        private static void ShowAdapterMigrationPrompt()
        {
            if (_migrationPromptShown) return;

            _migrationPromptShown = true;
            var migrateAdapters = EditorUtility.DisplayDialog("Adapter Detected in Mediation Folder",
                "It appears that you have an adapter in the Mediation folder while AppLovin's plugin is installed via UPM. This could potentially break the integration or cause unexpected errors. Would you like to automatically migrate all your adapters to UPM?", "Yes", "No");

            if (migrateAdapters)
            {
                AppLovinPluginMigrationHelper.MigrateAdapters(PluginData, AppLovinUpmManifest.Load());
            }
        }
#endif

        /// <summary>
        /// Refresh assets and update current versions after a slight delay to allow for Client.Resolve to finish.
        /// </summary>
        /// <param name="network">The network that was just installed/removed.</param>
        private static IEnumerator RefreshAssetsAtEndOfFrame(Network network)
        {
            yield return new WaitForEndOfFrame();
            UpdateCurrentVersions(network);
            AssetDatabase.Refresh();
        }

        #endregion
    }

#if UNITY_2019_2_OR_NEWER
    public class AppLovinUpmPackageManager : IPackageManagerClient
    {
        public const string PackageNamePrefixAppLovin = "com.applovin.mediation.ads";
        private const string PackageNamePrefixNetwork = "com.applovin.mediation.adapters";
        private const string PackageNamePrefixDsp = "com.applovin.mediation.dsp";

        private const float TimeoutFetchPackageCollectionSeconds = 10f;

#if !UNITY_2020_1_OR_NEWER
        private static Type packageManagerClientType;
        private static MethodInfo packageManagerResolveMethod;
#endif

        public List<string> GetInstalledMediationNetworks()
        {
            // Return empty list if we failed to get the package list
            var packageCollection = GetPackageCollectionSync(TimeoutFetchPackageCollectionSeconds);
            if (packageCollection == null)
            {
                return new List<string>();
            }

            return packageCollection.Where(package => package.name.StartsWith(PackageNamePrefixNetwork) || package.name.StartsWith(PackageNamePrefixDsp))
                .SelectMany(package => package.keywords)
                .Where(keyword => keyword.StartsWith("dir:"))
                .Select(keyword => keyword.Replace("dir:", ""))
                .Distinct()
                .ToList();
        }

        public IEnumerator AddNetwork(Network network, bool showImport)
        {
            var appLovinManifest = AppLovinUpmManifest.Load();
            AddPackages(network, appLovinManifest);
            appLovinManifest.Save();
            ResolvePackageManager();

            yield break;
        }

        public void RemoveNetwork(Network network)
        {
            var appLovinManifest = AppLovinUpmManifest.Load();
            RemovePackages(network, appLovinManifest);
            appLovinManifest.Save();
            ResolvePackageManager();
        }

        /// <summary>
        /// Adds a network's packages to the package manager removes any beta version that exists
        /// </summary>
        /// <param name="network">The network to add.</param>
        /// <param name="appLovinManifest">The AppLovinUpmManifest instance to edit</param>
        internal static void AddPackages(Network network, AppLovinUpmManifest appLovinManifest)
        {
            foreach (var packageInfo in network.Packages)
            {
                appLovinManifest.AddPackageDependency(packageInfo.Name, packageInfo.Version);
                RemoveBetaPackage(packageInfo.Name, appLovinManifest);
            }
        }

        /// <summary>
        /// Removes a network's packages from the package manager
        /// </summary>
        /// <param name="network">The network to add.</param>
        /// <param name="appLovinManifest">The AppLovinUpmManifest instance to edit</param>
        private static void RemovePackages(Network network, AppLovinUpmManifest appLovinManifest)
        {
            foreach (var packageInfo in network.Packages)
            {
                appLovinManifest.RemovePackageDependency(packageInfo.Name);
                RemoveBetaPackage(packageInfo.Name, appLovinManifest);
            }
        }

        /// <summary>
        /// Removes the beta version of a package name
        /// </summary>
        /// <param name="packageName">The name of the package to remove a beta for</param>
        /// <param name="appLovinManifest">The AppLovinUpmManifest instance to edit</param>
        private static void RemoveBetaPackage(string packageName, AppLovinUpmManifest appLovinManifest)
        {
            var prefix = "";
            if (packageName.Contains(PackageNamePrefixNetwork))
            {
                prefix = PackageNamePrefixNetwork;
            }
            else if (packageName.Contains(PackageNamePrefixDsp))
            {
                prefix = PackageNamePrefixDsp;
            }
            else if (packageName.Contains(PackageNamePrefixAppLovin))
            {
                prefix = PackageNamePrefixAppLovin;
            }
            else
            {
                return;
            }

            var betaPackageName = packageName.Replace(prefix, prefix + ".beta");
            appLovinManifest.RemovePackageDependency(betaPackageName);
        }

        /// <summary>
        /// Resolves the Unity Package Manager so any changes made to the manifest.json file are reflected in the Unity Editor.
        /// </summary>
        internal static void ResolvePackageManager()
        {
#if UNITY_2020_1_OR_NEWER
            Client.Resolve();
#else
            packageManagerClientType = packageManagerClientType ?? typeof(Client);
            if (packageManagerClientType != null)
            {
                packageManagerResolveMethod = packageManagerResolveMethod ?? packageManagerClientType.GetMethod("Resolve", BindingFlags.NonPublic | BindingFlags.Static);
            }

            if (packageManagerResolveMethod != null)
            {
                packageManagerResolveMethod.Invoke(null, null);
            }
#endif
        }

        /// <summary>
        /// Gets the PackageCollection from the Unity Package Manager synchronously.
        /// </summary>
        /// <param name="timeoutSeconds">How long to wait before exiting with a timeout error</param>
        /// <returns></returns>
        private static PackageCollection GetPackageCollectionSync(float timeoutSeconds = -1)
        {
            var request = Client.List();

            // Just wait till the request is complete
            var now = DateTime.Now;
            while (!request.IsCompleted)
            {
                // Wait indefinitely if there is no timeout set.
                if (timeoutSeconds < 0) continue;

                var delta = DateTime.Now - now;
                if (delta.TotalSeconds > timeoutSeconds)
                {
                    MaxSdkLogger.UserError("Failed to list UPM packages: Timeout");
                    break;
                }
            }

            if (!request.IsCompleted)
            {
                return null;
            }

            if (request.Status >= StatusCode.Failure)
            {
                MaxSdkLogger.UserError("Failed to list packages: " + request.Error.message);
                return null;
            }

            return (request.Status == StatusCode.Success) ? request.Result : null;
        }
    }

#endif

    public class AppLovinAssetsPackageManager : IPackageManagerClient
    {
        public List<string> GetInstalledMediationNetworks()
        {
            var maxMediationDirectory = Path.Combine(AppLovinIntegrationManager.PluginParentDirectory, "MaxSdk/Mediation/");
            if (!Directory.Exists(maxMediationDirectory)) return new List<string>();

            var mediationNetworkDirectories = Directory.GetDirectories(maxMediationDirectory);
            return mediationNetworkDirectories.Select(Path.GetFileName).ToList();
        }

        public IEnumerator AddNetwork(Network network, bool showImport)
        {
            yield return AppLovinIntegrationManager.Instance.DownloadPlugin(network, showImport);
        }

        public void RemoveNetwork(Network network)
        {
            foreach (var pluginFilePath in network.PluginFilePaths)
            {
                var filePath = Path.Combine(AppLovinIntegrationManager.PluginParentDirectory, pluginFilePath);
                FileUtil.DeleteFileOrDirectory(filePath);
                FileUtil.DeleteFileOrDirectory(filePath + ".meta");
            }
        }
    }
}
