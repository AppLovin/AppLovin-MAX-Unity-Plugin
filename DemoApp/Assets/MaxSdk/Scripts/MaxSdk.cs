//
// AppLovin MAX Unity Plugin C# Wrapper
//

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
    private const string _version = "8.1.0";

    /// <summary>
    /// Returns the current plugin version.
    /// </summary>
    public static string Version
    {
        get { return _version; }
    }
}
