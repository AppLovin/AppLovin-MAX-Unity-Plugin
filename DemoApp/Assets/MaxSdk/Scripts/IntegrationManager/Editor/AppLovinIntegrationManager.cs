//
//  MaxIntegrationManager.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 6/1/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

using System;
using System.Collections;
using System.IO;
using AppLovinMax.Internal;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    [Serializable]
    public class PluginData
    {
        // ReSharper disable InconsistentNaming - Consistent with JSON data.
        public Network AppLovinMax;
        public Network[] MediatedNetworks;
        public Network[] PartnerMicroSdks;
        public DynamicLibraryToEmbed[] ThirdPartyDynamicLibrariesToEmbed;
        public Alert[] Alerts;
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

        // ReSharper disable InconsistentNaming - Consistent with JSON data.
        public string Name;
        public string DisplayName;
        public string DownloadUrl;
        public string DependenciesFilePath;
        public PackageInfo[] Packages;
        public string[] PluginFilePaths;
        public Versions LatestVersions;
        public DynamicLibraryToEmbed[] DynamicLibrariesToEmbed;

        [NonSerialized] public Versions CurrentVersions;
        [NonSerialized] public MaxSdkUtils.VersionComparisonResult CurrentToLatestVersionComparisonResult = MaxSdkUtils.VersionComparisonResult.Lesser;
        [NonSerialized] public bool RequiresUpdate;
        [NonSerialized] public bool IsCurrentlyInstalling;
    }

    [Serializable]
    public class DynamicLibraryToEmbed
    {
        // ReSharper disable InconsistentNaming - Consistent with JSON data.
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

    public enum Severity
    {
        Info,
        Warning,
        Error
    }

    [Serializable]
    public class Alert
    {
        public string SeverityType;
        public string Title;
        public string Message;
        public string Url;

        public Severity Severity;

        public void InitializeSeverityEnum()
        {
            switch (SeverityType)
            {
                case "INFO":
                    Severity = Severity.Info;
                    break;
                case "WARNING":
                    Severity = Severity.Warning;
                    break;
                case "ERROR":
                    Severity = Severity.Error;
                    break;
                default:
                    MaxSdkLogger.E(string.Format("Alert <{0}> has unsupported severity type <{1}>.", Title, SeverityType));
                    Severity = Severity.Info;
                    break;
            }
        }
    }

    /// <summary>
    /// A helper data class used to get current versions from Dependency.xml files.
    /// </summary>
    [Serializable]
    public class Versions
    {
        // ReSharper disable InconsistentNaming - Consistent with JSON data.
        public string Unity;
        public string Android;
        public string Ios;

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
            return new {unity = Unity, android = Android, ios = Ios}.GetHashCode();
        }

        private static string AdapterSdkVersion(string adapterVersion)
        {
            if (string.IsNullOrEmpty(adapterVersion)) return "";

            var index = adapterVersion.LastIndexOf(".", StringComparison.Ordinal);
            return index > 0 ? adapterVersion.Substring(0, index) : adapterVersion;
        }
    }

    /// <summary>
    /// A manager class for MAX integration manager window.
    /// </summary>
    public class AppLovinIntegrationManager
    {
        /// <summary>
        /// Delegate to be called when a plugin package's import is started.
        /// </summary>
        internal delegate void ImportPackageStartedCallback(Network network);

        /// <summary>
        /// Delegate to be called when a plugin package is finished importing.
        /// </summary>
        /// <param name="network">The network data for which the package is imported.</param>
        internal delegate void ImportPackageCompletedCallback(Network network);

        private static readonly AppLovinIntegrationManager instance = new AppLovinIntegrationManager();

        internal static readonly string GradleTemplatePath = Path.Combine("Assets/Plugins/Android", "mainTemplate.gradle");
        private const string MaxSdkAssetExportPath = "MaxSdk/Scripts/MaxSdk.cs";
        private const string MaxSdkMediationExportPath = "MaxSdk/Mediation";

        private const string PluginDataEndpoint = "https://unity.applovin.com/max/1.0/integration_manager_info?plugin_version={0}";

        private static string externalDependencyManagerVersion;

        internal static ImportPackageStartedCallback OnImportPackageStartedCallback;
        internal static ImportPackageCompletedCallback OnImportPackageCompletedCallback;

        private MaxWebRequest maxWebRequest;
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
                // Search for the asset with the export path label.
                // Paths are normalized using AltDirectorySeparatorChar (/) to ensure compatibility across platforms (in case of migrating a project from Windows to Mac or vice versa).
                var maxSdkScriptAssetPath = MaxSdkUtils.GetAssetPathForExportPath(MaxSdkAssetExportPath);

                // maxSdkScriptAssetPath will always have AltDirectorySeparatorChar (/) as the path separator. Convert to platform specific path.
                return maxSdkScriptAssetPath.Replace(MaxSdkAssetExportPath, "")
                    .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }
        }

        public static string MediationDirectory
        {
            get
            {
                var mediationAssetPath = MaxSdkUtils.GetAssetPathForExportPath(MaxSdkMediationExportPath);
                return mediationAssetPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
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
                if (MaxSdkUtils.IsValidString(externalDependencyManagerVersion)) return externalDependencyManagerVersion;

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
            AssetDatabase.importPackageStarted += packageName =>
            {
                if (!IsImportingNetwork(packageName)) return;

                CallImportPackageStartedCallback(importingNetwork);
            };

            // Add asset import callbacks.
            AssetDatabase.importPackageCompleted += packageName =>
            {
                if (!IsImportingNetwork(packageName)) return;

                AssetDatabase.Refresh();

                CallImportPackageCompletedCallback(importingNetwork);
                importingNetwork = null;
            };

            AssetDatabase.importPackageCancelled += packageName =>
            {
                if (!IsImportingNetwork(packageName)) return;

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
            var webRequestConfig = new WebRequestConfig()
            {
                EndPoint = url,
            };

            var maxWebRequest = new MaxWebRequest(webRequestConfig);
            var webResponse = maxWebRequest.SendSync();

            return CreatePluginDataFromWebResponse(webResponse);
        }

        /// <summary>
        /// Loads the plugin data to be display by integration manager window.
        /// </summary>
        /// <param name="callback">Callback to be called once the plugin data download completes.</param>
        public IEnumerator LoadPluginData(Action<PluginData> callback)
        {
            var url = string.Format(PluginDataEndpoint, MaxSdk.Version);
            var webRequestConfig = new WebRequestConfig()
            {
                EndPoint = url,
            };

            maxWebRequest = new MaxWebRequest(webRequestConfig);
            yield return maxWebRequest.Send(webResponse =>
            {
                var pluginData = CreatePluginDataFromWebResponse(webResponse);
                callback(pluginData);
            });
        }

        private static PluginData CreatePluginDataFromWebResponse(WebResponse webResponse)
        {
            if (!webResponse.IsSuccess)
            {
                MaxSdkLogger.E("Failed to load plugin data. Please check your internet connection.");
                return null;
            }

            PluginData pluginData;
            try
            {
                pluginData = JsonUtility.FromJson<PluginData>(webResponse.ResponseMessage);
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

            if (pluginData.Alerts == null) return pluginData;

            // Initiate Severity enums from the raw strings in the response
            foreach (var alert in pluginData.Alerts)
            {
                alert.InitializeSeverityEnum();
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
            var webRequestConfig = new WebRequestConfig()
            {
                DownloadHandler = new DownloadHandlerFile(path),
                EndPoint = network.DownloadUrl
            };

            maxWebRequest = new MaxWebRequest(webRequestConfig);
            yield return maxWebRequest.Send(webResponse =>
            {
                if (webResponse.IsSuccess)
                {
                    importingNetwork = network;
                    AssetDatabase.ImportPackage(path, showImport);
                }
                else
                {
                    MaxSdkLogger.UserError("Failed to download plugin package: " + webResponse.ErrorMessage);
                }
            });
        }

        /// <summary>
        /// Cancels the plugin download if one is in progress.
        /// </summary>
        public void CancelDownload()
        {
            if (maxWebRequest == null) return;

            maxWebRequest.Abort();
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

        private static void CallImportPackageStartedCallback(Network network)
        {
            if (OnImportPackageStartedCallback == null) return;

            OnImportPackageStartedCallback(network);
        }

        private static void CallImportPackageCompletedCallback(Network network)
        {
            if (OnImportPackageCompletedCallback == null) return;

            OnImportPackageCompletedCallback(network);
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
