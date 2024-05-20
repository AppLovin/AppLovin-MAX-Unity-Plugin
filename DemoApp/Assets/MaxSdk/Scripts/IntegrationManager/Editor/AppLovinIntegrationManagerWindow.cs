//
//  MaxIntegrationManager.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 5/27/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VersionComparisonResult = MaxSdkUtils.VersionComparisonResult;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    public class AppLovinIntegrationManagerWindow : EditorWindow
    {
        private const string windowTitle = "AppLovin Integration Manager";

        private const string appLovinSdkKeyLink = "https://dash.applovin.com/o/account#keys";

        private const string userTrackingUsageDescriptionDocsLink = "https://developer.apple.com/documentation/bundleresources/information_property_list/nsusertrackingusagedescription";
        private const string documentationTermsAndPrivacyPolicyFlow = "https://developers.applovin.com/en/unity/overview/terms-and-privacy-policy-flow";
        private const string documentationAdaptersLink = "https://developers.applovin.com/en/unity/preparing-mediated-networks";
        private const string documentationNote = "Please ensure that integration instructions (e.g. permissions, ATS settings, etc) specific to each network are implemented as well. Click the link below for more info:";
        private const string uninstallIconExportPath = "MaxSdk/Resources/Images/uninstall_icon.png";
        private const string alertIconExportPath = "MaxSdk/Resources/Images/alert_icon.png";
        private const string warningIconExportPath = "MaxSdk/Resources/Images/warning_icon.png";

        private const string qualityServiceRequiresGradleBuildErrorMsg = "AppLovin Quality Service integration via AppLovin Integration Manager requires Custom Gradle Template enabled or Unity 2018.2 or higher.\n" +
                                                                         "If you would like to continue using your existing setup, please add Quality Service Plugin to your build.gradle manually.";

        private const string customGradleVersionTooltip = "To set the version to 6.9.3, set the field to: https://services.gradle.org/distributions/gradle-6.9.3-bin.zip";
        private const string customGradleToolsVersionTooltip = "To set the version to 4.2.0, set the field to: 4.2.0";

        private readonly string[] termsFlowPlatforms = new string[3] {"Both", "Android", "iOS"};
        private readonly string[] debugUserGeographies = new string[2] {"Not Set", "GDPR"};

        private Vector2 scrollPosition;
        private static readonly Vector2 windowMinSize = new Vector2(750, 750);
        private const float actionFieldWidth = 60f;
        private const float upgradeAllButtonWidth = 80f;
        private const float networkFieldMinWidth = 100f;
        private const float versionFieldMinWidth = 190f;
        private const float privacySettingLabelWidth = 250f;
        private const float networkFieldWidthPercentage = 0.22f;
        private const float versionFieldWidthPercentage = 0.36f; // There are two version fields. Each take 40% of the width, network field takes the remaining 20%.
        private static float previousWindowWidth = windowMinSize.x;
        private static GUILayoutOption networkWidthOption = GUILayout.Width(networkFieldMinWidth);
        private static GUILayoutOption versionWidthOption = GUILayout.Width(versionFieldMinWidth);

        private static GUILayoutOption privacySettingFieldWidthOption = GUILayout.Width(400);
        private static readonly GUILayoutOption fieldWidth = GUILayout.Width(actionFieldWidth);
        private static readonly GUILayoutOption upgradeAllButtonFieldWidth = GUILayout.Width(upgradeAllButtonWidth);

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
        private bool shouldShowGoogleWarning;
        private bool networkButtonsEnabled = true;

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
            // Script reloads can cause AppLovinSettings.Instance to be null for one frame,
            // so we load the Integration Manager on the following frame
            if (AppLovinSettings.Instance == null)
            {
                AppLovinEditorCoroutine.StartCoroutine(WaitForNextFrameForEnable());
            }
            else
            {
                OnWindowEnabled();
            }
        }

        private IEnumerator WaitForNextFrameForEnable()
        {
            yield return new WaitForEndOfFrame();
            OnWindowEnabled();
        }

        private void OnWindowEnabled()
        {
            AppLovinIntegrationManager.downloadPluginProgressCallback = OnDownloadPluginProgress;

            // Plugin downloaded and imported. Update current versions for the imported package.
            AppLovinIntegrationManager.importPackageCompletedCallback = OnImportPackageCompleted;

            // Disable old consent flow if new flow is enabled.
            if (AppLovinInternalSettings.Instance.ConsentFlowEnabled)
            {
                AppLovinSettings.Instance.ConsentFlowEnabled = false;
                AppLovinSettings.Instance.SaveAsync();
            }

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

                if (pluginData != null && pluginData.PartnerMicroSdks != null)
                {
                    EditorGUILayout.LabelField("AppLovin Micro SDK Partners", titleLabelStyle);
                    DrawPartnerMicroSdks();
                }

                // Draw mediated networks
                using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(false)))
                {
                    EditorGUILayout.LabelField("Mediated Networks", titleLabelStyle);
                    DrawUpgradeAllButton();
                }

                DrawMediatedNetworks();

                // Draw AppLovin Quality Service settings
                EditorGUILayout.LabelField("SDK Settings", titleLabelStyle);
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
                AppLovinInternalSettings.Instance.Save();
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

                    GUILayout.Space(10);
                }
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }

        private void DrawPartnerMicroSdks()
        {
            if (pluginData == null) return;

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawHeaders("Network", true);

                var partnerMicroSdks = pluginData.PartnerMicroSdks;
                foreach (var partnerMicroSdk in partnerMicroSdks)
                {
                    DrawNetworkDetailRow(partnerMicroSdk);
                }

                GUILayout.Space(10);
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
            var currentVersion = network.CurrentVersions != null ? network.CurrentVersions.Unity : "";
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

                GUI.enabled = networkButtonsEnabled && isActionEnabled;
                if (GUILayout.Button(new GUIContent(action), fieldWidth))
                {
                    // Download the plugin.
                    AppLovinEditorCoroutine.StartCoroutine(AppLovinIntegrationManager.Instance.DownloadPlugin(network));
                }

                GUI.enabled = true;
                GUILayout.Space(2);

                GUI.enabled = networkButtonsEnabled && isInstalled;
                if (GUILayout.Button(new GUIContent {image = uninstallIcon, tooltip = "Uninstall"}, iconStyle))
                {
                    EditorUtility.DisplayProgressBar("Integration Manager", "Deleting " + network.Name + "...", 0.5f);
                    var pluginRoot = AppLovinIntegrationManager.MediationSpecificPluginParentDirectory;
                    foreach (var pluginFilePath in network.PluginFilePaths)
                    {
                        var filePath = Path.Combine(pluginRoot, pluginFilePath);
                        FileUtil.DeleteFileOrDirectory(filePath);
                        FileUtil.DeleteFileOrDirectory(filePath + ".meta");
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
                DrawGoogleAppIdTextBoxIfNeeded(network);
            }
        }

        private void DrawGoogleAppIdTextBoxIfNeeded(Network network)
        {
            // Custom integration for AdMob where the user can enter the Android and iOS App IDs.
            if (network.Name.Equals("ADMOB_NETWORK"))
            {
                // Show only one set of text boxes if both ADMOB and GAM are installed
                if (AppLovinIntegrationManager.IsAdapterInstalled("GoogleAdManager")) return;

                DrawGoogleAppIdTextBox();
            }

            // Custom integration for GAM where the user can enter the Android and iOS App IDs.
            else if (network.Name.Equals("GOOGLE_AD_MANAGER_NETWORK"))
            {
                DrawGoogleAppIdTextBox();
            }
        }

        /// <summary>
        /// Draws the text box for GAM or ADMOB to input the App ID
        /// </summary>
        private void DrawGoogleAppIdTextBox()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Space(2);
                AppLovinSettings.Instance.AdMobAndroidAppId = DrawTextField("App ID (Android)", AppLovinSettings.Instance.AdMobAndroidAppId, networkWidthOption);
                AppLovinSettings.Instance.AdMobIosAppId = DrawTextField("App ID (iOS)", AppLovinSettings.Instance.AdMobIosAppId, networkWidthOption);
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the upgrade all button
        /// </summary>
        private void DrawUpgradeAllButton()
        {
            GUI.enabled = NetworksRequireUpgrade();
            if (GUILayout.Button(new GUIContent("Upgrade All"), upgradeAllButtonFieldWidth))
            {
                AppLovinEditorCoroutine.StartCoroutine(UpgradeAllNetworks());
            }

            GUI.enabled = true;
            GUILayout.Space(10);
        }

        private void DrawQualityServiceSettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Space(4);
                if (!AppLovinIntegrationManager.CanProcessAndroidQualityServiceSettings)
                {
                    GUILayout.Space(4);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(4);
                    EditorGUILayout.HelpBox(qualityServiceRequiresGradleBuildErrorMsg, MessageType.Warning);
                    GUILayout.Space(4);
                    GUILayout.EndHorizontal();

                    GUILayout.Space(4);
                }

                AppLovinSettings.Instance.SdkKey = DrawTextField("AppLovin SDK Key", AppLovinSettings.Instance.SdkKey, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
                GUILayout.BeginHorizontal();
                GUILayout.Space(4);
                GUILayout.Button("You can find your SDK key here: ", wrapTextLabelStyle, GUILayout.Width(185)); // Setting a fixed width since Unity adds arbitrary padding at the end leaving a space between link and text.
                if (GUILayout.Button(new GUIContent(appLovinSdkKeyLink), linkLabelStyle))
                {
                    Application.OpenURL(appLovinSdkKeyLink);
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                GUILayout.Space(4);
                AppLovinSettings.Instance.QualityServiceEnabled = GUILayout.Toggle(AppLovinSettings.Instance.QualityServiceEnabled, "  Enable MAX Ad Review");
                GUILayout.EndHorizontal();
                GUILayout.Space(4);

                GUILayout.Space(4);
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }

        private string DrawTextField(string fieldTitle, string text, GUILayoutOption labelWidth, GUILayoutOption textFieldWidthOption = null, bool isTextFieldEditable = true, string tooltip = "")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            var guiContent = MaxSdkUtils.IsValidString(tooltip) ? new GUIContent(fieldTitle, tooltip) : new GUIContent(fieldTitle);
            EditorGUILayout.LabelField(guiContent, labelWidth);
            GUILayout.Space(4);
            if (isTextFieldEditable)
            {
                text = (textFieldWidthOption == null) ? GUILayout.TextField(text) : GUILayout.TextField(text, textFieldWidthOption);
            }
            else
            {
                if (textFieldWidthOption == null)
                {
                    GUILayout.Label(text);
                }
                else
                {
                    GUILayout.Label(text, textFieldWidthOption);
                }
            }

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
                if (AppLovinSettings.Instance.ConsentFlowEnabled)
                {
                    DrawTermsFlowSettings();
                }
                else
                {
                    DrawConsentFlowSettings();
                }
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }

        private void DrawTermsFlowSettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            EditorGUILayout.HelpBox("The Terms Flow has been deprecated; switch to the MAX Terms and Privacy Policy Flow instead.", MessageType.Warning); // TODO Refine
            GUILayout.Space(4);
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Switch to MAX Terms and Privacy Policy Flow"))
            {
                AppLovinInternalSettings.Instance.ConsentFlowPrivacyPolicyUrl = AppLovinSettings.Instance.ConsentFlowPrivacyPolicyUrl;
                AppLovinInternalSettings.Instance.ConsentFlowTermsOfServiceUrl = AppLovinSettings.Instance.ConsentFlowTermsOfServiceUrl;
                AppLovinInternalSettings.Instance.ConsentFlowEnabled = true;
                AppLovinSettings.Instance.ConsentFlowEnabled = false;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            AppLovinSettings.Instance.ConsentFlowEnabled = GUILayout.Toggle(AppLovinSettings.Instance.ConsentFlowEnabled, "  Enable Terms Flow");
            GUILayout.FlexibleSpace();
            GUI.enabled = AppLovinSettings.Instance.ConsentFlowEnabled;
            AppLovinSettings.Instance.ConsentFlowPlatform = (Platform) EditorGUILayout.Popup((int) AppLovinSettings.Instance.ConsentFlowPlatform, termsFlowPlatforms);
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            AppLovinSettings.Instance.ConsentFlowPrivacyPolicyUrl = DrawTextField("Privacy Policy URL", AppLovinSettings.Instance.ConsentFlowPrivacyPolicyUrl, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
            AppLovinSettings.Instance.ConsentFlowTermsOfServiceUrl = DrawTextField("Terms of Service URL (optional)", AppLovinSettings.Instance.ConsentFlowTermsOfServiceUrl, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
            AppLovinSettings.Instance.UserTrackingUsageDescriptionEn = DrawTextField("User Tracking Usage Description (iOS only)", AppLovinSettings.Instance.UserTrackingUsageDescriptionEn, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);

            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            AppLovinSettings.Instance.UserTrackingUsageLocalizationEnabled = GUILayout.Toggle(AppLovinSettings.Instance.UserTrackingUsageLocalizationEnabled, "  Localize User Tracking Usage Description (iOS only)");
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            if (AppLovinSettings.Instance.UserTrackingUsageLocalizationEnabled)
            {
                AppLovinSettings.Instance.UserTrackingUsageDescriptionZhHans = DrawTextField("Chinese, Simplified (zh-Hans)", AppLovinSettings.Instance.UserTrackingUsageDescriptionZhHans, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
                AppLovinSettings.Instance.UserTrackingUsageDescriptionZhHant = DrawTextField("Chinese, Traditional (zh-Hant)", AppLovinSettings.Instance.UserTrackingUsageDescriptionZhHant, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption); // TODO: Remove new mark for next release.
                AppLovinSettings.Instance.UserTrackingUsageDescriptionFr = DrawTextField("French (fr)", AppLovinSettings.Instance.UserTrackingUsageDescriptionFr, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
                AppLovinSettings.Instance.UserTrackingUsageDescriptionDe = DrawTextField("German (de)", AppLovinSettings.Instance.UserTrackingUsageDescriptionDe, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
                AppLovinSettings.Instance.UserTrackingUsageDescriptionJa = DrawTextField("Japanese (ja)", AppLovinSettings.Instance.UserTrackingUsageDescriptionJa, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
                AppLovinSettings.Instance.UserTrackingUsageDescriptionKo = DrawTextField("Korean (ko)", AppLovinSettings.Instance.UserTrackingUsageDescriptionKo, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
                AppLovinSettings.Instance.UserTrackingUsageDescriptionEs = DrawTextField("Spanish (es)", AppLovinSettings.Instance.UserTrackingUsageDescriptionEs, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                GUILayout.Space(4);
                EditorGUILayout.HelpBox("MAX may add more localized strings to this list in the future, which will set the default value of the User Tracking Usage Description string for more locales. If you are overriding these with your own custom translations, you may want to review this list whenever you upgrade the plugin to see if there are new entries you may want to customize.", MessageType.Info);
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

        private void DrawConsentFlowSettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            AppLovinInternalSettings.Instance.ConsentFlowEnabled = GUILayout.Toggle(AppLovinInternalSettings.Instance.ConsentFlowEnabled, "  Enable MAX Terms and Privacy Policy Flow");
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
            GUILayout.Space(4);
            EditorGUILayout.HelpBox("This flow automatically includes Google UMP.", MessageType.Info);

            GUI.enabled = true;

            if (!AppLovinInternalSettings.Instance.ConsentFlowEnabled) return;

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            EditorGUILayout.LabelField("Click the link below to access the guide on creating the GDPR form within AdMob's dashboard.");
            GUILayout.Space(4);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            if (GUILayout.Button(new GUIContent(documentationTermsAndPrivacyPolicyFlow), linkLabelStyle))
            {
                Application.OpenURL(documentationTermsAndPrivacyPolicyFlow);
            }

            GUILayout.Space(4);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            AppLovinInternalSettings.Instance.ConsentFlowPrivacyPolicyUrl = DrawTextField("Privacy Policy URL", AppLovinInternalSettings.Instance.ConsentFlowPrivacyPolicyUrl, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);
            AppLovinInternalSettings.Instance.ConsentFlowTermsOfServiceUrl = DrawTextField("Terms of Service URL (optional)", AppLovinInternalSettings.Instance.ConsentFlowTermsOfServiceUrl, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption);

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            EditorGUILayout.LabelField("iOS specific settings:", headerLabelStyle);
            GUILayout.EndHorizontal();

            var isEditableTextField = AppLovinInternalSettings.Instance.OverrideDefaultUserTrackingUsageDescriptions;

            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            AppLovinInternalSettings.Instance.OverrideDefaultUserTrackingUsageDescriptions = GUILayout.Toggle(AppLovinInternalSettings.Instance.OverrideDefaultUserTrackingUsageDescriptions, "  Override Default User Tracking Usage Description");
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            GUILayout.Space(4);
            AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionEn = DrawTextField("User Tracking Usage Description", AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionEn, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption, isEditableTextField);

            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            AppLovinInternalSettings.Instance.UserTrackingUsageLocalizationEnabled = GUILayout.Toggle(AppLovinInternalSettings.Instance.UserTrackingUsageLocalizationEnabled, "  Localize User Tracking Usage Description");
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            if (AppLovinInternalSettings.Instance.UserTrackingUsageLocalizationEnabled)
            {
                AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionZhHans = DrawTextField("Chinese, Simplified (zh-Hans)", AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionZhHans, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption, isEditableTextField);
                AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionZhHant = DrawTextField("Chinese, Traditional (zh-Hant)", AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionZhHant, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption, isEditableTextField);
                AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionFr = DrawTextField("French (fr)", AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionFr, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption, isEditableTextField);
                AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionDe = DrawTextField("German (de)", AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionDe, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption, isEditableTextField);
                AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionJa = DrawTextField("Japanese (ja)", AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionJa, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption, isEditableTextField);
                AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionKo = DrawTextField("Korean (ko)", AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionKo, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption, isEditableTextField);
                AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionEs = DrawTextField("Spanish (es)", AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionEs, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption, isEditableTextField);

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                GUILayout.Space(4);
                EditorGUILayout.HelpBox("If you have your own implementation of InfoPlist.strings localization implementation, please use that instead. Using both at the same time may cause conflicts.", MessageType.Info);
                GUILayout.Space(4);
                GUILayout.EndHorizontal();
            }

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
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            EditorGUILayout.LabelField("Testing:", headerLabelStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            EditorGUILayout.LabelField("Debug User Geography");
            AppLovinInternalSettings.Instance.DebugUserGeography = (MaxSdkBase.ConsentFlowUserGeography) EditorGUILayout.Popup((int) AppLovinInternalSettings.Instance.DebugUserGeography, debugUserGeographies, privacySettingFieldWidthOption);
            GUILayout.Space(4);
            GUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Debug User Geography is only enabled in debug mode", MessageType.Info);
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
                AppLovinSettings.Instance.AddApsSkAdNetworkIds = DrawOtherSettingsToggle(AppLovinSettings.Instance.AddApsSkAdNetworkIds, "  Add Amazon Publisher Services SKAdNetworkID's");
                GUILayout.Space(5);
                var autoUpdateEnabled = DrawOtherSettingsToggle(EditorPrefs.GetBool(AppLovinAutoUpdater.KeyAutoUpdateEnabled, true), "  Enable Auto Update", "Checks for AppLovin MAX plugin updates and notifies you when an update is available.");
                EditorPrefs.SetBool(AppLovinAutoUpdater.KeyAutoUpdateEnabled, autoUpdateEnabled);
                GUILayout.Space(5);
                var verboseLoggingEnabled = DrawOtherSettingsToggle(EditorPrefs.GetBool(MaxSdkLogger.KeyVerboseLoggingEnabled, false), "  Enable Verbose Logging");
                EditorPrefs.SetBool(MaxSdkLogger.KeyVerboseLoggingEnabled, verboseLoggingEnabled);
                GUILayout.Space(5);
                AppLovinSettings.Instance.CustomGradleVersionUrl = DrawTextField("Custom Gradle Version URL", AppLovinSettings.Instance.CustomGradleVersionUrl, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption, tooltip: customGradleVersionTooltip);
                AppLovinSettings.Instance.CustomGradleToolsVersion = DrawTextField("Custom Gradle Tools Version", AppLovinSettings.Instance.CustomGradleToolsVersion, GUILayout.Width(privacySettingLabelWidth), privacySettingFieldWidthOption, tooltip: customGradleToolsVersionTooltip);
                EditorGUILayout.HelpBox("This will overwrite the gradle build tools version in your base gradle template.", MessageType.Info);
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }

        private bool DrawOtherSettingsToggle(bool value, string text, string tooltip = "")
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(4);
                var content = MaxSdkUtils.IsValidString(tooltip) ? new GUIContent(text, tooltip) : new GUIContent(text);
                var toggleValue = GUILayout.Toggle(value, content);
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

        /// <summary>
        /// Upgrades all outdated networks
        /// </summary>
        private IEnumerator UpgradeAllNetworks()
        {
            networkButtonsEnabled = false;
            EditorApplication.LockReloadAssemblies();
            var networks = pluginData.MediatedNetworks;
            foreach (var network in networks)
            {
                var comparison = network.CurrentToLatestVersionComparisonResult;
                // A newer version is available
                if (!string.IsNullOrEmpty(network.CurrentVersions.Unity) && comparison == VersionComparisonResult.Lesser)
                {
                    yield return AppLovinIntegrationManager.Instance.DownloadPlugin(network, false);
                }
            }

            EditorApplication.UnlockReloadAssemblies();
            networkButtonsEnabled = true;

            // The pluginData becomes stale after the networks have been updated, and we should re-load it.
            Load();
        }

        /// <summary>
        /// Returns whether any network adapter needs to be upgraded
        /// </summary>
        private bool NetworksRequireUpgrade()
        {
            if (pluginData == null || pluginData.AppLovinMax.CurrentVersions == null) return false;

            var networks = pluginData.MediatedNetworks;
            return networks.Any(network => !string.IsNullOrEmpty(network.CurrentVersions.Unity) && network.CurrentToLatestVersionComparisonResult == VersionComparisonResult.Lesser);
        }

        #endregion
    }
}
