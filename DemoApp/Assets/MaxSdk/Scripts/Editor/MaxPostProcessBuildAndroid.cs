//
//  MaxPostProcessBuildAndroid.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 4/10/20.
//  Copyright © 2020 AppLovin. All rights reserved.
//

#if UNITY_ANDROID && UNITY_2019_3_OR_NEWER

using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace AppLovinMax
{
    /// <summary>
    /// A post processor used to update the Android project once it is generated.
    /// </summary>
    public class MaxPostProcessBuildAndroid : IPostGenerateGradleAndroidProject
    {
        private const string PropertyAndroidX = "android.useAndroidX";
        private const string PropertyJetifier = "android.enableJetifier";
        private const string EnableProperty = "=true";

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var gradlePropertiesPath = Path.Combine(path, "../gradle.properties");

            var gradlePropertiesUpdated = new List<string>();

            // If the gradle properties file already exists, make sure to add any previous properties.
            if (File.Exists(gradlePropertiesPath))
            {
                var lines = File.ReadAllLines(gradlePropertiesPath);

                // Add all properties except AndroidX and Jetifier, since they could be disabled. We will add them below with those properties enabled.
                gradlePropertiesUpdated.AddRange(lines.Where(line => !line.Contains(PropertyAndroidX) && !line.Contains(PropertyJetifier)));
            }

            // Enable AndroidX and Jetifier properties 
            gradlePropertiesUpdated.Add(PropertyAndroidX + EnableProperty);
            gradlePropertiesUpdated.Add(PropertyJetifier + EnableProperty);

            try
            {
                File.WriteAllText(gradlePropertiesPath, string.Join("\n", gradlePropertiesUpdated.ToArray()) + "\n");
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Failed to enable AndroidX and Jetifier. gradle.properties file write failed.");
                Console.WriteLine(exception);
            }
        }

        public int callbackOrder
        {
            get { return int.MaxValue; }
        }
    }
}

#endif
