//
//  MaxIntegrationManager.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 8/29/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

#if UNITY_IOS || UNITY_IPHONE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_2019_3_OR_NEWER
using UnityEditor.iOS.Xcode.Extensions;
#endif
using UnityEditor.iOS.Xcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    [Serializable]
    public class SkAdNetworkData
    {
        [SerializeField] public string[] SkAdNetworkIds;
    }

    public class AppLovinPostProcessiOS
    {
        private const string OutputFileName = "AppLovinQualityServiceSetup.rb";

#if !UNITY_2019_3_OR_NEWER
        private const string UnityMainTargetName = "Unity-iPhone";
#endif
        // Use a priority of 90 to have AppLovin embed frameworks after Pods are installed (EDM finishes installing Pods at priority 60) and before Firebase Crashlytics runs their scripts (at priority 100).
        private const int AppLovinEmbedFrameworksPriority = 90;

        private const string TargetUnityIphonePodfileLine = "target 'Unity-iPhone' do";
        private const string UseFrameworksPodfileLine = "use_frameworks!";
        private const string UseFrameworksDynamicPodfileLine = "use_frameworks! :linkage => :dynamic";
        private const string UseFrameworksStaticPodfileLine = "use_frameworks! :linkage => :static";

        private const string ResourcesDirectoryName = "Resources";
        private const string AppLovinMaxResourcesDirectoryName = "AppLovinMAXResources";
        private const string AppLovinAdvertisingAttributionEndpoint = "https://postbacks-app.com";

        private const string AppLovinSettingsPlistFileName = "AppLovin-Settings.plist";

        private const string KeySdkKey = "SdkKey";

        private const string AppLovinVerboseLoggingOnKey = "AppLovinVerboseLoggingOn";

        private const string KeyConsentFlowInfo = "ConsentFlowInfo";
        private const string KeyConsentFlowEnabled = "ConsentFlowEnabled";
        private const string KeyConsentFlowTermsOfService = "ConsentFlowTermsOfService";
        private const string KeyConsentFlowPrivacyPolicy = "ConsentFlowPrivacyPolicy";
        private const string KeyConsentFlowDebugUserGeography = "ConsentFlowDebugUserGeography";

        private const string KeyAppLovinSdkKeyToRemove = "AppLovinSdkKey";

        private static readonly Regex PodfilePodLineRegex = new Regex("pod \'([^\']*)\'");

        /// <summary>
        /// Adds AppLovin Quality Service to the iOS project once the project has been exported.
        ///
        /// 1. Downloads the Quality Service ruby script.
        /// 2. Runs the script using Ruby which integrates AppLovin Quality Service to the project.
        /// </summary>
        [PostProcessBuild(int.MaxValue)] // We want to run Quality Service script last.
        public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (!AppLovinSettings.Instance.QualityServiceEnabled) return;

            var sdkKey = AppLovinSettings.Instance.SdkKey;
            if (string.IsNullOrEmpty(sdkKey))
            {
                MaxSdkLogger.UserError("Failed to install AppLovin Quality Service plugin. SDK Key is empty. Please enter the AppLovin SDK Key in the Integration Manager.");
                return;
            }

            var outputFilePath = Path.Combine(buildPath, OutputFileName);

            // Check if Quality Service is already installed.
            if (File.Exists(outputFilePath) && Directory.Exists(Path.Combine(buildPath, "AppLovinQualityService")))
            {
                // TODO: Check if there is a way to validate if the SDK key matches the script. Else the pub can't use append when/if they change the SDK Key.
                return;
            }

            // Download the ruby script needed to install Quality Service
            var downloadHandler = new DownloadHandlerFile(outputFilePath);
            var postJson = string.Format("{{\"sdk_key\" : \"{0}\"}}", sdkKey);
            var bodyRaw = Encoding.UTF8.GetBytes(postJson);
            var uploadHandler = new UploadHandlerRaw(bodyRaw);
            uploadHandler.contentType = "application/json";

            using (var unityWebRequest = new UnityWebRequest("https://api2.safedk.com/v1/build/ios_setup2"))
            {
                unityWebRequest.method = UnityWebRequest.kHttpVerbPOST;
                unityWebRequest.downloadHandler = downloadHandler;
                unityWebRequest.uploadHandler = uploadHandler;
                var operation = unityWebRequest.SendWebRequest();

                // Wait for the download to complete or the request to timeout.
                while (!operation.isDone) { }

#if UNITY_2020_1_OR_NEWER
                if (unityWebRequest.result != UnityWebRequest.Result.Success)
#else
                if (unityWebRequest.isNetworkError || unityWebRequest.isHttpError)
#endif
                {
                    MaxSdkLogger.UserError("AppLovin Quality Service installation failed. Failed to download script with error: " + unityWebRequest.error);
                    return;
                }

                // Check if Ruby is installed
                var rubyVersion = AppLovinCommandLine.Run("ruby", "--version", buildPath);
                if (rubyVersion.ExitCode != 0)
                {
                    MaxSdkLogger.UserError("AppLovin Quality Service installation requires Ruby. Please install Ruby, export it to your system PATH and re-export the project.");
                    return;
                }

                // Ruby is installed, run `ruby AppLovinQualityServiceSetup.rb`
                var result = AppLovinCommandLine.Run("ruby", OutputFileName, buildPath);

                // Check if we have an error.
                if (result.ExitCode != 0) MaxSdkLogger.UserError("Failed to set up AppLovin Quality Service");

                MaxSdkLogger.UserDebug(result.Message);
            }
        }

        [PostProcessBuild(AppLovinEmbedFrameworksPriority)]
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

            LocalizeUserTrackingDescriptionIfNeeded(AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionDe, "de", buildPath, project, unityMainTargetGuid);
            LocalizeUserTrackingDescriptionIfNeeded(AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionEn, "en", buildPath, project, unityMainTargetGuid);
            LocalizeUserTrackingDescriptionIfNeeded(AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionEs, "es", buildPath, project, unityMainTargetGuid);
            LocalizeUserTrackingDescriptionIfNeeded(AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionFr, "fr", buildPath, project, unityMainTargetGuid);
            LocalizeUserTrackingDescriptionIfNeeded(AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionJa, "ja", buildPath, project, unityMainTargetGuid);
            LocalizeUserTrackingDescriptionIfNeeded(AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionKo, "ko", buildPath, project, unityMainTargetGuid);
            LocalizeUserTrackingDescriptionIfNeeded(AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionZhHans, "zh-Hans", buildPath, project, unityMainTargetGuid);
            LocalizeUserTrackingDescriptionIfNeeded(AppLovinInternalSettings.Instance.UserTrackingUsageDescriptionZhHant, "zh-Hant", buildPath, project, unityMainTargetGuid);

            AddSwiftSupport(buildPath, project, unityFrameworkTargetGuid, unityMainTargetGuid);
            AddYandexSettingsIfNeeded(project, unityMainTargetGuid);

            project.WriteToFile(projectPath);
        }

        private static void EmbedDynamicLibrariesIfNeeded(string buildPath, PBXProject project, string targetGuid)
        {
            // Check that the Pods directory exists (it might not if a publisher is building with Generate Podfile setting disabled in EDM).
            var podsDirectory = Path.Combine(buildPath, "Pods");
            if (!Directory.Exists(podsDirectory) || !ShouldEmbedDynamicLibraries(buildPath)) return;

            var dynamicLibraryPathsToEmbed = GetDynamicLibraryPathsToEmbed(podsDirectory, buildPath);
            if (dynamicLibraryPathsToEmbed == null || dynamicLibraryPathsToEmbed.Count == 0) return;

#if UNITY_2019_3_OR_NEWER
            foreach (var dynamicLibraryPath in dynamicLibraryPathsToEmbed)
            {
                var fileGuid = project.AddFile(dynamicLibraryPath, dynamicLibraryPath);
                project.AddFileToEmbedFrameworks(targetGuid, fileGuid);
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

        /// <summary>
        /// |-----------------------------------------------------------------------------------------------------------------------------------------------------|
        /// |         embed             |  use_frameworks! (:linkage => :dynamic)  |  use_frameworks! :linkage => :static  |  `use_frameworks!` line not present  |
        /// |---------------------------|------------------------------------------|---------------------------------------|--------------------------------------|
        /// | Unity-iPhone present      | Do not embed dynamic libraries           | Embed dynamic libraries               | Do not embed dynamic libraries       |
        /// | Unity-iPhone not present  | Embed dynamic libraries                  | Embed dynamic libraries               | Embed dynamic libraries              |
        /// |-----------------------------------------------------------------------------------------------------------------------------------------------------|
        /// </summary>
        /// <param name="buildPath">An iOS build path</param>
        /// <returns>Whether or not the dynamic libraries should be embedded.</returns>
        private static bool ShouldEmbedDynamicLibraries(string buildPath)
        {
            var podfilePath = Path.Combine(buildPath, "Podfile");
            if (!File.Exists(podfilePath)) return false;

            // If the Podfile doesn't have a `Unity-iPhone` target, we should embed the dynamic libraries.
            var lines = File.ReadAllLines(podfilePath);
            var containsUnityIphoneTarget = lines.Any(line => line.Contains(TargetUnityIphonePodfileLine));
            if (!containsUnityIphoneTarget) return true;

            // If the Podfile does not have a `use_frameworks! :linkage => static` line, we should not embed the dynamic libraries.
            var useFrameworksStaticLineIndex = Array.FindIndex(lines, line => line.Contains(UseFrameworksStaticPodfileLine));
            if (useFrameworksStaticLineIndex == -1) return false;

            // If more than one of the `use_frameworks!` lines are present, CocoaPods will use the last one.
            var useFrameworksLineIndex = Array.FindIndex(lines, line => line.Trim() == UseFrameworksPodfileLine); // Check for exact line to avoid matching `use_frameworks! :linkage => static/dynamic`
            var useFrameworksDynamicLineIndex = Array.FindIndex(lines, line => line.Contains(UseFrameworksDynamicPodfileLine));

            // Check if `use_frameworks! :linkage => :static` is the last line of the three. If it is, we should embed the dynamic libraries.
            return useFrameworksLineIndex < useFrameworksStaticLineIndex && useFrameworksDynamicLineIndex < useFrameworksStaticLineIndex;
        }

        private static List<string> GetDynamicLibraryPathsToEmbed(string podsDirectory, string buildPath)
        {
            var podfilePath = Path.Combine(buildPath, "Podfile");
            var dynamicLibraryFrameworksToEmbed = GetDynamicLibraryFrameworksToEmbed(podfilePath);

            return GetDynamicLibraryPathsInProjectToEmbed(podsDirectory, dynamicLibraryFrameworksToEmbed);
        }

        private static List<string> GetDynamicLibraryFrameworksToEmbed(string podfilePath)
        {
            var dynamicLibrariesToEmbed = GetDynamicLibrariesToEmbed();

            var podsInUnityIphoneTarget = GetPodNamesInUnityIphoneTarget(podfilePath);
            var dynamicLibrariesToIgnore = dynamicLibrariesToEmbed.Where(dynamicLibraryToEmbed => podsInUnityIphoneTarget.Contains(dynamicLibraryToEmbed.PodName)).ToList();

            // Determine frameworks to embed based on the dynamic libraries to embed and ignore
            var dynamicLibraryFrameworksToIgnore = dynamicLibrariesToIgnore.SelectMany(library => library.FrameworkNames).Distinct().ToList();
            return dynamicLibrariesToEmbed.SelectMany(library => library.FrameworkNames).Except(dynamicLibraryFrameworksToIgnore).Distinct().ToList();
        }

        private static List<DynamicLibraryToEmbed> GetDynamicLibrariesToEmbed()
        {
            var pluginData = AppLovinIntegrationManager.LoadPluginDataSync();
            if (pluginData == null)
            {
                MaxSdkLogger.E("Failed to load plugin data. Dynamic libraries will not be embedded.");
                return null;
            }

            // Get the dynamic libraries to embed for each network
            var librariesToAdd = pluginData.MediatedNetworks
                .Where(network => network.DynamicLibrariesToEmbed != null)
                .SelectMany(network => network.DynamicLibrariesToEmbed
                    .Where(libraryToEmbed => IsRequiredNetworkVersionInstalled(libraryToEmbed, network)))
                .ToList();

            // Get the dynamic libraries to embed for AppLovin MAX
            if (pluginData.AppLovinMax.DynamicLibrariesToEmbed != null)
            {
                librariesToAdd.AddRange(pluginData.AppLovinMax.DynamicLibrariesToEmbed);
            }

            // Get the dynamic libraries to embed for third parties
            if (pluginData.ThirdPartyDynamicLibrariesToEmbed != null)
            {
                // TODO: Add version check for third party dynamic libraries.
                librariesToAdd.AddRange(pluginData.ThirdPartyDynamicLibrariesToEmbed);
            }

            return librariesToAdd;
        }

        private static List<string> GetPodNamesInUnityIphoneTarget(string podfilePath)
        {
            var lines = File.ReadAllLines(podfilePath);
            var podNamesInUnityIphone = new List<string>();

            var insideUnityIphoneTarget = false;
            foreach (var line in lines)
            {
                // Loop until we find the `target 'Unity-iPhone'` line
                if (insideUnityIphoneTarget)
                {
                    if (line.Trim() == "end") break;

                    if (PodfilePodLineRegex.IsMatch(line))
                    {
                        var podName = PodfilePodLineRegex.Match(line).Groups[1].Value;
                        podNamesInUnityIphone.Add(podName);
                    }
                }
                else if (line.Contains(TargetUnityIphonePodfileLine))
                {
                    insideUnityIphoneTarget = true;
                }
            }

            return podNamesInUnityIphone;
        }

        private static bool IsRequiredNetworkVersionInstalled(DynamicLibraryToEmbed libraryToEmbed, Network network)
        {
            var currentIosVersion = network.CurrentVersions.Ios;
            if (string.IsNullOrEmpty(currentIosVersion)) return false;

            var minIosVersion = libraryToEmbed.MinVersion;
            var maxIosVersion = libraryToEmbed.MaxVersion;

            var greaterThanOrEqualToMinVersion = string.IsNullOrEmpty(minIosVersion) || MaxSdkUtils.CompareVersions(currentIosVersion, minIosVersion) != MaxSdkUtils.VersionComparisonResult.Lesser;
            var lessThanOrEqualToMaxVersion = string.IsNullOrEmpty(maxIosVersion) || MaxSdkUtils.CompareVersions(currentIosVersion, maxIosVersion) != MaxSdkUtils.VersionComparisonResult.Greater;

            return greaterThanOrEqualToMinVersion && lessThanOrEqualToMaxVersion;
        }

        private static List<string> GetDynamicLibraryPathsInProjectToEmbed(string podsDirectory, List<string> dynamicLibrariesToEmbed)
        {
            var dynamicLibraryPathsPresentInProject = new List<string>();
            foreach (var dynamicLibraryToSearch in dynamicLibrariesToEmbed)
            {
                // both .framework and .xcframework are directories, not files
                var directories = Directory.GetDirectories(podsDirectory, dynamicLibraryToSearch, SearchOption.AllDirectories);
                if (directories.Length <= 0) continue;

                var dynamicLibraryAbsolutePath = directories[0];
                var relativePath = GetDynamicLibraryRelativePath(dynamicLibraryAbsolutePath);
                dynamicLibraryPathsPresentInProject.Add(relativePath);
            }

            return dynamicLibraryPathsPresentInProject;
        }

        private static string GetDynamicLibraryRelativePath(string dynamicLibraryAbsolutePath)
        {
            var index = dynamicLibraryAbsolutePath.LastIndexOf("Pods", StringComparison.Ordinal);
            return dynamicLibraryAbsolutePath.Substring(index);
        }

        private static void LocalizeUserTrackingDescriptionIfNeeded(string localizedUserTrackingDescription, string localeCode, string buildPath, PBXProject project, string targetGuid)
        {
            var resourcesDirectoryPath = Path.Combine(buildPath, AppLovinMaxResourcesDirectoryName);
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

            // Log an error if we detect a localization file for this language in the `Resources` directory
            var legacyResourcedDirectoryPath = Path.Combine(buildPath, ResourcesDirectoryName);
            var localeSpecificLegacyDirectoryPath = Path.Combine(legacyResourcedDirectoryPath, localeSpecificDirectoryName);
            if (Directory.Exists(localeSpecificLegacyDirectoryPath))
            {
                MaxSdkLogger.UserError("Detected existing localization resource for \"" + localeCode + "\" locale. Skipping localization for User Tracking Usage Description. Please disable localization in AppLovin Integration manager and add the localizations to your existing resource.");
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

            var localeSpecificDirectoryRelativePath = Path.Combine(AppLovinMaxResourcesDirectoryName, localeSpecificDirectoryName);
            var guid = project.AddFolderReference(localeSpecificDirectoryRelativePath, localeSpecificDirectoryRelativePath);
            project.AddFileToBuild(targetGuid, guid);
        }

        private static bool ShouldRemoveLocalization(string localizedUserTrackingDescription)
        {
            if (string.IsNullOrEmpty(localizedUserTrackingDescription)) return true;

            var internalSettings = AppLovinInternalSettings.Instance;
            return !internalSettings.ConsentFlowEnabled || !internalSettings.UserTrackingUsageLocalizationEnabled;
        }

        private static void AddSwiftSupport(string buildPath, PBXProject project, string unityFrameworkTargetGuid, string unityMainTargetGuid)
        {
            var swiftFileRelativePath = "Classes/MAXSwiftSupport.swift";
            var swiftFilePath = Path.Combine(buildPath, swiftFileRelativePath);

            // Add Swift file
            CreateSwiftFile(swiftFilePath);
            var swiftFileGuid = project.AddFile(swiftFileRelativePath, swiftFileRelativePath);
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

        [PostProcessBuild(int.MaxValue)]
        public static void MaxPostProcessPlist(BuildTarget buildTarget, string path)
        {
            var plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            SetAttributionReportEndpointIfNeeded(plist);

            EnableVerboseLoggingIfNeeded(plist);
            AddGoogleApplicationIdIfNeeded(plist);

            AddSdkSettings(plist, path);
            AddSkAdNetworksInfoIfNeeded(plist);
            RemoveSdkKeyIfNeeded(plist);

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

        private static void EnableVerboseLoggingIfNeeded(PlistDocument plist)
        {
            if (!EditorPrefs.HasKey(MaxSdkLogger.KeyVerboseLoggingEnabled)) return;

            var enabled = EditorPrefs.GetBool(MaxSdkLogger.KeyVerboseLoggingEnabled);
            if (enabled)
            {
                plist.root.SetBoolean(AppLovinVerboseLoggingOnKey, true);
            }
            else
            {
                plist.root.values.Remove(AppLovinVerboseLoggingOnKey);
            }
        }

        private static void AddGoogleApplicationIdIfNeeded(PlistDocument plist)
        {
            if (!AppLovinPackageManager.IsAdapterInstalled("Google") && !AppLovinPackageManager.IsAdapterInstalled("GoogleAdManager")) return;

            const string googleApplicationIdentifier = "GADApplicationIdentifier";
            var appId = AppLovinSettings.Instance.AdMobIosAppId;
            // Log error if the App ID is not set.
            if (string.IsNullOrEmpty(appId) || !appId.StartsWith("ca-app-pub-"))
            {
                MaxSdkLogger.UserError("[AppLovin MAX] Google App ID is not set. Please enter a valid app ID within the AppLovin Integration Manager window.");
                return;
            }

            plist.root.SetString(googleApplicationIdentifier, appId);
        }

        private static void AddYandexSettingsIfNeeded(PBXProject project, string unityMainTargetGuid)
        {
            if (!AppLovinPackageManager.IsAdapterInstalled("Yandex")) return;

            if (MaxSdkUtils.CompareVersions(PlayerSettings.iOS.targetOSVersionString, "12.0") == MaxSdkUtils.VersionComparisonResult.Lesser)
            {
                MaxSdkLogger.UserWarning("Your iOS target version is under the minimum required version by Yandex. Please update it to 12.0 or newer in your ProjectSettings and rebuild your project.");
                return;
            }

            project.SetBuildProperty(unityMainTargetGuid, "GENERATE_INFOPLIST_FILE", "NO");
        }

        private static void AddSdkSettings(PlistDocument infoPlist, string buildPath)
        {
            var sdkSettingsPlistPath = Path.Combine(buildPath, AppLovinSettingsPlistFileName);
            var sdkSettingsPlist = new PlistDocument();
            if (File.Exists(sdkSettingsPlistPath))
            {
                sdkSettingsPlist.ReadFromFile(sdkSettingsPlistPath);
            }

            // Add the SDK key to the SDK settings plist.
            sdkSettingsPlist.root.SetString(KeySdkKey, AppLovinSettings.Instance.SdkKey);

            // Add consent flow settings if needed.
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
            if (!consentFlowEnabled) return;

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
            // Get the list of installed ad networks to be passed up
            var installedNetworks = AppLovinPackageManager.GetInstalledMediationNetworks();
            var uriBuilder = new UriBuilder("https://unity.applovin.com/max/1.0/skadnetwork_ids");
            var adNetworks = string.Join(",", installedNetworks.ToArray());
            if (!string.IsNullOrEmpty(adNetworks))
            {
                uriBuilder.Query += string.Format("ad_networks={0}", adNetworks);
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

        private static void RemoveSdkKeyIfNeeded(PlistDocument plist)
        {
            if (!plist.root.values.ContainsKey(KeyAppLovinSdkKeyToRemove)) return;

            plist.root.values.Remove(KeyAppLovinSdkKeyToRemove);
        }
    }
}

#endif
