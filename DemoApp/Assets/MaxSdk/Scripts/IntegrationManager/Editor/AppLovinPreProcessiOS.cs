//
//  AppLovinBuildPreProcessiOS.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Jonathan Liu on 10/17/2023.
//  Copyright © 2023 AppLovin. All rights reserved.
//

#if UNITY_IOS

using System.Xml.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    public class AppLovinPreProcessiOS : AppLovinPreProcess, IPreprocessBuildWithReport
    {
        public void OnPreprocessBuild(BuildReport report)
        {
            AddGoogleCmpDependencyIfNeeded();
        }

        private const string ElementNameIosPods = "iosPods";
        private const string ElementNameIosPod = "iosPod";
        private const string AttributeNameName = "name";
        private const string AttributeNameVersion = "version";
        private const string UmpDependencyPod = "GoogleUserMessagingPlatform";
        private const string UmpDependencyVersion = "~> 2.1";

        private static void AddGoogleCmpDependencyIfNeeded()
        {
            if (AppLovinInternalSettings.Instance.ConsentFlowEnabled)
            {
                var umpDependency = new XElement(ElementNameIosPod,
                    new XAttribute(AttributeNameName, UmpDependencyPod),
                    new XAttribute(AttributeNameVersion, UmpDependencyVersion));
                var success = AddOrUpdateIosDependency(UmpDependencyPod, umpDependency);
                if (!success)
                {
                    MaxSdkLogger.UserWarning("Google CMP will not function. Unable to add GoogleUserMessagingPlatform dependency.");
                }
            }
            else
            {
                RemoveIosDependency(UmpDependencyPod);
            }
        }

        /// <summary>
        /// Adds or updates an iOS pod in the AppLovin Dependencies.xml file.
        /// </summary>
        /// <param name="pod">The pod that we are trying to update</param>
        /// <param name="newDependency">The new dependency to add if it doesn't exist</param>
        /// <returns>Returns true if the file was successfully edited</returns>
        private static bool AddOrUpdateIosDependency(string pod, XElement newDependency)
        {
            var dependenciesFilePath = AppLovinDependenciesFilePath;
            var dependenciesDocument = GetAppLovinDependenciesFile(dependenciesFilePath, AppLovinIntegrationManager.IsPluginInPackageManager);
            if (dependenciesDocument == null) return false;

            AddOrUpdateDependency(dependenciesDocument,
                ElementNameIosPods,
                ElementNameIosPod,
                AttributeNameName,
                pod,
                newDependency);
            return SaveDependenciesFile(dependenciesDocument, dependenciesFilePath);
        }

        /// <summary>
        /// Removed an iOS pod from the AppLovin Dependencies.xml file.
        /// </summary>
        /// <param name="pod">The pod to remove</param>
        private static void RemoveIosDependency(string pod)
        {
            var dependenciesFilePath = AppLovinDependenciesFilePath;
            var dependenciesDocument = GetAppLovinDependenciesFile(dependenciesFilePath);
            if (dependenciesDocument == null) return;

            var removed = RemoveDependency(dependenciesDocument,
                ElementNameIosPods,
                ElementNameIosPod,
                AttributeNameName,
                pod);

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
