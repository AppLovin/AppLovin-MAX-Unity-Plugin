//
//  AppLovinBuildPreProcessor.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 8/27/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

#if UNITY_ANDROID

using UnityEditor;
using UnityEditor.Build;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

/// <summary>
/// Adds the AppLovin Quality Service plugin to the gradle template file. See <see cref="AppLovinProcessGradleBuildFile"/> for more details.
/// </summary>
public class AppLovinPreProcessAndroid : AppLovinProcessGradleBuildFile,
#if UNITY_2018_1_OR_NEWER
    IPreprocessBuildWithReport
#else
    IPreprocessBuild
#endif
{
#if UNITY_2018_1_OR_NEWER
    public void OnPreprocessBuild(BuildReport report)
#else
    public void OnPreprocessBuild(BuildTarget target, string path)
#endif
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

    public int callbackOrder
    {
        get { return int.MaxValue; }
    }
}

#endif
