//
//  MaxIntegrationManager.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 6/1/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    [Serializable]
    public class PluginData
    {
        public Network AppLovinMax;
        public Network[] MediatedNetworks;
        public Network[] PartnerMicroSdks;
        public DynamicLibraryToEmbed[] ThirdPartyDynamicLibrariesToEmbed;
    }

    [Serializable]
    public class Network
    {
        //
        // Sample network data:
        //
        // {
        //   "Name": "adcolony",
        //   "DisplayName": "AdColony",
        //   "DownloadUrl": "https://bintray.com/applovin/Unity-Mediation-Packages/download_file?file_path=AppLovin-AdColony-Adapters-Android-3.3.10.1-iOS-3.3.7.2.unitypackage",
        //   "PluginFileName": "AppLovin-AdColony-Adapters-Android-3.3.10.1-iOS-3.3.7.2.unitypackage",
        //   "DependenciesFilePath": "MaxSdk/Mediation/AdColony/Editor/Dependencies.xml",
        //   "LatestVersions" : {
        //     "Unity": "android_3.3.10.1_ios_3.3.7.2",
        //     "Android": "3.3.10.1",
        //     "Ios": "3.3.7.2"
        //   }
        // }
        //

        public string Name;
        public string DisplayName;
        public string DownloadUrl;
        public string DependenciesFilePath;
        public PackageInfo[] Packages;
        public string[] PluginFilePaths;
        public Versions LatestVersions;
        [NonSerialized] public Versions CurrentVersions;
        [NonSerialized] public Versions.VersionComparisonResult CurrentToLatestVersionComparisonResult = Versions.VersionComparisonResult.Lesser;
        [NonSerialized] public bool RequiresUpdate;
        public DynamicLibraryToEmbed[] DynamicLibrariesToEmbed;
    }

    [Serializable]
    public class DynamicLibraryToEmbed
    {
        public string PodName;
        public string[] FrameworkNames;

        // Min and max versions are inclusive, so if the adapter is the min or max version, the xcframework will get embedded.
        public string MinVersion;
        public string MaxVersion;

        public DynamicLibraryToEmbed(string podName, string[] frameworkNames, string minVersion, string maxVersion)
        {
            PodName = podName;
            FrameworkNames = frameworkNames;
            MinVersion = minVersion;
            MaxVersion = maxVersion;
        }
    }

    /// <summary>
    /// A helper data class used to get current versions from Dependency.xml files.
    /// </summary>
    [Serializable]
    public class Versions
    {
        public string Unity;
        public string Android;
        public string Ios;

        /// <summary>
        /// An Enum to be used when comparing two versions.
        ///
        /// If:
        ///     A &lt; B    return <see cref="Lesser"/>
        ///     A == B      return <see cref="Equal"/>
        ///     A &gt; B    return <see cref="Greater"/>
        /// </summary>
        public enum VersionComparisonResult
        {
            Lesser = -1,
            Equal = 0,
            Greater = 1
        }

        public override bool Equals(object value)
        {
            var versions = value as Versions;

            return versions != null
                   && Unity.Equals(versions.Unity)
                   && (Android == null || Android.Equals(versions.Android))
                   && (Ios == null || Ios.Equals(versions.Ios));
        }

        public bool HasEqualSdkVersions(Versions versions)
        {
            return versions != null
                   && AdapterSdkVersion(Android).Equals(AdapterSdkVersion(versions.Android))
                   && AdapterSdkVersion(Ios).Equals(AdapterSdkVersion(versions.Ios));
        }

        public override int GetHashCode()
        {
            return new {Unity, Android, Ios}.GetHashCode();
        }

        private static string AdapterSdkVersion(string adapterVersion)
        {
            var index = adapterVersion.LastIndexOf(".");
            return index > 0 ? adapterVersion.Substring(0, index) : adapterVersion;
        }
    }

    /// <summary>
    /// A manager class for MAX integration manager window.
    ///
    /// TODO: Decide if we should namespace these classes.
    /// </summary>
    public class AppLovinIntegrationManager
    {
        /// <summary>
        /// Delegate to be called when downloading a plugin with the progress percentage. 
        /// </summary>
        /// <param name="pluginName">The name of the plugin being downloaded.</param>
        /// <param name="progress">Percentage downloaded.</param>
        /// <param name="done">Whether or not the download is complete.</param>
        public delegate void DownloadPluginProgressCallback(string pluginName, float progress, bool done);

        /// <summary>
        /// Delegate to be called when a plugin package is imported.
        /// </summary>
        /// <param name="network">The network data for which the package is imported.</param>
        public delegate void ImportPackageCompletedCallback(Network network);

        private static readonly AppLovinIntegrationManager instance = new AppLovinIntegrationManager();

        public static readonly string GradleTemplatePath = Path.Combine("Assets/Plugins/Android", "mainTemplate.gradle");
        public static readonly string DefaultPluginExportPath = Path.Combine("Assets", "MaxSdk");
        private const string MaxSdkAssetExportPath = "MaxSdk/Scripts/MaxSdk.cs";

        internal static readonly string PluginDataEndpoint = "https://unity.applovin.com/max/1.0/integration_manager_info?plugin_version={0}";

        /// <summary>
        /// Some publishers might re-export our plugin via Unity Package Manager and the plugin will not be under the Assets folder. This means that the mediation adapters, settings files should not be moved to the packages folder,
        /// since they get overridden when the package is updated. These are the files that should not be moved, if the plugin is not under the Assets/ folder.
        /// 
        /// Note: When we distribute the plugin via Unity Package Manager, we need to distribute the adapters as separate packages, and the adapters won't be in the MaxSdk folder. So we need to take that into account.
        /// </summary>
        private static readonly List<string> PluginPathsToIgnoreMoveWhenPluginInPackageManager = new List<string>
        {
            "MaxSdk/Mediation",
            "MaxSdk/Mediation.meta",
            "MaxSdk/Resources.meta",
            AppLovinSettings.SettingsExportPath,
            AppLovinSettings.SettingsExportPath + ".meta"
        };

        private static string externalDependencyManagerVersion;

        public static DownloadPluginProgressCallback downloadPluginProgressCallback;
        public static ImportPackageCompletedCallback importPackageCompletedCallback;

        private UnityWebRequest webRequest;
        private Network importingNetwork;

        /// <summary>
        /// An Instance of the Integration manager.
        /// </summary>
        public static AppLovinIntegrationManager Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// The parent directory path where the MaxSdk plugin directory is placed.
        /// </summary>
        public static string PluginParentDirectory
        {
            get
            {
                // Search for the asset with the default exported path first, In most cases, we should be able to find the asset.
                // In some cases where we don't, use the platform specific export path to search for the asset (in case of migrating a project from Windows to Mac or vice versa).
                var maxSdkScriptAssetPath = MaxSdkUtils.GetAssetPathForExportPath(MaxSdkAssetExportPath);

                // maxSdkScriptAssetPath will always have AltDirectorySeparatorChar (/) as the path separator. Convert to platform specific path.
                return maxSdkScriptAssetPath.Replace(MaxSdkAssetExportPath, "")
                    .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }
        }

        /// <summary>
        /// Whether or not the plugin is in the Unity Package Manager.
        /// </summary>
        public static bool IsPluginInPackageManager
        {
            get { return PluginParentDirectory.StartsWith("Packages"); }
        }

        /// <summary>
        /// Whether or not gradle build system is enabled.
        /// </summary>
        public static bool GradleBuildEnabled
        {
            get { return GetEditorUserBuildSetting("androidBuildSystem", "").ToString().Equals("Gradle"); }
        }

        /// <summary>
        /// Whether or not Gradle template is enabled.
        /// </summary>
        public static bool GradleTemplateEnabled
        {
            get { return GradleBuildEnabled && File.Exists(GradleTemplatePath); }
        }

        /// <summary>
        /// Whether or not the Quality Service settings can be processed which requires Gradle template enabled or Unity IDE newer than version 2018_2.
        /// </summary>
        public static bool CanProcessAndroidQualityServiceSettings
        {
            get { return GradleTemplateEnabled || GradleBuildEnabled; }
        }

        /// <summary>
        /// The External Dependency Manager version obtained dynamically.
        /// </summary>
        public static string ExternalDependencyManagerVersion
        {
            get
            {
                if (!string.IsNullOrEmpty(externalDependencyManagerVersion)) return externalDependencyManagerVersion;

                try
                {
                    var versionHandlerVersionNumberType = Type.GetType("Google.VersionHandlerVersionNumber, Google.VersionHandlerImpl");
                    externalDependencyManagerVersion = versionHandlerVersionNumberType.GetProperty("Value").GetValue(null, null).ToString();
                }
#pragma warning disable 0168
                catch (Exception ignored)
#pragma warning restore 0168
                {
                    externalDependencyManagerVersion = "Failed to get version.";
                }

                return externalDependencyManagerVersion;
            }
        }

        private AppLovinIntegrationManager()
        {
            // Add asset import callbacks.
            AssetDatabase.importPackageCompleted += packageName =>
            {
                if (!IsImportingNetwork(packageName)) return;

                MovePluginFilesIfNeeded(PluginParentDirectory, IsPluginInPackageManager);
                AssetDatabase.Refresh();

                CallImportPackageCompletedCallback(importingNetwork);
                importingNetwork = null;
            };

            AssetDatabase.importPackageCancelled += packageName =>
            {
                if (!IsImportingNetwork(packageName)) return;

                MaxSdkLogger.UserDebug("Package import cancelled.");
                importingNetwork = null;
            };

            AssetDatabase.importPackageFailed += (packageName, errorMessage) =>
            {
                if (!IsImportingNetwork(packageName)) return;

                MaxSdkLogger.UserError(errorMessage);
                importingNetwork = null;
            };
        }

        static AppLovinIntegrationManager() { }

        public static PluginData LoadPluginDataSync()
        {
            var url = string.Format(PluginDataEndpoint, MaxSdk.Version);
            using (var unityWebRequest = UnityWebRequest.Get(url))
            {
                var operation = unityWebRequest.SendWebRequest();

                // Just wait till www is done
                while (!operation.isDone) { }

                return CreatePluginDataFromWebResponse(unityWebRequest);
            }
        }

        /// <summary>
        /// Loads the plugin data to be display by integration manager window.
        /// </summary>
        /// <param name="callback">Callback to be called once the plugin data download completes.</param>
        public IEnumerator LoadPluginData(Action<PluginData> callback)
        {
            var url = string.Format(PluginDataEndpoint, MaxSdk.Version);
            using (var unityWebRequest = UnityWebRequest.Get(url))
            {
                var operation = unityWebRequest.SendWebRequest();

                while (!operation.isDone) yield return new WaitForSeconds(0.1f); // Just wait till www is done. Our coroutine is pretty rudimentary.

                var pluginData = CreatePluginDataFromWebResponse(unityWebRequest);

                callback(pluginData);
            }
        }

        private static PluginData CreatePluginDataFromWebResponse(UnityWebRequest unityWebRequest)
        {
#if UNITY_2020_1_OR_NEWER
            if (unityWebRequest.result != UnityWebRequest.Result.Success)
#else
            if (unityWebRequest.isNetworkError || unityWebRequest.isHttpError)
#endif
            {
                MaxSdkLogger.E("Failed to load plugin data. Please check your internet connection.");
                return null;
            }

            PluginData pluginData;
            try
            {
                pluginData = JsonUtility.FromJson<PluginData>(unityWebRequest.downloadHandler.text);
                AppLovinPackageManager.PluginData = pluginData;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                pluginData = null;
            }

            if (pluginData == null) return null;

            // Get current version of the plugin
            var appLovinMax = pluginData.AppLovinMax;
            AppLovinPackageManager.UpdateCurrentVersions(appLovinMax);

            // Get current versions for all the mediation networks.
            foreach (var network in pluginData.MediatedNetworks)
            {
                AppLovinPackageManager.UpdateCurrentVersions(network);
            }

            foreach (var partnerMicroSdk in pluginData.PartnerMicroSdks)
            {
                AppLovinPackageManager.UpdateCurrentVersions(partnerMicroSdk);
            }

            return pluginData;
        }

        /// <summary>
        /// Downloads the plugin file for a given network.
        /// </summary>
        /// <param name="network">Network for which to download the current version.</param>
        /// <param name="showImport">Whether or not to show the import window when downloading. Defaults to <c>true</c>.</param>
        /// <returns></returns>
        public IEnumerator DownloadPlugin(Network network, bool showImport = true)
        {
            var path = Path.Combine(Application.temporaryCachePath, GetPluginFileName(network)); // TODO: Maybe delete plugin file after finishing import.
            var downloadHandler = new DownloadHandlerFile(path);
            webRequest = new UnityWebRequest(network.DownloadUrl)
            {
                method = UnityWebRequest.kHttpVerbGET,
                downloadHandler = downloadHandler
            };

            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
            {
                yield return new WaitForSeconds(0.1f); // Just wait till webRequest is completed. Our coroutine is pretty rudimentary.
                CallDownloadPluginProgressCallback(network.DisplayName, operation.progress, operation.isDone);
            }

#if UNITY_2020_1_OR_NEWER
            if (webRequest.result != UnityWebRequest.Result.Success)
#else
            if (webRequest.isNetworkError || webRequest.isHttpError)
#endif
            {
                MaxSdkLogger.UserError(webRequest.error);
            }
            else
            {
                importingNetwork = network;
                AssetDatabase.ImportPackage(path, showImport);
            }

            webRequest.Dispose();
            webRequest = null;
        }

        /// <summary>
        /// Cancels the plugin download if one is in progress.
        /// </summary>
        public void CancelDownload()
        {
            if (webRequest == null) return;

            webRequest.Abort();
        }

        /// <summary>
        /// Shows a dialog to the user with the given message and logs the error message to console.
        /// </summary>
        /// <param name="message">The failure message to be shown to the user.</param>
        public static void ShowBuildFailureDialog(string message)
        {
            var openIntegrationManager = EditorUtility.DisplayDialog("AppLovin MAX", message, "Open Integration Manager", "Dismiss");
            if (openIntegrationManager)
            {
                AppLovinIntegrationManagerWindow.ShowManager();
            }

            MaxSdkLogger.UserError(message);
        }

        #region Utility Methods

        /// <summary>
        /// Checks whether or not the given package name is the currently importing package.
        /// </summary>
        /// <param name="packageName">The name of the package that needs to be checked.</param>
        /// <returns>true if the importing package matches the given package name.</returns>
        private bool IsImportingNetwork(string packageName)
        {
            // Note: The pluginName doesn't have the '.unitypackage' extension included in its name but the pluginFileName does. So using Contains instead of Equals.
            return importingNetwork != null && GetPluginFileName(importingNetwork).Contains(packageName);
        }

        /// <summary>
        /// Moves the imported plugin files to the MaxSdk directory if the publisher has moved the plugin to a different directory. This is a failsafe for when some plugin files are not imported to the new location.
        /// </summary>
        /// <returns>True if the adapters have been moved.</returns>
        public static bool MovePluginFilesIfNeeded(string pluginParentDirectory, bool isPluginInPackageManager)
        {
            var pluginDir = Path.Combine(pluginParentDirectory, "MaxSdk");

            // Check if the user has moved the Plugin and if new assets have been imported to the default directory.
            if (DefaultPluginExportPath.Equals(pluginDir) || !Directory.Exists(DefaultPluginExportPath)) return false;

            MovePluginFiles(DefaultPluginExportPath, pluginDir, isPluginInPackageManager);
            if (!isPluginInPackageManager)
            {
                FileUtil.DeleteFileOrDirectory(DefaultPluginExportPath + ".meta");
            }

            AssetDatabase.Refresh();
            return true;
        }

        /// <summary>
        /// A helper function to move all the files recursively from the default plugin dir to a custom location the publisher moved the plugin to.
        /// </summary>
        private static void MovePluginFiles(string fromDirectory, string pluginRoot, bool isPluginInPackageManager)
        {
            var files = Directory.GetFiles(fromDirectory);
            foreach (var file in files)
            {
                // We have to ignore some files, if the plugin is outside the Assets/ directory.
                if (isPluginInPackageManager && PluginPathsToIgnoreMoveWhenPluginInPackageManager.Any(pluginPathsToIgnore => file.Contains(pluginPathsToIgnore))) continue;

                // Check if the destination folder exists and create it if it doesn't exist
                var parentDirectory = Path.GetDirectoryName(file);
                var destinationDirectoryPath = parentDirectory.Replace(DefaultPluginExportPath, pluginRoot);
                if (!Directory.Exists(destinationDirectoryPath))
                {
                    Directory.CreateDirectory(destinationDirectoryPath);
                }

                // If the meta file is of a folder asset and doesn't have labels (it is auto generated by Unity), just delete it.
                if (IsAutoGeneratedFolderMetaFile(file))
                {
                    FileUtil.DeleteFileOrDirectory(file);
                    continue;
                }

                var destinationPath = file.Replace(DefaultPluginExportPath, pluginRoot);

                // Check if the file is already present at the destination path and delete it.
                if (File.Exists(destinationPath))
                {
                    FileUtil.DeleteFileOrDirectory(destinationPath);
                }

                FileUtil.MoveFileOrDirectory(file, destinationPath);
            }

            var directories = Directory.GetDirectories(fromDirectory);
            foreach (var directory in directories)
            {
                // We might have to ignore some directories, if the plugin is outside the Assets/ directory.
                if (isPluginInPackageManager && PluginPathsToIgnoreMoveWhenPluginInPackageManager.Any(pluginPathsToIgnore => directory.Contains(pluginPathsToIgnore))) continue;

                MovePluginFiles(directory, pluginRoot, isPluginInPackageManager);
            }

            if (!isPluginInPackageManager)
            {
                FileUtil.DeleteFileOrDirectory(fromDirectory);
            }
        }

        private static bool IsAutoGeneratedFolderMetaFile(string assetPath)
        {
            // Check if it is a meta file.
            if (!assetPath.EndsWith(".meta")) return false;

            var lines = File.ReadAllLines(assetPath);
            var isFolderAsset = false;
            var hasLabels = false;
            foreach (var line in lines)
            {
                if (line.Contains("folderAsset: yes"))
                {
                    isFolderAsset = true;
                }

                if (line.Contains("labels:"))
                {
                    hasLabels = true;
                }
            }

            // If it is a folder asset and doesn't have a label, the meta file is auto generated by
            return isFolderAsset && !hasLabels;
        }

        private static void CallDownloadPluginProgressCallback(string pluginName, float progress, bool isDone)
        {
            if (downloadPluginProgressCallback == null) return;

            downloadPluginProgressCallback(pluginName, progress, isDone);
        }

        private static void CallImportPackageCompletedCallback(Network network)
        {
            if (importPackageCompletedCallback == null) return;

            importPackageCompletedCallback(network);
        }

        private static object GetEditorUserBuildSetting(string name, object defaultValue)
        {
            var editorUserBuildSettingsType = typeof(EditorUserBuildSettings);
            var property = editorUserBuildSettingsType.GetProperty(name);
            if (property != null)
            {
                var value = property.GetValue(null, null);
                if (value != null) return value;
            }

            return defaultValue;
        }

        private static string GetPluginFileName(Network network)
        {
            return network.Name.ToLowerInvariant() + "_" + network.LatestVersions.Unity + ".unitypackage";
        }

        #endregion
    }
}
