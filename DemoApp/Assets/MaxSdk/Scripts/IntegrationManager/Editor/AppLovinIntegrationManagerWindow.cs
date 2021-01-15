//
//  MaxIntegrationManager.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 5/27/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using VersionComparisonResult = MaxSdkUtils.VersionComparisonResult;

public class AppLovinIntegrationManagerWindow : EditorWindow
{
    private const string windowTitle = "AppLovin Integration Manager";
    private const string appLovinSdkKeyLink = "https://dash.applovin.com/o/account#keys";
    private const string userTrackingUsageDescriptionDocsLink = "https://developer.apple.com/documentation/bundleresources/information_property_list/nsusertrackingusagedescription";
    private const string documentationAdaptersLink = "https://dash.applovin.com/documentation/mediation/unity/mediation-adapters";
    private const string documentationNote = "Please ensure that integration instructions (e.g. permissions, ATS settings, etc) specific to each network are implemented as well. Click the link below for more info:";
    private static readonly string uninstallIconPath = Path.Combine("Assets", "MaxSdk/Resources/Images/uninstall_icon.png");
    private static readonly string alertIconPath = Path.Combine("Assets", "MaxSdk/Resources/Images/alert_icon.png");

    private const string qualityServiceRequiresGradleBuildErrorMsg = "AppLovin Quality Service integration via AppLovin Integration Manager requires Custom Gradle Template enabled or Unity 2018.2 or higher.\n" +
                                                                     "If you would like to continue using your existing setup, please add Quality Service Plugin to your build.gradle manually.";

    private Vector2 scrollPosition;
    private static readonly Vector2 windowMinSize = new Vector2(750, 750);
    private const float actionFieldWidth = 60f;
    private const float networkFieldMinWidth = 100f;
    private const float versionFieldMinWidth = 190f;
    private const float privacySettingLabelWidth = 200f;
    private const float networkFieldWidthPercentage = 0.22f;
    private const float versionFieldWidthPercentage = 0.36f; // There are two version fields. Each take 40% of the width, network field takes the remaining 20%.
    private static float previousWindowWidth = windowMinSize.x;
    private static GUILayoutOption networkWidthOption = GUILayout.Width(networkFieldMinWidth);
    private static GUILayoutOption versionWidthOption = GUILayout.Width(versionFieldMinWidth);
    private static GUILayoutOption sdkKeyTextFieldWidthOption = GUILayout.Width(520);
    private static GUILayoutOption privacySettingFieldWidthOption = GUILayout.Width(400);
    private static readonly GUILayoutOption fieldWidth = GUILayout.Width(actionFieldWidth);

    private GUIStyle titleLabelStyle;
    private GUIStyle headerLabelStyle;
    private GUIStyle environmentValueStyle;
    private GUIStyle wrapTextLabelStyle;
    private GUIStyle linkLabelStyle;
    private GUIStyle uninstallButtonStyle;

    private PluginData pluginData;
    private bool pluginDataLoadFailed;

    private AppLovinEditorCoroutine loadDataCoroutine;
    private Texture2D uninstallIcon;
    private Texture2D alertIcon;

    public static void ShowManager()
    {
        var manager = GetWindow<AppLovinIntegrationManagerWindow>(utility: true, title: windowTitle, focus: true);
        manager.minSize = windowMinSize;
    }

    #region Editor Window Lifecyle Methods

    private void Awake()
    {
        titleLabelStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            fixedHeight = 20
        };

        headerLabelStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            fixedHeight = 18
        };

        environmentValueStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleRight
        };

        linkLabelStyle = new GUIStyle(EditorStyles.label)
        {
            wordWrap = true,
            normal = {textColor = Color.blue}
        };

        wrapTextLabelStyle = new GUIStyle(EditorStyles.label)
        {
            wordWrap = true
        };

        uninstallButtonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fixedWidth = 18,
            fixedHeight = 18,
            padding = new RectOffset(1, 1, 1, 1)
        };

        // Load uninstall icon texture.
        var uninstallIconData = File.ReadAllBytes(uninstallIconPath);
        uninstallIcon = new Texture2D(0, 0, TextureFormat.RGBA32, false); // 1. Initial size doesn't matter here, will be automatically resized once the image asset is loaded. 2. Set mipChain to false, else the texture has a weird blurry effect.
        uninstallIcon.LoadImage(uninstallIconData);

        // Load alert icon texture.
        var alertIconData = File.ReadAllBytes(alertIconPath);
        alertIcon = new Texture2D(0, 0, TextureFormat.RGBA32, false);
        alertIcon.LoadImage(alertIconData);
    }

    private void OnEnable()
    {
        AppLovinIntegrationManager.downloadPluginProgressCallback = OnDownloadPluginProgress;

        // Plugin downloaded and imported. Update current versions for the imported package.
        AppLovinIntegrationManager.importPackageCompletedCallback = AppLovinIntegrationManager.UpdateCurrentVersions;

        Load();
    }

    private void OnDisable()
    {
        if (loadDataCoroutine != null)
        {
            loadDataCoroutine.Stop();
            loadDataCoroutine = null;
        }

        AppLovinIntegrationManager.Instance.CancelDownload();
        EditorUtility.ClearProgressBar();

        // Saves the AppLovinSettings object if it has been changed.
        AssetDatabase.SaveAssets();
    }

    private void OnGUI()
    {
        // Immediately after downloading and importing a plugin the entire IDE reloads and current versions can be null in that case. Will just show loading text in that case.
        if (pluginData == null || pluginData.AppLovinMax.CurrentVersions == null)
        {
            DrawEmptyPluginData();
            return;
        }

        // OnGUI is called on each frame draw, so we don't want to do any unnecessary calculation if we can avoid it. So only calculate it when the width actually changed.
        if (Math.Abs(previousWindowWidth - position.width) > 1)
        {
            previousWindowWidth = position.width;
            CalculateFieldWidth();
        }

        using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition, false, false))
        {
            scrollPosition = scrollView.scrollPosition;

            GUILayout.Space(5);

            // Draw AppLovin MAX plugin details
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AppLovin MAX Plugin Details", titleLabelStyle, GUILayout.Width(220f)); // Seems like LabelField has an arbitrarily fixed width set by default. Need to set it to the preferred width (arrived at by trail & error) for the text to not be cut off (https://docs.unity3d.com/ScriptReference/EditorGUIUtility-labelWidth.html)
            GUILayout.FlexibleSpace();
            
            var autoUpdateEnabled = GUILayout.Toggle(EditorPrefs.GetBool(AppLovinAutoUpdater.KeyAutoUpdateEnabled, true), "  Enable Auto Update");
            EditorPrefs.SetBool(AppLovinAutoUpdater.KeyAutoUpdateEnabled, autoUpdateEnabled);
            GUILayout.Space(10);
            
            var verboseLoggingEnabled = GUILayout.Toggle(EditorPrefs.GetBool(MaxSdkLogger.KeyVerboseLoggingEnabled, false), "  Enable Verbose Logging");
            EditorPrefs.SetBool(MaxSdkLogger.KeyVerboseLoggingEnabled, verboseLoggingEnabled);
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
            DrawPluginDetails();

            // Draw mediated networks
            EditorGUILayout.LabelField("Mediated Networks", titleLabelStyle);
            DrawMediatedNetworks();

            // Draw AppLovin Quality Service settings
            EditorGUILayout.LabelField("AppLovin Quality Service", titleLabelStyle);
            DrawQualityServiceSettings();

//            EditorGUILayout.LabelField("Privacy Settings", titleLabelStyle);
//            DrawPrivacySettings();

            // Draw Unity environment details
            EditorGUILayout.LabelField("Unity Environment Details", titleLabelStyle);
            DrawUnityEnvironmentDetails();

            // Draw documentation notes
            EditorGUILayout.LabelField(new GUIContent(documentationNote), wrapTextLabelStyle);
            if (GUILayout.Button(new GUIContent(documentationAdaptersLink), linkLabelStyle))
            {
                Application.OpenURL(documentationAdaptersLink);
            }
        }

        if (GUI.changed)
        {
            AppLovinSettings.Instance.SaveAsync();
        }
    }

    #endregion

    #region UI Methods

    /// <summary>
    /// Shows failure or loading screen based on whether or not plugin data failed to load.
    /// </summary>
    private void DrawEmptyPluginData()
    {
        // Plugin data failed to load. Show error and retry button.
        if (pluginDataLoadFailed)
        {
            EditorGUILayout.LabelField("Failed to load plugin data. Please click retry or restart the integration manager.", headerLabelStyle);
            if (GUILayout.Button("Retry", fieldWidth))
            {
                pluginDataLoadFailed = false;
                Load();
            }
        }
        // Still loading, show loading label.
        else
        {
            EditorGUILayout.LabelField("Loading data...", headerLabelStyle);
        }
    }

    /// <summary>
    /// Draws AppLovin MAX plugin details.
    /// </summary>
    private void DrawPluginDetails()
    {
        var appLovinMax = pluginData.AppLovinMax;
        // Check if a newer version is available to enable the upgrade button.
        var upgradeButtonEnabled = appLovinMax.CurrentToLatestVersionComparisonResult == VersionComparisonResult.Lesser;

        GUILayout.BeginHorizontal();
        GUILayout.Space(10);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            // Draw plugin version details
            DrawHeaders("Platform", false);
            DrawPluginDetailRow("Unity 3D", appLovinMax.CurrentVersions.Unity, appLovinMax.LatestVersions.Unity);
            DrawPluginDetailRow("Android", appLovinMax.CurrentVersions.Android, appLovinMax.LatestVersions.Android);
            DrawPluginDetailRow("iOS", appLovinMax.CurrentVersions.Ios, appLovinMax.LatestVersions.Ios);
            
            // BeginHorizontal combined with FlexibleSpace makes sure that the button is centered horizontally.
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.enabled = upgradeButtonEnabled;
            if (GUILayout.Button(new GUIContent("Upgrade"), fieldWidth))
            {
                AppLovinEditorCoroutine.StartCoroutine(AppLovinIntegrationManager.Instance.DownloadPlugin(appLovinMax));
            }

            GUI.enabled = true;
            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
        }

        GUILayout.Space(5);
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draws the headers for a table.
    /// </summary>
    private void DrawHeaders(string firstColumnTitle, bool drawAction)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField(firstColumnTitle, headerLabelStyle, networkWidthOption);
            EditorGUILayout.LabelField("Current Version", headerLabelStyle, versionWidthOption);
            GUILayout.Space(3);
            EditorGUILayout.LabelField("Latest Version", headerLabelStyle, versionWidthOption);
            GUILayout.Space(3);
            if (drawAction)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Button("Actions", headerLabelStyle, fieldWidth);
                GUILayout.Space(5);
            }
        }

        GUILayout.Space(4);
    }

    /// <summary>
    /// Draws the platform specific version details for AppLovin MAX plugin.
    /// </summary>
    private void DrawPluginDetailRow(string platform, string currentVersion, string latestVersion)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField(new GUIContent(platform), networkWidthOption);
            EditorGUILayout.LabelField(new GUIContent(currentVersion), versionWidthOption);
            GUILayout.Space(3);
            EditorGUILayout.LabelField(new GUIContent(latestVersion), versionWidthOption);
            GUILayout.Space(3);
        }

        GUILayout.Space(4);
    }

    /// <summary>
    /// Draws mediated network details table.
    /// </summary>
    private void DrawMediatedNetworks()
    {
        var networks = pluginData.MediatedNetworks;
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            DrawHeaders("Network", true);
            foreach (var network in networks)
            {
                DrawNetworkDetailRow(network);
            }

            GUILayout.Space(5);
        }

        GUILayout.Space(5);
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draws the network specific details for a given network.
    /// </summary>
    private void DrawNetworkDetailRow(Network network)
    {
        string action;
        var currentVersion = network.CurrentVersions.Unity;
        var latestVersion = network.LatestVersions.Unity;
        bool isActionEnabled;
        bool isInstalled;
        if (string.IsNullOrEmpty(currentVersion))
        {
            action = "Install";
            currentVersion = "Not Installed";
            isActionEnabled = true;
            isInstalled = false;
        }
        else
        {
            isInstalled = true;

            var comparison = network.CurrentToLatestVersionComparisonResult;
            // A newer version is available
            if (comparison == VersionComparisonResult.Lesser)
            {
                action = "Upgrade";
                isActionEnabled = true;
            }
            // Current installed version is newer than latest version from DB (beta version)
            else if (comparison == VersionComparisonResult.Greater)
            {
                action = "Installed";
                isActionEnabled = false;
            }
            // Already on the latest version
            else
            {
                action = "Installed";
                isActionEnabled = false;
            }
        }

        GUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(false)))
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField(new GUIContent(network.DisplayName), networkWidthOption);
            EditorGUILayout.LabelField(new GUIContent(currentVersion), versionWidthOption);
            GUILayout.Space(3);
            EditorGUILayout.LabelField(new GUIContent(latestVersion), versionWidthOption);
            GUILayout.Space(3);
            GUILayout.FlexibleSpace();

            if (network.RequiresUpdate)
            {
                GUILayout.Label(new GUIContent {image = alertIcon, tooltip = "Adapter not compatible, please update to the latest version."}, uninstallButtonStyle);
            }

            GUI.enabled = isActionEnabled;
            if (GUILayout.Button(new GUIContent(action), fieldWidth))
            {
                // Download the plugin.
                AppLovinEditorCoroutine.StartCoroutine(AppLovinIntegrationManager.Instance.DownloadPlugin(network));
            }

            GUI.enabled = true;
            GUILayout.Space(2);

            GUI.enabled = isInstalled;
            if (GUILayout.Button(new GUIContent {image = uninstallIcon, tooltip = "Uninstall"}, uninstallButtonStyle))
            {
                EditorUtility.DisplayProgressBar("Integration Manager", "Deleting " + network.Name + "...", 0.5f);
                foreach (var pluginFilePath in network.PluginFilePaths)
                {
                    FileUtil.DeleteFileOrDirectory(Path.Combine("Assets", pluginFilePath));
                }

                AppLovinIntegrationManager.UpdateCurrentVersions(network);

                // Refresh UI
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
            }

            GUI.enabled = true;
            GUILayout.Space(5);
        }

        // Custom integration for AdMob where the user can enter the Android and iOS App IDs.
        if (network.Name.Equals("ADMOB_NETWORK") && isInstalled)
        {
            // Custom integration requires Google AdMob adapter version newer than android_19.0.1.0_ios_7.57.0.0.
            if (MaxSdkUtils.CompareUnityMediationVersions(network.CurrentVersions.Unity, "android_19.0.1.0_ios_7.57.0.0") == VersionComparisonResult.Greater)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    GUILayout.Space(2);
                    if (MaxSdkUtils.CompareUnityMediationVersions(network.CurrentVersions.Unity, "android_19.2.0.0_ios_7.61.0.0") == VersionComparisonResult.Greater)
                    {
                        AppLovinSettings.Instance.AdMobAndroidAppId = DrawTextField("App ID (Android)", AppLovinSettings.Instance.AdMobAndroidAppId, networkWidthOption);
                        AppLovinSettings.Instance.AdMobIosAppId = DrawTextField("App ID (iOS)", AppLovinSettings.Instance.AdMobIosAppId, networkWidthOption);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("The current version of AppLovin MAX plugin requires Google adapter version newer than android_19.2.0.0_ios_7.61.0.0 to enable auto-export of AdMob App ID.", MessageType.Warning);
                    }
                }

                GUILayout.EndHorizontal();
            }
        }
    }

    private void DrawQualityServiceSettings()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            AppLovinSettings.Instance.QualityServiceEnabled = GUILayout.Toggle(AppLovinSettings.Instance.QualityServiceEnabled, "  Enable MAX Ad Review");
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            if (!AppLovinIntegrationManager.CanProcessAndroidQualityServiceSettings)
            {
                GUILayout.Space(2);
                GUILayout.BeginHorizontal();
                GUILayout.Space(4);
                EditorGUILayout.HelpBox(qualityServiceRequiresGradleBuildErrorMsg, MessageType.Warning);
                GUILayout.Space(4);
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
            }

            GUI.enabled = AppLovinSettings.Instance.QualityServiceEnabled;
            AppLovinSettings.Instance.SdkKey = DrawTextField("AppLovin SDK Key", AppLovinSettings.Instance.SdkKey, networkWidthOption, sdkKeyTextFieldWidthOption);
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            GUILayout.Button("You can find your SDK key here: ", wrapTextLabelStyle, GUILayout.Width(185)); // Setting a fixed width since Unity adds arbitrary padding at the end leaving a space between link and text.
            if (GUILayout.Button(new GUIContent(appLovinSdkKeyLink), linkLabelStyle))
            {
                Application.OpenURL(appLovinSdkKeyLink);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            GUILayout.Space(4);
        }

        GUILayout.Space(5);
        GUILayout.EndHorizontal();
    }

    private string DrawTextField(string fieldTitle, string text, GUILayoutOption labelWidth, GUILayoutOption textFieldWidthOption = null)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(4);
        EditorGUILayout.LabelField(new GUIContent(fieldTitle), labelWidth);
        GUILayout.Space(4);
        text = (textFieldWidthOption == null) ? GUILayout.TextField(text) : GUILayout.TextField(text, textFieldWidthOption);
        GUILayout.Space(4);
        GUILayout.EndHorizontal();
        GUILayout.Space(4);

        return text;
    }

//    private void DrawPrivacySettings()
//    {
//        GUILayout.BeginHorizontal();
//        GUILayout.Space(10);
//        using (new EditorGUILayout.VerticalScope("box"))
//        {
//            GUILayout.Space(4);
//            GUILayout.BeginHorizontal();
//            GUILayout.Space(4);
//            AppLovinSettings.Instance.ConsentFlowEnabled = GUILayout.Toggle(AppLovinSettings.Instance.ConsentFlowEnabled, "  Enable Consent Flow (iOS Only)");
//            GUILayout.EndHorizontal();
//            GUILayout.Space(4);
//
//            GUI.enabled = AppLovinSettings.Instance.ConsentFlowEnabled;
//            if (!AppLovinSettings.Instance.ConsentFlowEnabled)
//            {
//                AppLovinSettings.Instance.ConsentFlowTermsOfServiceUrl = string.Empty;
//                AppLovinSettings.Instance.ConsentFlowPrivacyPolicyUrl = string.Empty;
//                AppLovinSettings.Instance.UserTrackingUsageDescription = string.Empty;
//            }
//            AppLovinSettings.Instance.ConsentFlowTermsOfServiceUrl = DrawTextField("Terms of Service URL", AppLovinSettings.Instance.ConsentFlowTermsOfServiceUrl, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
//            AppLovinSettings.Instance.ConsentFlowPrivacyPolicyUrl = DrawTextField("Privacy Policy URL", AppLovinSettings.Instance.ConsentFlowPrivacyPolicyUrl, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
//            AppLovinSettings.Instance.UserTrackingUsageDescription = DrawTextField("User Tracking Usage Description", AppLovinSettings.Instance.UserTrackingUsageDescription, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
//            GUI.enabled = true;
//
//            GUILayout.Space(4);
//            GUILayout.BeginHorizontal();
//            GUILayout.Space(4);
//            GUILayout.Button("Click the link below for more information about User Tracking Usage Description: ", wrapTextLabelStyle);
//            GUILayout.Space(4);
//            GUILayout.EndHorizontal();
//            GUILayout.BeginHorizontal();
//            GUILayout.Space(4);
//            if (GUILayout.Button(new GUIContent(userTrackingUsageDescriptionDocsLink), linkLabelStyle))
//            {
//                Application.OpenURL(userTrackingUsageDescriptionDocsLink);
//            }
//            GUILayout.Space(4);
//            GUILayout.EndHorizontal();
//            GUILayout.Space(4);
//        }
//
//        GUILayout.Space(5);
//        GUILayout.EndHorizontal();
//    }

    private void DrawUnityEnvironmentDetails()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            DrawUnityEnvironmentDetailRow("Unity Version", Application.unityVersion);
            GUILayout.Space(5);
            DrawUnityEnvironmentDetailRow("Platform", Application.platform.ToString());
            GUILayout.Space(5);
            DrawUnityEnvironmentDetailRow("External Dependency Manager Version", AppLovinIntegrationManager.ExternalDependencyManagerVersion);
            GUILayout.Space(5);
            DrawUnityEnvironmentDetailRow("Gradle Template Enabled", AppLovinIntegrationManager.GradleTemplateEnabled.ToString());
        }

        GUILayout.Space(5);
        GUILayout.EndHorizontal();
    }

    private void DrawUnityEnvironmentDetailRow(string key, string value)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField(key, GUILayout.Width(250));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(value, environmentValueStyle);
            GUILayout.Space(5);
        }
    }

    /// <summary>
    /// Calculates the fields width based on the width of the window.
    /// </summary>
    private void CalculateFieldWidth()
    {
        var currentWidth = position.width;
        var availableWidth = currentWidth - actionFieldWidth - 80; // NOTE: Magic number alert. This is the sum of all the spacing the fields and other UI elements.
        var networkLabelWidth = Math.Max(networkFieldMinWidth, availableWidth * networkFieldWidthPercentage);
        networkWidthOption = GUILayout.Width(networkLabelWidth);

        var versionLabelWidth = Math.Max(versionFieldMinWidth, availableWidth * versionFieldWidthPercentage);
        versionWidthOption = GUILayout.Width(versionLabelWidth);

        const int textFieldOtherUiElementsWidth = 45; // NOTE: Magic number alert. This is the sum of all the spacing the fields and other UI elements.
        var availableTextFieldWidth = currentWidth - networkLabelWidth - textFieldOtherUiElementsWidth;
        sdkKeyTextFieldWidthOption = GUILayout.Width(availableTextFieldWidth);

        var availableUserDescriptionTextFieldWidth = currentWidth - privacySettingLabelWidth - textFieldOtherUiElementsWidth;
        privacySettingFieldWidthOption = GUILayout.Width(availableUserDescriptionTextFieldWidth);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Loads the plugin data to be displayed by this window.
    /// </summary>
    private void Load()
    {
        loadDataCoroutine = AppLovinEditorCoroutine.StartCoroutine(AppLovinIntegrationManager.Instance.LoadPluginData(data =>
        {
            if (data == null)
            {
                pluginDataLoadFailed = true;
            }
            else
            {
                pluginData = data;
                pluginDataLoadFailed = false;
            }

            CalculateFieldWidth();
            Repaint();
        }));
    }

    /// <summary>
    /// Callback method that will be called with progress updates when the plugin is being downloaded.
    /// </summary>
    public static void OnDownloadPluginProgress(string pluginName, float progress, bool done)
    {
        // Download is complete. Clear progress bar.
        if (done)
        {
            EditorUtility.ClearProgressBar();
        }
        // Download is in progress, update progress bar.
        else
        {
            if (EditorUtility.DisplayCancelableProgressBar(windowTitle, string.Format("Downloading {0} plugin...", pluginName), progress))
            {
                AppLovinIntegrationManager.Instance.CancelDownload();
                EditorUtility.ClearProgressBar();
            }
        }
    }

    #endregion
}
