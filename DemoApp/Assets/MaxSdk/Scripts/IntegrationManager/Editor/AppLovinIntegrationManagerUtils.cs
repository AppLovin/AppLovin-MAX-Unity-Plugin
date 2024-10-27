using System;
using System.Linq;
using UnityEngine;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    public static class AppLovinIntegrationManagerUtils
    {
        /// <summary>
        /// Compares AppLovin MAX Unity mediation adapter plugin versions. Returns <see cref="MaxSdkUtils.VersionComparisonResult.Lesser"/>, <see cref="MaxSdkUtils.VersionComparisonResult.Equal"/>,
        /// or <see cref="MaxSdkUtils.VersionComparisonResult.Greater"/> as the first version is less than, equal to, or greater than the second.
        ///
        /// If a version for a specific platform is only present in one of the provided versions, the one that contains it is considered newer.
        /// </summary>
        /// <param name="versionA">The first version to be compared.</param>
        /// <param name="versionB">The second version to be compared.</param>
        /// <returns>
        /// <see cref="MaxSdkUtils.VersionComparisonResult.Lesser"/> if versionA is less than versionB.
        /// <see cref="MaxSdkUtils.VersionComparisonResult.Equal"/> if versionA and versionB are equal.
        /// <see cref="MaxSdkUtils.VersionComparisonResult.Greater"/> if versionA is greater than versionB.
        /// </returns>
        internal static MaxSdkUtils.VersionComparisonResult CompareUnityMediationVersions(string versionA, string versionB)
        {
            if (versionA.Equals(versionB)) return MaxSdkUtils.VersionComparisonResult.Equal;

            // Unity version would be of format:      android_w.x.y.z_ios_a.b.c.d
            // For Android only versions it would be: android_w.x.y.z
            // For iOS only version it would be:      ios_a.b.c.d

            // After splitting into their respective components, the versions would be at the odd indices.
            var versionAComponents = versionA.Split('_').ToList();
            var versionBComponents = versionB.Split('_').ToList();

            var androidComparison = MaxSdkUtils.VersionComparisonResult.Equal;
            if (versionA.Contains("android") && versionB.Contains("android"))
            {
                var androidVersionA = versionAComponents[1];
                var androidVersionB = versionBComponents[1];
                androidComparison = MaxSdkUtils.CompareVersions(androidVersionA, androidVersionB);

                // Remove the Android version component so that iOS versions can be processed.
                versionAComponents.RemoveRange(0, 2);
                versionBComponents.RemoveRange(0, 2);
            }
            else if (versionA.Contains("android"))
            {
                androidComparison = MaxSdkUtils.VersionComparisonResult.Greater;

                // Remove the Android version component so that iOS versions can be processed.
                versionAComponents.RemoveRange(0, 2);
            }
            else if (versionB.Contains("android"))
            {
                androidComparison = MaxSdkUtils.VersionComparisonResult.Lesser;

                // Remove the Android version component so that iOS version can be processed.
                versionBComponents.RemoveRange(0, 2);
            }

            var iosComparison = MaxSdkUtils.VersionComparisonResult.Equal;
            if (versionA.Contains("ios") && versionB.Contains("ios"))
            {
                var iosVersionA = versionAComponents[1];
                var iosVersionB = versionBComponents[1];
                iosComparison = MaxSdkUtils.CompareVersions(iosVersionA, iosVersionB);
            }
            else if (versionA.Contains("ios"))
            {
                iosComparison = MaxSdkUtils.VersionComparisonResult.Greater;
            }
            else if (versionB.Contains("ios"))
            {
                iosComparison = MaxSdkUtils.VersionComparisonResult.Lesser;
            }

            // If either one of the Android or iOS version is greater, the entire version should be greater.
            return (androidComparison == MaxSdkUtils.VersionComparisonResult.Greater || iosComparison == MaxSdkUtils.VersionComparisonResult.Greater) ? MaxSdkUtils.VersionComparisonResult.Greater : MaxSdkUtils.VersionComparisonResult.Lesser;
        }
    }
}
