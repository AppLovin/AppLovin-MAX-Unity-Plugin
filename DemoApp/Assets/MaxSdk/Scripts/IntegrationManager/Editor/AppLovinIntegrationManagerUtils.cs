using System;
using System.Linq;
using UnityEngine;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    public static class AppLovinIntegrationManagerUtils
    {
        /// <summary>
        /// Compares AppLovin MAX Unity mediation adapter plugin versions. Returns <see cref="Versions.VersionComparisonResult.Lesser"/>, <see cref="Versions.VersionComparisonResult.Equal"/>,
        /// or <see cref="Versions.VersionComparisonResult.Greater"/> as the first version is less than, equal to, or greater than the second.
        ///
        /// If a version for a specific platform is only present in one of the provided versions, the one that contains it is considered newer.
        /// </summary>
        /// <param name="versionA">The first version to be compared.</param>
        /// <param name="versionB">The second version to be compared.</param>
        /// <returns>
        /// <see cref="Versions.VersionComparisonResult.Lesser"/> if versionA is less than versionB.
        /// <see cref="Versions.VersionComparisonResult.Equal"/> if versionA and versionB are equal.
        /// <see cref="Versions.VersionComparisonResult.Greater"/> if versionA is greater than versionB.
        /// </returns>
        internal static Versions.VersionComparisonResult CompareUnityMediationVersions(string versionA, string versionB)
        {
            if (versionA.Equals(versionB)) return Versions.VersionComparisonResult.Equal;

            // Unity version would be of format:      android_w.x.y.z_ios_a.b.c.d
            // For Android only versions it would be: android_w.x.y.z
            // For iOS only version it would be:      ios_a.b.c.d

            // After splitting into their respective components, the versions would be at the odd indices.
            var versionAComponents = versionA.Split('_').ToList();
            var versionBComponents = versionB.Split('_').ToList();

            var androidComparison = Versions.VersionComparisonResult.Equal;
            if (versionA.Contains("android") && versionB.Contains("android"))
            {
                var androidVersionA = versionAComponents[1];
                var androidVersionB = versionBComponents[1];
                androidComparison = CompareVersions(androidVersionA, androidVersionB);

                // Remove the Android version component so that iOS versions can be processed.
                versionAComponents.RemoveRange(0, 2);
                versionBComponents.RemoveRange(0, 2);
            }
            else if (versionA.Contains("android"))
            {
                androidComparison = Versions.VersionComparisonResult.Greater;

                // Remove the Android version component so that iOS versions can be processed.
                versionAComponents.RemoveRange(0, 2);
            }
            else if (versionB.Contains("android"))
            {
                androidComparison = Versions.VersionComparisonResult.Lesser;

                // Remove the Android version component so that iOS version can be processed.
                versionBComponents.RemoveRange(0, 2);
            }

            var iosComparison = Versions.VersionComparisonResult.Equal;
            if (versionA.Contains("ios") && versionB.Contains("ios"))
            {
                var iosVersionA = versionAComponents[1];
                var iosVersionB = versionBComponents[1];
                iosComparison = CompareVersions(iosVersionA, iosVersionB);
            }
            else if (versionA.Contains("ios"))
            {
                iosComparison = Versions.VersionComparisonResult.Greater;
            }
            else if (versionB.Contains("ios"))
            {
                iosComparison = Versions.VersionComparisonResult.Lesser;
            }

            // If either one of the Android or iOS version is greater, the entire version should be greater.
            return (androidComparison == Versions.VersionComparisonResult.Greater || iosComparison == Versions.VersionComparisonResult.Greater) ? Versions.VersionComparisonResult.Greater : Versions.VersionComparisonResult.Lesser;
        }

        /// <summary>
        /// Compares its two arguments for order.  Returns <see cref="Versions.VersionComparisonResult.Lesser"/>, <see cref="Versions.VersionComparisonResult.Equal"/>,
        /// or <see cref="Versions.VersionComparisonResult.Greater"/> as the first version is less than, equal to, or greater than the second.
        /// </summary>
        /// <param name="versionA">The first version to be compared.</param>
        /// <param name="versionB">The second version to be compared.</param>
        /// <returns>
        /// <see cref="Versions.VersionComparisonResult.Lesser"/> if versionA is less than versionB.
        /// <see cref="Versions.VersionComparisonResult.Equal"/> if versionA and versionB are equal.
        /// <see cref="Versions.VersionComparisonResult.Greater"/> if versionA is greater than versionB.
        /// </returns>
        internal static Versions.VersionComparisonResult CompareVersions(string versionA, string versionB)
        {
            if (versionA.Equals(versionB)) return Versions.VersionComparisonResult.Equal;

            // Check if either of the versions are beta versions. Beta versions could be of format x.y.z-beta or x.y.z-betaX.
            // Split the version string into beta component and the underlying version.
            int piece;
            var isVersionABeta = versionA.Contains("-beta");
            var versionABetaNumber = 0;
            if (isVersionABeta)
            {
                var components = versionA.Split(new[] {"-beta"}, StringSplitOptions.None);
                versionA = components[0];
                versionABetaNumber = int.TryParse(components[1], out piece) ? piece : 0;
            }

            var isVersionBBeta = versionB.Contains("-beta");
            var versionBBetaNumber = 0;
            if (isVersionBBeta)
            {
                var components = versionB.Split(new[] {"-beta"}, StringSplitOptions.None);
                versionB = components[0];
                versionBBetaNumber = int.TryParse(components[1], out piece) ? piece : 0;
            }

            // Now that we have separated the beta component, check if the underlying versions are the same.
            if (versionA.Equals(versionB))
            {
                // The versions are the same, compare the beta components.
                if (isVersionABeta && isVersionBBeta)
                {
                    if (versionABetaNumber < versionBBetaNumber) return Versions.VersionComparisonResult.Lesser;

                    if (versionABetaNumber > versionBBetaNumber) return Versions.VersionComparisonResult.Greater;
                }
                // Only VersionA is beta, so A is older.
                else if (isVersionABeta)
                {
                    return Versions.VersionComparisonResult.Lesser;
                }
                // Only VersionB is beta, A is newer.
                else
                {
                    return Versions.VersionComparisonResult.Greater;
                }
            }

            // Compare the non beta component of the version string.
            var versionAComponents = versionA.Split('.').Select(version => int.TryParse(version, out piece) ? piece : 0).ToArray();
            var versionBComponents = versionB.Split('.').Select(version => int.TryParse(version, out piece) ? piece : 0).ToArray();
            var length = Mathf.Max(versionAComponents.Length, versionBComponents.Length);
            for (var i = 0; i < length; i++)
            {
                var aComponent = i < versionAComponents.Length ? versionAComponents[i] : 0;
                var bComponent = i < versionBComponents.Length ? versionBComponents[i] : 0;

                if (aComponent < bComponent) return Versions.VersionComparisonResult.Lesser;

                if (aComponent > bComponent) return Versions.VersionComparisonResult.Greater;
            }

            return Versions.VersionComparisonResult.Equal;
        }
    }
}
