using UnityEngine;

public class MaxUserServiceAndroid
{
    private static readonly AndroidJavaClass _maxUnityPluginClass = new AndroidJavaClass("com.applovin.mediation.unity.MaxUnityPlugin");
    private static readonly MaxUserServiceAndroid _instance = new MaxUserServiceAndroid();

    public static MaxUserServiceAndroid Instance
    {
        get { return _instance; }
    }

    /// <summary>
    /// Show the user consent dialog to the user using one from AppLovin's SDK. You should check that you actually need to show the consent dialog
    /// by checking <see cref="SdkConfiguration.ConsentDialogState"/> in the completion block of <see cref="MaxSdkCallbacks.OnSdkInitializedEvent"/>.
    /// Please make sure to implement the callback <see cref="MaxSdkCallbacks.OnSdkConsentDialogDismissedEvent"/>.
    /// </summary>    
    public void ShowConsentDialog()
    {
        _maxUnityPluginClass.CallStatic("showConsentDialog");
    }
}
