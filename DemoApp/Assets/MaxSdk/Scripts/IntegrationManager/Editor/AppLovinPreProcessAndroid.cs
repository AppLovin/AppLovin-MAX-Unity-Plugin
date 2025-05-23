//
//  AppLovinBuildPreProcessor.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 8/27/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

#if UNITY_ANDROID

using System.Xml.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    /// <summary>
    /// Adds the AppLovin Quality Service plugin to the gradle template file. See <see cref="AppLovinProcessGradleBuildFile"/> for more details.
    /// </summary>
    public class AppLovinPreProcessAndroid : AppLovinProcessGradleBuildFile, IPreprocessBuildWithReport
    {
        private const string ElementNameAndroidPackages = "androidPackages";
        private const string ElementNameAndroidPackage = "androidPackage";
        private const string AttributeNameSpec = "spec";
        private const string UmpDependencyPackage = "com.google.android.ump:user-messaging-platform:";
        private const string UmpDependencyVersion = "2.1.0";

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
            if (AppLovinInternalSettings.Instance.ConsentFlowEnabled)
            {
                var umpPackage = new XElement(ElementNameAndroidPackage,
                    new XAttribute(AttributeNameSpec, UmpDependencyPackage + UmpDependencyVersion)); 
                var success = AddOrUpdateAndroidDependency(UmpDependencyPackage, umpPackage );
                if (!success)
                {
                    MaxSdkLogger.UserWarning("Google CMP will not function. Unable to add user-messaging-platform dependency.");
                }
            }
            else
            {
                RemoveAndroidDependency(UmpDependencyPackage);
            }
        }

        /// <summary>
        /// Adds or updates an Android dependency in the AppLovin Dependencies.xml file.
        /// </summary>
        /// <param name="package">The package that we are trying to update</param>
        /// <param name="newDependency">The new dependency to add if it doesn't exist</param>
        /// <returns>Returns true if the file was successfully edited</returns>
        private static bool AddOrUpdateAndroidDependency(string package, XElement newDependency)
        {
            var dependenciesFilePath = AppLovinDependenciesFilePath;
            var dependenciesDocument = GetAppLovinDependenciesFile(dependenciesFilePath, AppLovinIntegrationManager.IsPluginInPackageManager);
            if (dependenciesDocument == null) return false;

            AddOrUpdateDependency(dependenciesDocument,
                ElementNameAndroidPackages,
                ElementNameAndroidPackage,
                AttributeNameSpec,
                package,
                newDependency);
            return SaveDependenciesFile(dependenciesDocument, dependenciesFilePath);
        }

        /// <summary>
        /// Removed an android dependency from the AppLovin Dependencies.xml file.
        /// </summary>
        /// <param name="package">The package to remove</param>
        private static void RemoveAndroidDependency(string package)
        {
            var dependenciesFilePath = AppLovinDependenciesFilePath;
            var dependenciesDocument = GetAppLovinDependenciesFile(dependenciesFilePath);
            if (dependenciesDocument == null) return;

            var removed = RemoveDependency(dependenciesDocument,
                ElementNameAndroidPackages,
                ElementNameAndroidPackage,
                AttributeNameSpec,
                package);
            
            if (!removed) return;

            SaveDependenciesFile(dependenciesDocument, dependenciesFilePath);
        }

        public int callbackOrder
        {
            get { return CallbackOrder; }
        }
    }
}

#endif
