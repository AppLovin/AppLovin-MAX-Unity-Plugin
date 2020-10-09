/**
 * AppLovin MAX Unity Plugin C# Wrapper
 */

using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class MaxSdk :
#if UNITY_EDITOR
    MaxSdkUnityEditor
#elif UNITY_ANDROID
    MaxSdkAndroid
#else
    MaxSdkiOS
#endif
{
    private const string _version = "3.1.7";

    /// <summary>
    /// Returns the current plugin version.
    /// </summary>
    public static string Version
    {
        get { return _version; }
    }
}