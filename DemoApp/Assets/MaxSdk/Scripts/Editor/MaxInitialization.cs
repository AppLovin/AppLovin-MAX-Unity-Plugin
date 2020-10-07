//
//  MaxInitialization.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Thomas So on 5/24/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class MaxInitialize
{
    private const string MigrationProgressBarTitle = "AppLovin MAX Migration";
    private const string AndroidChangelog = "ANDROID_CHANGELOG.md";
    private const string IosChangelog = "IOS_CHANGELOG.md";

    private static readonly List<string> Networks = new List<string>
    {
        "AdColony",
        "Amazon",
        "ByteDance",
        "Chartboost",
        "Facebook",
        "Fyber",
        "Google",
        "InMobi",
        "IronSource",
        "Maio",
        "Mintegral",
        "MyTarget",
        "MoPub",
        "Nend",
        "Ogury",
        "Smaato",
        "Tapjoy",
        "TencentGDT",
        "UnityAds",
        "VerizonAds",
        "Vungle",
        "Yandex"
    };

    private static readonly List<string> ObsoleteNetworks = new List<string>
    {
        "Mintegral",
        "VoodooAds"
    };

    static MaxInitialize()
    {
        AppLovinAutoUpdater.Update();

#if UNITY_IOS
        // Check that the publisher is targeting iOS 9.0+
        if (!PlayerSettings.iOS.targetOSVersionString.StartsWith("9.") && !PlayerSettings.iOS.targetOSVersionString.StartsWith("1"))
        {
            Debug.LogError("Detected iOS project version less than iOS 9 - The AppLovin MAX SDK WILL NOT WORK ON < iOS9!!!");
        }
#endif

        var legacyDir = Path.Combine("Assets", "MaxSdk/Plugins");
        var changesMade = false;

        // Check for if directory from older versions of the AppLovin MAX Unity Plugin exists
        if (CheckExistence(legacyDir))
        {
            Debug.Log("Legacy directories from AppLovin MAX Unity Plugin found. Running migration...");

            var androidDir = Path.Combine("Assets", "MaxSdk/Plugins/Android/AppLovin");
            if (CheckExistence(androidDir))
            {
                Debug.Log("Deleting " + androidDir + "...");
                EditorUtility.DisplayProgressBar(MigrationProgressBarTitle, "Deleting " + androidDir + "...", 0.33f);
                FileUtil.DeleteFileOrDirectory(androidDir);
                changesMade = true;
            }

            var iOSDir = Path.Combine("Assets", "MaxSdk/Plugins/iOS/AppLovin");
            if (CheckExistence(iOSDir))
            {
                Debug.Log("Deleting " + iOSDir + "...");
                EditorUtility.DisplayProgressBar(MigrationProgressBarTitle, "Deleting " + iOSDir + "...", 0.66f);
                FileUtil.DeleteFileOrDirectory(iOSDir);
                changesMade = true;
            }
        }

        // Check if we have legacy adapter directories
        foreach (var network in Networks)
        {
            var newDir = Path.Combine("Assets", "MaxSdk/Mediation/" + network);

            // If new directory exists
            if (CheckExistence(newDir))
            {
                var legacyAndroidDir = Path.Combine("Assets", "MaxSdk/Plugins/Android/" + network);
                var legacyIOSDir = Path.Combine("Assets", "MaxSdk/Plugins/iOS/" + network);

                // Delete legacy iOS directory if exists
                if (CheckExistence(legacyIOSDir))
                {
                    Debug.Log("Deleting " + legacyIOSDir + "...");
                    FileUtil.DeleteFileOrDirectory(legacyIOSDir);
                    changesMade = true;
                }

                // Delete legacy Android director(ies) if exists
                if (CheckExistence(legacyAndroidDir))
                {
                    Debug.Log("Deleting " + legacyAndroidDir + "...");
                    FileUtil.DeleteFileOrDirectory(legacyAndroidDir);

                    // Check if it contains shared dependencies
                    var deletedSharedDependencies = false;
                    if (network.Equals("Facebook"))
                    {
                        deletedSharedDependencies = true;
                        FileUtil.DeleteFileOrDirectory(Path.Combine("Assets", "MaxSdk/Plugins/Android/Shared Dependencies/exoplayer-core.aar"));
                        FileUtil.DeleteFileOrDirectory(Path.Combine("Assets", "MaxSdk/Plugins/Android/Shared Dependencies/exoplayer-dash.aar"));
                        FileUtil.DeleteFileOrDirectory(Path.Combine("Assets", "MaxSdk/Plugins/Android/Shared Dependencies/recyclerview-v7.aar"));
                    }
                    else if (network.Equals("Fyber"))
                    {
                        deletedSharedDependencies = true;
                        FileUtil.DeleteFileOrDirectory(Path.Combine("Assets", "MaxSdk/Plugins/Android/Shared Dependencies/gson.jar"));
                    }
                    else if (network.Equals("InMobi"))
                    {
                        deletedSharedDependencies = true;
                        FileUtil.DeleteFileOrDirectory(Path.Combine("Assets", "MaxSdk/Plugins/Android/Shared Dependencies/picasso.jar"));
                        FileUtil.DeleteFileOrDirectory(Path.Combine("Assets", "MaxSdk/Plugins/Android/Shared Dependencies/recyclerview-v7.aar"));
                    }
                    else if (network.Equals("Vungle"))
                    {
                        deletedSharedDependencies = true;
                        FileUtil.DeleteFileOrDirectory(Path.Combine("Assets", "MaxSdk/Plugins/Android/Shared Dependencies/common.jar"));
                        FileUtil.DeleteFileOrDirectory(Path.Combine("Assets", "MaxSdk/Plugins/Android/Shared Dependencies/converter-gson.jar"));
                        FileUtil.DeleteFileOrDirectory(Path.Combine("Assets", "MaxSdk/Plugins/Android/Shared Dependencies/fetch.jar"));
                        FileUtil.DeleteFileOrDirectory(Path.Combine("Assets", "MaxSdk/Plugins/Android/Shared Dependencies/gson.jar"));
                        FileUtil.DeleteFileOrDirectory(Path.Combine("Assets", "MaxSdk/Plugins/Android/Shared Dependencies/okhttp.jar"));
                        FileUtil.DeleteFileOrDirectory(Path.Combine("Assets", "MaxSdk/Plugins/Android/Shared Dependencies/okio.jar"));
                        FileUtil.DeleteFileOrDirectory(Path.Combine("Assets", "MaxSdk/Plugins/Android/Shared Dependencies/retrofit.jar"));
                    }

                    if (deletedSharedDependencies)
                    {
                        Debug.Log("Deleting " + network + " shared dependencies...");
                    }

                    changesMade = true;
                }

                var androidChangelogFile = Path.Combine(newDir, AndroidChangelog);
                if (CheckExistence(androidChangelogFile))
                {
                    FileUtil.DeleteFileOrDirectory(androidChangelogFile);
                    changesMade = true;
                }

                var iosChangelogFile = Path.Combine(newDir, IosChangelog);
                if (CheckExistence(iosChangelogFile))
                {
                    FileUtil.DeleteFileOrDirectory(iosChangelogFile);
                    changesMade = true;
                }
            }
        }

        // Check if any obsolete networks are installed
        foreach (var obsoleteNetwork in ObsoleteNetworks)
        {
            var networkDir = Path.Combine("Assets", "MaxSdk/Mediation/" + obsoleteNetwork);
            if (CheckExistence(networkDir))
            {
                Debug.Log("Deleting obsolete network " + obsoleteNetwork + " from path " + networkDir + "...");
                FileUtil.DeleteFileOrDirectory(networkDir);
                changesMade = true;
            }
        }

        // Refresh UI
        if (changesMade)
        {
            AssetDatabase.Refresh();
            Debug.Log("AppLovin MAX Migration completed");
        }

        EditorUtility.ClearProgressBar();
    }

    private static bool CheckExistence(string location)
    {
        return File.Exists(location) ||
               Directory.Exists(location) ||
               (location.EndsWith("/*") && Directory.Exists(Path.GetDirectoryName(location)));
    }
}
