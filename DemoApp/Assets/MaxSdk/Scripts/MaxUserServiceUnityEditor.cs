
/// <summary>
/// This class has been deprecated and will be removed in a future release
/// </summary>
public class MaxUserServiceUnityEditor
{
    private static readonly MaxUserServiceUnityEditor _instance = new MaxUserServiceUnityEditor();

    public static MaxUserServiceUnityEditor Instance
    {
        get { return _instance; }
    }

    /// <summary>
    /// Preload the user consent dialog. You have the option to call this so the consent dialog appears
    /// more quickly when you call <see cref="MaxUserServiceUnityEditor.ShowConsentDialog"/>.
    /// </summary>
    public void PreloadConsentDialog()
    {
        MaxSdkLogger.UserWarning("The consent dialog cannot be pre-loaded in the Unity Editor. Please export the project to Android first.");
    }

    /// <summary>
    /// Show the user consent dialog to the user using one from AppLovin's SDK. You should check that you actually need to show the consent dialog
    /// by checking <see cref="SdkConfiguration.ConsentDialogState"/> in the completion block of <see cref="MaxSdkCallbacks.OnSdkInitializedEvent"/>.
    /// Please make sure to implement the callback <see cref="MaxSdkCallbacks.OnSdkConsentDialogDismissedEvent"/>.
    /// </summary>
    public void ShowConsentDialog()
    {
        MaxSdkLogger.UserWarning("The consent dialog cannot be shown in the Unity Editor. Please export the project to Android first.");
    }
}
