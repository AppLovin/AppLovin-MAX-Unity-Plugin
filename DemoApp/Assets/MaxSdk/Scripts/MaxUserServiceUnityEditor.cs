using UnityEngine;

public class MaxUserServiceUnityEditor
{
    private static readonly MaxUserServiceUnityEditor _instance = new MaxUserServiceUnityEditor();

    public static MaxUserServiceUnityEditor Instance
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
        MaxSdkLogger.UserWarning("The consent dialog cannot be shown in the Unity Editor. Please export the project to Android or iOS first.");
    }
}
