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

        private static readonly List<string> AtsRequiringNetworks = new List<string>
        {
            "AdColony",
            "ByteDance",
            "Fyber",
            "Google",
            "GoogleAdManager",
            "HyprMX",
            "InMobi",
            "IronSource",
            "Smaato"
        };

        private static List<string> DynamicLibraryPathsToEmbed
        {
            get
            {
                var dynamicLibraryPathsToEmbed = new List<string>(2);
                dynamicLibraryPathsToEmbed.Add(Path.Combine("Pods/", "HyprMX/HyprMX.xcframework"));
                dynamicLibraryPathsToEmbed.Add(Path.Combine("Pods/", "smaato-ios-sdk/vendor/OMSDK_Smaato.xcframework"));
                dynamicLibraryPathsToEmbed.Add(Path.Combine("Pods/", "FBSDKCoreKit_Basics/XCFrameworks/FBSDKCoreKit_Basics.xcframework"));
                if (ShouldEmbedSnapSdk())
                {
                    dynamicLibraryPathsToEmbed.Add(Path.Combine("Pods/", "SAKSDK/SAKSDK.framework"));
                }

                return dynamicLibraryPathsToEmbed;
            }
        }

        private static readonly List<string> SwiftLanguageNetworks = new List<string>
        {
            "MoPub"
        };

        private static readonly List<string> EmbedSwiftStandardLibrariesNetworks = new List<string>
        {
            "Facebook",
            "MoPub"
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

            LocalizeUserTrackingDescriptionIfNeeded(AppLovinSettings.Instance.UserTrackingUsageDescriptionDe, "de", buildPath, project, unityMainTargetGuid);
            LocalizeUserTrackingDescriptionIfNeeded(AppLovinSettings.Instance.UserTrackingUsageDescriptionEn, "en", buildPath, project, unityMainTargetGuid);
            LocalizeUserTrackingDescriptionIfNeeded(AppLovinSettings.Instance.UserTrackingUsageDescriptionEs, "es", buildPath, project, unityMainTargetGuid);
            LocalizeUserTrackingDescriptionIfNeeded(AppLovinSettings.Instance.UserTrackingUsageDescriptionFr, "fr", buildPath, project, unityMainTargetGuid);
            LocalizeUserTrackingDescriptionIfNeeded(AppLovinSettings.Instance.UserTrackingUsageDescriptionJa, "ja", buildPath, project, unityMainTargetGuid);
            LocalizeUserTrackingDescriptionIfNeeded(AppLovinSettings.Instance.UserTrackingUsageDescriptionKo, "ko", buildPath, project, unityMainTargetGuid);
            LocalizeUserTrackingDescriptionIfNeeded(AppLovinSettings.Instance.UserTrackingUsageDescriptionZhHans, "zh-Hans", buildPath, project, unityMainTargetGuid);
            LocalizeUserTrackingDescriptionIfNeeded(AppLovinSettings.Instance.UserTrackingUsageDescriptionZhHant, "zh-Hant", buildPath, project, unityMainTargetGuid);

            AddSwiftSupportIfNeeded(buildPath, project, unityFrameworkTargetGuid);
            EmbedSwiftStandardLibrariesIfNeeded(buildPath, project, unityMainTargetGuid);

            project.WriteToFile(projectPath);
        }

        private static void EmbedDynamicLibrariesIfNeeded(string buildPath, PBXProject project, string targetGuid)
        {
            var dynamicLibraryPathsPresentInProject = DynamicLibraryPathsToEmbed.Where(dynamicLibraryPath => Directory.Exists(Path.Combine(buildPath, dynamicLibraryPath))).ToList();
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
#if UNITY_2018_2_OR_NEWER
            runpathSearchPaths = project.GetBuildPropertyForAnyConfig(targetGuid, "LD_RUNPATH_SEARCH_PATHS");
#else
            runpathSearchPaths = "$(inherited)";          
#endif
            runpathSearchPaths += string.IsNullOrEmpty(runpathSearchPaths) ? "" : " ";

            // Check if runtime search paths already contains the required search paths for dynamic libraries.
            if (runpathSearchPaths.Contains("@executable_path/Frameworks")) return;

            runpathSearchPaths += "@executable_path/Frameworks";
            project.SetBuildProperty(targetGuid, "LD_RUNPATH_SEARCH_PATHS", runpathSearchPaths);
#endif

            if (ShouldEmbedSnapSdk())
            {
                // Needed to build successfully on Xcode 12+, as Snap was build with latest Xcode but not as an xcframework
                project.AddBuildProperty(targetGuid, "VALIDATE_WORKSPACE", "YES");
            }
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
            var settings = AppLovinSettings.Instance;
            if (!settings.ConsentFlowEnabled || !settings.UserTrackingUsageLocalizationEnabled || string.IsNullOrEmpty(localizedUserTrackingDescription))
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

            var guid = project.AddFolderReference(localeSpecificDirectoryPath, Path.Combine(resourcesDirectoryName, localeSpecificDirectoryName));
            project.AddFileToBuild(targetGuid, guid);
        }

        private static void AddSwiftSupportIfNeeded(string buildPath, PBXProject project, string targetGuid)
        {
            var swiftFileRelativePath = "Classes/MAXSwiftSupport.swift";
            var swiftFilePath = Path.Combine(buildPath, swiftFileRelativePath);
            var maxMediationDirectory = PluginMediationDirectory;
            var hasSwiftLanguageNetworksInProject = SwiftLanguageNetworks.Any(network => Directory.Exists(Path.Combine(maxMediationDirectory, network)));

            // Remove Swift file if no need to support Swift
            if (!hasSwiftLanguageNetworksInProject)
            {
                if (File.Exists(swiftFilePath))
                {
                    MaxSdkLogger.D("Removing Swift file references.");

                    var fileGuid = project.FindFileGuidByRealPath(swiftFilePath, PBXSourceTree.Source);
                    if (!string.IsNullOrEmpty(fileGuid))
                    {
                        project.RemoveFile(fileGuid);
                        project.RemoveFileFromBuild(targetGuid, fileGuid);

                        FileUtil.DeleteFileOrDirectory(swiftFilePath);
                    }
                }

                return;
            }

            // Add Swift file
            CreateSwiftFile(swiftFilePath);
            var swiftFileGuid = project.AddFile(swiftFilePath, swiftFileRelativePath, PBXSourceTree.Source);
            project.AddFileToBuild(targetGuid, swiftFileGuid);

            // Add Swift build properties
            project.AddBuildProperty(targetGuid, "SWIFT_VERSION", "5");
            project.AddBuildProperty(targetGuid, "CLANG_ENABLE_MODULES", "YES");
        }

        /// <summary>
        /// For Swift 5+ code that uses the standard libraries, the Swift Standard Libraries MUST be embedded for iOS < 12.2
        /// Swift 5 introduced ABI stability, which allowed iOS to start bundling the standard libraries in the OS starting with iOS 12.2
        /// Issue Reference: https://github.com/facebook/facebook-sdk-for-unity/issues/506
        /// </summary>
        private static void EmbedSwiftStandardLibrariesIfNeeded(string buildPath, PBXProject project, string mainTargetGuid)
        {
            var maxMediationDirectory = PluginMediationDirectory;
            var hasEmbedSwiftStandardLibrariesNetworksInProject = EmbedSwiftStandardLibrariesNetworks.Any(network => Directory.Exists(Path.Combine(maxMediationDirectory, network)));
            if (!hasEmbedSwiftStandardLibrariesNetworksInProject) return;

            // This needs to be added the main target. App Store may reject builds if added to UnityFramework (i.e. MoPub in FT).
            project.AddBuildProperty(mainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
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

            SetAttributionReportEndpointIfNeeded(plist);

#if UNITY_2018_2_OR_NEWER
            EnableVerboseLoggingIfNeeded(plist);
#endif
            EnableConsentFlowIfNeeded(plist);
            AddSkAdNetworksInfoIfNeeded(plist);
            UpdateAppTransportSecuritySettingsIfNeeded(plist);
            AddSnapAppStoreAppIdIfNeeded(plist);

            plist.WriteToFile(plistPath);
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

#if UNITY_2018_2_OR_NEWER
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
#endif

        private static void EnableConsentFlowIfNeeded(PlistDocument plist)
        {
            // Check if consent flow is enabled. No need to update info.plist if consent flow is disabled.
            var consentFlowEnabled = AppLovinSettings.Instance.ConsentFlowEnabled;
            if (!consentFlowEnabled) return;

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
            var uriBuilder = new UriBuilder("https://dash.applovin.com/docs/v1/unity_integration_manager/sk_ad_networks_info");

            // Get the list of installed ad networks to be passed up
            var maxMediationDirectory = PluginMediationDirectory;
            if (Directory.Exists(maxMediationDirectory))
            {
                var mediationNetworkDirectories = Directory.GetDirectories(maxMediationDirectory);
                var installedNetworks = mediationNetworkDirectories.Select(Path.GetFileName).ToArray();
                var adNetworks = string.Join(",", installedNetworks);
                if (!string.IsNullOrEmpty(adNetworks))
                {
                    uriBuilder.Query += string.Format("adnetworks={0}", adNetworks);
                }
            }

            var unityWebRequest = UnityWebRequest.Get(uriBuilder.ToString());

#if UNITY_2017_2_OR_NEWER
            var operation = unityWebRequest.SendWebRequest();
#else
            var operation = unityWebRequest.Send();
#endif
            // Wait for the download to complete or the request to timeout.
            while (!operation.isDone) { }


#if UNITY_2020_1_OR_NEWER
            if (unityWebRequest.result != UnityWebRequest.Result.Success)
#elif UNITY_2017_2_OR_NEWER
            if (unityWebRequest.isNetworkError || unityWebRequest.isHttpError)
#else
            if (unityWebRequest.isError)
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

        private static void UpdateAppTransportSecuritySettingsIfNeeded(PlistDocument plist)
        {
            var mediationDir = PluginMediationDirectory;
            var projectHasAtsRequiringNetworks = AtsRequiringNetworks.Any(atsRequiringNetwork => Directory.Exists(Path.Combine(mediationDir, atsRequiringNetwork)));
            if (!projectHasAtsRequiringNetworks) return;

            var root = plist.root.values;
            PlistElement atsRoot;
            root.TryGetValue("NSAppTransportSecurity", out atsRoot);

            if (atsRoot == null || atsRoot.GetType() != typeof(PlistElementDict))
            {
                // Add the missing App Transport Security settings for publishers if needed. 
                MaxSdkLogger.UserDebug("Adding App Transport Security settings...");
                atsRoot = plist.root.CreateDict("NSAppTransportSecurity");
                atsRoot.AsDict().SetBoolean("NSAllowsArbitraryLoads", true);
            }

            var atsRootDict = atsRoot.AsDict().values;
            // Check if both NSAllowsArbitraryLoads and NSAllowsArbitraryLoadsInWebContent are present and remove NSAllowsArbitraryLoadsInWebContent if both are present.
            if (atsRootDict.ContainsKey("NSAllowsArbitraryLoads") && atsRootDict.ContainsKey("NSAllowsArbitraryLoadsInWebContent"))
            {
                MaxSdkLogger.UserDebug("Removing NSAllowsArbitraryLoadsInWebContent");
                atsRootDict.Remove("NSAllowsArbitraryLoadsInWebContent");
            }
        }

        private static void AddSnapAppStoreAppIdIfNeeded(PlistDocument plist)
        {
            var snapDependencyPath = Path.Combine(PluginMediationDirectory, "Snap/Editor/Dependencies.xml");
            if (!File.Exists(snapDependencyPath)) return;

            // App Store App ID is only needed for iOS versions 2.0.0.0 or newer.
            var currentVersion = AppLovinIntegrationManager.GetCurrentVersions(snapDependencyPath);
            var iosVersionComparison = MaxSdkUtils.CompareVersions(currentVersion.Ios, AppLovinSettings.SnapAppStoreAppIdMinVersion);
            if (iosVersionComparison == MaxSdkUtils.VersionComparisonResult.Lesser) return;

            if (AppLovinSettings.Instance.SnapAppStoreAppId <= 0)
            {
                MaxSdkLogger.UserError("Snap App Store App ID is not set. Please enter a valid App ID within the AppLovin Integration Manager window.");
                return;
            }

            plist.root.SetInteger("SCAppStoreAppID", AppLovinSettings.Instance.SnapAppStoreAppId);
        }

        private static bool ShouldEmbedSnapSdk()
        {
            var snapDependencyPath = Path.Combine(PluginMediationDirectory, "Snap/Editor/Dependencies.xml");
            if (!File.Exists(snapDependencyPath)) return false;

            // Return true for UNITY_2019_3_OR_NEWER because app will crash on launch unless embedded.
#if UNITY_2019_3_OR_NEWER
            return true;
#else
            var currentVersion = AppLovinIntegrationManager.GetCurrentVersions(snapDependencyPath);
            var iosVersionComparison = MaxSdkUtils.CompareVersions(currentVersion.Ios, "1.0.7.2");
            return iosVersionComparison != MaxSdkUtils.VersionComparisonResult.Lesser;
#endif
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
