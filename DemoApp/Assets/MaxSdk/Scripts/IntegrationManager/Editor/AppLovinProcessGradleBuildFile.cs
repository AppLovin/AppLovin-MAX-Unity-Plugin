//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 9/3/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

#if UNITY_ANDROID

using System.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    [Serializable]
    public class AppLovinQualityServiceData
    {
        public string api_key;
    }

    /// <summary>
    /// Adds or updates the AppLovin Quality Service plugin to the provided build.gradle file.
    /// If the gradle file already has the plugin, the API key is updated.
    /// </summary>
    public abstract class AppLovinProcessGradleBuildFile
    {
        private static readonly Regex TokenBuildScriptRepositories = new Regex(".*repositories.*");
        private static readonly Regex TokenBuildScriptDependencies = new Regex(".*classpath \'com.android.tools.build:gradle.*");
        private static readonly Regex TokenApplicationPlugin = new Regex(".*apply plugin: \'com.android.application\'.*");
        private static readonly Regex TokenApiKey = new Regex(".*apiKey.*");
        private static readonly Regex TokenAppLovinPlugin = new Regex(".*apply plugin:.+?(?=applovin-quality-service).*");

#if UNITY_2022_2_OR_NEWER
        private const string PluginsMatcher = "plugins";
        private const string PluginManagementMatcher = "pluginManagement";
        private const string QualityServicePluginRoot = "    id 'com.applovin.quality' version '+' apply false // NOTE: Requires version 4.8.3+ for Gradle version 7.2+";
#endif

        private const string BuildScriptMatcher = "buildscript";
        private const string QualityServiceMavenRepo = "maven { url 'https://artifacts.applovin.com/android'; content { includeGroupByRegex 'com.applovin.*' } }";
        private const string QualityServiceDependencyClassPath = "classpath 'com.applovin.quality:AppLovinQualityServiceGradlePlugin:+'";
        private const string QualityServiceApplyPlugin = "apply plugin: 'applovin-quality-service'";
        private const string QualityServicePlugin = "applovin {";
        private const string QualityServiceApiKey = "    apiKey '{0}'";
        private const string QualityServiceBintrayMavenRepo = "https://applovin.bintray.com/Quality-Service";
        private const string QualityServiceNoRegexMavenRepo = "maven { url 'https://artifacts.applovin.com/android' }";

        // Legacy plugin detection variables
        private const string QualityServiceDependencyClassPathV3 = "classpath 'com.applovin.quality:AppLovinQualityServiceGradlePlugin:3.+'";
        private static readonly Regex TokenSafeDkLegacyApplyPlugin = new Regex(".*apply plugin:.+?(?=safedk).*");
        private const string SafeDkLegacyPlugin = "safedk {";
        private const string SafeDkLegacyMavenRepo = "http://download.safedk.com";
        private const string SafeDkLegacyDependencyClassPath = "com.safedk:SafeDKGradlePlugin:";

        /// <summary>
        /// Updates the provided Gradle script to add Quality Service plugin.
        /// </summary>
        /// <param name="applicationGradleBuildFilePath">The gradle file to update.</param>
        protected void AddAppLovinQualityServicePlugin(string applicationGradleBuildFilePath)
        {
            if (!AppLovinSettings.Instance.QualityServiceEnabled) return;

            var sdkKey = AppLovinSettings.Instance.SdkKey;
            if (string.IsNullOrEmpty(sdkKey))
            {
                MaxSdkLogger.UserError("Failed to install AppLovin Quality Service plugin. SDK Key is empty. Please enter the AppLovin SDK Key in the Integration Manager.");
                return;
            }

            // Retrieve the API Key using the SDK Key.
            var qualityServiceData = RetrieveQualityServiceData(sdkKey);
            var apiKey = qualityServiceData.api_key;
            if (string.IsNullOrEmpty(apiKey))
            {
                MaxSdkLogger.UserError("Failed to install AppLovin Quality Service plugin. API Key is empty.");
                return;
            }

            // Generate the updated Gradle file that needs to be written.
            var lines = File.ReadAllLines(applicationGradleBuildFilePath).ToList();
            var sanitizedLines = RemoveLegacySafeDkPlugin(lines);
            var outputLines = GenerateUpdatedBuildFileLines(
                sanitizedLines,
                apiKey,
#if UNITY_2019_3_OR_NEWER
                false // On Unity 2019.3+, the buildscript closure related lines will to be added to the root build.gradle file.
#else
                true
#endif
            );
            // outputLines can be null if we couldn't add the plugin. 
            if (outputLines == null) return;

            try
            {
                File.WriteAllText(applicationGradleBuildFilePath, string.Join("\n", outputLines.ToArray()) + "\n");
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Failed to install AppLovin Quality Service plugin. Gradle file write failed.");
                Console.WriteLine(exception);
            }
        }
#if UNITY_2022_2_OR_NEWER
        /// <summary>
        /// Adds AppLovin Quality Service plugin DSL element to the project's root build.gradle file. 
        /// </summary>
        /// <param name="rootGradleBuildFile">The path to project's root build.gradle file.</param>
        /// <returns><c>true</c> when the plugin was added successfully.</returns>
        protected bool AddPluginToRootGradleBuildFile(string rootGradleBuildFile)
        {
            var lines = File.ReadAllLines(rootGradleBuildFile).ToList();
            var outputLines = new List<string>();
            var pluginAdded = false;
            var insidePluginsClosure = false;
            foreach (var line in lines)
            {
                if (line.Contains(PluginsMatcher))
                {
                    insidePluginsClosure = true;
                }

                if (!pluginAdded && insidePluginsClosure && line.Contains("}"))
                {
                    outputLines.Add(QualityServicePluginRoot);
                    pluginAdded = true;
                    insidePluginsClosure = false;
                }

                outputLines.Add(line);
            }

            if (!pluginAdded)
            {
                MaxSdkLogger.UserError("Failed to add AppLovin Quality Service plugin to root gradle file.");
                return false;
            }

            try
            {
                File.WriteAllText(rootGradleBuildFile, string.Join("\n", outputLines.ToArray()) + "\n");
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Failed to install AppLovin Quality Service plugin. Root Gradle file write failed.");
                Console.WriteLine(exception);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds the AppLovin maven repository to the project's settings.gradle file.
        /// </summary>
        /// <param name="settingsGradleFile">The path to the project's settings.gradle file.</param>
        /// <returns><c>true</c> if the repository was added successfully.</returns>
        protected bool AddAppLovinRepository(string settingsGradleFile)
        {
            var lines = File.ReadLines(settingsGradleFile).ToList();
            var outputLines = new List<string>();
            var mavenRepoAdded = false;
            var pluginManagementClosureDepth = 0;
            var insidePluginManagementClosure = false;
            var pluginManagementMatched = false;
            foreach (var line in lines)
            {
                outputLines.Add(line);

                if (!pluginManagementMatched && line.Contains(PluginManagementMatcher))
                {
                    pluginManagementMatched = true;
                    insidePluginManagementClosure = true;
                }

                if (insidePluginManagementClosure)
                {
                    if (line.Contains("{"))
                    {
                        pluginManagementClosureDepth++;
                    }

                    if (line.Contains("}"))
                    {
                        pluginManagementClosureDepth--;
                    }

                    if (pluginManagementClosureDepth == 0)
                    {
                        insidePluginManagementClosure = false;
                    }
                }

                if (insidePluginManagementClosure)
                {
                    if (!mavenRepoAdded && TokenBuildScriptRepositories.IsMatch(line))
                    {
                        outputLines.Add(GetFormattedBuildScriptLine(QualityServiceMavenRepo));
                        mavenRepoAdded = true;
                    }
                }
            }

            if (!mavenRepoAdded)
            {
                MaxSdkLogger.UserError("Failed to add AppLovin Quality Service plugin maven repo to settings gradle file.");
                return false;
            }

            try
            {
                File.WriteAllText(settingsGradleFile, string.Join("\n", outputLines.ToArray()) + "\n");
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Failed to install AppLovin Quality Service plugin. Setting Gradle file write failed.");
                Console.WriteLine(exception);
                return false;
            }

            return true;
        }
#endif

#if UNITY_2019_3_OR_NEWER
        /// <summary>
        /// Adds the necessary AppLovin Quality Service dependency and maven repo lines to the provided root build.gradle file.
        /// </summary>
        /// <param name="rootGradleBuildFile">The root build.gradle file path</param>
        /// <returns><c>true</c> if the build script lines were applied correctly.</returns>
        protected bool AddQualityServiceBuildScriptLines(string rootGradleBuildFile)
        {
            var lines = File.ReadAllLines(rootGradleBuildFile).ToList();
            var outputLines = GenerateUpdatedBuildFileLines(lines, null, true);

            // outputLines will be null if we couldn't add the build script lines.
            if (outputLines == null) return false;

            try
            {
                File.WriteAllText(rootGradleBuildFile, string.Join("\n", outputLines.ToArray()) + "\n");
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Failed to install AppLovin Quality Service plugin. Root Gradle file write failed.");
                Console.WriteLine(exception);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes the AppLovin Quality Service Plugin or Legacy SafeDK plugin from the given gradle template file if either of them are present.
        /// </summary>
        /// <param name="gradleTemplateFile">The gradle template file from which to remove the plugin from</param>
        protected static void RemoveAppLovinQualityServiceOrSafeDkPlugin(string gradleTemplateFile)
        {
            var lines = File.ReadAllLines(gradleTemplateFile).ToList();
            lines = RemoveLegacySafeDkPlugin(lines);
            lines = RemoveAppLovinQualityServicePlugin(lines);

            try
            {
                File.WriteAllText(gradleTemplateFile, string.Join("\n", lines.ToArray()) + "\n");
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Failed to remove AppLovin Quality Service Plugin from mainTemplate.gradle. Please remove the Quality Service plugin from the mainTemplate.gradle manually.");
                Console.WriteLine(exception);
            }
        }
#endif

        private static AppLovinQualityServiceData RetrieveQualityServiceData(string sdkKey)
        {
            var postJson = string.Format("{{\"sdk_key\" : \"{0}\"}}", sdkKey);
            var bodyRaw = Encoding.UTF8.GetBytes(postJson);
            // Upload handler is automatically disposed when UnityWebRequest is disposed
            var uploadHandler = new UploadHandlerRaw(bodyRaw);
            uploadHandler.contentType = "application/json";

            using (var unityWebRequest = new UnityWebRequest("https://api2.safedk.com/v1/build/cred"))
            {
                unityWebRequest.method = UnityWebRequest.kHttpVerbPOST;
                unityWebRequest.uploadHandler = uploadHandler;
                unityWebRequest.downloadHandler = new DownloadHandlerBuffer();

#if UNITY_2017_2_OR_NEWER
                var operation = unityWebRequest.SendWebRequest();
#else
                var operation = webRequest.Send();
#endif

                // Wait for the download to complete or the request to timeout.
                while (!operation.isDone) { }

#if UNITY_2020_1_OR_NEWER
                if (unityWebRequest.result != UnityWebRequest.Result.Success)
#elif UNITY_2017_2_OR_NEWER
                if (unityWebRequest.isNetworkError || unityWebRequest.isHttpError)
#else
                if (webRequest.isError)
#endif
                {
                    MaxSdkLogger.UserError("Failed to retrieve API Key for SDK Key: " + sdkKey + "with error: " + unityWebRequest.error);
                    return new AppLovinQualityServiceData();
                }

                try
                {
                    return JsonUtility.FromJson<AppLovinQualityServiceData>(unityWebRequest.downloadHandler.text);
                }
                catch (Exception exception)
                {
                    MaxSdkLogger.UserError("Failed to parse API Key." + exception);
                    return new AppLovinQualityServiceData();
                }
            }
        }

        private static List<string> RemoveLegacySafeDkPlugin(List<string> lines)
        {
            return RemovePlugin(lines, SafeDkLegacyPlugin, SafeDkLegacyMavenRepo, SafeDkLegacyDependencyClassPath, TokenSafeDkLegacyApplyPlugin);
        }

        private static List<string> RemoveAppLovinQualityServicePlugin(List<string> lines)
        {
            return RemovePlugin(lines, QualityServicePlugin, QualityServiceMavenRepo, QualityServiceDependencyClassPath, TokenAppLovinPlugin);
        }

        private static List<string> RemovePlugin(List<string> lines, string pluginLine, string mavenRepo, string dependencyClassPath, Regex applyPluginToken)
        {
            var sanitizedLines = new List<string>();
            var legacyRepoRemoved = false;
            var legacyDependencyClassPathRemoved = false;
            var legacyPluginRemoved = false;
            var legacyPluginMatched = false;
            var insideLegacySafeDkClosure = false;
            foreach (var line in lines)
            {
                if (!legacyPluginMatched && line.Contains(pluginLine))
                {
                    legacyPluginMatched = true;
                    insideLegacySafeDkClosure = true;
                }

                if (insideLegacySafeDkClosure && line.Contains("}"))
                {
                    insideLegacySafeDkClosure = false;
                    continue;
                }

                if (insideLegacySafeDkClosure)
                {
                    continue;
                }

                if (!legacyRepoRemoved && line.Contains(mavenRepo))
                {
                    legacyRepoRemoved = true;
                    continue;
                }

                if (!legacyDependencyClassPathRemoved && line.Contains(dependencyClassPath))
                {
                    legacyDependencyClassPathRemoved = true;
                    continue;
                }

                if (!legacyPluginRemoved && applyPluginToken.IsMatch(line))
                {
                    legacyPluginRemoved = true;
                    continue;
                }

                sanitizedLines.Add(line);
            }

            return sanitizedLines;
        }

        private static List<string> GenerateUpdatedBuildFileLines(List<string> lines, string apiKey, bool addBuildScriptLines)
        {
            var addPlugin = !string.IsNullOrEmpty(apiKey);
            // A sample of the template file.
            // ...
            // allprojects {
            //     repositories {**ARTIFACTORYREPOSITORY**
            //         google()
            //         jcenter()
            //         flatDir {
            //             dirs 'libs'
            //         }
            //     }
            // }
            //
            // apply plugin: 'com.android.application'
            //     **APPLY_PLUGINS**
            //
            // dependencies {
            //     implementation fileTree(dir: 'libs', include: ['*.jar'])
            //     **DEPS**}
            // ...
            var outputLines = new List<string>();
            // Check if the plugin exists, if so, update the SDK Key.
            var pluginExists = lines.Any(line => TokenAppLovinPlugin.IsMatch(line));
            if (pluginExists)
            {
                var pluginMatched = false;
                var insideAppLovinClosure = false;
                var updatedApiKey = false;
                var mavenRepoUpdated = false;
                var dependencyClassPathUpdated = false;
                foreach (var line in lines)
                {
                    // Bintray maven repo is no longer being used. Update to s3 maven repo with regex check
                    if (!mavenRepoUpdated && (line.Contains(QualityServiceBintrayMavenRepo) || line.Contains(QualityServiceNoRegexMavenRepo)))
                    {
                        outputLines.Add(GetFormattedBuildScriptLine(QualityServiceMavenRepo));
                        mavenRepoUpdated = true;
                        continue;
                    }

                    // We no longer use version specific dependency class path. Just use + for version to always pull the latest.
                    if (!dependencyClassPathUpdated && line.Contains(QualityServiceDependencyClassPathV3))
                    {
                        outputLines.Add(GetFormattedBuildScriptLine(QualityServiceDependencyClassPath));
                        dependencyClassPathUpdated = true;
                        continue;
                    }

                    if (!pluginMatched && line.Contains(QualityServicePlugin))
                    {
                        insideAppLovinClosure = true;
                        pluginMatched = true;
                    }

                    if (insideAppLovinClosure && line.Contains("}"))
                    {
                        insideAppLovinClosure = false;
                    }

                    // Update the API key.
                    if (insideAppLovinClosure && !updatedApiKey && TokenApiKey.IsMatch(line))
                    {
                        outputLines.Add(string.Format(QualityServiceApiKey, apiKey));
                        updatedApiKey = true;
                    }
                    // Keep adding the line until we find and update the plugin.
                    else
                    {
                        outputLines.Add(line);
                    }
                }
            }
            // Plugin hasn't been added yet, add it.
            else
            {
                var buildScriptClosureDepth = 0;
                var insideBuildScriptClosure = false;
                var buildScriptMatched = false;
                var qualityServiceRepositoryAdded = false;
                var qualityServiceDependencyClassPathAdded = false;
                var qualityServicePluginAdded = false;
                foreach (var line in lines)
                {
                    // Add the line to the output lines.
                    outputLines.Add(line);

                    // Check if we need to add the build script lines and add them.
                    if (addBuildScriptLines)
                    {
                        if (!buildScriptMatched && line.Contains(BuildScriptMatcher))
                        {
                            buildScriptMatched = true;
                            insideBuildScriptClosure = true;
                        }

                        // Match the parenthesis to track if we are still inside the buildscript closure.
                        if (insideBuildScriptClosure)
                        {
                            if (line.Contains("{"))
                            {
                                buildScriptClosureDepth++;
                            }

                            if (line.Contains("}"))
                            {
                                buildScriptClosureDepth--;
                            }

                            if (buildScriptClosureDepth == 0)
                            {
                                insideBuildScriptClosure = false;

                                // There may be multiple buildscript closures and we need to keep looking until we added both the repository and classpath.
                                buildScriptMatched = qualityServiceRepositoryAdded && qualityServiceDependencyClassPathAdded;
                            }
                        }

                        if (insideBuildScriptClosure)
                        {
                            // Add the build script dependency repositories.
                            if (!qualityServiceRepositoryAdded && TokenBuildScriptRepositories.IsMatch(line))
                            {
                                outputLines.Add(GetFormattedBuildScriptLine(QualityServiceMavenRepo));
                                qualityServiceRepositoryAdded = true;
                            }
                            // Add the build script dependencies.
                            else if (!qualityServiceDependencyClassPathAdded && TokenBuildScriptDependencies.IsMatch(line))
                            {
                                outputLines.Add(GetFormattedBuildScriptLine(QualityServiceDependencyClassPath));
                                qualityServiceDependencyClassPathAdded = true;
                            }
                        }
                    }

                    // Check if we need to add the plugin and add it.
                    if (addPlugin)
                    {
                        // Add the plugin.
                        if (!qualityServicePluginAdded && TokenApplicationPlugin.IsMatch(line))
                        {
                            outputLines.Add(QualityServiceApplyPlugin);
                            outputLines.AddRange(GenerateAppLovinPluginClosure(apiKey));
                            qualityServicePluginAdded = true;
                        }
                    }
                }

                if ((addBuildScriptLines && (!qualityServiceRepositoryAdded || !qualityServiceDependencyClassPathAdded)) || (addPlugin && !qualityServicePluginAdded))
                {
                    MaxSdkLogger.UserError("Failed to add AppLovin Quality Service plugin. Quality Service Plugin Added?: " + qualityServicePluginAdded + ", Quality Service Repo added?: " + qualityServiceRepositoryAdded + ", Quality Service dependency added?: " + qualityServiceDependencyClassPathAdded);
                    return null;
                }
            }

            return outputLines;
        }

        private static string GetFormattedBuildScriptLine(string buildScriptLine)
        {
#if UNITY_2022_2_OR_NEWER
            return "        "
#elif UNITY_2019_3_OR_NEWER
            return "            "
#else
            return "        "
#endif
                   + buildScriptLine;
        }

        private static IEnumerable<string> GenerateAppLovinPluginClosure(string apiKey)
        {
            // applovin {
            //     // NOTE: DO NOT CHANGE - this is NOT your AppLovin MAX SDK key - this is a derived key.
            //     apiKey "456...a1b"
            // }
            var linesToInject = new List<string>(5);
            linesToInject.Add("");
            linesToInject.Add("applovin {");
            linesToInject.Add("    // NOTE: DO NOT CHANGE - this is NOT your AppLovin MAX SDK key - this is a derived key.");
            linesToInject.Add(string.Format(QualityServiceApiKey, apiKey));
            linesToInject.Add("}");

            return linesToInject;
        }
    }
}

#endif
