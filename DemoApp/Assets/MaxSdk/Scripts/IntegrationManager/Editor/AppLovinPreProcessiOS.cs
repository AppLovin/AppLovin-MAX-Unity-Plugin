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

        private static void AddGoogleCmpDependencyIfNeeded()
        {
            const string umpDependencyLine = "<iosPod name=\"GoogleUserMessagingPlatform\" version=\"2.1.0\" />";
            const string containerElementString = "iosPods";

            if (AppLovinInternalSettings.Instance.ConsentFlowEnabled)
            {
                TryAddStringToDependencyFile(umpDependencyLine, containerElementString);
            }
            else
            {
                TryRemoveStringFromDependencyFile(umpDependencyLine, containerElementString);
            }
        }

        public int callbackOrder
        {
            get { return int.MaxValue; }
        }
    }
}

#endif
