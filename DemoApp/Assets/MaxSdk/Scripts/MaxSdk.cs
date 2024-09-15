/**
 * AppLovin MAX Unity Plugin C# Wrapper
 */

using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class MaxSdk :
#if UNITY_EDITOR
    // Check for Unity Editor first since the editor also responds to the currently selected platform.
    MaxSdkUnityEditor
#elif UNITY_ANDROID
    MaxSdkAndroid
#elif UNITY_IPHONE || UNITY_IOS
    MaxSdkiOS
#else
    MaxSdkUnityEditor
#endif
{
    private const string _version = "7.0.0";

    /// <summary>
    /// Returns the current plugin version.
    /// </summary>
    public static string Version
    {
        get { return _version; }
    }
}
