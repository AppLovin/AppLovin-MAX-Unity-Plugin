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

    [System.Obsolete("This version of the iOS consent flow has been deprecated as of MAX Unity Plugin v4.0.0 + iOS SDK v7.0.0, please refer to our documentation for enabling the new consent flow.")]   
    public void ShowConsentDialog()
    {
        _MaxShowConsentDialog();
    }
}
#endif
