//
//  AppLovinBuildPostProcessor.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 8/29/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

#if UNITY_2018_2_OR_NEWER && UNITY_ANDROID

using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    /// <summary>
    /// Adds Quality Service plugin to the Gradle project once the project has been exported. See <see cref="AppLovinProcessGradleBuildFile"/> for more details.
    /// </summary>
    public class AppLovinPostProcessGradleProject : AppLovinProcessGradleBuildFile, IPostGenerateGradleAndroidProject
    {
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (!AppLovinSettings.Instance.QualityServiceEnabled) return;

#if UNITY_2019_3_OR_NEWER
            // On Unity 2019.3+, the path returned is the path to the unityLibrary's module.
            // The AppLovin Quality Service buildscript closure related lines need to be added to the root build.gradle file.
            var rootGradleBuildFilePath = Path.Combine(path, "../build.gradle");
#if UNITY_2022_2_OR_NEWER
            if (!AddPluginToRootGradleBuildFile(rootGradleBuildFilePath)) return;
            
            var rootSettingsGradleFilePath = Path.Combine(path, "../settings.gradle");
            if (!AddAppLovinRepository(rootSettingsGradleFilePath)) return;
#else
            var buildScriptChangesAdded = AddQualityServiceBuildScriptLines(rootGradleBuildFilePath);
            if (!buildScriptChangesAdded) return;
#endif

            // The plugin needs to be added to the application module (named launcher)
            var applicationGradleBuildFilePath = Path.Combine(path, "../launcher/build.gradle");
#else
            // If Gradle template is enabled, we would have already updated the plugin.
            if (AppLovinIntegrationManager.GradleTemplateEnabled) return;

            var applicationGradleBuildFilePath = Path.Combine(path, "build.gradle");
#endif

            if (!File.Exists(applicationGradleBuildFilePath))
            {
                MaxSdkLogger.UserWarning("Couldn't find build.gradle file. Failed to add AppLovin Quality Service plugin to the gradle project.");
                return;
            }

            AddAppLovinQualityServicePlugin(applicationGradleBuildFilePath);
        }

        public int callbackOrder
        {
            get { return int.MaxValue; }
        }
    }
}

#endif
