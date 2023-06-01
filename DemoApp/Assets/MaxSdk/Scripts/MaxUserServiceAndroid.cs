using UnityEngine;

/// <summary>
/// This class has been deprecated and will be removed in a future release
/// </summary>
public class MaxUserServiceAndroid
{
    private static readonly AndroidJavaClass _maxUnityPluginClass = new AndroidJavaClass("com.applovin.mediation.unity.MaxUnityPlugin");
    private static readonly MaxUserServiceAndroid _instance = new MaxUserServiceAndroid();

    public static MaxUserServiceAndroid Instance
    {
        get { return _instance; }
    }


    /// <summary>
    /// Preload the user consent dialog. You have the option to call this so the consent dialog appears
    /// more quickly when you call <see cref="MaxUserServiceAndroid.ShowConsentDialog"/>.
    /// </summary>
    public void PreloadConsentDialog()
    {
        _maxUnityPluginClass.CallStatic("preloadConsentDialog");
    }

    /// <summary>
    /// Show the user consent dialog to the user using one from AppLovin's SDK. You should check that you actually need to show the consent dialog
    /// by checking <see cref="MaxSdkBase.ConsentDialogState"/> in the completion block of <see cref="MaxSdkCallbacks.OnSdkInitializedEvent"/>.
    /// Please make sure to implement the callback <see cref="MaxSdkCallbacks.OnSdkConsentDialogDismissedEvent"/>.
    /// </summary>    
    public void ShowConsentDialog()
    {
        _maxUnityPluginClass.CallStatic("showConsentDialog");
    }
}
