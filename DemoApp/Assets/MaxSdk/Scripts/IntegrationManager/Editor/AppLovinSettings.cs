//
//  AppLovinSettings.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 1/27/20.
//  Copyright © 2019 AppLovin. All rights reserved.
//

using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// A <see cref="ScriptableObject"/> representing the AppLovin Settings that can be set in the Integration Manager Window.
///
/// The scriptable object asset is created with the name <c>AppLovinSettings.asset</c> and is placed under the directory <c>Assets/MaxSdk/Resources</c>.
/// </summary>
public class AppLovinSettings : ScriptableObject
{
    private static readonly string SettingsFile = Path.Combine("Assets/MaxSdk/Resources", "AppLovinSettings.asset");

    private static AppLovinSettings instance;

    [SerializeField] private bool qualityServiceEnabled = true;
    [SerializeField] private string sdkKey;

//    [SerializeField] private bool consentFlowEnabled;
//    [SerializeField] private string consentFlowTermsOfServiceUrl = string.Empty;
//    [SerializeField] private string consentFlowPrivacyPolicyUrl = string.Empty;
//    [SerializeField] private string userTrackingUsageDescription = string.Empty;

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
                var settingsDir = Path.GetDirectoryName(SettingsFile);
                if (!Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }

                instance = AssetDatabase.LoadAssetAtPath<AppLovinSettings>(SettingsFile);
                if (instance != null) return instance;

                instance = CreateInstance<AppLovinSettings>();
                AssetDatabase.CreateAsset(instance, SettingsFile);
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

//    /// <summary>
//    /// Whether or not AppLovin Consent Flow is enabled.
//    /// </summary>
//    public bool ConsentFlowEnabled
//    {
//        get { return Instance.consentFlowEnabled; }
//        set { Instance.consentFlowEnabled = value; }
//    }
//
//    /// <summary>
//    /// A URL pointing to the Terms of Service for the app to be shown when prompting the user for consent. 
//    /// </summary>
//    public string ConsentFlowTermsOfServiceUrl
//    {
//        get { return Instance.consentFlowTermsOfServiceUrl; }
//        set { Instance.consentFlowTermsOfServiceUrl = value; }
//    }
//
//    /// <summary>
//    /// A URL pointing to the Privacy Policy for the app to be shown when prompting the user for consent.
//    /// </summary>
//    public string ConsentFlowPrivacyPolicyUrl
//    {
//        get { return Instance.consentFlowPrivacyPolicyUrl; }
//        set { Instance.consentFlowPrivacyPolicyUrl = value; }
//    }
//
//    /// <summary>
//    /// A User Tracking Usage Description to be shown to users when requesting permission to use data for tracking.
//    /// For more information see <see cref="https://developer.apple.com/documentation/bundleresources/information_property_list/nsusertrackingusagedescription">Apple's documentation</see>.
//    /// </summary>
//    public string UserTrackingUsageDescription
//    {
//        get { return Instance.userTrackingUsageDescription; }
//        set { Instance.userTrackingUsageDescription = value; }
//    }

    /// <summary>
    /// AdMob Android App ID.
    /// </summary>
    public string AdMobAndroidAppId
    {
        get { return Instance.adMobAndroidAppId; }
        set { Instance.adMobAndroidAppId = value; }
    }

    /// <summary>
    /// AdMob iOS App ID
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
