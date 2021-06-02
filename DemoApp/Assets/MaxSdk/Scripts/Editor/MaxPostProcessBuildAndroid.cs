//
//  MaxPostProcessBuildAndroid.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 4/10/20.
//  Copyright © 2020 AppLovin. All rights reserved.
//

#if UNITY_ANDROID && UNITY_2018_2_OR_NEWER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Android;

namespace AppLovinMax.Scripts.Editor
{
    /// <summary>
    /// A post processor used to update the Android project once it is generated.
    /// </summary>
    public class MaxPostProcessBuildAndroid : IPostGenerateGradleAndroidProject
    {
#if UNITY_2019_3_OR_NEWER
        private const string PropertyAndroidX = "android.useAndroidX";
        private const string PropertyJetifier = "android.enableJetifier";
        private const string EnableProperty = "=true";
#endif
        private const string PropertyDexingArtifactTransform = "android.enableDexingArtifactTransform";
        private const string DisableProperty = "=false";
        private const string AppLovinVerboseLoggingOnKey = "applovin.sdk.verbose_logging";

        public void OnPostGenerateGradleAndroidProject(string path)
        {
#if UNITY_2019_3_OR_NEWER
            var gradlePropertiesPath = Path.Combine(path, "../gradle.properties");
#else
            var gradlePropertiesPath = Path.Combine(path, "gradle.properties");
#endif
            var gradlePropertiesUpdated = new List<string>();

            // If the gradle properties file already exists, make sure to add any previous properties.
            if (File.Exists(gradlePropertiesPath))
            {
                var lines = File.ReadAllLines(gradlePropertiesPath);

#if UNITY_2019_3_OR_NEWER
                // Add all properties except AndroidX, Jetifier, and DexingArtifactTransform since they may already exist. We will re-add them below.
                gradlePropertiesUpdated.AddRange(lines.Where(line => !line.Contains(PropertyAndroidX) && !line.Contains(PropertyJetifier) && !line.Contains(PropertyDexingArtifactTransform)));
#else
                // Add all properties except DexingArtifactTransform since it may already exist. We will re-add it below.
                gradlePropertiesUpdated.AddRange(lines.Where(line => !line.Contains(PropertyDexingArtifactTransform)));
#endif
            }

#if UNITY_2019_3_OR_NEWER
            // Enable AndroidX and Jetifier properties 
            gradlePropertiesUpdated.Add(PropertyAndroidX + EnableProperty);
            gradlePropertiesUpdated.Add(PropertyJetifier + EnableProperty);
#endif
            // Disable dexing using artifact transform (it causes issues for ExoPlayer with Gradle plugin 3.5.0+)
            gradlePropertiesUpdated.Add(PropertyDexingArtifactTransform + DisableProperty);

            try
            {
                File.WriteAllText(gradlePropertiesPath, string.Join("\n", gradlePropertiesUpdated.ToArray()) + "\n");
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Failed to enable AndroidX and Jetifier. gradle.properties file write failed.");
                Console.WriteLine(exception);
            }
            
            EnableVerboseLoggingIfNeeded(path);
        }

        public int callbackOrder
        {
            get { return int.MaxValue; }
        }

        private static void EnableVerboseLoggingIfNeeded(string path)
        {
            if (!EditorPrefs.HasKey(MaxSdkLogger.KeyVerboseLoggingEnabled)) return;

            var enabled = EditorPrefs.GetBool(MaxSdkLogger.KeyVerboseLoggingEnabled);
            var manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
            XDocument manifest;
            try
            {
                manifest = XDocument.Load(manifestPath);
            }
#pragma warning disable 0168
            catch (IOException exception)
#pragma warning restore 0168
            {
                MaxSdkLogger.UserWarning("[AppLovin MAX] AndroidManifest.xml is missing.");
                return;
            }

            // Get the `manifest` element.
            var elementManifest = manifest.Element("manifest");
            if (elementManifest == null)
            {
                MaxSdkLogger.UserWarning("[AppLovin MAX] AndroidManifest.xml is invalid.");
                return;
            }

            var elementApplication = elementManifest.Element("application");
            if (elementApplication == null)
            {
                MaxSdkLogger.UserWarning("[AppLovin MAX] AndroidManifest.xml is invalid.");
                return;
            }

            var descendants = elementApplication.Descendants();
            var verboseLoggingMetaData = descendants.FirstOrDefault(descendant => descendant.FirstAttribute != null &&
                                                                                  descendant.FirstAttribute.Name.LocalName.Equals("name") &&
                                                                                  descendant.FirstAttribute.Value.Equals(AppLovinVerboseLoggingOnKey) &&
                                                                                  descendant.LastAttribute != null &&
                                                                                  descendant.LastAttribute.Name.LocalName.Equals("value"));
            
            // check if applovin.sdk.verbose_logging meta data exists.
            if (verboseLoggingMetaData != null)
            {
                if (enabled)
                {
                    // update applovin.sdk.verbose_logging meta data value.
                    verboseLoggingMetaData.LastAttribute.Value = enabled.ToString();
                }
                else
                {
                    // remove applovin.sdk.verbose_logging meta data.
                    verboseLoggingMetaData.Remove();
                }
            }
            else
            {
                if (enabled)
                {
                    // add applovin.sdk.verbose_logging meta data if it does not exist.
                    var metaData = new XElement("meta-data");
                    XNamespace androidNamespace = "http://schemas.android.com/apk/res/android";
                    metaData.Add(new XAttribute(androidNamespace + "name", AppLovinVerboseLoggingOnKey));
                    metaData.Add(new XAttribute(androidNamespace + "value", enabled.ToString()));
                    elementApplication.Add(metaData);
                }
            }

            // Save the updated manifest file.
            manifest.Save(manifestPath);
        }
    }
}

#endif
