using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MaxSdkUtils
{
    private static readonly char _DictKeyValueSeparator = (char) 28;
    private static readonly char _DictKeyValuePairSeparator = (char) 29;
    private static readonly string _ArrayItemSeparator = ",\n";

    /// <summary>
    /// An Enum to be used when comparing two versions.
    ///
    /// If:
    ///     A &lt; B    return <see cref="Lesser"/>
    ///     A == B      return <see cref="Equal"/>
    ///     A &gt; B    return <see cref="Greater"/>
    /// </summary>
    public enum VersionComparisonResult
    {
        Lesser = -1,
        Equal = 0,
        Greater = 1
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static readonly AndroidJavaClass MaxUnityPluginClass = new AndroidJavaClass("com.applovin.mediation.unity.MaxUnityPlugin");
#endif

#if UNITY_IOS
    [DllImport("__Internal")]
    private static extern float _MaxGetAdaptiveBannerHeight(float width);
#endif

    /// <summary>
    /// Get the adaptive banner size for the provided width.
    /// If the width is not provided, will assume full screen width for the current orientation.
    ///
    /// NOTE: Only AdMob / Google Ad Manager currently has support for adaptive banners and the maximum height is 15% the height of the screen.
    /// </summary>
    ///
    /// <param name="width">The width to retrieve the adaptive banner height for.</param>
    /// <returns>The adaptive banner height for the current orientation and width.</returns>
    public static float GetAdaptiveBannerHeight(float width = -1.0f)
    {
#if UNITY_EDITOR
        return 50.0f;
#elif UNITY_IOS
        return _MaxGetAdaptiveBannerHeight(width);
#elif UNITY_ANDROID
        return MaxUnityPluginClass.CallStatic<float>("getAdaptiveBannerHeight", width);
#else
        return -1.0f;
#endif
    }

    /// <summary>
    /// The native iOS and Android plugins forward dictionaries as a string such as:
    ///
    /// "key_1=value1
    ///  key_2=value2
    ///  key_3=value3"
    ///  
    /// </summary>
    public static IDictionary<string, string> PropsStringToDict(string str)
    {
        var result = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(str)) return result;

        var components = str.Split('\n');
        foreach (var component in components)
        {
            var ix = component.IndexOf('=');
            if (ix > 0 && ix < component.Length)
            {
                var key = component.Substring(0, ix);
                var value = component.Substring(ix + 1, component.Length - ix - 1);
                if (!result.ContainsKey(key))
                {
                    result[key] = value;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// The native iOS and Android plugins forward dictionaries as a string such as:
    ///
    /// "key_1=value1,key_2=value2,key_3=value3"
    ///  
    /// </summary>
    public static String DictToPropsString(IDictionary<string, string> dict)
    {
        StringBuilder serialized = new StringBuilder();

        if (dict != null)
        {
            foreach (KeyValuePair<string, string> entry in dict)
            {
                if (entry.Key != null && entry.Value != null)
                {
                    serialized.Append(entry.Key);
                    serialized.Append(_DictKeyValueSeparator);
                    serialized.Append(entry.Value);
                    serialized.Append(_DictKeyValuePairSeparator);
                }
            }
        }

        return serialized.ToString();
    }

    /// <summary>
    /// The native iOS and Android plugins forward arrays of dictionaries as a string such as:
    ///
    /// key_1=value1
    /// key_2=value2
    /// ,
    /// key_1=value1
    /// key_2=value2
    ///  
    /// </summary>
    public static List<T> PropsStringsToList<T>(string str)
    {
        var result = new List<T>();

        if (string.IsNullOrEmpty(str)) return result;

        var infoStrings = str.Split(new[] { _ArrayItemSeparator }, StringSplitOptions.None);
        foreach (var infoString in infoStrings)
        {
            // Dynamically construct generic type with string argument.
            // The type T must have a constructor that creates a new object from an info string, i.e., new T(infoString)
            var instance = (T)Activator.CreateInstance(typeof(T), infoString);
            result.Add(instance);
        }

        return result;
    }

    /// <summary>
    /// Returns the hexidecimal color code string for the given Color.
    /// </summary>
    public static String ParseColor(Color color)
    {
        int a = (int) (Mathf.Clamp01(color.a) * Byte.MaxValue);
        int r = (int) (Mathf.Clamp01(color.r) * Byte.MaxValue);
        int g = (int) (Mathf.Clamp01(color.g) * Byte.MaxValue);
        int b = (int) (Mathf.Clamp01(color.b) * Byte.MaxValue);

        return BitConverter.ToString(new[]
        {
            Convert.ToByte(a),
            Convert.ToByte(r),
            Convert.ToByte(g),
            Convert.ToByte(b),
        }).Replace("-", "").Insert(0, "#");
    }

#if UNITY_IOS
    [DllImport("__Internal")]
    private static extern bool _MaxIsTablet();
#endif

    /// <summary>
    /// Returns whether or not the device is a tablet.
    /// </summary>
    public static bool IsTablet()
    {
#if UNITY_EDITOR
        return false;
#elif UNITY_IOS
        return _MaxIsTablet();
#elif UNITY_ANDROID
        return MaxUnityPluginClass.CallStatic<bool>("isTablet");
#else
        return false;
#endif
    }

#if UNITY_IOS
    [DllImport("__Internal")]
    private static extern float _MaxScreenDensity();
#endif

    /// <summary>
    /// Returns the screen density.
    /// </summary>
    public static float GetScreenDensity()
    {
#if UNITY_IOS
        return _MaxScreenDensity();
#elif UNITY_ANDROID && !UNITY_EDITOR
        return MaxUnityPluginClass.CallStatic<float>("getScreenDensity");
#else
        return -1;
#endif
    }

    /// <summary>
    /// Compares AppLovin MAX Unity mediation adapter plugin versions. Returns <see cref="VersionComparisonResult.Lesser"/>, <see cref="VersionComparisonResult.Equal"/>,
    /// or <see cref="VersionComparisonResult.Greater"/> as the first version is less than, equal to, or greater than the second.
    ///
    /// If a version for a specific platform is only present in one of the provided versions, the one that contains it is considered newer.
    /// </summary>
    /// <param name="versionA">The first version to be compared.</param>
    /// <param name="versionB">The second version to be compared.</param>
    /// <returns>
    /// <see cref="VersionComparisonResult.Lesser"/> if versionA is less than versionB.
    /// <see cref="VersionComparisonResult.Equal"/> if versionA and versionB are equal.
    /// <see cref="VersionComparisonResult.Greater"/> if versionA is greater than versionB.
    /// </returns>
    public static VersionComparisonResult CompareUnityMediationVersions(string versionA, string versionB)
    {
        if (versionA.Equals(versionB)) return VersionComparisonResult.Equal;

        // Unity version would be of format:      android_w.x.y.z_ios_a.b.c.d
        // For Android only versions it would be: android_w.x.y.z
        // For iOS only version it would be:      ios_a.b.c.d

        // After splitting into their respective components, the versions would be at the odd indices.
        var versionAComponents = versionA.Split('_').ToList();
        var versionBComponents = versionB.Split('_').ToList();

        var androidComparison = VersionComparisonResult.Equal;
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
            androidComparison = VersionComparisonResult.Greater;

            // Remove the Android version component so that iOS versions can be processed.
            versionAComponents.RemoveRange(0, 2);
        }
        else if (versionB.Contains("android"))
        {
            androidComparison = VersionComparisonResult.Lesser;

            // Remove the Android version component so that iOS version can be processed.
            versionBComponents.RemoveRange(0, 2);
        }

        var iosComparison = VersionComparisonResult.Equal;
        if (versionA.Contains("ios") && versionB.Contains("ios"))
        {
            var iosVersionA = versionAComponents[1];
            var iosVersionB = versionBComponents[1];
            iosComparison = CompareVersions(iosVersionA, iosVersionB);
        }
        else if (versionA.Contains("ios"))
        {
            iosComparison = VersionComparisonResult.Greater;
        }
        else if (versionB.Contains("ios"))
        {
            iosComparison = VersionComparisonResult.Lesser;
        }


        // If either one of the Android or iOS version is greater, the entire version should be greater.
        return (androidComparison == VersionComparisonResult.Greater || iosComparison == VersionComparisonResult.Greater) ? VersionComparisonResult.Greater : VersionComparisonResult.Lesser;
    }

    /// <summary>
    /// Compares its two arguments for order.  Returns <see cref="VersionComparisonResult.Lesser"/>, <see cref="VersionComparisonResult.Equal"/>,
    /// or <see cref="VersionComparisonResult.Greater"/> as the first version is less than, equal to, or greater than the second.
    /// </summary>
    /// <param name="versionA">The first version to be compared.</param>
    /// <param name="versionB">The second version to be compared.</param>
    /// <returns>
    /// <see cref="VersionComparisonResult.Lesser"/> if versionA is less than versionB.
    /// <see cref="VersionComparisonResult.Equal"/> if versionA and versionB are equal.
    /// <see cref="VersionComparisonResult.Greater"/> if versionA is greater than versionB.
    /// </returns>
    public static VersionComparisonResult CompareVersions(string versionA, string versionB)
    {
        if (versionA.Equals(versionB)) return VersionComparisonResult.Equal;

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
                if (versionABetaNumber < versionBBetaNumber) return VersionComparisonResult.Lesser;

                if (versionABetaNumber > versionBBetaNumber) return VersionComparisonResult.Greater;
            }
            // Only VersionA is beta, so A is older.
            else if (isVersionABeta)
            {
                return VersionComparisonResult.Lesser;
            }
            // Only VersionB is beta, A is newer.
            else
            {
                return VersionComparisonResult.Greater;
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

            if (aComponent < bComponent) return VersionComparisonResult.Lesser;

            if (aComponent > bComponent) return VersionComparisonResult.Greater;
        }

        return VersionComparisonResult.Equal;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Gets the path of the asset in the project for a given MAX plugin export path.
    /// </summary>
    /// <param name="exportPath">The actual exported path of the asset.</param>
    /// <returns>The exported path of the MAX plugin asset or the default export path if the asset is not found.</returns>
    public static string GetAssetPathForExportPath(string exportPath)
    {
        var defaultPath = Path.Combine("Assets", exportPath);
        var assetGuids = AssetDatabase.FindAssets("l:al_max_export_path-" + exportPath);

        return assetGuids.Length < 1 ? defaultPath : AssetDatabase.GUIDToAssetPath(assetGuids[0]);
    }
#endif
}
