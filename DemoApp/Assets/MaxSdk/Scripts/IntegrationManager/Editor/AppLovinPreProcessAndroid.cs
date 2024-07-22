//
//  AppLovinBuildPreProcessor.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 8/27/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

#if UNITY_ANDROID

using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    /// <summary>
    /// Adds the AppLovin Quality Service plugin to the gradle template file. See <see cref="AppLovinProcessGradleBuildFile"/> for more details.
    /// </summary>
    public class AppLovinPreProcessAndroid : AppLovinProcessGradleBuildFile, IPreprocessBuildWithReport
    {
        private const string UmpLegacyDependencyLine = "<androidPackage spec=\"com.google.android.ump:user-messaging-platform:2.1.0\" />";
        private const string UmpDependencyLine = "<androidPackage spec=\"com.google.android.ump:user-messaging-platform:2.+\" />";
        private const string AndroidPackagesContainerElementString = "androidPackages";

        public void OnPreprocessBuild(BuildReport report)
        {
            PreprocessAppLovinQualityServicePlugin();
            AddGoogleCmpDependencyIfNeeded();
        }

        private static void PreprocessAppLovinQualityServicePlugin()
        {
            // We can only process gradle template file here. If it is not available, we will try again in post build on Unity IDEs newer than 2018_2 (see AppLovinPostProcessGradleProject).
            if (!AppLovinIntegrationManager.GradleTemplateEnabled) return;

#if UNITY_2019_3_OR_NEWER
            // The publisher could be migrating from older Unity versions to 2019_3 or newer.
            // If so, we should delete the plugin from the template. The plugin will be added to the project's application module in the post processing script (AppLovinPostProcessGradleProject).
            RemoveAppLovinQualityServiceOrSafeDkPlugin(AppLovinIntegrationManager.GradleTemplatePath);
#else
            AddAppLovinQualityServicePlugin(AppLovinIntegrationManager.GradleTemplatePath);
#endif
        }

        private static void AddGoogleCmpDependencyIfNeeded()
        {
            // Remove the legacy fixed UMP version if it exists, we'll add the dependency with a dynamic version below.
            TryRemoveStringFromDependencyFile(UmpLegacyDependencyLine, AndroidPackagesContainerElementString);

            if (AppLovinInternalSettings.Instance.ConsentFlowEnabled)
            {
                TryAddStringToDependencyFile(UmpDependencyLine, AndroidPackagesContainerElementString);
            }
            else
            {
                TryRemoveStringFromDependencyFile(UmpDependencyLine, AndroidPackagesContainerElementString);
            }
        }

        public int callbackOrder
        {
            get { return int.MaxValue; }
        }
    }
}

#endif
