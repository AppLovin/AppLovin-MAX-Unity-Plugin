//
//  MaxPostProcessBuildiOS.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 8/5/20.
//  Copyright © 2020 AppLovin. All rights reserved.
//

#if UNITY_IOS || UNITY_IPHONE

using AppLovinMax.Scripts.IntegrationManager.Editor;
#if UNITY_2019_3_OR_NEWER
using UnityEditor.iOS.Xcode.Extensions;
#endif
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace AppLovinMax.Scripts.Editor
{
    [Serializable]
    public class SkAdNetworkData
    {
        [SerializeField] public string[] SkAdNetworkIds;
    }

    public class MaxPostProcessBuildiOS
    {
#if !UNITY_2019_3_OR_NEWER
        private const string UnityMainTargetName = "Unity-iPhone";
#endif
        private const string TargetUnityIphonePodfileLine = "target 'Unity-iPhone' do";
        private const string LegacyResourcesDirectoryName = "Resources";
        private const string AppLovinMaxResourcesDirectoryName = "AppLovinMAXResources";
        private const string AppLovinAdvertisingAttributionEndpoint = "https://postbacks-app.com";

        private const string AppLovinSettingsPlistFileName = "AppLovin-Settings.plist";
        private const string KeyConsentFlowInfo = "ConsentFlowInfo";
        private const string KeyConsentFlowEnabled = "ConsentFlowEnabled";
        private const string KeyConsentFlowTermsOfService = "ConsentFlowTermsOfService";
        private const string KeyConsentFlowPrivacyPolicy = "ConsentFlowPrivacyPolicy";
        private const string KeyConsentFlowDebugUserGeography = "ConsentFlowDebugUserGeography";

        private static readonly List<string> DynamicLibrariesToEmbed = new List<string>
        {
            "DTBiOSSDK.xcframework",
            "FBAEMKit.xcframework",
            "FBSDKCoreKit_Basics.xcframework",
            "FBSDKCoreKit.xcframework",
            "FBSDKGamingServicesKit.xcframework",
            "FBSDKLoginKit.xcframework",
            "FBSDKShareKit.xcframework",
            "HyprMX.xcframework",
            "LinkedinAudienceNetwork.xcframework",
            "IASDKCore.xcframework",
            "Maio.xcframework",
            "MobileFuseSDK.xcframework",
            "OMSDK_Appodeal.xcframework",
            "OMSDK_Ogury.xcframework",
            "OMSDK_Pubnativenet.xcframework",
            "OMSDK_Smaato.xcframework"
        };

        private static string PluginMediationDirectory
        {
            get
            {
                var pluginParentDir = AppLovinIntegrationManager.MediationSpecificPluginParentDirectory;
                return Path.Combine(pluginParentDir, "MaxSdk/Mediation/");
            }
        }

        [PostProcessBuildAttribute(int.MaxValue)]
        public static void MaxPostProcessPbxProject(BuildTarget buildTarget, string buildPath)
        {
            var projectPath = PBXProject.GetPBXProjectPath(buildPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

#if UNITY_2019_3_OR_NEWER
            var unityMainTargetGuid = project.GetUnityMainTargetGuid();
            var unityFrameworkTargetGuid = project.GetUnityFrameworkTargetGuid();
#else
            var unityMainTargetGuid = project.TargetGuidByName(UnityMainTargetName);
            var unityFrameworkTargetGuid = project.TargetGuidByName(UnityMainTargetName);
#endif
            EmbedDynamicLibrariesIfNeeded(buildPath, project, unityMainTargetGuid);

            var internalSettingsEnabled = AppLovinInternalSettings.Instance.ConsentFlowEnabled;
            var userTrackingUsageDescriptionDe = internalSettingsEnabled ? AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionDe : AppLovinSettings.Instance.UserTrackingUsageDescriptionDe;
            LocalizeUserTrackingDescriptionIfNeeded(userTrackingUsageDescriptionDe, "de", buildPath, project, unityMainTargetGuid);
            var userTrackingUsageDescriptionEn = internalSettingsEnabled ? AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionEn : AppLovinSettings.Instance.UserTrackingUsageDescriptionEn;
            LocalizeUserTrackingDescriptionIfNeeded(userTrackingUsageDescriptionEn, "en", buildPath, project, unityMainTargetGuid);
            var userTrackingUsageDescriptionEs = internalSettingsEnabled ? AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionEs : AppLovinSettings.Instance.UserTrackingUsageDescriptionEs;
            LocalizeUserTrackingDescriptionIfNeeded(userTrackingUsageDescriptionEs, "es", buildPath, project, unityMainTargetGuid);
            var userTrackingUsageDescriptionFr = internalSettingsEnabled ? AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionFr : AppLovinSettings.Instance.UserTrackingUsageDescriptionFr;
            LocalizeUserTrackingDescriptionIfNeeded(userTrackingUsageDescriptionFr, "fr", buildPath, project, unityMainTargetGuid);
            var userTrackingUsageDescriptionJa = internalSettingsEnabled ? AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionJa : AppLovinSettings.Instance.UserTrackingUsageDescriptionJa;
            LocalizeUserTrackingDescriptionIfNeeded(userTrackingUsageDescriptionJa, "ja", buildPath, project, unityMainTargetGuid);
            var userTrackingUsageDescriptionKo = internalSettingsEnabled ? AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionKo : AppLovinSettings.Instance.UserTrackingUsageDescriptionKo;
            LocalizeUserTrackingDescriptionIfNeeded(userTrackingUsageDescriptionKo, "ko", buildPath, project, unityMainTargetGuid);
            var userTrackingUsageDescriptionZhHans = internalSettingsEnabled ? AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionZhHans : AppLovinSettings.Instance.UserTrackingUsageDescriptionZhHans;
            LocalizeUserTrackingDescriptionIfNeeded(userTrackingUsageDescriptionZhHans, "zh-Hans", buildPath, project, unityMainTargetGuid);
            var userTrackingUsageDescriptionZhHant = internalSettingsEnabled ? AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionZhHant : AppLovinSettings.Instance.UserTrackingUsageDescriptionZhHant;
            LocalizeUserTrackingDescriptionIfNeeded(userTrackingUsageDescriptionZhHant, "zh-Hant", buildPath, project, unityMainTargetGuid);

            AddSwiftSupport(buildPath, project, unityFrameworkTargetGuid, unityMainTargetGuid);
            AddYandexSettingsIfNeeded(project, unityMainTargetGuid);

            project.WriteToFile(projectPath);
        }

        private static void EmbedDynamicLibrariesIfNeeded(string buildPath, PBXProject project, string targetGuid)
        {
            // Check that the Pods directory exists (it might not if a publisher is building with Generate Podfile setting disabled in EDM).
            var podsDirectory = Path.Combine(buildPath, "Pods");
            if (!Directory.Exists(podsDirectory)) return;

            var dynamicLibraryPathsPresentInProject = new List<string>();
            foreach (var dynamicLibraryToSearch in DynamicLibrariesToEmbed)
            {
                // both .framework and .xcframework are directories, not files
                var directories = Directory.GetDirectories(podsDirectory, dynamicLibraryToSearch, SearchOption.AllDirectories);
                if (directories.Length <= 0) continue;

                var dynamicLibraryAbsolutePath = directories[0];
                var index = dynamicLibraryAbsolutePath.LastIndexOf("Pods");
                var relativePath = dynamicLibraryAbsolutePath.Substring(index);
                dynamicLibraryPathsPresentInProject.Add(relativePath);
            }

            if (dynamicLibraryPathsPresentInProject.Count <= 0) return;

#if UNITY_2019_3_OR_NEWER
            // Embed framework only if the podfile does not contain target `Unity-iPhone`.
            if (!ContainsUnityIphoneTargetInPodfile(buildPath))
            {
                foreach (var dynamicLibraryPath in dynamicLibraryPathsPresentInProject)
                {
                    var fileGuid = project.AddFile(dynamicLibraryPath, dynamicLibraryPath);
                    project.AddFileToEmbedFrameworks(targetGuid, fileGuid);
                }
            }
#else
            string runpathSearchPaths;
            runpathSearchPaths = project.GetBuildPropertyForAnyConfig(targetGuid, "LD_RUNPATH_SEARCH_PATHS");
            runpathSearchPaths += string.IsNullOrEmpty(runpathSearchPaths) ? "" : " ";

            // Check if runtime search paths already contains the required search paths for dynamic libraries.
            if (runpathSearchPaths.Contains("@executable_path/Frameworks")) return;

            runpathSearchPaths += "@executable_path/Frameworks";
            project.SetBuildProperty(targetGuid, "LD_RUNPATH_SEARCH_PATHS", runpathSearchPaths);
#endif
        }

        private static void LocalizeUserTrackingDescriptionIfNeeded(string localizedUserTrackingDescription, string localeCode, string buildPath, PBXProject project, string targetGuid)
        {
            // Use the legacy resources directory name if the build is being appended (the "Resources" directory already exists if it is an incremental build).
            var resourcesDirectoryName = Directory.Exists(Path.Combine(buildPath, LegacyResourcesDirectoryName)) ? LegacyResourcesDirectoryName : AppLovinMaxResourcesDirectoryName;
            var resourcesDirectoryPath = Path.Combine(buildPath, resourcesDirectoryName);
            var localeSpecificDirectoryName = localeCode + ".lproj";
            var localeSpecificDirectoryPath = Path.Combine(resourcesDirectoryPath, localeSpecificDirectoryName);
            var infoPlistStringsFilePath = Path.Combine(localeSpecificDirectoryPath, "InfoPlist.strings");

            // Check if localization has been disabled between builds, and remove them as needed.
            if (ShouldRemoveLocalization(localizedUserTrackingDescription))
            {
                if (!File.Exists(infoPlistStringsFilePath)) return;

                File.Delete(infoPlistStringsFilePath);
                return;
            }

            // Create intermediate directories as needed.
            if (!Directory.Exists(resourcesDirectoryPath))
            {
                Directory.CreateDirectory(resourcesDirectoryPath);
            }

            if (!Directory.Exists(localeSpecificDirectoryPath))
            {
                Directory.CreateDirectory(localeSpecificDirectoryPath);
            }

            var localizedDescriptionLine = "\"NSUserTrackingUsageDescription\" = \"" + localizedUserTrackingDescription + "\";\n";
            // File already exists, update it in case the value changed between builds.
            if (File.Exists(infoPlistStringsFilePath))
            {
                var output = new List<string>();
                var lines = File.ReadAllLines(infoPlistStringsFilePath);
                var keyUpdated = false;
                foreach (var line in lines)
                {
                    if (line.Contains("NSUserTrackingUsageDescription"))
                    {
                        output.Add(localizedDescriptionLine);
                        keyUpdated = true;
                    }
                    else
                    {
                        output.Add(line);
                    }
                }

                if (!keyUpdated)
                {
                    output.Add(localizedDescriptionLine);
                }

                File.WriteAllText(infoPlistStringsFilePath, string.Join("\n", output.ToArray()) + "\n");
            }
            // File doesn't exist, create one.
            else
            {
                File.WriteAllText(infoPlistStringsFilePath, "/* Localized versions of Info.plist keys - Generated by AL MAX plugin */\n" + localizedDescriptionLine);
            }

            var localeSpecificDirectoryRelativePath = Path.Combine(resourcesDirectoryName, localeSpecificDirectoryName);
            var guid = project.AddFolderReference(localeSpecificDirectoryRelativePath, localeSpecificDirectoryRelativePath);
            project.AddFileToBuild(targetGuid, guid);
        }

        private static bool ShouldRemoveLocalization(string localizedUserTrackingDescription)
        {
            if (string.IsNullOrEmpty(localizedUserTrackingDescription)) return true;

            var settings = AppLovinSettings.Instance;
            var internalSettings = AppLovinInternalSettings.Instance;

            return (!internalSettings.ConsentFlowEnabled || !internalSettings.UserTrackingUsageLocalizationEnabled)
                   && (!settings.ConsentFlowEnabled || !settings.UserTrackingUsageLocalizationEnabled);
        }

        private static void AddSwiftSupport(string buildPath, PBXProject project, string unityFrameworkTargetGuid, string unityMainTargetGuid)
        {
            var swiftFileRelativePath = "Classes/MAXSwiftSupport.swift";
            var swiftFilePath = Path.Combine(buildPath, swiftFileRelativePath);

            // Add Swift file
            CreateSwiftFile(swiftFilePath);
            var swiftFileGuid = project.AddFile(swiftFileRelativePath, swiftFileRelativePath, PBXSourceTree.Source);
            project.AddFileToBuild(unityFrameworkTargetGuid, swiftFileGuid);

            // Add Swift version property if needed
            var swiftVersion = project.GetBuildPropertyForAnyConfig(unityFrameworkTargetGuid, "SWIFT_VERSION");
            if (string.IsNullOrEmpty(swiftVersion))
            {
                project.SetBuildProperty(unityFrameworkTargetGuid, "SWIFT_VERSION", "5.0");
            }

            // Enable Swift modules
            project.AddBuildProperty(unityFrameworkTargetGuid, "CLANG_ENABLE_MODULES", "YES");
            project.AddBuildProperty(unityMainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
        }

        private static void CreateSwiftFile(string swiftFilePath)
        {
            if (File.Exists(swiftFilePath)) return;

            // Create a file to write to.
            using (var writer = File.CreateText(swiftFilePath))
            {
                writer.WriteLine("//\n//  MAXSwiftSupport.swift\n//");
                writer.WriteLine("\nimport Foundation\n");
                writer.WriteLine("// This file ensures the project includes Swift support.");
                writer.WriteLine("// It is automatically generated by the MAX Unity Plugin.");
                writer.Close();
            }
        }

        [PostProcessBuildAttribute(int.MaxValue)]
        public static void MaxPostProcessPlist(BuildTarget buildTarget, string path)
        {
            var plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            SetSdkKeyIfNeeded(plist);
            SetAttributionReportEndpointIfNeeded(plist);

            EnableVerboseLoggingIfNeeded(plist);
            AddGoogleApplicationIdIfNeeded(plist);

            AddSdkSettingsIfNeeded(plist, path);
            EnableTermsFlowIfNeeded(plist);
            AddSkAdNetworksInfoIfNeeded(plist);

            plist.WriteToFile(plistPath);
        }

        private static void SetSdkKeyIfNeeded(PlistDocument plist)
        {
            var sdkKey = AppLovinSettings.Instance.SdkKey;
            if (string.IsNullOrEmpty(sdkKey)) return;

            const string AppLovinVerboseLoggingOnKey = "AppLovinSdkKey";
            plist.root.SetString(AppLovinVerboseLoggingOnKey, sdkKey);
        }

        private static void SetAttributionReportEndpointIfNeeded(PlistDocument plist)
        {
            if (AppLovinSettings.Instance.SetAttributionReportEndpoint)
            {
                plist.root.SetString("NSAdvertisingAttributionReportEndpoint", AppLovinAdvertisingAttributionEndpoint);
            }
            else
            {
                PlistElement attributionReportEndPoint;
                plist.root.values.TryGetValue("NSAdvertisingAttributionReportEndpoint", out attributionReportEndPoint);

                // Check if we had previously set the attribution endpoint and un-set it.
                if (attributionReportEndPoint != null && AppLovinAdvertisingAttributionEndpoint.Equals(attributionReportEndPoint.AsString()))
                {
                    plist.root.values.Remove("NSAdvertisingAttributionReportEndpoint");
                }
            }
        }

        private static void EnableVerboseLoggingIfNeeded(PlistDocument plist)
        {
            if (!EditorPrefs.HasKey(MaxSdkLogger.KeyVerboseLoggingEnabled)) return;

            var enabled = EditorPrefs.GetBool(MaxSdkLogger.KeyVerboseLoggingEnabled);
            const string AppLovinVerboseLoggingOnKey = "AppLovinVerboseLoggingOn";
            if (enabled)
            {
                plist.root.SetBoolean(AppLovinVerboseLoggingOnKey, enabled);
            }
            else
            {
                plist.root.values.Remove(AppLovinVerboseLoggingOnKey);
            }
        }

        private static void AddGoogleApplicationIdIfNeeded(PlistDocument plist)
        {
            if (!AppLovinIntegrationManager.IsAdapterInstalled("Google") && !AppLovinIntegrationManager.IsAdapterInstalled("GoogleAdManager")) return;

            const string googleApplicationIdentifier = "GADApplicationIdentifier";
            var appId = AppLovinSettings.Instance.AdMobIosAppId;
            // Log error if the App ID is not set.
            if (string.IsNullOrEmpty(appId) || !appId.StartsWith("ca-app-pub-"))
            {
                Debug.LogError("[AppLovin MAX] Google App ID is not set. Please enter a valid app ID within the AppLovin Integration Manager window.");
                return;
            }

            plist.root.SetString(googleApplicationIdentifier, appId);
        }

        private static void AddYandexSettingsIfNeeded(PBXProject project, string unityMainTargetGuid)
        {
            if (!AppLovinIntegrationManager.IsAdapterInstalled("Yandex")) return;

            if (MaxSdkUtils.CompareVersions(PlayerSettings.iOS.targetOSVersionString, "12.0") == MaxSdkUtils.VersionComparisonResult.Lesser)
            {
                Debug.LogWarning("Your iOS target version is under the minimum required version by Yandex. Please update it to 12.0 or newer in your ProjectSettings and rebuild your project.");
                return;
            }

            project.SetBuildProperty(unityMainTargetGuid, "GENERATE_INFOPLIST_FILE", "NO");
        }

        private static void AddSdkSettingsIfNeeded(PlistDocument infoPlist, string buildPath)
        {
            // Right now internal settings is only needed for Consent Flow. Remove this setting once we add more settings.
            if (!AppLovinInternalSettings.Instance.ConsentFlowEnabled) return;

            var sdkSettingsPlistPath = Path.Combine(buildPath, AppLovinSettingsPlistFileName);
            var sdkSettingsPlist = new PlistDocument();
            if (File.Exists(sdkSettingsPlistPath))
            {
                sdkSettingsPlist.ReadFromFile(sdkSettingsPlistPath);
            }

            EnableConsentFlowIfNeeded(sdkSettingsPlist, infoPlist);

            sdkSettingsPlist.WriteToFile(sdkSettingsPlistPath);

            var projectPath = PBXProject.GetPBXProjectPath(buildPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

#if UNITY_2019_3_OR_NEWER
            var unityMainTargetGuid = project.GetUnityMainTargetGuid();
#else
            var unityMainTargetGuid = project.TargetGuidByName(UnityMainTargetName);
#endif

            var guid = project.AddFile(AppLovinSettingsPlistFileName, AppLovinSettingsPlistFileName, PBXSourceTree.Source);
            project.AddFileToBuild(unityMainTargetGuid, guid);
            project.WriteToFile(projectPath);
        }

        private static void EnableConsentFlowIfNeeded(PlistDocument applovinSettingsPlist, PlistDocument infoPlist)
        {
            var consentFlowEnabled = AppLovinInternalSettings.Instance.ConsentFlowEnabled;
            var userTrackingUsageDescription = AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionEn;
            var privacyPolicyUrl = AppLovinInternalSettings.Instance.ConsentFlowPrivacyPolicyUrl;
            if (string.IsNullOrEmpty(userTrackingUsageDescription) || string.IsNullOrEmpty(privacyPolicyUrl))
            {
                AppLovinIntegrationManager.ShowBuildFailureDialog("You cannot use the AppLovin SDK's consent flow without defining a Privacy Policy URL and the `User Tracking Usage Description` in the AppLovin Integration Manager. \n\n" +
                                                                  "Both values must be included to enable the SDK's consent flow.");

                // No need to update the info.plist here. Default consent flow state will be determined on the SDK side.
                return;
            }

            var consentFlowInfoRoot = applovinSettingsPlist.root.CreateDict(KeyConsentFlowInfo);
            consentFlowInfoRoot.SetBoolean(KeyConsentFlowEnabled, consentFlowEnabled);
            consentFlowInfoRoot.SetString(KeyConsentFlowPrivacyPolicy, privacyPolicyUrl);

            var termsOfServiceUrl = AppLovinInternalSettings.Instance.ConsentFlowTermsOfServiceUrl;
            if (MaxSdkUtils.IsValidString(termsOfServiceUrl))
            {
                consentFlowInfoRoot.SetString(KeyConsentFlowTermsOfService, termsOfServiceUrl);
            }

            var debugUserGeography = AppLovinInternalSettings.Instance.DebugUserGeography;
            if (debugUserGeography == MaxSdkBase.ConsentFlowUserGeography.Gdpr)
            {
                consentFlowInfoRoot.SetString(KeyConsentFlowDebugUserGeography, "gdpr");
            }

            infoPlist.root.SetString("NSUserTrackingUsageDescription", userTrackingUsageDescription);
        }

        private static void EnableTermsFlowIfNeeded(PlistDocument plist)
        {
            // Check if terms flow is enabled. No need to update info.plist if consent flow is disabled.
            var consentFlowEnabled = AppLovinSettings.Instance.ConsentFlowEnabled;
            if (!consentFlowEnabled) return;

            // Check if terms flow is enabled for this format.
            var consentFlowPlatform = AppLovinSettings.Instance.ConsentFlowPlatform;
            if (consentFlowPlatform != Platform.All && consentFlowPlatform != Platform.iOS) return;

            var userTrackingUsageDescription = AppLovinSettings.Instance.UserTrackingUsageDescriptionEn;
            var privacyPolicyUrl = AppLovinSettings.Instance.ConsentFlowPrivacyPolicyUrl;
            if (string.IsNullOrEmpty(userTrackingUsageDescription) || string.IsNullOrEmpty(privacyPolicyUrl))
            {
                AppLovinIntegrationManager.ShowBuildFailureDialog("You cannot use the AppLovin SDK's consent flow without defining a Privacy Policy URL and the `User Tracking Usage Description` in the AppLovin Integration Manager. \n\n" +
                                                                  "Both values must be included to enable the SDK's consent flow.");

                // No need to update the info.plist here. Default consent flow state will be determined on the SDK side.
                return;
            }

            var consentFlowInfoRoot = plist.root.CreateDict("AppLovinConsentFlowInfo");
            consentFlowInfoRoot.SetBoolean("AppLovinConsentFlowEnabled", consentFlowEnabled);
            consentFlowInfoRoot.SetString("AppLovinConsentFlowPrivacyPolicy", privacyPolicyUrl);

            var termsOfServiceUrl = AppLovinSettings.Instance.ConsentFlowTermsOfServiceUrl;
            if (!string.IsNullOrEmpty(termsOfServiceUrl))
            {
                consentFlowInfoRoot.SetString("AppLovinConsentFlowTermsOfService", termsOfServiceUrl);
            }

            plist.root.SetString("NSUserTrackingUsageDescription", userTrackingUsageDescription);
        }

        private static void AddSkAdNetworksInfoIfNeeded(PlistDocument plist)
        {
            var skAdNetworkData = GetSkAdNetworkData();
            var skAdNetworkIds = skAdNetworkData.SkAdNetworkIds;
            // Check if we have a valid list of SKAdNetworkIds that need to be added.
            if (skAdNetworkIds == null || skAdNetworkIds.Length < 1) return;

            //
            // Add the SKAdNetworkItems to the plist. It should look like following:
            //
            //    <key>SKAdNetworkItems</key>
            //    <array>
            //        <dict>
            //            <key>SKAdNetworkIdentifier</key>
            //            <string>ABC123XYZ.skadnetwork</string>
            //        </dict>
            //        <dict>
            //            <key>SKAdNetworkIdentifier</key>
            //            <string>123QWE456.skadnetwork</string>
            //        </dict>
            //        <dict>
            //            <key>SKAdNetworkIdentifier</key>
            //            <string>987XYZ123.skadnetwork</string>
            //        </dict>
            //    </array>
            //
            PlistElement skAdNetworkItems;
            plist.root.values.TryGetValue("SKAdNetworkItems", out skAdNetworkItems);
            var existingSkAdNetworkIds = new HashSet<string>();
            // Check if SKAdNetworkItems array is already in the Plist document and collect all the IDs that are already present.
            if (skAdNetworkItems != null && skAdNetworkItems.GetType() == typeof(PlistElementArray))
            {
                var plistElementDictionaries = skAdNetworkItems.AsArray().values.Where(plistElement => plistElement.GetType() == typeof(PlistElementDict));
                foreach (var plistElement in plistElementDictionaries)
                {
                    PlistElement existingId;
                    plistElement.AsDict().values.TryGetValue("SKAdNetworkIdentifier", out existingId);
                    if (existingId == null || existingId.GetType() != typeof(PlistElementString) || string.IsNullOrEmpty(existingId.AsString())) continue;

                    existingSkAdNetworkIds.Add(existingId.AsString());
                }
            }
            // Else, create an array of SKAdNetworkItems into which we will add our IDs.
            else
            {
                skAdNetworkItems = plist.root.CreateArray("SKAdNetworkItems");
            }

            foreach (var skAdNetworkId in skAdNetworkIds)
            {
                // Skip adding IDs that are already in the array.
                if (existingSkAdNetworkIds.Contains(skAdNetworkId)) continue;

                var skAdNetworkItemDict = skAdNetworkItems.AsArray().AddDict();
                skAdNetworkItemDict.SetString("SKAdNetworkIdentifier", skAdNetworkId);
            }
        }

        private static SkAdNetworkData GetSkAdNetworkData()
        {
            var uriBuilder = new UriBuilder("https://unity.applovin.com/max/1.0/skadnetwork_ids");

            // Get the list of installed ad networks to be passed up
            var maxMediationDirectory = PluginMediationDirectory;
            if (Directory.Exists(maxMediationDirectory))
            {
                var mediationNetworkDirectories = Directory.GetDirectories(maxMediationDirectory);
                var installedNetworks = mediationNetworkDirectories.Select(Path.GetFileName).ToList();
                if (AppLovinSettings.Instance.AddApsSkAdNetworkIds)
                {
                    installedNetworks.Add("AmazonPublisherServices");
                }

                var adNetworks = string.Join(",", installedNetworks.ToArray());
                if (!string.IsNullOrEmpty(adNetworks))
                {
                    uriBuilder.Query += string.Format("ad_networks={0}", adNetworks);
                }
            }

            using (var unityWebRequest = UnityWebRequest.Get(uriBuilder.ToString()))
            {
                var operation = unityWebRequest.SendWebRequest();
                // Wait for the download to complete or the request to timeout.
                while (!operation.isDone) { }

#if UNITY_2020_1_OR_NEWER
                if (unityWebRequest.result != UnityWebRequest.Result.Success)
#else
                if (unityWebRequest.isNetworkError || unityWebRequest.isHttpError)
#endif
                {
                    MaxSdkLogger.UserError("Failed to retrieve SKAdNetwork IDs with error: " + unityWebRequest.error);
                    return new SkAdNetworkData();
                }

                try
                {
                    return JsonUtility.FromJson<SkAdNetworkData>(unityWebRequest.downloadHandler.text);
                }
                catch (Exception exception)
                {
                    MaxSdkLogger.UserError("Failed to parse data '" + unityWebRequest.downloadHandler.text + "' with exception: " + exception);
                    return new SkAdNetworkData();
                }
            }
        }

#if UNITY_2019_3_OR_NEWER
        private static bool ContainsUnityIphoneTargetInPodfile(string buildPath)
        {
            var podfilePath = Path.Combine(buildPath, "Podfile");
            if (!File.Exists(podfilePath)) return false;

            var lines = File.ReadAllLines(podfilePath);
            return lines.Any(line => line.Contains(TargetUnityIphonePodfileLine));
        }
#endif
    }
}

#endif
