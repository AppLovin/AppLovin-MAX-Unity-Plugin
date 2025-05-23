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

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    public class AppLovinIntegrationManagerWindow : EditorWindow
    {
        private const string WindowTitle = "AppLovin Integration Manager";

        private const string AppLovinSdkKeyLink = "https://dash.applovin.com/o/account#keys";

        private const string UserTrackingUsageDescriptionDocsLink = "https://developer.apple.com/documentation/bundleresources/information_property_list/nsusertrackingusagedescription";
        private const string DocumentationTermsAndPrivacyPolicyFlow = "https://developers.applovin.com/en/unity/overview/terms-and-privacy-policy-flow";
        private const string DocumentationAdaptersLink = "https://developers.applovin.com/en/unity/preparing-mediated-networks";
        private const string DocumentationNote = "Please ensure that integration instructions (e.g. permissions, ATS settings, etc) specific to each network are implemented as well. Click the link below for more info:";
        private const string UninstallIconExportPath = "MaxSdk/Resources/Images/uninstall_icon.png";
        private const string AlertIconExportPath = "MaxSdk/Resources/Images/alert_icon.png";
        private const string WarningIconExportPath = "MaxSdk/Resources/Images/warning_icon.png";

        private const string QualityServiceRequiresGradleBuildErrorMsg = "AppLovin Quality Service integration via AppLovin Integration Manager requires Custom Gradle Template enabled or Unity 2018.2 or higher.\n" +
                                                                         "If you would like to continue using your existing setup, please add Quality Service Plugin to your build.gradle manually.";

        private const string CustomGradleVersionTooltip = "To set the version to 6.9.3, set the field to: https://services.gradle.org/distributions/gradle-6.9.3-bin.zip";
        private const string CustomGradleToolsVersionTooltip = "To set the version to 4.2.0, set the field to: 4.2.0";

        private const string KeyShowMicroSdkPartners = "com.applovin.show_micro_sdk_partners";
        private const string KeyShowMediatedNetworks = "com.applovin.show_mediated_networks";
        private const string KeyShowSdkSettings = "com.applovin.show_sdk_settings";
        private const string KeyShowPrivacySettings = "com.applovin.show_privacy_settings";
        private const string KeyShowOtherSettings = "com.applovin.show_other_settings";

        private const string ExpandButtonText = "+";
        private const string CollapseButtonText = "-";

        private const string ExternalDependencyManagerPath = "Assets/ExternalDependencyManager";

        private readonly string[] debugUserGeographies = new string[2] {"Not Set", "GDPR"};

        private Vector2 scrollPosition;
        private static readonly Vector2 WindowMinSize = new Vector2(750, 750);
        private const float ActionFieldWidth = 60f;
        private const float UpgradeAllButtonWidth = 80f;
        private const float NetworkFieldMinWidth = 100f;
        private const float VersionFieldMinWidth = 190f;
        private const float PrivacySettingLabelWidth = 250f;
        private const float NetworkFieldWidthPercentage = 0.22f;
        private const float VersionFieldWidthPercentage = 0.36f; // There are two version fields. Each take 40% of the width, network field takes the remaining 20%.
        private static float _previousWindowWidth = WindowMinSize.x;
        private static GUILayoutOption _networkWidthOption = GUILayout.Width(NetworkFieldMinWidth);
        private static GUILayoutOption _versionWidthOption = GUILayout.Width(VersionFieldMinWidth);

        private static GUILayoutOption _privacySettingFieldWidthOption = GUILayout.Width(400);
        private static readonly GUILayoutOption FieldWidth = GUILayout.Width(ActionFieldWidth);
        private static readonly GUILayoutOption UpgradeAllButtonFieldWidth = GUILayout.Width(UpgradeAllButtonWidth);
        private static readonly GUILayoutOption CollapseButtonWidthOption = GUILayout.Width(20f);

        private static readonly Color DarkModeTextColor = new Color(0.29f, 0.6f, 0.8f);

        private GUIStyle titleLabelStyle;
        private GUIStyle headerLabelStyle;
        private GUIStyle environmentValueStyle;
        private GUIStyle wrapTextLabelStyle;
        private GUIStyle linkLabelStyle;
        private GUIStyle iconStyle;

        private PluginData pluginData;
        private bool pluginDataLoadFailed;
        private bool shouldShowGoogleWarning;
        private bool networkButtonsEnabled = true;

        private AppLovinEditorCoroutine loadDataCoroutine;
        private Texture2D uninstallIcon;
        private Texture2D alertIcon;
        private Texture2D warningIcon;

        public static void ShowManager()
        {
            var manager = GetWindow<AppLovinIntegrationManagerWindow>(utility: true, title: WindowTitle, focus: true);
            manager.minSize = WindowMinSize;
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
                normal = {textColor = EditorGUIUtility.isProSkin ? DarkModeTextColor : Color.blue}
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
            var uninstallIconData = File.ReadAllBytes(MaxSdkUtils.GetAssetPathForExportPath(UninstallIconExportPath));
            // 1. Set the initial size to 1, as Unity 6000 no longer supports a width or height of 0.
            // 2. The image will be automatically resized once the image asset is loaded.
            // 3. Set mipChain to false, else the texture has a weird blurry effect.
            uninstallIcon = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            uninstallIcon.LoadImage(uninstallIconData);

            // Load alert icon texture.
            var alertIconData = File.ReadAllBytes(MaxSdkUtils.GetAssetPathForExportPath(AlertIconExportPath));
            alertIcon = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            alertIcon.LoadImage(alertIconData);

            // Load warning icon texture.
            var warningIconData = File.ReadAllBytes(MaxSdkUtils.GetAssetPathForExportPath(WarningIconExportPath));
            warningIcon = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            warningIcon.LoadImage(warningIconData);
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
            AppLovinIntegrationManager.OnDownloadPluginProgressCallback = OnDownloadPluginProgress;

            // Plugin downloaded and imported. Update current versions for the imported package.
            AppLovinIntegrationManager.OnImportPackageCompletedCallback = OnImportPackageCompleted;

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
            if (Math.Abs(_previousWindowWidth - position.width) > 1)
            {
                _previousWindowWidth = position.width;
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
                    DrawCollapsableSection(KeyShowMicroSdkPartners, "AppLovin Micro SDK Partners", DrawPartnerMicroSdks);
                }

                // Draw mediated networks);
                EditorGUILayout.BeginHorizontal();
                var showDetails = DrawExpandCollapseButton(KeyShowMediatedNetworks);
                EditorGUILayout.LabelField("Mediated Networks", titleLabelStyle);
                GUILayout.FlexibleSpace();
                DrawUpgradeAllButton();
                EditorGUILayout.EndHorizontal();
                if (showDetails)
                {
                    DrawMediatedNetworks();
                }

#if UNITY_2019_2_OR_NEWER
                if (!AppLovinIntegrationManager.IsPluginInPackageManager)
                {
                    EditorGUILayout.LabelField("Unity Package Manager Migration", titleLabelStyle);
                    DrawPluginMigrationHelper();
                }
#endif

                // Draw AppLovin Quality Service settings
                DrawCollapsableSection(KeyShowSdkSettings, "SDK Settings", DrawQualityServiceSettings);

                DrawCollapsableSection(KeyShowPrivacySettings, "Privacy Settings", DrawPrivacySettings);

                DrawCollapsableSection(KeyShowOtherSettings, "Other Settings", DrawOtherSettings);

                // Draw Unity environment details
                EditorGUILayout.LabelField("Unity Environment Details", titleLabelStyle);
                DrawUnityEnvironmentDetails();

                // Draw documentation notes
                EditorGUILayout.LabelField(new GUIContent(DocumentationNote), wrapTextLabelStyle);
                if (GUILayout.Button(new GUIContent(DocumentationAdaptersLink), linkLabelStyle))
                {
                    Application.OpenURL(DocumentationAdaptersLink);
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
                if (GUILayout.Button("Retry", FieldWidth))
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
                    var upgradeButtonEnabled = appLovinMax.CurrentToLatestVersionComparisonResult == MaxSdkUtils.VersionComparisonResult.Lesser;
                    DrawPluginDetailRow("Unity 3D", appLovinMax.CurrentVersions.Unity, appLovinMax.LatestVersions.Unity);
                    DrawPluginDetailRow("Android", appLovinMax.CurrentVersions.Android, appLovinMax.LatestVersions.Android);
                    DrawPluginDetailRow("iOS", appLovinMax.CurrentVersions.Ios, appLovinMax.LatestVersions.Ios);

                    // BeginHorizontal combined with FlexibleSpace makes sure that the button is centered horizontally.
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    GUI.enabled = upgradeButtonEnabled;
                    if (GUILayout.Button(new GUIContent("Upgrade"), FieldWidth))
                    {
                        AppLovinEditorCoroutine.StartCoroutine(AppLovinPackageManager.AddNetwork(pluginData.AppLovinMax, true));
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
                EditorGUILayout.LabelField(firstColumnTitle, headerLabelStyle, _networkWidthOption);
                EditorGUILayout.LabelField("Current Version", headerLabelStyle, _versionWidthOption);
                GUILayout.Space(3);
                EditorGUILayout.LabelField("Latest Version", headerLabelStyle, _versionWidthOption);
                GUILayout.Space(3);
                if (drawAction)
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Button("Actions", headerLabelStyle, FieldWidth);
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
                EditorGUILayout.LabelField(new GUIContent(platform), _networkWidthOption);
                EditorGUILayout.LabelField(new GUIContent(currentVersion), _versionWidthOption);
                GUILayout.Space(3);
                EditorGUILayout.LabelField(new GUIContent(latestVersion), _versionWidthOption);
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
                if (comparison == MaxSdkUtils.VersionComparisonResult.Lesser)
                {
                    action = "Upgrade";
                    isActionEnabled = true;
                }
                // Current installed version is newer than latest version from DB (beta version)
                else if (comparison == MaxSdkUtils.VersionComparisonResult.Greater)
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
                EditorGUILayout.LabelField(new GUIContent(network.DisplayName), _networkWidthOption);
                EditorGUILayout.LabelField(new GUIContent(currentVersion), _versionWidthOption);
                GUILayout.Space(3);
                EditorGUILayout.LabelField(new GUIContent(latestVersion), _versionWidthOption);
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
                if (GUILayout.Button(new GUIContent(action), FieldWidth))
                {
                    AppLovinEditorCoroutine.StartCoroutine(AppLovinPackageManager.AddNetwork(network, true));
                }

                GUI.enabled = true;
                GUILayout.Space(2);

                GUI.enabled = networkButtonsEnabled && isInstalled;
                if (GUILayout.Button(new GUIContent {image = uninstallIcon, tooltip = "Uninstall"}, iconStyle))
                {
                    EditorUtility.DisplayProgressBar("Integration Manager", "Deleting " + network.Name + "...", 0.5f);
                    AppLovinPackageManager.RemoveNetwork(network);
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
                if (AppLovinPackageManager.IsAdapterInstalled(pluginData, "GOOGLE_AD_MANAGER_NETWORK")) return;

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
                AppLovinSettings.Instance.AdMobAndroidAppId = DrawTextField("App ID (Android)", AppLovinSettings.Instance.AdMobAndroidAppId, _networkWidthOption);
                AppLovinSettings.Instance.AdMobIosAppId = DrawTextField("App ID (iOS)", AppLovinSettings.Instance.AdMobIosAppId, _networkWidthOption);
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the upgrade all button
        /// </summary>
        private void DrawUpgradeAllButton()
        {
            GUI.enabled = NetworksRequireUpgrade();
            if (GUILayout.Button(new GUIContent("Upgrade All"), UpgradeAllButtonFieldWidth))
            {
                AppLovinEditorCoroutine.StartCoroutine(UpgradeAllNetworks());
            }

            GUI.enabled = true;
            GUILayout.Space(10);
        }

#if UNITY_2019_2_OR_NEWER
        private void DrawPluginMigrationHelper()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Space(4);
                EditorGUILayout.LabelField(new GUIContent("This will migrate the AppLovin MAX Unity Plugin and adapters to the Unity Package Manager."), wrapTextLabelStyle);
                GUILayout.Space(4);
                GUILayout.EndHorizontal();

                GUI.enabled = true;
                GUILayout.Space(3);
                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                var migrationText = "Upgrade All Adapters and Migrate to UPM";
                if (GUILayout.Button(new GUIContent(migrationText)))
                {
                    if (EditorUtility.DisplayDialog("Migrate to UPM?",
                            "Are you sure you want to migrate the SDK and adapters to UPM? This action will move both the MAX SDK and its adapters.", "Yes", "No"))
                    {
                        var deleteExternalDependencyManager = false;
                        if (Directory.Exists(ExternalDependencyManagerPath))
                        {
                            deleteExternalDependencyManager = EditorUtility.DisplayDialog("External Dependency Manager Detected",
                                "Our plugin includes the External Dependency Manager via the Unity Package Manager. Would you like us to automatically remove the existing External Dependency Manager folder, or would you prefer to manage it manually?", "Remove Automatically", "Manage Manually");
                        }

                        AppLovinPluginMigrationHelper.MigrateToUnityPackageManager(pluginData, deleteExternalDependencyManager);
                    }
                }

                GUILayout.Space(10);
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                EditorGUILayout.HelpBox("Ensure all changes are committed before migration.", MessageType.Warning);
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }
#endif

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
                    EditorGUILayout.HelpBox(QualityServiceRequiresGradleBuildErrorMsg, MessageType.Warning);
                    GUILayout.Space(4);
                    GUILayout.EndHorizontal();

                    GUILayout.Space(4);
                }

                AppLovinSettings.Instance.SdkKey = DrawTextField("AppLovin SDK Key", AppLovinSettings.Instance.SdkKey, GUILayout.Width(PrivacySettingLabelWidth), _privacySettingFieldWidthOption);
                GUILayout.BeginHorizontal();
                GUILayout.Space(4);
                GUILayout.Button("You can find your SDK key here: ", wrapTextLabelStyle, GUILayout.Width(185)); // Setting a fixed width since Unity adds arbitrary padding at the end leaving a space between link and text.
                if (GUILayout.Button(new GUIContent(AppLovinSdkKeyLink), linkLabelStyle))
                {
                    Application.OpenURL(AppLovinSdkKeyLink);
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
                DrawConsentFlowSettings();
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
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
            if (GUILayout.Button(new GUIContent(DocumentationTermsAndPrivacyPolicyFlow), linkLabelStyle))
            {
                Application.OpenURL(DocumentationTermsAndPrivacyPolicyFlow);
            }

            GUILayout.Space(4);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            AppLovinInternalSettings.Instance.ConsentFlowPrivacyPolicyUrl = DrawTextField("Privacy Policy URL", AppLovinInternalSettings.Instance.ConsentFlowPrivacyPolicyUrl, GUILayout.Width(PrivacySettingLabelWidth), _privacySettingFieldWidthOption);
            AppLovinInternalSettings.Instance.ConsentFlowTermsOfServiceUrl = DrawTextField("Terms of Service URL (optional)", AppLovinInternalSettings.Instance.ConsentFlowTermsOfServiceUrl, GUILayout.Width(PrivacySettingLabelWidth), _privacySettingFieldWidthOption);

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            AppLovinInternalSettings.Instance.ShouldShowTermsAndPrivacyPolicyAlertInGDPR = GUILayout.Toggle(AppLovinInternalSettings.Instance.ShouldShowTermsAndPrivacyPolicyAlertInGDPR, " Show Terms and Privacy Policy Flow when in GDPR Regions");
            GUILayout.EndHorizontal();

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
            AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionEn = DrawTextField("User Tracking Usage Description", AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionEn, GUILayout.Width(PrivacySettingLabelWidth), _privacySettingFieldWidthOption, isEditableTextField);

            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            AppLovinInternalSettings.Instance.UserTrackingUsageLocalizationEnabled = GUILayout.Toggle(AppLovinInternalSettings.Instance.UserTrackingUsageLocalizationEnabled, "  Localize User Tracking Usage Description");
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            if (AppLovinInternalSettings.Instance.UserTrackingUsageLocalizationEnabled)
            {
                AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionZhHans = DrawTextField("Chinese, Simplified (zh-Hans)", AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionZhHans, GUILayout.Width(PrivacySettingLabelWidth), _privacySettingFieldWidthOption, isEditableTextField);
                AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionZhHant = DrawTextField("Chinese, Traditional (zh-Hant)", AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionZhHant, GUILayout.Width(PrivacySettingLabelWidth), _privacySettingFieldWidthOption, isEditableTextField);
                AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionFr = DrawTextField("French (fr)", AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionFr, GUILayout.Width(PrivacySettingLabelWidth), _privacySettingFieldWidthOption, isEditableTextField);
                AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionDe = DrawTextField("German (de)", AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionDe, GUILayout.Width(PrivacySettingLabelWidth), _privacySettingFieldWidthOption, isEditableTextField);
                AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionJa = DrawTextField("Japanese (ja)", AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionJa, GUILayout.Width(PrivacySettingLabelWidth), _privacySettingFieldWidthOption, isEditableTextField);
                AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionKo = DrawTextField("Korean (ko)", AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionKo, GUILayout.Width(PrivacySettingLabelWidth), _privacySettingFieldWidthOption, isEditableTextField);
                AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionEs = DrawTextField("Spanish (es)", AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionEs, GUILayout.Width(PrivacySettingLabelWidth), _privacySettingFieldWidthOption, isEditableTextField);

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
            if (GUILayout.Button(new GUIContent(UserTrackingUsageDescriptionDocsLink), linkLabelStyle))
            {
                Application.OpenURL(UserTrackingUsageDescriptionDocsLink);
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
            AppLovinInternalSettings.Instance.DebugUserGeography = (MaxSdkBase.ConsentFlowUserGeography) EditorGUILayout.Popup((int) AppLovinInternalSettings.Instance.DebugUserGeography, debugUserGeographies, _privacySettingFieldWidthOption);
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
                var autoUpdateEnabled = DrawOtherSettingsToggle(EditorPrefs.GetBool(AppLovinAutoUpdater.KeyAutoUpdateEnabled, true), "  Enable Auto Update", "Checks for AppLovin MAX plugin updates and notifies you when an update is available.");
                EditorPrefs.SetBool(AppLovinAutoUpdater.KeyAutoUpdateEnabled, autoUpdateEnabled);
                GUILayout.Space(5);
                var verboseLoggingEnabled = DrawOtherSettingsToggle(EditorPrefs.GetBool(MaxSdkLogger.KeyVerboseLoggingEnabled, false), "  Enable Verbose Logging");
                EditorPrefs.SetBool(MaxSdkLogger.KeyVerboseLoggingEnabled, verboseLoggingEnabled);
                GUILayout.Space(5);
                AppLovinSettings.Instance.CustomGradleVersionUrl = DrawTextField("Custom Gradle Version URL", AppLovinSettings.Instance.CustomGradleVersionUrl, GUILayout.Width(PrivacySettingLabelWidth), _privacySettingFieldWidthOption, tooltip: CustomGradleVersionTooltip);
                AppLovinSettings.Instance.CustomGradleToolsVersion = DrawTextField("Custom Gradle Tools Version", AppLovinSettings.Instance.CustomGradleToolsVersion, GUILayout.Width(PrivacySettingLabelWidth), _privacySettingFieldWidthOption, tooltip: CustomGradleToolsVersionTooltip);
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

        private void DrawCollapsableSection(string keyShowDetails, string label, Action drawContent)
        {
            EditorGUILayout.BeginHorizontal();
            var showDetails = DrawExpandCollapseButton(keyShowDetails);

            EditorGUILayout.LabelField(label, titleLabelStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            if (showDetails)
            {
                drawContent();
            }
        }

        private bool DrawExpandCollapseButton(string keyShowDetails)
        {
            var showDetails = EditorPrefs.GetBool(keyShowDetails, true);
            var buttonText = showDetails ? CollapseButtonText : ExpandButtonText;
            if (GUILayout.Button(buttonText, CollapseButtonWidthOption))
            {
                EditorPrefs.SetBool(keyShowDetails, !showDetails);
            }

            return showDetails;
        }

        /// <summary>
        /// Calculates the fields width based on the width of the window.
        /// </summary>
        private void CalculateFieldWidth()
        {
            var currentWidth = position.width;
            var availableWidth = currentWidth - ActionFieldWidth - 80; // NOTE: Magic number alert. This is the sum of all the spacing the fields and other UI elements.
            var networkLabelWidth = Math.Max(NetworkFieldMinWidth, availableWidth * NetworkFieldWidthPercentage);
            _networkWidthOption = GUILayout.Width(networkLabelWidth);

            var versionLabelWidth = Math.Max(VersionFieldMinWidth, availableWidth * VersionFieldWidthPercentage);
            _versionWidthOption = GUILayout.Width(versionLabelWidth);

            const int textFieldOtherUiElementsWidth = 55; // NOTE: Magic number alert. This is the sum of all the spacing the fields and other UI elements.
            var availableUserDescriptionTextFieldWidth = currentWidth - PrivacySettingLabelWidth - textFieldOtherUiElementsWidth;
            _privacySettingFieldWidthOption = GUILayout.Width(availableUserDescriptionTextFieldWidth);
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
                if (EditorUtility.DisplayCancelableProgressBar(WindowTitle, string.Format("Downloading {0} plugin...", pluginName), progress))
                {
                    AppLovinIntegrationManager.Instance.CancelDownload();
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        private void OnImportPackageCompleted(Network network)
        {
            AppLovinPackageManager.UpdateCurrentVersions(network);

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
                MaxSdkUtils.IsValidString(googleNetwork.CurrentVersions.Unity) && MaxSdkUtils.IsValidString(googleAdManagerNetwork.CurrentVersions.Unity) &&
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
                if (MaxSdkUtils.IsValidString(network.CurrentVersions.Unity) && comparison == MaxSdkUtils.VersionComparisonResult.Lesser)
                {
                    yield return AppLovinPackageManager.AddNetwork(network, false);
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
            return networks.Any(network => MaxSdkUtils.IsValidString(network.CurrentVersions.Unity) && network.CurrentToLatestVersionComparisonResult == MaxSdkUtils.VersionComparisonResult.Lesser);
        }

        #endregion
    }
}
