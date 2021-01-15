using System.Runtime.InteropServices;

#if UNITY_IOS
public class MaxUserServiceiOS
{
    private static readonly MaxUserServiceiOS _instance = new MaxUserServiceiOS();

    public static MaxUserServiceiOS Instance
    {
        get { return _instance; }
    }

    [DllImport("__Internal")]
    private static extern void _MaxShowConsentDialog();

    /// <summary>
    /// Show the user consent dialog to the user using one from AppLovin's SDK. You should check that you actually need to show the consent dialog
    /// by checking <see cref="SdkConfiguration.ConsentDialogState"/> in the completion block of <see cref="MaxSdkCallbacks.OnSdkInitializedEvent"/>.
    /// Please make sure to implement the callback <see cref="MaxSdkCallbacks.OnSdkConsentDialogDismissedEvent"/>.
    /// </summary>   
    public void ShowConsentDialog()
    {
        _MaxShowConsentDialog();
    }
}
#endif
