//
//  AppLovinBuildPreProcessiOS.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Jonathan Liu on 10/17/2023.
//  Copyright © 2023 AppLovin. All rights reserved.
//

#if UNITY_IOS

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

        private const string UmpLegacyDependencyLine = "<iosPod name=\"GoogleUserMessagingPlatform\" version=\"2.1.0\" />";
        private const string UmpDependencyLine = "<iosPod name=\"GoogleUserMessagingPlatform\" version=\"~&gt; 2.1\" />";
        private const string IosPodsContainerElementString = "iosPods";

        private static void AddGoogleCmpDependencyIfNeeded()
        {
            // Remove the legacy fixed UMP version if it exists, we'll add the dependency with a dynamic version below.
            TryRemoveStringFromDependencyFile(UmpLegacyDependencyLine, IosPodsContainerElementString);

            if (AppLovinInternalSettings.Instance.ConsentFlowEnabled)
            {
                CreateAppLovinDependenciesFileIfNeeded();
                TryAddStringToDependencyFile(UmpDependencyLine, IosPodsContainerElementString);
            }
            else
            {
                TryRemoveStringFromDependencyFile(UmpDependencyLine, IosPodsContainerElementString);
            }
        }

        public int callbackOrder
        {
            get { return int.MaxValue; }
        }
    }
}

#endif
