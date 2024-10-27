//
//  AppLovinSettings.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 1/27/20.
//  Copyright © 2019 AppLovin. All rights reserved.
//

using AppLovinMax.Scripts.IntegrationManager.Editor;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// A <see cref="ScriptableObject"/> representing the AppLovin Settings that can be set in the Integration Manager Window.
///
/// The scriptable object asset is created with the name <c>AppLovinSettings.asset</c> and is placed under the directory <c>Assets/MaxSdk/Resources</c>.
///
/// NOTE: Not name spacing this class since it is reflected upon by the Google adapter and will break compatibility.
/// </summary>
public class AppLovinSettings : ScriptableObject
{
    public const string SettingsExportPath = "MaxSdk/Resources/AppLovinSettings.asset";

    private static AppLovinSettings instance;

    [SerializeField] private bool qualityServiceEnabled = true;
    [SerializeField] private string sdkKey;

    [SerializeField] private bool setAttributionReportEndpoint;
    [SerializeField] private bool addApsSkAdNetworkIds;

    [SerializeField] private string customGradleVersionUrl;
    [SerializeField] private string customGradleToolsVersion;

    [SerializeField] private string adMobAndroidAppId = string.Empty;
    [SerializeField] private string adMobIosAppId = string.Empty;

    /// <summary>
    /// An instance of AppLovin Setting.
    /// </summary>
    public static AppLovinSettings Instance
    {
        get
        {
            if (instance == null)
            {
                // Check for an existing AppLovinSettings somewhere in the project
                var guids = AssetDatabase.FindAssets("AppLovinSettings t:ScriptableObject");
                if (guids.Length > 1)
                {
                    MaxSdkLogger.UserWarning("Multiple AppLovinSettings found. This may cause unexpected results.");
                }

                if (guids.Length != 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    instance = AssetDatabase.LoadAssetAtPath<AppLovinSettings>(path);
                    return instance;
                }

                // If there is no existing AppLovinSettings asset, create one in the default location
                string settingsFilePath;
                // The settings file should be under the Assets/ folder so that it can be version controlled and cannot be overriden when updating.
                // If the plugin is outside the Assets folder, create the settings asset at the default location.
                if (AppLovinIntegrationManager.IsPluginInPackageManager)
                {
                    // Note: Can't use absolute path when calling `CreateAsset`. Should use relative path to Assets/ directory.
                    settingsFilePath = Path.Combine("Assets", SettingsExportPath);

                    var maxSdkDir = Path.Combine(Application.dataPath, "MaxSdk");
                    if (!Directory.Exists(maxSdkDir))
                    {
                        Directory.CreateDirectory(maxSdkDir);
                    }
                }
                else
                {
                    settingsFilePath = Path.Combine(AppLovinIntegrationManager.PluginParentDirectory, SettingsExportPath);
                }

                var settingsDir = Path.GetDirectoryName(settingsFilePath);
                if (!Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }

                // On script reload AssetDatabase.FindAssets() can fail and will overwrite AppLovinSettings without this check
                if (!File.Exists(settingsFilePath))
                {
                    instance = CreateInstance<AppLovinSettings>();
                    AssetDatabase.CreateAsset(instance, settingsFilePath);
                    MaxSdkLogger.D("Creating new AppLovinSettings asset at path: " + settingsFilePath);
                }
            }

            return instance;
        }
    }

    /// <summary>
    /// Whether or not to install Quality Service plugin.
    /// </summary>
    public bool QualityServiceEnabled
    {
        get { return Instance.qualityServiceEnabled; }
        set { Instance.qualityServiceEnabled = value; }
    }

    /// <summary>
    /// AppLovin SDK Key.
    /// </summary>
    public string SdkKey
    {
        get { return Instance.sdkKey; }
        set { Instance.sdkKey = value; }
    }

    /// <summary>
    /// Whether or not to set `NSAdvertisingAttributionReportEndpoint` in Info.plist.
    /// </summary>
    public bool SetAttributionReportEndpoint
    {
        get { return Instance.setAttributionReportEndpoint; }
        set { Instance.setAttributionReportEndpoint = value; }
    }

    /// <summary>
    /// Whether or not to add Amazon Publisher Services SKAdNetworkID's.
    /// </summary>
    public bool AddApsSkAdNetworkIds
    {
        get { return Instance.addApsSkAdNetworkIds; }
        set { Instance.addApsSkAdNetworkIds = value; }
    }

    /// <summary>
    /// A URL to set the distributionUrl in the gradle-wrapper.properties file (ex: https\://services.gradle.org/distributions/gradle-6.9.3-bin.zip)
    /// </summary>
    public string CustomGradleVersionUrl
    {
        get { return Instance.customGradleVersionUrl;  }
        set { Instance.customGradleVersionUrl = value; }
    }

    /// <summary>
    /// A string to set the custom gradle tools version (ex: com.android.tools.build:gradle:4.2.0)
    /// </summary>
    public string CustomGradleToolsVersion
    {
        get { return Instance.customGradleToolsVersion;  }
        set { Instance.customGradleToolsVersion = value; }
    }

    /// <summary>
    /// AdMob Android App ID.
    /// </summary>
    public string AdMobAndroidAppId
    {
        get { return Instance.adMobAndroidAppId; }
        set { Instance.adMobAndroidAppId = value; }
    }

    /// <summary>
    /// AdMob iOS App ID.
    /// </summary>
    public string AdMobIosAppId
    {
        get { return Instance.adMobIosAppId; }
        set { Instance.adMobIosAppId = value; }
    }

    /// <summary>
    /// Saves the instance of the settings.
    /// </summary>
    public void SaveAsync()
    {
        EditorUtility.SetDirty(instance);
    }
}
