/**
 * Class containing pre-defined constants to use with AppLovin event tracking APIs.
 */
public static class MaxEvents
{
    /**
     * Nested class representing pre-defined AppLovin events to be fired with AppLovin event tracking APIs.
     */
    public class AppLovin
    {
        public const string UserLoggedIn = "login";
        public const string UserCreatedAccount = "registration";
        public const string UserCompletedTutorial = "tutorial";
        public const string UserCompletedLevel = "level";
        public const string UserCompletedAchievement = "achievement";
        public const string UserSpentVirtualCurrency = "vcpurchase";
        public const string UserCompletedInAppPurchase = "iap";
        public const string UserSentInvitation = "invite";
        public const string UserSharedLink = "share";
    }
}