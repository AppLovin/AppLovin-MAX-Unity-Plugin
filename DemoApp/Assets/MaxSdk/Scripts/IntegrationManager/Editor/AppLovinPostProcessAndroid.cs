//
//  MaxPostProcessBuildAndroid.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 4/10/20.
//  Copyright © 2020 AppLovin. All rights reserved.
//

#if UNITY_ANDROID
using AppLovinMax.Scripts.IntegrationManager.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace AppLovinMax.Scripts.Editor
{
    /// <summary>
    /// A post processor used to update the Android project once it is generated.
    /// </summary>
    public class AppLovinPostProcessAndroid : IPostGenerateGradleAndroidProject
    {
#if UNITY_2019_3_OR_NEWER
        private const string PropertyAndroidX = "android.useAndroidX";
        private const string PropertyJetifier = "android.enableJetifier";
        private const string EnableProperty = "=true";
#endif
        private const string PropertyDexingArtifactTransform = "android.enableDexingArtifactTransform";
        private const string DisableProperty = "=false";

        private const string KeyMetaDataAppLovinSdkKey = "applovin.sdk.key";
        private const string KeyMetaDataAppLovinVerboseLoggingOn = "applovin.sdk.verbose_logging";
        private const string KeyMetaDataGoogleApplicationId = "com.google.android.gms.ads.APPLICATION_ID";
        private const string KeyMetaDataGoogleOptimizeInitialization = "com.google.android.gms.ads.flag.OPTIMIZE_INITIALIZATION";
        private const string KeyMetaDataGoogleOptimizeAdLoading = "com.google.android.gms.ads.flag.OPTIMIZE_AD_LOADING";

        private const string KeyMetaDataMobileFuseAutoInit = "com.mobilefuse.sdk.disable_auto_init";
        private const string KeyMetaDataMyTargetAutoInit = "com.my.target.autoInitMode";

#if UNITY_2022_3_OR_NEWER
        // To match "'com.android.library' version '7.3.1'" line in build.gradle
        private static readonly Regex TokenGradleVersionLibrary = new Regex(".*id ['\"]com\\.android\\.library['\"] version");
        private static readonly Regex TokenGradleVersion = new Regex(".*id ['\"]com\\.android\\.application['\"] version");
#else
        // To match "classpath 'com.android.tools.build:gradle:4.0.1'" line in build.gradle
        private static readonly Regex TokenGradleVersion = new Regex(".*classpath ['\"]com\\.android\\.tools\\.build:gradle:.*");
#endif

        // To match "distributionUrl=..." in gradle-wrapper.properties file
        private static readonly Regex TokenDistributionUrl = new Regex(".*distributionUrl.*");

        private static readonly XNamespace AndroidNamespace = "http://schemas.android.com/apk/res/android";

        private static string PluginMediationDirectory
        {
            get
            {
                var pluginParentDir = AppLovinIntegrationManager.MediationSpecificPluginParentDirectory;
                return Path.Combine(pluginParentDir, "MaxSdk/Mediation/");
            }
        }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
#if UNITY_2019_3_OR_NEWER
            var rootGradleBuildFilePath = Path.Combine(path, "../build.gradle");
            var gradlePropertiesPath = Path.Combine(path, "../gradle.properties");
            var gradleWrapperPropertiesPath = Path.Combine(path, "../gradle/wrapper/gradle-wrapper.properties");
#else
            var rootGradleBuildFilePath = Path.Combine(path, "build.gradle");
            var gradlePropertiesPath = Path.Combine(path, "gradle.properties");
            var gradleWrapperPropertiesPath = Path.Combine(path, "gradle/wrapper/gradle-wrapper.properties");
#endif

            UpdateGradleVersionsIfNeeded(gradleWrapperPropertiesPath, rootGradleBuildFilePath);

            var gradlePropertiesUpdated = new List<string>();

            // If the gradle properties file already exists, make sure to add any previous properties.
            if (File.Exists(gradlePropertiesPath))
            {
                var lines = File.ReadAllLines(gradlePropertiesPath);

#if UNITY_2019_3_OR_NEWER
                // Add all properties except AndroidX, Jetifier, and DexingArtifactTransform since they may already exist. We will re-add them below.
                gradlePropertiesUpdated.AddRange(lines.Where(line => !line.Contains(PropertyAndroidX) && !line.Contains(PropertyJetifier) && !line.Contains(PropertyDexingArtifactTransform)));
#else
                // Add all properties except DexingArtifactTransform since it may already exist. We will re-add it below.
                gradlePropertiesUpdated.AddRange(lines.Where(line => !line.Contains(PropertyDexingArtifactTransform)));
#endif
            }

#if UNITY_2019_3_OR_NEWER
            // Enable AndroidX and Jetifier properties
            gradlePropertiesUpdated.Add(PropertyAndroidX + EnableProperty);
            gradlePropertiesUpdated.Add(PropertyJetifier + EnableProperty);
#endif
            // Disable dexing using artifact transform (it causes issues for ExoPlayer with Gradle plugin 3.5.0+)
            gradlePropertiesUpdated.Add(PropertyDexingArtifactTransform + DisableProperty);

            try
            {
                File.WriteAllText(gradlePropertiesPath, string.Join("\n", gradlePropertiesUpdated.ToArray()) + "\n");
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Failed to enable AndroidX and Jetifier. gradle.properties file write failed.");
                Console.WriteLine(exception);
            }

            ProcessAndroidManifest(path);

            var rawResourceDirectory = Path.Combine(path, "src/main/res/raw");
            if (AppLovinInternalSettings.Instance.ConsentFlowEnabled)
            {
                AppLovinPreProcessAndroid.EnableConsentFlowIfNeeded(rawResourceDirectory);
            }
            else
            {
                AppLovinPreProcessAndroid.EnableTermsFlowIfNeeded(rawResourceDirectory);
            }
        }

        public int callbackOrder
        {
            get { return int.MaxValue; }
        }

        private static void ProcessAndroidManifest(string path)
        {
            var manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
            XDocument manifest;
            try
            {
                manifest = XDocument.Load(manifestPath);
            }
#pragma warning disable 0168
            catch (IOException exception)
#pragma warning restore 0168
            {
                MaxSdkLogger.UserWarning("[AppLovin MAX] AndroidManifest.xml is missing.");
                return;
            }

            // Get the `manifest` element.
            var elementManifest = manifest.Element("manifest");
            if (elementManifest == null)
            {
                MaxSdkLogger.UserWarning("[AppLovin MAX] AndroidManifest.xml is invalid.");
                return;
            }

            var elementApplication = elementManifest.Element("application");
            if (elementApplication == null)
            {
                MaxSdkLogger.UserWarning("[AppLovin MAX] AndroidManifest.xml is invalid.");
                return;
            }

            var metaDataElements = elementApplication.Descendants().Where(element => element.Name.LocalName.Equals("meta-data"));

            AddSdkKeyIfNeeded(elementApplication);
            EnableVerboseLoggingIfNeeded(elementApplication);
            AddGoogleApplicationIdIfNeeded(elementApplication, metaDataElements);
            AddGoogleOptimizationFlagsIfNeeded(elementApplication, metaDataElements);
            DisableAutoInitIfNeeded(elementApplication, metaDataElements);

            // Save the updated manifest file.
            manifest.Save(manifestPath);
        }

        private static void AddSdkKeyIfNeeded(XElement elementApplication)
        {
            var sdkKey = AppLovinSettings.Instance.SdkKey;
            if (string.IsNullOrEmpty(sdkKey)) return;

            var descendants = elementApplication.Descendants();
            var sdkKeyMetaData = descendants.FirstOrDefault(descendant => descendant.FirstAttribute != null &&
                                                                          descendant.FirstAttribute.Name.LocalName.Equals("name") &&
                                                                          descendant.FirstAttribute.Value.Equals(KeyMetaDataAppLovinSdkKey) &&
                                                                          descendant.LastAttribute != null &&
                                                                          descendant.LastAttribute.Name.LocalName.Equals("value"));

            // check if applovin.sdk.key meta data exists.
            if (sdkKeyMetaData != null)
            {
                sdkKeyMetaData.LastAttribute.Value = sdkKey;
            }
            else
            {
                // add applovin.sdk.key meta data if it does not exist.
                var metaData = CreateMetaDataElement(KeyMetaDataAppLovinSdkKey, sdkKey);
                elementApplication.Add(metaData);
            }
        }

        private static void EnableVerboseLoggingIfNeeded(XElement elementApplication)
        {
            var enabled = EditorPrefs.GetBool(MaxSdkLogger.KeyVerboseLoggingEnabled, false);

            var descendants = elementApplication.Descendants();
            var verboseLoggingMetaData = descendants.FirstOrDefault(descendant => descendant.FirstAttribute != null &&
                                                                                  descendant.FirstAttribute.Name.LocalName.Equals("name") &&
                                                                                  descendant.FirstAttribute.Value.Equals(KeyMetaDataAppLovinVerboseLoggingOn) &&
                                                                                  descendant.LastAttribute != null &&
                                                                                  descendant.LastAttribute.Name.LocalName.Equals("value"));

            // check if applovin.sdk.verbose_logging meta data exists.
            if (verboseLoggingMetaData != null)
            {
                if (enabled)
                {
                    // update applovin.sdk.verbose_logging meta data value.
                    verboseLoggingMetaData.LastAttribute.Value = enabled.ToString();
                }
                else
                {
                    // remove applovin.sdk.verbose_logging meta data.
                    verboseLoggingMetaData.Remove();
                }
            }
            else
            {
                if (enabled)
                {
                    // add applovin.sdk.verbose_logging meta data if it does not exist.
                    var metaData = CreateMetaDataElement(KeyMetaDataAppLovinVerboseLoggingOn, enabled.ToString());
                    elementApplication.Add(metaData);
                }
            }
        }

        private static void AddGoogleApplicationIdIfNeeded(XElement elementApplication, IEnumerable<XElement> metaDataElements)
        {
            if (!AppLovinIntegrationManager.IsAdapterInstalled("Google") && !AppLovinIntegrationManager.IsAdapterInstalled("GoogleAdManager")) return;

            var googleApplicationIdMetaData = GetMetaDataElement(metaDataElements, KeyMetaDataGoogleApplicationId);
            var appId = AppLovinSettings.Instance.AdMobAndroidAppId;
            // Log error if the App ID is not set.
            if (string.IsNullOrEmpty(appId) || !appId.StartsWith("ca-app-pub-"))
            {
                MaxSdkLogger.UserError("Google App ID is not set. Please enter a valid app ID within the AppLovin Integration Manager window.");
                return;
            }

            // Check if the Google App ID meta data already exists. Update if it already exists.
            if (googleApplicationIdMetaData != null)
            {
                googleApplicationIdMetaData.SetAttributeValue(AndroidNamespace + "value", appId);
            }
            // Meta data doesn't exist, add it.
            else
            {
                elementApplication.Add(CreateMetaDataElement(KeyMetaDataGoogleApplicationId, appId));
            }
        }

        private static void AddGoogleOptimizationFlagsIfNeeded(XElement elementApplication, IEnumerable<XElement> metaDataElements)
        {
            if (!AppLovinIntegrationManager.IsAdapterInstalled("Google") && !AppLovinIntegrationManager.IsAdapterInstalled("GoogleAdManager")) return;

            var googleOptimizeInitializationMetaData = GetMetaDataElement(metaDataElements, KeyMetaDataGoogleOptimizeInitialization);
            // If meta data doesn't exist, add it
            if (googleOptimizeInitializationMetaData == null)
            {
                elementApplication.Add(CreateMetaDataElement(KeyMetaDataGoogleOptimizeInitialization, true));
            }

            var googleOptimizeAdLoadingMetaData = GetMetaDataElement(metaDataElements, KeyMetaDataGoogleOptimizeAdLoading);
            // If meta data doesn't exist, add it
            if (googleOptimizeAdLoadingMetaData == null)
            {
                elementApplication.Add(CreateMetaDataElement(KeyMetaDataGoogleOptimizeAdLoading, true));
            }
        }

        private static void DisableAutoInitIfNeeded(XElement elementApplication, IEnumerable<XElement> metaDataElements)
        {
            if (AppLovinIntegrationManager.IsAdapterInstalled("MobileFuse"))
            {
                var mobileFuseMetaData = GetMetaDataElement(metaDataElements, KeyMetaDataMobileFuseAutoInit);
                // If MobileFuse meta data doesn't exist, add it
                if (mobileFuseMetaData == null)
                {
                    elementApplication.Add(CreateMetaDataElement(KeyMetaDataMobileFuseAutoInit, true));
                }
            }

            if (AppLovinIntegrationManager.IsAdapterInstalled("MyTarget"))
            {
                var myTargetMetaData = GetMetaDataElement(metaDataElements, KeyMetaDataMyTargetAutoInit);
                // If MyTarget meta data doesn't exist, add it
                if (myTargetMetaData == null)
                {
                    elementApplication.Add(CreateMetaDataElement(KeyMetaDataMyTargetAutoInit, 0));
                }
            }
        }

        private static void UpdateGradleVersionsIfNeeded(string gradleWrapperPropertiesPath, string rootGradleBuildFilePath)
        {
            var customGradleVersionUrl = AppLovinSettings.Instance.CustomGradleVersionUrl;
            var customGradleToolsVersion = AppLovinSettings.Instance.CustomGradleToolsVersion;

            if (MaxSdkUtils.IsValidString(customGradleVersionUrl))
            {
                var newDistributionUrl = string.Format("distributionUrl={0}", customGradleVersionUrl);
                if (ReplaceStringInFile(gradleWrapperPropertiesPath, TokenDistributionUrl, newDistributionUrl))
                {
                    MaxSdkLogger.D("Distribution url set to " + newDistributionUrl);
                }
                else
                {
                    MaxSdkLogger.E("Failed to set distribution URL");
                }
            }

            if (MaxSdkUtils.IsValidString(customGradleToolsVersion))
            {
#if UNITY_2022_3_OR_NEWER
                // Unity 2022.3+ requires Gradle Plugin version 7.1.2+.
                if (MaxSdkUtils.CompareVersions(customGradleToolsVersion, "7.1.2") == MaxSdkUtils.VersionComparisonResult.Lesser)
                {
                    MaxSdkLogger.E("Failed to set gradle plugin version. Unity 2022.3+ requires gradle plugin version 7.1.2+");
                    return;
                }

                var newGradleVersionLibraryLine = AppLovinProcessGradleBuildFile.GetFormattedBuildScriptLine(string.Format("id 'com.android.library' version '{0}' apply false", customGradleToolsVersion));
                if (ReplaceStringInFile(rootGradleBuildFilePath, TokenGradleVersionLibrary, newGradleVersionLibraryLine))
                {
                    MaxSdkLogger.D("Gradle library version set to " + newGradleVersionLibraryLine);
                }
                else
                {
                    MaxSdkLogger.E("Failed to set gradle library version");
                }

                var newGradleVersionLine = AppLovinProcessGradleBuildFile.GetFormattedBuildScriptLine(string.Format("id 'com.android.application' version '{0}' apply false", customGradleToolsVersion));
#else
                var newGradleVersionLine = AppLovinProcessGradleBuildFile.GetFormattedBuildScriptLine(string.Format("classpath 'com.android.tools.build:gradle:{0}'", customGradleToolsVersion));
#endif
                if (ReplaceStringInFile(rootGradleBuildFilePath, TokenGradleVersion, newGradleVersionLine))
                {
                    MaxSdkLogger.D("Gradle version set to " + newGradleVersionLine);
                }
                else
                {
                    MaxSdkLogger.E("Failed to set gradle plugin version");
                }
            }
        }

        /// <summary>
        /// Creates and returns a <c>meta-data</c> element with the given name and value. 
        /// </summary>
        private static XElement CreateMetaDataElement(string name, object value)
        {
            var metaData = new XElement("meta-data");
            metaData.Add(new XAttribute(AndroidNamespace + "name", name));
            metaData.Add(new XAttribute(AndroidNamespace + "value", value));

            return metaData;
        }

        /// <summary>
        /// Looks through all the given meta-data elements to check if the required one exists. Returns <c>null</c> if it doesn't exist.
        /// </summary>
        private static XElement GetMetaDataElement(IEnumerable<XElement> metaDataElements, string metaDataName)
        {
            foreach (var metaDataElement in metaDataElements)
            {
                var attributes = metaDataElement.Attributes();
                if (attributes.Any(attribute => attribute.Name.Namespace.Equals(AndroidNamespace)
                                                && attribute.Name.LocalName.Equals("name")
                                                && attribute.Value.Equals(metaDataName)))
                {
                    return metaDataElement;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the first line that contains regexToMatch and replaces the whole line with replacement
        /// </summary>
        /// <param name="path">Path to the file you want to replace a line in</param>
        /// <param name="regexToMatch">Regex to search for in the line you want to replace</param>
        /// <param name="replacement">String that you want as the new line</param>
        /// <returns>Returns whether the string was successfully replaced or not</returns>
        private static bool ReplaceStringInFile(string path, Regex regexToMatch, string replacement)
        {
            if (!File.Exists(path)) return false;

            var lines = File.ReadAllLines(path);
            for (var i = 0; i < lines.Length; i++)
            {
                if (regexToMatch.IsMatch(lines[i]))
                {
                    lines[i] = replacement;
                    File.WriteAllLines(path, lines);
                    return true;
                }
            }

            return false;
        }
    }
}

#endif
