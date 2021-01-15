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
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using VersionComparisonResult = MaxSdkUtils.VersionComparisonResult;

[Serializable]
public class PluginData
{
    public Network AppLovinMax;
    public Network[] MediatedNetworks;
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
    public string PluginFileName;
    public string DependenciesFilePath;
    public string[] PluginFilePaths;
    public Versions LatestVersions;
    [NonSerialized] public Versions CurrentVersions;
    [NonSerialized] public VersionComparisonResult CurrentToLatestVersionComparisonResult = VersionComparisonResult.Lesser;
    [NonSerialized] public bool RequiresUpdate;
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

    public override bool Equals(object value)
    {
        var versions = value as Versions;

        return versions != null
               && Unity.Equals(versions.Unity)
               && (Android == null || Android.Equals(versions.Android))
               && (Ios == null || Ios.Equals(versions.Ios));
    }

    public override int GetHashCode()
    {
        return new {Unity, Android, Ios}.GetHashCode();
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
        get { return GradleTemplateEnabled || (GradleBuildEnabled && IsUnity2018_2OrNewer()); }
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

    /// <summary>
    /// Loads the plugin data to be display by integration manager window.
    /// </summary>
    /// <param name="callback">Callback to be called once the plugin data download completes.</param>
    public IEnumerator LoadPluginData(Action<PluginData> callback)
    {
        var url = string.Format("https://dash.applovin.com/docs/v1/unity_integration_manager?plugin_version={0}", GetPluginVersionForUrl());
        var www = UnityWebRequest.Get(url);

#if UNITY_2017_2_OR_NEWER
        var operation = www.SendWebRequest();
#else
        var operation = www.Send();
#endif

        while (!operation.isDone) yield return new WaitForSeconds(0.1f); // Just wait till www is done. Our coroutine is pretty rudimentary.

#if UNITY_2017_2_OR_NEWER
        if (www.isNetworkError || www.isHttpError)
#else
        if (www.isError)
#endif
        {
            callback(null);
        }
        else
        {
            PluginData pluginData;
            try
            {
                pluginData = JsonUtility.FromJson<PluginData>(www.downloadHandler.text);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                pluginData = null;
            }

            if (pluginData != null)
            {
                // Get current version of the plugin
                var appLovinMax = pluginData.AppLovinMax;
                UpdateCurrentVersions(appLovinMax);

                // Get current versions for all the mediation networks.
                foreach (var network in pluginData.MediatedNetworks)
                {
                    UpdateCurrentVersions(network);
                }
            }

            callback(pluginData);
        }
    }

    /// <summary>
    /// Updates the CurrentVersion fields for a given network data object.
    /// </summary>
    /// <param name="network">Network for which to update the current versions.</param>
    public static void UpdateCurrentVersions(Network network)
    {
        var dependencyFilePath = Path.Combine(Application.dataPath, network.DependenciesFilePath);
        var currentVersions = GetCurrentVersions(dependencyFilePath);

        network.CurrentVersions = currentVersions;

        // If AppLovin mediation plugin, get the version from MaxSdk and the latest and current version comparison.
        if (network.Name.Equals("APPLOVIN_NETWORK"))
        {
            network.CurrentVersions.Unity = MaxSdk.Version;

            var unityVersionComparison = MaxSdkUtils.CompareVersions(network.CurrentVersions.Unity, network.LatestVersions.Unity);
            var androidVersionComparison = MaxSdkUtils.CompareVersions(network.CurrentVersions.Android, network.LatestVersions.Android);
            var iosVersionComparison = MaxSdkUtils.CompareVersions(network.CurrentVersions.Ios, network.LatestVersions.Ios);

            // Overall version is same if all the current and latest (from db) versions are same.
            if (unityVersionComparison == VersionComparisonResult.Equal &&
                androidVersionComparison == VersionComparisonResult.Equal &&
                iosVersionComparison == VersionComparisonResult.Equal)
            {
                network.CurrentToLatestVersionComparisonResult = VersionComparisonResult.Equal;
            }
            // One of the installed versions is newer than the latest versions which means that the publisher is on a beta version.
            else if (unityVersionComparison == VersionComparisonResult.Greater ||
                     androidVersionComparison == VersionComparisonResult.Greater ||
                     iosVersionComparison == VersionComparisonResult.Greater)
            {
                network.CurrentToLatestVersionComparisonResult = VersionComparisonResult.Greater;
            }
            // We have a new version available if all Android, iOS and Unity has a newer version available in db.
            else
            {
                network.CurrentToLatestVersionComparisonResult = VersionComparisonResult.Lesser;
            }
        }
        // For all other mediation adapters, get the version comparison using their Unity versions.
        else
        {
            // If adapter is indeed installed, compare the current (installed) and the latest (from db) versions, so that we can determine if the publisher is on an older, current or a newer version of the adapter.
            // If the publisher is on a newer version of the adapter than the db version, that means they are on a beta version.
            if (!string.IsNullOrEmpty(currentVersions.Unity))
            {
                network.CurrentToLatestVersionComparisonResult = MaxSdkUtils.CompareUnityMediationVersions(currentVersions.Unity, network.LatestVersions.Unity);
            }

            if (!string.IsNullOrEmpty(network.CurrentVersions.Unity) && AppLovinAutoUpdater.MinAdapterVersions.ContainsKey(network.Name))
            {
                var comparisonResult = MaxSdkUtils.CompareUnityMediationVersions(network.CurrentVersions.Unity, AppLovinAutoUpdater.MinAdapterVersions[network.Name]);
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
    /// Downloads the plugin file for a given network.
    /// </summary>
    /// <param name="network">Network for which to download the current version.</param>
    /// <returns></returns>
    public IEnumerator DownloadPlugin(Network network)
    {
        var path = Path.Combine(Application.temporaryCachePath, network.PluginFileName); // TODO: Maybe delete plugin file after finishing import.
#if UNITY_2017_2_OR_NEWER
        var downloadHandler = new DownloadHandlerFile(path);
#else
        var downloadHandler = new AppLovinDownloadHandler(path);
#endif
        webRequest = new UnityWebRequest(network.DownloadUrl)
        {
            method = UnityWebRequest.kHttpVerbGET,
            downloadHandler = downloadHandler
        };

#if UNITY_2017_2_OR_NEWER
        var operation = webRequest.SendWebRequest();
#else
        var operation = webRequest.Send();
#endif

        while (!operation.isDone)
        {
            yield return new WaitForSeconds(0.1f); // Just wait till webRequest is completed. Our coroutine is pretty rudimentary.
            CallDownloadPluginProgressCallback(network.DisplayName, operation.progress, operation.isDone);
        }


#if UNITY_2017_2_OR_NEWER
        if (webRequest.isNetworkError || webRequest.isHttpError)
#else
        if (webRequest.isError)
#endif
        {
            MaxSdkLogger.UserError(webRequest.error);
        }
        else
        {
            importingNetwork = network;
            AssetDatabase.ImportPackage(path, true);
        }

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
    /// Gets the current versions for a given network's dependency file path.
    /// </summary>
    /// <param name="dependencyPath">A dependency file path that from which to extract current versions.</param>
    /// <returns>Current versions of a given network's dependency file.</returns>
    private static Versions GetCurrentVersions(string dependencyPath)
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
            return new Versions();
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

        var currentVersions = new Versions();
        if (androidVersion != null && iosVersion != null)
        {
            currentVersions.Unity = string.Format("android_{0}_ios_{1}", androidVersion, iosVersion);
            currentVersions.Android = androidVersion;
            currentVersions.Ios = iosVersion;
        }
        else if (androidVersion != null)
        {
            currentVersions.Unity = string.Format("android_{0}", androidVersion);
            currentVersions.Android = androidVersion;
        }
        else if (iosVersion != null)
        {
            currentVersions.Unity = string.Format("ios_{0}", iosVersion);
            currentVersions.Ios = iosVersion;
        }

        return currentVersions;
    }

    /// <summary>
    /// Checks whether or not the given package name is the currently importing package.
    /// </summary>
    /// <param name="packageName">The name of the package that needs to be checked.</param>
    /// <returns>true if the importing package matches the given package name.</returns>
    private bool IsImportingNetwork(string packageName)
    {
        // Note: The pluginName doesn't have the '.unitypacakge' extension included in its name but the pluginFileName does. So using Contains instead of Equals.
        return importingNetwork != null && importingNetwork.PluginFileName.Contains(packageName);
    }

    /// <summary>
    /// Returns a URL friendly version string by replacing periods with underscores.
    /// </summary>
    private static string GetPluginVersionForUrl()
    {
        var version = MaxSdk.Version;
        var versionsSplit = version.Split('.');
        return string.Join("_", versionsSplit);
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

    private static bool IsUnity2018_2OrNewer()
    {
#if UNITY_2018_2_OR_NEWER
        return true;
#else
        return false;
#endif
    }

    #endregion
}
