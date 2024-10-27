using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using AppLovinMax.ThirdParty.MiniJson;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

public class MaxSdkUtils
{
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
    /// Tries to get a dictionary for the given key if available, returns the default value if unavailable.
    /// </summary>
    /// <param name="dictionary">The dictionary from which to get the dictionary</param>
    /// <param name="key">The key to be used to retrieve the dictionary</param>
    /// <param name="defaultValue">The default value to be returned when a value for the given key is not found.</param>
    /// <returns>The dictionary for the given key if available, the default value otherwise.</returns>
    public static Dictionary<string, object> GetDictionaryFromDictionary(IDictionary<string, object> dictionary, string key, Dictionary<string, object> defaultValue = null)
    {
        if (dictionary == null) return defaultValue;

        object value;
        if (dictionary.TryGetValue(key, out value) && value is Dictionary<string, object>)
        {
            return value as Dictionary<string, object>;
        }

        return defaultValue;
    }

    /// <summary>
    /// Tries to get a list from the dictionary for the given key if available, returns the default value if unavailable.
    /// </summary>
    /// <param name="dictionary">The dictionary from which to get the list</param>
    /// <param name="key">The key to be used to retrieve the list</param>
    /// <param name="defaultValue">The default value to be returned when a value for the given key is not found.</param>
    /// <returns>The list for the given key if available, the default value otherwise.</returns>
    public static List<object> GetListFromDictionary(IDictionary<string, object> dictionary, string key, List<object> defaultValue = null)
    {
        if (dictionary == null) return defaultValue;

        object value;
        if (dictionary.TryGetValue(key, out value) && value is List<object>)
        {
            return value as List<object>;
        }

        return defaultValue;
    }

    /// <summary>
    /// Tries to get a <c>string</c> value from dictionary for the given key if available, returns the default value if unavailable.  
    /// </summary>
    /// <param name="dictionary">The dictionary from which to get the <c>string</c> value.</param>
    /// <param name="key">The key to be used to retrieve the <c>string</c> value.</param>
    /// <param name="defaultValue">The default value to be returned when a value for the given key is not found.</param>
    /// <returns>The <c>string</c> value from the dictionary if available, the default value otherwise.</returns>
    public static string GetStringFromDictionary(IDictionary<string, object> dictionary, string key, string defaultValue = "")
    {
        if (dictionary == null) return defaultValue;

        object value;
        if (dictionary.TryGetValue(key, out value) && value != null)
        {
            return value.ToString();
        }

        return defaultValue;
    }

    /// <summary>
    /// Tries to get a <c>bool</c> value from dictionary for the given key if available, returns the default value if unavailable.
    /// </summary>
    /// <param name="dictionary">The dictionary from which to get the <c>bool</c> value.</param>
    /// <param name="key">The key to be used to retrieve the <c>bool</c> value.</param>
    /// <param name="defaultValue">The default value to be returned when a <c>bool</c> value for the given key is not found.</param>
    /// <returns>The <c>bool</c> value from the dictionary if available, the default value otherwise.</returns>
    public static bool GetBoolFromDictionary(IDictionary<string, object> dictionary, string key, bool defaultValue = false)
    {
        if (dictionary == null) return defaultValue;

        object obj;
        bool value;
        if (dictionary.TryGetValue(key, out obj) && obj != null && bool.TryParse(obj.ToString(), out value))
        {
            return value;
        }

        return defaultValue;
    }

    /// <summary>
    /// Tries to get a <c>int</c> value from dictionary for the given key if available, returns the default value if unavailable.
    /// </summary>
    /// <param name="dictionary">The dictionary from which to get the <c>int</c> value.</param>
    /// <param name="key">The key to be used to retrieve the <c>int</c> value.</param>
    /// <param name="defaultValue">The default value to be returned when a <c>int</c> value for the given key is not found.</param>
    /// <returns>The <c>int</c> value from the dictionary if available, the default value otherwise.</returns>
    public static int GetIntFromDictionary(IDictionary<string, object> dictionary, string key, int defaultValue = 0)
    {
        if (dictionary == null) return defaultValue;

        object obj;
        int value;
        if (dictionary.TryGetValue(key, out obj) &&
            obj != null &&
            int.TryParse(InvariantCultureToString(obj), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
        {
            return value;
        }

        return defaultValue;
    }

    /// <summary>
    /// Tries to get a <c>long</c> value from dictionary for the given key if available, returns the default value if unavailable.
    /// </summary>
    /// <param name="dictionary">The dictionary from which to get the <c>long</c> value.</param>
    /// <param name="key">The key to be used to retrieve the <c>long</c> value.</param>
    /// <param name="defaultValue">The default value to be returned when a <c>long</c> value for the given key is not found.</param>
    /// <returns>The <c>long</c> value from the dictionary if available, the default value otherwise.</returns>
    public static long GetLongFromDictionary(IDictionary<string, object> dictionary, string key, long defaultValue = 0L)
    {
        if (dictionary == null) return defaultValue;

        object obj;
        long value;
        if (dictionary.TryGetValue(key, out obj) &&
            obj != null &&
            long.TryParse(InvariantCultureToString(obj), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
        {
            return value;
        }

        return defaultValue;
    }

    /// <summary>
    /// Tries to get a <c>float</c> value from dictionary for the given key if available, returns the default value if unavailable.
    /// </summary>
    /// <param name="dictionary">The dictionary from which to get the <c>float</c> value.</param>
    /// <param name="key">The key to be used to retrieve the <c>float</c> value.</param>
    /// <param name="defaultValue">The default value to be returned when a <c>string</c> value for the given key is not found.</param>
    /// <returns>The <c>float</c> value from the dictionary if available, the default value otherwise.</returns>
    public static float GetFloatFromDictionary(IDictionary<string, object> dictionary, string key, float defaultValue = 0F)
    {
        if (dictionary == null) return defaultValue;

        object obj;
        float value;
        if (dictionary.TryGetValue(key, out obj) &&
            obj != null &&
            float.TryParse(InvariantCultureToString(obj), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
        {
            return value;
        }

        return defaultValue;
    }

    /// <summary>
    /// Tries to get a <c>double</c> value from dictionary for the given key if available, returns the default value if unavailable.
    /// </summary>
    /// <param name="dictionary">The dictionary from which to get the <c>double</c> value.</param>
    /// <param name="key">The key to be used to retrieve the <c>double</c> value.</param>
    /// <param name="defaultValue">The default value to be returned when a <c>double</c> value for the given key is not found.</param>
    /// <returns>The <c>double</c> value from the dictionary if available, the default value otherwise.</returns>
    public static double GetDoubleFromDictionary(IDictionary<string, object> dictionary, string key, int defaultValue = 0)
    {
        if (dictionary == null) return defaultValue;

        object obj;
        double value;
        if (dictionary.TryGetValue(key, out obj) &&
            obj != null &&
            double.TryParse(InvariantCultureToString(obj), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
        {
            return value;
        }

        return defaultValue;
    }

    /// <summary>
    /// Converts the given object to a string without locale specific conversions.
    /// </summary>
    public static string InvariantCultureToString(object obj)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}", obj);
    }

    /// <summary>
    /// The native iOS and Android plugins forward JSON arrays of JSON Objects.
    /// </summary>
    public static List<T> PropsStringsToList<T>(string str)
    {
        var result = new List<T>();

        if (string.IsNullOrEmpty(str)) return result;

        var infoArray = Json.Deserialize(str) as List<object>;
        if (infoArray == null) return result;

        foreach (var infoObject in infoArray)
        {
            var dictionary = infoObject as Dictionary<string, object>;
            if (dictionary == null) continue;

            // Dynamically construct generic type with string argument.
            // The type T must have a constructor that creates a new object from an info string, i.e., new T(infoString)
            var instance = (T) Activator.CreateInstance(typeof(T), dictionary);
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
    private static extern bool _MaxIsPhysicalDevice();
#endif

    /// <summary>
    /// Returns whether or not a physical device is being used, as opposed to an emulator / simulator.
    /// </summary>
    public static bool IsPhysicalDevice()
    {
#if UNITY_EDITOR
        return false;
#elif UNITY_IOS
        return _MaxIsPhysicalDevice();
#elif UNITY_ANDROID
        return MaxUnityPluginClass.CallStatic<bool>("isPhysicalDevice");
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
#if UNITY_EDITOR
        return 1;
#elif UNITY_IOS
        return _MaxScreenDensity();
#elif UNITY_ANDROID
        return MaxUnityPluginClass.CallStatic<float>("getScreenDensity");
#else
        return -1;
#endif
    }

    /// <summary>
    /// Parses the IABTCF_VendorConsents string to determine the consent status of the IAB vendor with the provided ID.
    /// NOTE: Must be called after AppLovin MAX SDK has been initialized.
    /// </summary>
    /// <param name="vendorId">Vendor ID as defined in the Global Vendor List.</param>
    /// <returns><c>true</c> if the vendor has consent, <c>false</c> if not, or <c>null</c> if TC data is not available on disk.</returns>
    /// <see href="https://vendor-list.consensu.org/v3/vendor-list.json">Current Version of Global Vendor List</see>
    public static bool? GetTcfConsentStatus(int vendorId)
    {
        var tcfConsentStatus = GetPlatformSpecificTcfConsentStatus(vendorId);
        return GetConsentStatusValue(tcfConsentStatus);
    }

#if UNITY_IOS
    [DllImport("__Internal")]
    private static extern int _MaxGetTcfVendorConsentStatus(int vendorIdentifier);
#endif

    private static int GetPlatformSpecificTcfConsentStatus(int vendorId)
    {
#if UNITY_EDITOR
        return -1;
#elif UNITY_IOS
        return _MaxGetTcfVendorConsentStatus(vendorId);
#elif UNITY_ANDROID
        return MaxUnityPluginClass.CallStatic<int>("getTcfVendorConsentStatus", vendorId);
#else
        return -1;
#endif
    }

    /// <summary>
    /// Parses the IABTCF_AddtlConsent string to determine the consent status of the advertising entity with the provided Ad Technology Provider (ATP) ID.
    /// NOTE: Must be called after AppLovin MAX SDK has been initialized.
    /// </summary>
    /// <param name="atpId">ATP ID of the advertising entity (e.g. 89 for Meta Audience Network).</param>
    /// <returns>
    /// <c>true</c> if the advertising entity has consent, <c>false</c> if not, or <c>null</c> if no AC string is available on disk or the ATP network was not listed in the CMP flow.
    /// </returns>
    /// <see href="https://support.google.com/admanager/answer/9681920">Google’s Additional Consent Mode technical specification</see>
    /// <see href="https://storage.googleapis.com/tcfac/additional-consent-providers.csv">List of Google ATPs and their IDs</see>
    public static bool? GetAdditionalConsentStatus(int atpId)
    {
        var additionalConsentStatus = GetPlatformSpecificAdditionalConsentStatus(atpId);
        return GetConsentStatusValue(additionalConsentStatus);
    }

#if UNITY_IOS
    [DllImport("__Internal")]
    private static extern int _MaxGetAdditionalConsentStatus(int atpIdentifier);
#endif

    private static int GetPlatformSpecificAdditionalConsentStatus(int atpId)
    {
#if UNITY_EDITOR
        return -1;
#elif UNITY_IOS
        return _MaxGetAdditionalConsentStatus(atpId);
#elif UNITY_ANDROID
        return MaxUnityPluginClass.CallStatic<int>("getAdditionalConsentStatus", atpId);
#else
        return -1;
#endif
    }

    /// <summary>
    /// Parses the IABTCF_PurposeConsents String to determine the consent status of the IAB defined data processing purpose.
    /// NOTE: Must be called after AppLovin MAX SDK has been initialized.
    /// </summary>
    /// <param name="purposeId">Purpose ID.</param>
    /// <returns><c>true</c> if the purpose has consent, <c>false</c> if not, or <c>null</c> if TC data is not available on disk.</returns>
    /// <see href="https://storage.googleapis.com/tcfac/additional-consent-providers.csv">see IAB Europe Transparency and Consent Framework Policies (Appendix A) for purpose definitions.</see>
    public static bool? GetPurposeConsentStatus(int purposeId)
    {
        var purposeConsentStatus = GetPlatformSpecificPurposeConsentStatus(purposeId);
        return GetConsentStatusValue(purposeConsentStatus);
    }

#if UNITY_IOS
    [DllImport("__Internal")]
    private static extern int _MaxGetPurposeConsentStatus(int purposeIdentifier);
#endif

    private static int GetPlatformSpecificPurposeConsentStatus(int purposeId)
    {
#if UNITY_EDITOR
        return -1;
#elif UNITY_IOS
        return _MaxGetPurposeConsentStatus(purposeId);
#elif UNITY_ANDROID
        return MaxUnityPluginClass.CallStatic<int>("getPurposeConsentStatus", purposeId);
#else
        return -1;
#endif
    }

    /// <summary>
    /// Parses the IABTCF_SpecialFeaturesOptIns String to determine the opt-in status of the IAB defined special feature.
    /// NOTE: Must be called after AppLovin MAX SDK has been initialized.
    /// </summary>
    /// <param name="specialFeatureId">Special feature ID.</param>
    /// <returns><c>true</c> if the user opted in for the special feature, <c>false</c> if not, or <c>null</c> if TC data is not available on disk.</returns>
    /// <see href="https://iabeurope.eu/iab-europe-transparency-consent-framework-policies">IAB Europe Transparency and Consent Framework Policies (Appendix A) for special features </see>
    public static bool? GetSpecialFeatureOptInStatus(int specialFeatureId)
    {
        var specialFeatureOptInStatus = GetPlatformSpecificSpecialFeatureOptInStatus(specialFeatureId);
        return GetConsentStatusValue(specialFeatureOptInStatus);
    }

#if UNITY_IOS
    [DllImport("__Internal")]
    private static extern int _MaxGetSpecialFeatureOptInStatus(int specialFeatureIdentifier);
#endif

    private static int GetPlatformSpecificSpecialFeatureOptInStatus(int specialFeatureId)
    {
#if UNITY_EDITOR
        return -1;
#elif UNITY_IOS
        return _MaxGetSpecialFeatureOptInStatus(specialFeatureId);
#elif UNITY_ANDROID
        return MaxUnityPluginClass.CallStatic<int>("getSpecialFeatureOptInStatus", specialFeatureId);
#else
        return -1;
#endif
    }

    private static bool? GetConsentStatusValue(int consentStatus)
    {
        if (consentStatus == -1)
        {
            return null;
        }
        else
        {
            return consentStatus == 1;
        }
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

    /// <summary>
    /// Check if the given string is valid - not <c>null</c> and not empty.
    /// </summary>
    /// <param name="toCheck">The string to be checked.</param>
    /// <returns><c>true</c> if the given string is not <c>null</c> and not empty.</returns>
    public static bool IsValidString(string toCheck)
    {
        return !string.IsNullOrEmpty(toCheck);
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
        var assetLabelToFind = "l:al_max_export_path-" + exportPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var assetGuids = AssetDatabase.FindAssets(assetLabelToFind);

        return assetGuids.Length < 1 ? defaultPath : AssetDatabase.GUIDToAssetPath(assetGuids[0]);
    }
#endif
}
