using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class MaxSdkUtils
{
    private static readonly char _DictKeyValueSeparator = (char) 28;
    private static readonly char _DictKeyValuePairSeparator = (char) 29;

#if UNITY_ANDROID && !UNITY_EDITOR
    private static readonly AndroidJavaClass MaxUnityPluginClass = new AndroidJavaClass("com.applovin.mediation.unity.MaxUnityPlugin");
#endif

    /// <summary>
    /// The native iOS and Android plugins forward dictionaries as a string such as:
    ///
    /// "key_1=value1
    ///  key_2=value2,
    ///  key=3-value3"
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
    /// "key_1=value1,key_2=value2,key=3-value3"
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
#if UNITY_IOS
        return _MaxIsTablet();
#elif UNITY_ANDROID && !UNITY_EDITOR
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
    /// Compares AppLovin MAX Unity mediation adapter plugin versions. Returns a negative integer, zero, or a positive integer as the first version is less than, equal to, or greater than the second.
    ///
    /// If a version for a specific platform is only present in one of the provided versions, the one that contains it is considered newer.
    /// </summary>
    /// <param name="versionA">The first version to be compared.</param>
    /// <param name="versionB">The second version to be compared.</param>
    /// <returns>A negative integer, zero, or a positive integer as the first version is less than, equal to, or greater than the second.</returns>
    public static int CompareUnityMediationVersions(string versionA, string versionB)
    {
        if (versionA.Equals(versionB)) return 0;

        // Unity version would be of format:      android_w.x.y.z_ios_a.b.c.d
        // For Android only versions it would be: android_w.x.y.z
        // For iOS only version it would be:      ios_a.b.c.d

        // After splitting into their respective components, the versions would be at the odd indices.
        var versionAComponents = versionA.Split('_').ToList();
        var versionBComponents = versionB.Split('_').ToList();

        var androidComparison = 0;
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
            androidComparison = 1;

            // Remove the Android version component so that iOS versions can be processed.
            versionAComponents.RemoveRange(0, 2);
        }
        else if (versionB.Contains("android"))
        {
            androidComparison = -1;

            // Remove the Android version component so that iOS version can be processed.
            versionBComponents.RemoveRange(0, 2);
        }

        var iosComparison = 0;
        if (versionA.Contains("ios") && versionB.Contains("ios"))
        {
            var iosVersionA = versionAComponents[1];
            var iosVersionB = versionBComponents[1];
            iosComparison = CompareVersions(iosVersionA, iosVersionB);
        }
        else if (versionA.Contains("ios"))
        {
            iosComparison = 1;
        }
        else if (versionB.Contains("ios"))
        {
            iosComparison = -1;
        }


        // If either one of the Android or iOS version is greater, the entire version should be greater.
        return androidComparison > 0 || iosComparison > 0 ? 1 : -1;
    }

    /// <summary>
    /// Compares its two arguments for order.  Returns a negative integer, zero, or a positive integer as the first version is less than, equal to, or greater than the second.
    /// </summary>
    /// <param name="versionA">The first version to be compared.</param>
    /// <param name="versionB">The second version to be compared.</param>
    /// <returns>A negative integer, zero, or a positive integer as the first version is less than, equal to, or greater than the second.</returns>
    public static int CompareVersions(string versionA, string versionB)
    {
        if (versionA.Equals(versionB)) return 0;

        int piece;
        var versionAComponents = versionA.Split('.').Select(version => int.TryParse(version, out piece) ? piece : 0).ToArray();
        var versionBComponents = versionB.Split('.').Select(version => int.TryParse(version, out piece) ? piece : 0).ToArray();
        var length = Mathf.Max(versionAComponents.Length, versionBComponents.Length);
        for (var i = 0; i < length; i++)
        {
            var aComponent = i < versionAComponents.Length ? versionAComponents[i] : 0;
            var bComponent = i < versionBComponents.Length ? versionBComponents[i] : 0;

            if (aComponent < bComponent) return -1;

            if (aComponent > bComponent) return 1;
        }

        return 0;
    }
}
