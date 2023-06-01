//
//  MaxIntegrationManager.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 5/27/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VersionComparisonResult = MaxSdkUtils.VersionComparisonResult;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    public class AppLovinIntegrationManagerWindow : EditorWindow
    {
        private const string keyNewLocalizationsMarked = "com.applovin.new_localizations_marked_v0"; // Update the key version each time new localizations are added.

        private const string windowTitle = "AppLovin Integration Manager";

        private const string appLovinSdkKeyLink = "https://dash.applovin.com/o/account#keys";

        private const string userTrackingUsageDescriptionDocsLink = "https://developer.apple.com/documentation/bundleresources/information_property_list/nsusertrackingusagedescription";
        private const string documentationAdaptersLink = "https://dash.applovin.com/documentation/mediation/unity/mediation-adapters";
        private const string documentationNote = "Please ensure that integration instructions (e.g. permissions, ATS settings, etc) specific to each network are implemented as well. Click the link below for more info:";
        private const string uninstallIconExportPath = "MaxSdk/Resources/Images/uninstall_icon.png";
        private const string alertIconExportPath = "MaxSdk/Resources/Images/alert_icon.png";
        private const string warningIconExportPath = "MaxSdk/Resources/Images/warning_icon.png";

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

        private static readonly Color darkModeTextColor = new Color(0.29f, 0.6f, 0.8f);

        private GUIStyle titleLabelStyle;
        private GUIStyle headerLabelStyle;
        private GUIStyle environmentValueStyle;
        private GUIStyle wrapTextLabelStyle;
        private GUIStyle linkLabelStyle;
        private GUIStyle iconStyle;

        private PluginData pluginData;
        private bool pluginDataLoadFailed;
        private bool isPluginMoved;
        private bool shouldMarkNewLocalizations;
        private bool shouldShowGoogleWarning;

        private AppLovinEditorCoroutine loadDataCoroutine;
        private Texture2D uninstallIcon;
        private Texture2D alertIcon;
        private Texture2D warningIcon;

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
                normal = {textColor = EditorGUIUtility.isProSkin ? darkModeTextColor : Color.blue}
            };

            wrapTextLabelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };

            iconStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fixedWidth = 18,
                fixedHeight = 18,
                padding = new RectOffset(1, 1, 1, 1)
            };

            // Load uninstall icon texture.
            var uninstallIconData = File.ReadAllBytes(MaxSdkUtils.GetAssetPathForExportPath(uninstallIconExportPath));
            uninstallIcon = new Texture2D(0, 0, TextureFormat.RGBA32, false); // 1. Initial size doesn't matter here, will be automatically resized once the image asset is loaded. 2. Set mipChain to false, else the texture has a weird blurry effect.
            uninstallIcon.LoadImage(uninstallIconData);

            // Load alert icon texture.
            var alertIconData = File.ReadAllBytes(MaxSdkUtils.GetAssetPathForExportPath(alertIconExportPath));
            alertIcon = new Texture2D(0, 0, TextureFormat.RGBA32, false);
            alertIcon.LoadImage(alertIconData);

            // Load warning icon texture.
            var warningIconData = File.ReadAllBytes(MaxSdkUtils.GetAssetPathForExportPath(warningIconExportPath));
            warningIcon = new Texture2D(0, 0, TextureFormat.RGBA32, false);
            warningIcon.LoadImage(warningIconData);

            var pluginPath = Path.Combine(AppLovinIntegrationManager.PluginParentDirectory, "MaxSdk");
            isPluginMoved = !AppLovinIntegrationManager.DefaultPluginExportPath.Equals(pluginPath);
        }

        private void OnEnable()
        {
            shouldMarkNewLocalizations = !EditorPrefs.GetBool(keyNewLocalizationsMarked, false);

            AppLovinIntegrationManager.downloadPluginProgressCallback = OnDownloadPluginProgress;

            // Plugin downloaded and imported. Update current versions for the imported package.
            AppLovinIntegrationManager.importPackageCompletedCallback = OnImportPackageCompleted;

            Load();
        }

        private void OnDisable()
        {
            // New localizations have been shown to the publisher, now remove them.
            if (shouldMarkNewLocalizations)
            {
                EditorPrefs.SetBool(keyNewLocalizationsMarked, true);
            }

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
                EditorGUILayout.LabelField("AppLovin MAX Plugin Details", titleLabelStyle);

                DrawPluginDetails();

                // Draw mediated networks
                EditorGUILayout.LabelField("Mediated Networks", titleLabelStyle);
                DrawMediatedNetworks();

                // Draw AppLovin Quality Service settings
                EditorGUILayout.LabelField("AppLovin Quality Service", titleLabelStyle);
                DrawQualityServiceSettings();

                EditorGUILayout.LabelField("Privacy Settings", titleLabelStyle);
                DrawPrivacySettings();

                EditorGUILayout.LabelField("Other Settings", titleLabelStyle);
                DrawOtherSettings();

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
            GUILayout.Space(5);

            // Plugin data failed to load. Show error and retry button.
            if (pluginDataLoadFailed)
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Failed to load plugin data. Please click retry or restart the integration manager.", titleLabelStyle);
                if (GUILayout.Button("Retry", fieldWidth))
                {
                    pluginDataLoadFailed = false;
                    Load();
                }

                GUILayout.Space(5);
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }
            // Still loading, show loading label.
            else
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Loading data...", titleLabelStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }

            GUILayout.Space(5);
        }

        /// <summary>
        /// Draws AppLovin MAX plugin details.
        /// </summary>
        private void DrawPluginDetails()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                // Draw plugin version details
                DrawHeaders("Platform", false);

                // Immediately after downloading and importing a plugin the entire IDE reloads and current versions can be null in that case. Will just show loading text in that case.
                if (pluginData == null || pluginData.AppLovinMax.CurrentVersions == null)
                {
                    DrawEmptyPluginData();
                }
                else
                {
                    var appLovinMax = pluginData.AppLovinMax;
                    // Check if a newer version is available to enable the upgrade button.
                    var upgradeButtonEnabled = appLovinMax.CurrentToLatestVersionComparisonResult == VersionComparisonResult.Lesser;
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
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawHeaders("Network", true);

                // Immediately after downloading and importing a plugin the entire IDE reloads and current versions can be null in that case. Will just show loading text in that case.
                if (pluginData == null || pluginData.AppLovinMax.CurrentVersions == null)
                {
                    DrawEmptyPluginData();
                }
                else
                {
                    var networks = pluginData.MediatedNetworks;
                    foreach (var network in networks)
                    {
                        DrawNetworkDetailRow(network);
                    }

                    GUILayout.Space(5);
                }
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
                    GUILayout.Label(new GUIContent {image = alertIcon, tooltip = "Adapter not compatible, please update to the latest version."}, iconStyle);
                }
                else if ((network.Name.Equals("ADMOB_NETWORK") || network.Name.Equals("GOOGLE_AD_MANAGER_NETWORK")) && shouldShowGoogleWarning)
                {
                    GUILayout.Label(new GUIContent {image = warningIcon, tooltip = "You may see unexpected errors if you use different versions of the AdMob and Google Ad Manager adapter SDKs."}, iconStyle);
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
                if (GUILayout.Button(new GUIContent {image = uninstallIcon, tooltip = "Uninstall"}, iconStyle))
                {
                    EditorUtility.DisplayProgressBar("Integration Manager", "Deleting " + network.Name + "...", 0.5f);
                    var pluginRoot = AppLovinIntegrationManager.MediationSpecificPluginParentDirectory;
                    foreach (var pluginFilePath in network.PluginFilePaths)
                    {
                        FileUtil.DeleteFileOrDirectory(Path.Combine(pluginRoot, pluginFilePath));
                    }

                    AppLovinIntegrationManager.UpdateCurrentVersions(network, pluginRoot);

                    // Refresh UI
                    AssetDatabase.Refresh();
                    EditorUtility.ClearProgressBar();
                }

                GUI.enabled = true;
                GUILayout.Space(5);
            }

            if (isInstalled)
            {
                // Custom integration for AdMob where the user can enter the Android and iOS App IDs.
                if (network.Name.Equals("ADMOB_NETWORK"))
                {
                    // Custom integration requires Google AdMob adapter version newer than android_19.0.1.0_ios_7.57.0.0.
                    if (MaxSdkUtils.CompareUnityMediationVersions(network.CurrentVersions.Unity, "android_19.0.1.0_ios_7.57.0.0") == VersionComparisonResult.Greater)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        using (new EditorGUILayout.VerticalScope("box"))
                        {
                            string requiredVersion;
                            string warningMessage;
                            if (isPluginMoved)
                            {
                                requiredVersion = "android_19.6.0.1_ios_7.69.0.0";
                                warningMessage = "Looks like the MAX plugin has been moved to a different directory. This requires Google adapter version newer than " + requiredVersion + " for auto-export of AdMob App ID to work correctly.";
                            }
                            else
                            {
                                requiredVersion = "android_19.2.0.0_ios_7.61.0.0";
                                warningMessage = "The current version of AppLovin MAX plugin requires Google adapter version newer than " + requiredVersion + " to enable auto-export of AdMob App ID.";
                            }

                            GUILayout.Space(2);
                            if (MaxSdkUtils.CompareUnityMediationVersions(network.CurrentVersions.Unity, requiredVersion) == VersionComparisonResult.Greater)
                            {
                                AppLovinSettings.Instance.AdMobAndroidAppId = DrawTextField("App ID (Android)", AppLovinSettings.Instance.AdMobAndroidAppId, networkWidthOption);
                                AppLovinSettings.Instance.AdMobIosAppId = DrawTextField("App ID (iOS)", AppLovinSettings.Instance.AdMobIosAppId, networkWidthOption);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox(warningMessage, MessageType.Warning);
                            }
                        }

                        GUILayout.EndHorizontal();
                    }
                }
                // Snap requires SCAppStoreAppID to be set starting adapter version 2.0.0.0 or newer. Show a text field for the publisher to input the App ID.
                else if (network.Name.Equals("SNAP_NETWORK") &&
                         MaxSdkUtils.CompareVersions(network.CurrentVersions.Ios, AppLovinSettings.SnapAppStoreAppIdMinVersion) != VersionComparisonResult.Lesser)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        GUILayout.Space(2);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(4);
                        EditorGUILayout.LabelField(new GUIContent("App Store App ID (iOS)"), networkWidthOption);
                        GUILayout.Space(4);
                        AppLovinSettings.Instance.SnapAppStoreAppId = EditorGUILayout.IntField(AppLovinSettings.Instance.SnapAppStoreAppId);
                        GUILayout.Space(4);
                        GUILayout.EndHorizontal();
                        GUILayout.Space(2);
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

        private void DrawPrivacySettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                GUILayout.Space(4);
                AppLovinSettings.Instance.ConsentFlowEnabled = GUILayout.Toggle(AppLovinSettings.Instance.ConsentFlowEnabled, "  Enable Consent Flow (iOS Only)");
                GUILayout.EndHorizontal();
                GUILayout.Space(4);

                GUI.enabled = AppLovinSettings.Instance.ConsentFlowEnabled;

                AppLovinSettings.Instance.ConsentFlowPrivacyPolicyUrl = DrawTextField("Privacy Policy URL", AppLovinSettings.Instance.ConsentFlowPrivacyPolicyUrl, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
                AppLovinSettings.Instance.ConsentFlowTermsOfServiceUrl = DrawTextField("Terms of Service URL (optional)", AppLovinSettings.Instance.ConsentFlowTermsOfServiceUrl, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
                AppLovinSettings.Instance.UserTrackingUsageDescriptionEn = DrawTextField("User Tracking Usage Description", AppLovinSettings.Instance.UserTrackingUsageDescriptionEn, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);

                GUILayout.BeginHorizontal();
                GUILayout.Space(4);
                AppLovinSettings.Instance.UserTrackingUsageLocalizationEnabled = GUILayout.Toggle(AppLovinSettings.Instance.UserTrackingUsageLocalizationEnabled, "  Localize User Tracking Usage Description");
                GUILayout.EndHorizontal();
                GUILayout.Space(4);

                if (AppLovinSettings.Instance.UserTrackingUsageLocalizationEnabled)
                {
                    AppLovinSettings.Instance.UserTrackingUsageDescriptionZhHans = DrawTextField("Chinese, Simplified (zh-Hans)", AppLovinSettings.Instance.UserTrackingUsageDescriptionZhHans, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
                    AppLovinSettings.Instance.UserTrackingUsageDescriptionZhHant = DrawTextField("Chinese, Traditional (zh-Hant)" + (shouldMarkNewLocalizations ? " *" : ""), AppLovinSettings.Instance.UserTrackingUsageDescriptionZhHant, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption); // TODO: Remove new mark for next release.
                    AppLovinSettings.Instance.UserTrackingUsageDescriptionFr = DrawTextField("French (fr)", AppLovinSettings.Instance.UserTrackingUsageDescriptionFr, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
                    AppLovinSettings.Instance.UserTrackingUsageDescriptionDe = DrawTextField("German (de)", AppLovinSettings.Instance.UserTrackingUsageDescriptionDe, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
                    AppLovinSettings.Instance.UserTrackingUsageDescriptionJa = DrawTextField("Japanese (ja)", AppLovinSettings.Instance.UserTrackingUsageDescriptionJa, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
                    AppLovinSettings.Instance.UserTrackingUsageDescriptionKo = DrawTextField("Korean (ko)", AppLovinSettings.Instance.UserTrackingUsageDescriptionKo, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
                    AppLovinSettings.Instance.UserTrackingUsageDescriptionEs = DrawTextField("Spanish (es)", AppLovinSettings.Instance.UserTrackingUsageDescriptionEs, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);

                    GUILayout.Space(4);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(4);
                    EditorGUILayout.HelpBox((shouldMarkNewLocalizations ? "* " : "") + "MAX may add more localized strings to this list in the future, which will set the default value of the User Tracking Usage Description string for more locales. If you are overriding these with your own custom translations, you may want to review this list whenever you upgrade the plugin to see if there are new entries you may want to customize.", MessageType.Info);
                    GUILayout.Space(4);
                    GUILayout.EndHorizontal();

                    GUILayout.Space(4);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(4);
                    EditorGUILayout.HelpBox("If you have your own implementation of InfoPlist.strings localization implementation, please use that instead. Using both at the same time may cause conflicts.", MessageType.Info);
                    GUILayout.Space(4);
                    GUILayout.EndHorizontal();
                }

                GUI.enabled = true;

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                GUILayout.Space(4);
                GUILayout.Button("Click the link below for more information about User Tracking Usage Description: ", wrapTextLabelStyle);
                GUILayout.Space(4);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(4);
                if (GUILayout.Button(new GUIContent(userTrackingUsageDescriptionDocsLink), linkLabelStyle))
                {
                    Application.OpenURL(userTrackingUsageDescriptionDocsLink);
                }

                GUILayout.Space(4);
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }

        private void DrawOtherSettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Space(5);
                AppLovinSettings.Instance.SetAttributionReportEndpoint = DrawOtherSettingsToggle(AppLovinSettings.Instance.SetAttributionReportEndpoint, "  Set Advertising Attribution Report Endpoint in Info.plist (iOS only)");
                GUILayout.Space(5);
                var autoUpdateEnabled = DrawOtherSettingsToggle(EditorPrefs.GetBool(AppLovinAutoUpdater.KeyAutoUpdateEnabled, true), "  Enable Auto Update");
                EditorPrefs.SetBool(AppLovinAutoUpdater.KeyAutoUpdateEnabled, autoUpdateEnabled);
                GUILayout.Space(5);
                var verboseLoggingEnabled = DrawOtherSettingsToggle(EditorPrefs.GetBool(MaxSdkLogger.KeyVerboseLoggingEnabled, false),
#if UNITY_2018_2_OR_NEWER
                    "  Enable Verbose Logging"
#else
                    "  Enable Build Verbose Logging"
#endif
                );
                EditorPrefs.SetBool(MaxSdkLogger.KeyVerboseLoggingEnabled, verboseLoggingEnabled);
                GUILayout.Space(5);
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }

        private bool DrawOtherSettingsToggle(bool value, string text)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(4);
                var toggleValue = GUILayout.Toggle(value, text);
                GUILayout.Space(4);

                return toggleValue;
            }
        }

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

                    UpdateShouldShowGoogleWarningIfNeeded();
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

        private void OnImportPackageCompleted(Network network)
        {
            var parentDirectory = network.Name.Equals("APPLOVIN_NETWORK") ? AppLovinIntegrationManager.PluginParentDirectory : AppLovinIntegrationManager.MediationSpecificPluginParentDirectory;
            AppLovinIntegrationManager.UpdateCurrentVersions(network, parentDirectory);

            UpdateShouldShowGoogleWarningIfNeeded();
        }

        private void UpdateShouldShowGoogleWarningIfNeeded()
        {
            if (pluginData == null)
            {
                shouldShowGoogleWarning = false;
                return;
            }

            var networks = pluginData.MediatedNetworks;
            var googleNetwork = networks.FirstOrDefault(foundNetwork => foundNetwork.Name.Equals("ADMOB_NETWORK"));
            var googleAdManagerNetwork = networks.FirstOrDefault(foundNetwork => foundNetwork.Name.Equals("GOOGLE_AD_MANAGER_NETWORK"));

            if (googleNetwork != null && googleAdManagerNetwork != null &&
                !string.IsNullOrEmpty(googleNetwork.CurrentVersions.Unity) && !string.IsNullOrEmpty(googleAdManagerNetwork.CurrentVersions.Unity) &&
                !googleNetwork.CurrentVersions.HasEqualSdkVersions(googleAdManagerNetwork.CurrentVersions))
            {
                shouldShowGoogleWarning = true;
            }
            else
            {
                shouldShowGoogleWarning = false;
            }
        }

        #endregion
    }
}
