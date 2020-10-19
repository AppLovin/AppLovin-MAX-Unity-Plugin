//
//  MAUnityAdManager.m
//  AppLovin MAX Unity Plugin
//

#import "MAUnityAdManager.h"

#define VERSION @"3.1.8"

#define DEVICE_SPECIFIC_ADVIEW_AD_FORMAT ([[UIDevice currentDevice] userInterfaceIdiom] == UIUserInterfaceIdiomPad) ? MAAdFormat.leader : MAAdFormat.banner

#ifdef __cplusplus
extern "C" {
#endif
    // life cycle management
    void UnityPause(int pause);
    void UnitySendMessage(const char* obj, const char* method, const char* msg);
#ifdef __cplusplus
}
#endif

@interface MAUnityAdManager()<MAAdDelegate, MAAdViewAdDelegate, MARewardedAdDelegate, ALVariableServiceDelegate>

// Parent Fields
@property (nonatomic, weak) ALSdk *sdk;

// Fullscreen Ad Fields
@property (nonatomic, strong) NSMutableDictionary<NSString *, MAInterstitialAd *> *interstitials;
@property (nonatomic, strong) NSMutableDictionary<NSString *, MARewardedAd *> *rewardedAds;
@property (nonatomic, strong) NSMutableDictionary<NSString *, MARewardedInterstitialAd *> *rewardedInterstitialAds;

// Banner Fields
@property (nonatomic, strong) NSMutableDictionary<NSString *, MAAdView *> *adViews;
@property (nonatomic, strong) NSMutableDictionary<NSString *, MAAdFormat *> *adViewAdFormats;
@property (nonatomic, strong) NSMutableDictionary<NSString *, NSString *> *adViewPositions;
@property (nonatomic, strong) NSMutableDictionary<NSString *, MAAdFormat *> *verticalAdViewFormats;
@property (nonatomic, strong) NSMutableDictionary<NSString *, NSArray<NSLayoutConstraint *> *> *adViewConstraints;
@property (nonatomic, strong) NSMutableArray<NSString *> *adUnitIdentifiersToShowAfterCreate;
@property (nonatomic, strong) UIView *safeAreaBackground;
@property (nonatomic, strong, nullable) UIColor *publisherBannerBackgroundColor;

@property (nonatomic, strong) NSMutableDictionary<NSString *, MAAd *> *adInfoDict;
@property (nonatomic, strong) NSObject *adInfoDictLock;

@end

// Internal
@interface UIColor (ALUtils)
+ (nullable UIColor *)al_colorWithHexString:(NSString *)hexString;
@end

@interface NSNumber (ALUtils)
+ (NSNumber *)al_numberWithString:(NSString *)string;
@end

@interface MAAdFormat (ALUtils)
@property (nonatomic, assign, readonly, getter=isFullscreenAd) BOOL fullscreenAd;
@property (nonatomic, assign, readonly, getter=isAdViewAd) BOOL adViewAd;
@end

@implementation MAUnityAdManager
static NSString *const SDK_TAG = @"AppLovinSdk";
static NSString *const TAG = @"MAUnityAdManager";
static NSString *ALSerializeKeyValueSeparator;
static NSString *ALSerializeKeyValuePairSeparator;

#pragma mark - Initialization

+ (void)initialize
{
    [super initialize];
    
    ALSerializeKeyValueSeparator = [NSString stringWithFormat: @"%c", 28];
    ALSerializeKeyValuePairSeparator = [NSString stringWithFormat: @"%c", 29];
}

- (instancetype)init
{
    self = [super init];
    if ( self )
    {
        self.interstitials = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.rewardedAds = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.rewardedInterstitialAds = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.adViews = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.adViewAdFormats = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.adViewPositions = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.verticalAdViewFormats = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.adViewConstraints = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.adUnitIdentifiersToShowAfterCreate = [NSMutableArray arrayWithCapacity: 2];
        self.safeAreaBackground = [[UIView alloc] init];
        self.safeAreaBackground.hidden = YES;
        self.safeAreaBackground.backgroundColor = UIColor.clearColor;
        self.safeAreaBackground.translatesAutoresizingMaskIntoConstraints = NO;
        self.safeAreaBackground.userInteractionEnabled = NO;
        
        self.adInfoDict = [NSMutableDictionary dictionary];
        self.adInfoDictLock = [[NSObject alloc] init];
        
        UIViewController *rootViewController = [MAUnityAdManager unityViewController];
        [rootViewController.view addSubview: self.safeAreaBackground];
        
        // Enable orientation change listener, so that the position can be updated for vertical banners.
        [[NSNotificationCenter defaultCenter] addObserverForName: UIDeviceOrientationDidChangeNotification
                                                          object: nil queue: [NSOperationQueue mainQueue]
                                                      usingBlock:^(NSNotification *notification) {
            
            for ( NSString *adUnitIdentifier in self.verticalAdViewFormats )
            {
                [self positionAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: self.verticalAdViewFormats[adUnitIdentifier]];
            }
        }];
    }
    return self;
}

+ (MAUnityAdManager *)shared
{
    static dispatch_once_t token;
    static MAUnityAdManager *shared;
    dispatch_once(&token, ^{
        shared = [[MAUnityAdManager alloc] init];
    });
    return shared;
}

#pragma mark - Plugin Initialization

- (ALSdk *)initializeSdkWithAdUnitIdentifiers:(NSString *)serializedAdUnitIdentifiers metaData:(NSString *)serializedMetaData andCompletionHandler:(ALSdkInitializationCompletionHandler)completionHandler
{
    NSDictionary *infoDict = [[NSBundle mainBundle] infoDictionary];
    NSString *sdkKey = infoDict[@"AppLovinSdkKey"];
    self.sdk = [ALSdk sharedWithKey: sdkKey settings: [self generateSDKSettingsForAdUnitIdentifiers: serializedAdUnitIdentifiers metaData: serializedMetaData]];
    self.sdk.variableService.delegate = self;
    [self.sdk setPluginVersion: [@"Max-Unity-" stringByAppendingString: VERSION]];
    self.sdk.mediationProvider = @"max";
    [self.sdk initializeSdkWithCompletionHandler:^(ALSdkConfiguration *configuration)
     {
        // Note: internal state should be updated first
        completionHandler( configuration );
        
        NSString *consentDialogStateStr = @(configuration.consentDialogState).stringValue;
        //NSString *appTrackingStatus = @(configuration.appTrackingTransparencyStatus).stringValue; // Deliberately name it `appTrackingStatus` to be a bit more generic (in case Android introduces a similar concept)
        [MAUnityAdManager forwardUnityEventWithArgs: @{@"name" : @"OnSdkInitializedEvent",
                                                       @"consentDialogState" : consentDialogStateStr}];
                                                       //@"appTrackingStatus" : appTrackingStatus}];
    }];
    
    return self.sdk;
}

#pragma mark - Banners

- (void)createBannerWithAdUnitIdentifier:(NSString *)adUnitIdentifier atPosition:(NSString *)bannerPosition
{
    [self createAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: DEVICE_SPECIFIC_ADVIEW_AD_FORMAT atPosition: bannerPosition];
}

- (void)setBannerBackgroundColorForAdUnitIdentifier:(NSString *)adUnitIdentifier hexColorCode:(NSString *)hexColorCode
{
    [self setAdViewBackgroundColorForAdUnitIdentifier: adUnitIdentifier adFormat: DEVICE_SPECIFIC_ADVIEW_AD_FORMAT hexColorCode: hexColorCode];
}

- (void)setBannerPlacement:(nullable NSString *)placement forAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self setAdViewPlacement: placement forAdUnitIdentifier: adUnitIdentifier adFormat: DEVICE_SPECIFIC_ADVIEW_AD_FORMAT];
}

- (void)updateBannerPosition:(NSString *)bannerPosition forAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self updateAdViewPosition: bannerPosition forAdUnitIdentifier: adUnitIdentifier adFormat: DEVICE_SPECIFIC_ADVIEW_AD_FORMAT];
}

- (void)setBannerExtraParameterForAdUnitIdentifier:(NSString *)adUnitIdentifier key:(NSString *)key value:(nullable NSString *)value
{
    [self setAdViewExtraParameterForAdUnitIdentifier: adUnitIdentifier adFormat: DEVICE_SPECIFIC_ADVIEW_AD_FORMAT key: key value: value];
}

- (void)showBannerWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self showAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: DEVICE_SPECIFIC_ADVIEW_AD_FORMAT];
}

- (void)hideBannerWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self hideAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: DEVICE_SPECIFIC_ADVIEW_AD_FORMAT];
}

- (void)destroyBannerWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self destroyAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: DEVICE_SPECIFIC_ADVIEW_AD_FORMAT];
}

#pragma mark - MRECs

- (void)createMRecWithAdUnitIdentifier:(NSString *)adUnitIdentifier atPosition:(NSString *)mrecPosition
{
    [self createAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec atPosition: mrecPosition];
}

- (void)setMRecPlacement:(nullable NSString *)placement forAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self setAdViewPlacement: placement forAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

- (void)updateMRecPosition:(NSString *)mrecPosition forAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self updateAdViewPosition: mrecPosition forAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

- (void)showMRecWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self showAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

- (void)destroyMRecWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self destroyAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

- (void)hideMRecWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self hideAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

#pragma mark - Interstitials

- (void)loadInterstitialWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    MAInterstitialAd *interstitial = [self retrieveInterstitialForAdUnitIdentifier: adUnitIdentifier];
    [interstitial loadAd];
}

- (BOOL)isInterstitialReadyWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    MAInterstitialAd *interstitial = [self retrieveInterstitialForAdUnitIdentifier: adUnitIdentifier];
    return [interstitial isReady];
}

- (void)showInterstitialWithAdUnitIdentifier:(NSString *)adUnitIdentifier placement:(NSString *)placement
{
    MAInterstitialAd *interstitial = [self retrieveInterstitialForAdUnitIdentifier: adUnitIdentifier];
    [interstitial showAdForPlacement: placement];
}

- (void)setInterstitialExtraParameterForAdUnitIdentifier:(NSString *)adUnitIdentifier key:(NSString *)key value:(nullable NSString *)value
{
    MAInterstitialAd *interstitial = [self retrieveInterstitialForAdUnitIdentifier: adUnitIdentifier];
    [interstitial setExtraParameterForKey: key value: value];
}

#pragma mark - Rewarded

- (void)loadRewardedAdWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    MARewardedAd *rewardedAd = [self retrieveRewardedAdForAdUnitIdentifier: adUnitIdentifier];
    [rewardedAd loadAd];
}

- (BOOL)isRewardedAdReadyWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    MARewardedAd *rewardedAd = [self retrieveRewardedAdForAdUnitIdentifier: adUnitIdentifier];
    return [rewardedAd isReady];
}

- (void)showRewardedAdWithAdUnitIdentifier:(NSString *)adUnitIdentifier placement:(NSString *)placement
{
    MARewardedAd *rewardedAd = [self retrieveRewardedAdForAdUnitIdentifier: adUnitIdentifier];
    [rewardedAd showAdForPlacement: placement];
}

- (void)setRewardedAdExtraParameterForAdUnitIdentifier:(NSString *)adUnitIdentifier key:(NSString *)key value:(nullable NSString *)value
{
    MARewardedAd *rewardedAd = [self retrieveRewardedAdForAdUnitIdentifier: adUnitIdentifier];
    [rewardedAd setExtraParameterForKey: key value: value];
}

#pragma mark - Rewarded Interstitials

- (void)loadRewardedInterstitialAdWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    MARewardedInterstitialAd *rewardedInterstitialAd = [self retrieveRewardedInterstitialAdForAdUnitIdentifier: adUnitIdentifier];
    [rewardedInterstitialAd loadAd];
}

- (BOOL)isRewardedInterstitialAdReadyWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    MARewardedInterstitialAd *rewardedInterstitialAd = [self retrieveRewardedInterstitialAdForAdUnitIdentifier: adUnitIdentifier];
    return [rewardedInterstitialAd isReady];
}

- (void)showRewardedInterstitialAdWithAdUnitIdentifier:(NSString *)adUnitIdentifier placement:(NSString *)placement
{
    MARewardedInterstitialAd *rewardedInterstitialAd = [self retrieveRewardedInterstitialAdForAdUnitIdentifier: adUnitIdentifier];
    [rewardedInterstitialAd showAdForPlacement: placement];
}

- (void)setRewardedInterstitialAdExtraParameterForAdUnitIdentifier:(NSString *)adUnitIdentifier key:(NSString *)key value:(nullable NSString *)value
{
    MARewardedInterstitialAd *rewardedInterstitialAd = [self retrieveRewardedInterstitialAdForAdUnitIdentifier: adUnitIdentifier];
    [rewardedInterstitialAd setExtraParameterForKey: key value: value];
}

#pragma mark - Event Tracking

- (void)trackEvent:(NSString *)event parameters:(NSString *)parameters
{
    NSDictionary<NSString *, NSString *> *deserializedParameters = [self deserializeParameters: parameters];
    [self.sdk.eventService trackEvent: event parameters: deserializedParameters];
}

#pragma mark - Ad Info

- (NSString *)adInfoForAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    if ( adUnitIdentifier.length == 0 ) return @"";
    
    MAAd *ad;
    @synchronized ( self.adInfoDictLock )
    {
        ad = self.adInfoDict[adUnitIdentifier];
    }
    
    if ( !ad ) return @"";
    
    return [MAUnityAdManager propsStrFromDictionary: @{@"adUnitId" : adUnitIdentifier,
                                                       @"networkName" : ad.networkName}];
}

#pragma mark - Ad Callbacks

- (void)didLoadAd:(MAAd *)ad
{
    NSString *name;
    MAAdFormat *adFormat = ad.format;
    if ( [adFormat isAdViewAd] )
    {
        MAAdView *adView = [self retrieveAdViewForAdUnitIdentifier: ad.adUnitIdentifier adFormat: adFormat];
        // An ad is now being shown, enable user interaction.
        adView.userInteractionEnabled = YES;
        
        name = ( MAAdFormat.mrec == adFormat ) ? @"OnMRecAdLoadedEvent" : @"OnBannerAdLoadedEvent";
        [self positionAdViewForAd: ad];
        
        // Do not auto-refresh by default if the ad view is not showing yet (e.g. first load during app launch and publisher does not automatically show banner upon load success)
        // We will resume auto-refresh in -[MAUnityAdManager showBannerWithAdUnitIdentifier:].
        if ( adView && [adView isHidden] )
        {
            [adView stopAutoRefresh];
        }
    }
    else if ( MAAdFormat.interstitial == adFormat )
    {
        name = @"OnInterstitialLoadedEvent";
    }
    else if ( MAAdFormat.rewarded == adFormat )
    {
        name = @"OnRewardedAdLoadedEvent";
    }
    else if ( MAAdFormat.rewardedInterstitial == adFormat )
    {
        name = @"OnRewardedInterstitialAdLoadedEvent";
    }
    else
    {
        [self logInvalidAdFormat: adFormat];
        return;
    }
    
    @synchronized ( self.adInfoDictLock )
    {
        self.adInfoDict[ad.adUnitIdentifier] = ad;
    }
    
    [MAUnityAdManager forwardUnityEventWithArgs: @{@"name": name,
                                                   @"adUnitId": ad.adUnitIdentifier}];
}

- (void)didFailToLoadAdForAdUnitIdentifier:(NSString *)adUnitIdentifier withErrorCode:(NSInteger)errorCode
{
    if ( !adUnitIdentifier )
    {
        [self log: @"adUnitIdentifier cannot be nil from %@", [NSThread callStackSymbols]];
        return;
    }
    
    NSString *name;
    if ( self.adViews[adUnitIdentifier] )
    {
        name = ( MAAdFormat.mrec == self.adViewAdFormats[adUnitIdentifier] ) ? @"OnMRecAdLoadFailedEvent" : @"OnBannerAdLoadFailedEvent";
    }
    else if ( self.interstitials[adUnitIdentifier] )
    {
        name = @"OnInterstitialLoadFailedEvent";
    }
    else if ( self.rewardedAds[adUnitIdentifier] )
    {
        name = @"OnRewardedAdLoadFailedEvent";
    }
    else if ( self.rewardedInterstitialAds[adUnitIdentifier] )
    {
        name = @"OnRewardedInterstitialAdLoadFailedEvent";
    }
    else
    {
        [self log: @"invalid adUnitId from %@", [NSThread callStackSymbols]];
        return;
    }
    
    @synchronized ( self.adInfoDictLock )
    {
        [self.adInfoDict removeObjectForKey: adUnitIdentifier];
    }
    
    NSString *errorCodeStr = [@(errorCode) stringValue];
    [MAUnityAdManager forwardUnityEventWithArgs: @{@"name": name,
                                                   @"adUnitId": adUnitIdentifier,
                                                   @"errorCode": errorCodeStr}];
}

- (void)didClickAd:(MAAd *)ad
{
    NSString *name;
    MAAdFormat *adFormat = ad.format;
    if ( MAAdFormat.banner == adFormat || MAAdFormat.leader == adFormat )
    {
        name = @"OnBannerAdClickedEvent";
    }
    else if ( MAAdFormat.mrec == adFormat )
    {
        name = @"OnMRecAdClickedEvent";
    }
    else if ( MAAdFormat.interstitial == adFormat )
    {
        name = @"OnInterstitialClickedEvent";
    }
    else if ( MAAdFormat.rewarded == adFormat )
    {
        name = @"OnRewardedAdClickedEvent";
    }
    else if ( MAAdFormat.rewardedInterstitial == adFormat )
    {
        name = @"OnRewardedInterstitialAdClickedEvent";
    }
    else
    {
        [self logInvalidAdFormat: adFormat];
        return;
    }
    
    [MAUnityAdManager forwardUnityEventWithArgs: @{@"name": name,
                                                   @"adUnitId": ad.adUnitIdentifier}];
}

- (void)didDisplayAd:(MAAd *)ad
{
    // BMLs do not support [DISPLAY] events in Unity
    MAAdFormat *adFormat = ad.format;
    if ( ![adFormat isFullscreenAd] ) return;
    
    NSString *name;
    if ( MAAdFormat.interstitial == adFormat )
    {
        name = @"OnInterstitialDisplayedEvent";
    }
    else if ( MAAdFormat.rewarded == adFormat )
    {
        name = @"OnRewardedAdDisplayedEvent";
    }
    else // rewarded inters
    {
        name = @"OnRewardedInterstitialAdDisplayedEvent";
    }
    
    [MAUnityAdManager forwardUnityEventWithArgs: @{@"name": name,
                                                   @"adUnitId": ad.adUnitIdentifier}];
}

- (void)didFailToDisplayAd:(MAAd *)ad withErrorCode:(NSInteger)errorCode
{
    // BMLs do not support [DISPLAY] events in Unity
    MAAdFormat *adFormat = ad.format;
    if ( ![adFormat isFullscreenAd] ) return;
    
    NSString *name;
    if ( MAAdFormat.interstitial == adFormat )
    {
        name = @"OnInterstitialAdFailedToDisplayEvent";
    }
    else if ( MAAdFormat.rewarded == adFormat )
    {
        name = @"OnRewardedAdFailedToDisplayEvent";
    }
    else // rewarded inters
    {
        name = @"OnRewardedInterstitialAdFailedToDisplayEvent";
    }
    
    NSString *errorCodeStr = [@(errorCode) stringValue];
    [MAUnityAdManager forwardUnityEventWithArgs: @{@"name": name,
                                                   @"adUnitId": ad.adUnitIdentifier,
                                                   @"errorCode": errorCodeStr}];
}

- (void)didHideAd:(MAAd *)ad
{
    // BMLs do not support [HIDDEN] events in Unity
    MAAdFormat *adFormat = ad.format;
    if ( ![adFormat isFullscreenAd] ) return;
    
    NSString *name;
    if ( MAAdFormat.interstitial == adFormat )
    {
        name = @"OnInterstitialHiddenEvent";
    }
    else if ( MAAdFormat.rewarded == adFormat )
    {
        name = @"OnRewardedAdHiddenEvent";
    }
    else // rewarded inters
    {
        name = @"OnRewardedInterstitialAdHiddenEvent";
    }
    
    [MAUnityAdManager forwardUnityEventWithArgs: @{@"name": name,
                                                   @"adUnitId": ad.adUnitIdentifier}];
}

- (void)didCollapseAd:(MAAd *)ad
{
    MAAdFormat *adFormat = ad.format;
    if ( ![adFormat isAdViewAd] )
    {
        [self logInvalidAdFormat: adFormat];
        return;
    }
    
    [MAUnityAdManager forwardUnityEventWithArgs: @{@"name": ( MAAdFormat.mrec == adFormat ) ? @"OnMRecAdCollapsedEvent" : @"OnBannerAdCollapsedEvent",
                                                   @"adUnitId": ad.adUnitIdentifier}];
}

- (void)didExpandAd:(MAAd *)ad
{
    MAAdFormat *adFormat = ad.format;
    if ( ![adFormat isAdViewAd] )
    {
        [self logInvalidAdFormat: adFormat];
        return;
    }
    
    [MAUnityAdManager forwardUnityEventWithArgs: @{@"name": ( MAAdFormat.mrec == adFormat ) ? @"OnMRecAdExpandedEvent" : @"OnBannerAdExpandedEvent",
                                                   @"adUnitId": ad.adUnitIdentifier}];
}

- (void)didCompleteRewardedVideoForAd:(MAAd *)ad
{
    // This event is not forwarded
}

- (void)didStartRewardedVideoForAd:(MAAd *)ad
{
    // This event is not forwarded
}

- (void)didRewardUserForAd:(MAAd *)ad withReward:(MAReward *)reward
{
    MAAdFormat *adFormat = ad.format;
    if ( adFormat != MAAdFormat.rewarded && adFormat != MAAdFormat.rewardedInterstitial )
    {
        [self logInvalidAdFormat: adFormat];
        return;
    }
    
    NSString *rewardLabel = reward ? reward.label : @"";
    NSInteger rewardAmountInt = reward ? reward.amount : 0;
    NSString *rewardAmount = [@(rewardAmountInt) stringValue];
    
    NSString *name = (adFormat == MAAdFormat.rewarded) ? @"OnRewardedAdReceivedRewardEvent" : @"OnRewardedInterstitialAdReceivedRewardEvent";
    
    [MAUnityAdManager forwardUnityEventWithArgs: @{@"name": name,
                                                   @"adUnitId": ad.adUnitIdentifier,
                                                   @"rewardLabel": rewardLabel,
                                                   @"rewardAmount": rewardAmount}];
}

#pragma mark - Internal Methods

- (void)createAdViewWithAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat atPosition:(NSString *)adViewPosition
{
    [self log: @"Creating %@ with ad unit identifier \"%@\" and position: \"%@\"", adFormat, adUnitIdentifier, adViewPosition];
    
    // Retrieve ad view from the map
    MAAdView *adView = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat atPosition: adViewPosition];
    adView.hidden = YES;
    self.safeAreaBackground.hidden = YES;
    
    // Position ad view immediately so if publisher sets color before ad loads, it will not be the size of the screen
    self.adViewAdFormats[adUnitIdentifier] = adFormat;
    [self positionAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
    
    [adView loadAd];
    
    // The publisher may have requested to show the banner before it was created. Now that the banner is created, show it.
    if ( [self.adUnitIdentifiersToShowAfterCreate containsObject: adUnitIdentifier] )
    {
        [self showAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
        [self.adUnitIdentifiersToShowAfterCreate removeObject: adUnitIdentifier];
    }
}

- (void)setAdViewBackgroundColorForAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat hexColorCode:(NSString *)hexColorCode
{
    [self log: @"Setting %@ with ad unit identifier \"%@\" to color: \"%@\"", adFormat, adUnitIdentifier, hexColorCode];
    
    // In some cases, black color may get redrawn on each frame update, resulting in an undesired flicker
    if ( [hexColorCode containsString: @"FF000000"] ) hexColorCode = @"FF000001";
    
    UIColor *convertedColor = [UIColor al_colorWithHexString: hexColorCode];
    
    MAAdView *view = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
    self.publisherBannerBackgroundColor = convertedColor;
    self.safeAreaBackground.backgroundColor = view.backgroundColor = convertedColor;
}

- (void)setAdViewPlacement:(nullable NSString *)placement forAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    [self log: @"Setting placement \"%@\" for \"%@\" with ad unit identifier \"%@\"", placement, adFormat, adUnitIdentifier];
    
    MAAdView *adView = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
    adView.placement = placement;
}

- (void)updateAdViewPosition:(NSString *)adViewPosition forAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    // Check if the previous position is same as the new position. If so, no need to update the position again.
    NSString *previousPosition = self.adViewPositions[adUnitIdentifier];
    if ( !adViewPosition || [adViewPosition isEqualToString: previousPosition] ) return;
    
    self.adViewPositions[adUnitIdentifier] = adViewPosition;
    [self positionAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
}

- (void)setAdViewExtraParameterForAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat key:(NSString *)key value:(nullable NSString *)value
{
    [self log: @"Setting %@ extra with key: \"%@\" value: \"%@\"", adFormat, key, value];
    
    MAAdView *adView = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
    [adView setExtraParameterForKey: key value: value];
    
    if (  [@"force_banner" isEqualToString: key] && MAAdFormat.mrec != adFormat )
    {
        // Handle local changes as needed
        MAAdFormat *adFormat;
        
        BOOL shouldForceBanner = [NSNumber al_numberWithString: value].boolValue;
        if ( shouldForceBanner )
        {
            adFormat = MAAdFormat.banner;
        }
        else
        {
            adFormat = DEVICE_SPECIFIC_ADVIEW_AD_FORMAT;
        }
        
        self.adViewAdFormats[adUnitIdentifier] = adFormat;
        [self positionAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
    }
}

- (void)showAdViewWithAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    [self log: @"Showing %@ with ad unit identifier \"%@\"", adFormat, adUnitIdentifier];
    
    MAAdView *view = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
    if ( !view )
    {
        [self log: @"%@ does not exist for ad unit identifier %@.", adFormat, adUnitIdentifier];
        
        // The adView has not yet been created. Store the ad unit ID, so that it can be displayed once the banner has been created.
        [self.adUnitIdentifiersToShowAfterCreate addObject: adUnitIdentifier];
    }
    
    self.safeAreaBackground.hidden = NO;
    view.hidden = NO;
    
    [view startAutoRefresh];
}

- (void)hideAdViewWithAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    [self log: @"Hiding %@ with ad unit identifier \"%@\"", adFormat, adUnitIdentifier];
    [self.adUnitIdentifiersToShowAfterCreate removeObject: adUnitIdentifier];
    
    MAAdView *view = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
    view.hidden = YES;
    self.safeAreaBackground.hidden = YES;
    
    [view stopAutoRefresh];
}

- (void)destroyAdViewWithAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    [self log: @"Destroying %@ with ad unit identifier \"%@\"", adFormat, adUnitIdentifier];
    
    MAAdView *view = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
    view.delegate = nil;
    
    [view removeFromSuperview];
    
    [self.adViews removeObjectForKey: adUnitIdentifier];
    [self.adViewPositions removeObjectForKey: adUnitIdentifier];
    [self.adViewAdFormats removeObjectForKey: adUnitIdentifier];
    [self.verticalAdViewFormats removeObjectForKey: adUnitIdentifier];
}

- (void)logInvalidAdFormat:(MAAdFormat *)adFormat
{
    [self log: @"invalid ad format: %@, from %@", adFormat, [NSThread callStackSymbols]];
}

- (void)log:(NSString *)format, ...
{
    va_list valist;
    va_start(valist, format);
    NSString *message = [[NSString alloc] initWithFormat: format arguments: valist];
    va_end(valist);
    
    NSLog(@"[%@] [%@] %@", SDK_TAG, TAG, message);
}

- (MAInterstitialAd *)retrieveInterstitialForAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    MAInterstitialAd *result = self.interstitials[adUnitIdentifier];
    if ( !result )
    {
        result = [[MAInterstitialAd alloc] initWithAdUnitIdentifier: adUnitIdentifier sdk: self.sdk];
        result.delegate = self;
        
        self.interstitials[adUnitIdentifier] = result;
    }
    
    return result;
}

- (MARewardedAd *)retrieveRewardedAdForAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    MARewardedAd *result = self.rewardedAds[adUnitIdentifier];
    if ( !result )
    {
        result = [MARewardedAd sharedWithAdUnitIdentifier: adUnitIdentifier sdk: self.sdk];
        result.delegate = self;
        
        self.rewardedAds[adUnitIdentifier] = result;
    }
    
    return result;
}

- (MARewardedInterstitialAd *)retrieveRewardedInterstitialAdForAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    MARewardedInterstitialAd *result = self.rewardedInterstitialAds[adUnitIdentifier];
    if ( !result )
    {
        result = [[MARewardedInterstitialAd alloc] initWithAdUnitIdentifier: adUnitIdentifier sdk: self.sdk];
        result.delegate = self;
        
        self.rewardedInterstitialAds[adUnitIdentifier] = result;
    }
    
    return result;
}

- (MAAdView *)retrieveAdViewForAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    return [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat atPosition: nil];
}

- (MAAdView *)retrieveAdViewForAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat atPosition:(NSString *)adViewPosition
{
    MAAdView *result = self.adViews[adUnitIdentifier];
    if ( !result && adViewPosition )
    {
        result = [[MAAdView alloc] initWithAdUnitIdentifier: adUnitIdentifier adFormat: adFormat sdk: self.sdk];
        // There is a Unity bug where if an empty UIView is on screen with user interaction enabled, and a user interacts with it, it just passes the events to random parts of the screen.
        result.userInteractionEnabled = NO;
        result.translatesAutoresizingMaskIntoConstraints = NO;
        result.delegate = self;
        
        self.adViews[adUnitIdentifier] = result;
        self.adViewPositions[adUnitIdentifier] = adViewPosition;
        
        UIViewController *rootViewController = [MAUnityAdManager unityViewController];
        [rootViewController.view addSubview: result];
    }
    
    return result;
}

- (void)positionAdViewForAd:(MAAd *)ad
{
    [self positionAdViewForAdUnitIdentifier: ad.adUnitIdentifier adFormat: ad.format];
}

- (void)positionAdViewForAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    MAAdView *adView = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
    NSString *adViewPosition = self.adViewPositions[adUnitIdentifier];
    
    UIView *superview = adView.superview;
    if ( !superview ) return;
    
    // Deactivate any previous constraints and reset rotation so that the banner can be positioned again.
    NSArray<NSLayoutConstraint *> *activeConstraints = self.adViewConstraints[adUnitIdentifier];
    [NSLayoutConstraint deactivateConstraints: activeConstraints];
    adView.transform = CGAffineTransformIdentity;
    [self.verticalAdViewFormats removeObjectForKey: adUnitIdentifier];
    
    // Ensure superview contains the safe area background.
    if ( ![superview.subviews containsObject: self.safeAreaBackground] )
    {
        [self.safeAreaBackground removeFromSuperview];
        [superview insertSubview: self.safeAreaBackground belowSubview: adView];
    }
    
    // Deactivate any previous constraints and reset visibility state so that the safe area background can be positioned again.
    [NSLayoutConstraint deactivateConstraints: self.safeAreaBackground.constraints];
    self.safeAreaBackground.hidden = NO;
    
    CGSize adViewSize = [MAUnityAdManager adViewSizeForAdFormat: adFormat];
    
    // All positions have constant height
    NSMutableArray<NSLayoutConstraint*> *constraints = [NSMutableArray arrayWithObject: [adView.heightAnchor constraintEqualToConstant: adViewSize.height]];
    
    UILayoutGuide *layoutGuide;
    if ( @available(iOS 11.0, *) )
    {
        layoutGuide = superview.safeAreaLayoutGuide;
    }
    else
    {
        layoutGuide = superview.layoutMarginsGuide;
    }
    
    // If top of bottom center, stretch width of screen
    if ( [adViewPosition isEqual: @"top_center"] || [adViewPosition isEqual: @"bottom_center"] )
    {
        // If publisher actually provided a banner background color, span the banner across the realm
        if ( self.publisherBannerBackgroundColor && adFormat != MAAdFormat.mrec )
        {
            [constraints addObjectsFromArray: @[[self.safeAreaBackground.leftAnchor constraintEqualToAnchor: superview.leftAnchor],
                                                [self.safeAreaBackground.rightAnchor constraintEqualToAnchor: superview.rightAnchor]]];
            
            if ( [adViewPosition isEqual: @"top_center"] )
            {
                [constraints addObjectsFromArray: @[[adView.topAnchor constraintEqualToAnchor: layoutGuide.topAnchor],
                                                    [adView.leftAnchor constraintEqualToAnchor: superview.leftAnchor],
                                                    [adView.rightAnchor constraintEqualToAnchor: superview.rightAnchor]]];
                [constraints addObjectsFromArray: @[[self.safeAreaBackground.topAnchor constraintEqualToAnchor: superview.topAnchor],
                                                    [self.safeAreaBackground.bottomAnchor constraintEqualToAnchor: adView.topAnchor]]];
            }
            else // BottomCenter
            {
                [constraints addObjectsFromArray: @[[adView.bottomAnchor constraintEqualToAnchor: layoutGuide.bottomAnchor],
                                                    [adView.leftAnchor constraintEqualToAnchor: superview.leftAnchor],
                                                    [adView.rightAnchor constraintEqualToAnchor: superview.rightAnchor]]];
                [constraints addObjectsFromArray: @[[self.safeAreaBackground.topAnchor constraintEqualToAnchor: adView.bottomAnchor],
                                                    [self.safeAreaBackground.bottomAnchor constraintEqualToAnchor: superview.bottomAnchor]]];
            }
        }
        // If pub does not have a background color set - we shouldn't span the banner the width of the realm (there might be user-interactable UI on the sides)
        else
        {
            self.safeAreaBackground.hidden = YES;
            
            // Assign constant width of 320 or 728
            [constraints addObject: [adView.widthAnchor constraintEqualToConstant: adViewSize.width]];
            [constraints addObject: [adView.centerXAnchor constraintEqualToAnchor: layoutGuide.centerXAnchor]];
            
            if ( [adViewPosition isEqual: @"top_center"] )
            {
                [constraints addObject: [adView.topAnchor constraintEqualToAnchor: layoutGuide.topAnchor]];
            }
            else // BottomCenter
            {
                [constraints addObject: [adView.bottomAnchor constraintEqualToAnchor: layoutGuide.bottomAnchor]];
            }
        }
    }
    // Check if the publisher wants vertical banners.
    else if ( [adViewPosition isEqual: @"center_left"] || [adViewPosition isEqual: @"center_right"] )
    {
        if ( MAAdFormat.mrec == adFormat )
        {
            [constraints addObject: [adView.widthAnchor constraintEqualToConstant: adViewSize.width]];
            
            if ( [adViewPosition isEqual: @"center_left"] )
            {
                [constraints addObjectsFromArray: @[[adView.centerYAnchor constraintEqualToAnchor: layoutGuide.centerYAnchor],
                                                    [adView.leftAnchor constraintEqualToAnchor: superview.leftAnchor]]];
                
                [constraints addObjectsFromArray: @[[self.safeAreaBackground.rightAnchor constraintEqualToAnchor: layoutGuide.leftAnchor],
                                                    [self.safeAreaBackground.leftAnchor constraintEqualToAnchor: superview.leftAnchor]]];
            }
            else // center_right
            {
                [constraints addObjectsFromArray: @[[adView.centerYAnchor constraintEqualToAnchor: layoutGuide.centerYAnchor],
                                                    [adView.rightAnchor constraintEqualToAnchor: superview.rightAnchor]]];
                
                [constraints addObjectsFromArray: @[[self.safeAreaBackground.leftAnchor constraintEqualToAnchor: layoutGuide.rightAnchor],
                                                    [self.safeAreaBackground.rightAnchor constraintEqualToAnchor: superview.rightAnchor]]];
            }
        }
        else
        {
            /* Align the center of the view such that when rotated it snaps into place.
             *
             *                  +---+---+-------+
             *                  |   |           |
             *                  |   |           |
             *                  |   |           |
             *                  |   |           |
             *                  |   |           |
             *                  |   |           |
             *    +-------------+---+-----------+--+
             *    |             | + |   +       |  |
             *    +-------------+---+-----------+--+
             *                  <+> |           |
             *                  |+  |           |
             *                  ||  |           |
             *                  ||  |           |
             *                  ||  |           |
             *                  ||  |           |
             *                  +|--+-----------+
             *                   v
             *            Banner Half Height
             */
            self.safeAreaBackground.hidden = YES;
            
            adView.transform = CGAffineTransformRotate(CGAffineTransformIdentity, M_PI_2);
            
            CGFloat width;
            // If the publiser has a background color set - set the width to the longest side of the screen, to span the ad across the screen after it is rotated.
            if ( self.publisherBannerBackgroundColor )
            {
                CGSize screenSize = UIScreen.mainScreen.bounds.size;
                width = MAX( screenSize.height, screenSize.width );
            }
            // Otherwise - we shouldn't span the banner the width of the realm (there might be user-interactable UI on the sides)
            else
            {
                width = adViewSize.width;
            }
            [constraints addObject: [adView.widthAnchor constraintEqualToConstant: width]];
            
            // Set constraints such that the center of the banner aligns with the center left or right as needed. That way, once rotated, the banner snaps into place.
            [constraints addObject: [adView.centerYAnchor constraintEqualToAnchor: superview.centerYAnchor]];
            
            // Place the center of the banner half the height of the banner away from the side. If we align the center exactly with the left/right anchor, only half the banner will be visible.
            CGFloat bannerHalfHeight = adViewSize.height / 2.0;
            UIInterfaceOrientation orientation = [UIApplication sharedApplication].statusBarOrientation;
            if ( [adViewPosition isEqual: @"center_left"] )
            {
                NSLayoutAnchor *anchor = ( orientation == UIInterfaceOrientationLandscapeRight ) ? layoutGuide.leftAnchor : superview.leftAnchor;
                [constraints addObject: [adView.centerXAnchor constraintEqualToAnchor: anchor constant: bannerHalfHeight]];
            }
            else // CenterRight
            {
                NSLayoutAnchor *anchor = ( orientation == UIInterfaceOrientationLandscapeLeft ) ? layoutGuide.rightAnchor : superview.rightAnchor;
                [constraints addObject: [adView.centerXAnchor constraintEqualToAnchor: anchor constant: -bannerHalfHeight]];
            }
            
            // Store the ad view with format, so that it can be updated when the orientation changes.
            self.verticalAdViewFormats[adUnitIdentifier] = adFormat;
        }
    }
    // Otherwise, publisher will likely construct his own views around the adview
    else
    {
        self.safeAreaBackground.hidden = YES;
        
        // Assign constant width of 320 or 728
        [constraints addObject: [adView.widthAnchor constraintEqualToConstant: adViewSize.width]];
        
        if ( [adViewPosition isEqual: @"top_left"] )
        {
            [constraints addObjectsFromArray: @[[adView.topAnchor constraintEqualToAnchor: layoutGuide.topAnchor],
                                                [adView.leftAnchor constraintEqualToAnchor: superview.leftAnchor]]];
        }
        else if ( [adViewPosition isEqual: @"top_right"] )
        {
            [constraints addObjectsFromArray: @[[adView.topAnchor constraintEqualToAnchor: layoutGuide.topAnchor],
                                                [adView.rightAnchor constraintEqualToAnchor: superview.rightAnchor]]];
        }
        else if ( [adViewPosition isEqual: @"centered"] )
        {
            [constraints addObjectsFromArray: @[[adView.centerXAnchor constraintEqualToAnchor: layoutGuide.centerXAnchor],
                                                [adView.centerYAnchor constraintEqualToAnchor: layoutGuide.centerYAnchor]]];
        }
        else if ( [adViewPosition isEqual: @"bottom_left"] )
        {
            [constraints addObjectsFromArray: @[[adView.bottomAnchor constraintEqualToAnchor: layoutGuide.bottomAnchor],
                                                [adView.leftAnchor constraintEqualToAnchor: superview.leftAnchor]]];
        }
        else if ( [adViewPosition isEqual: @"bottom_right"] )
        {
            [constraints addObjectsFromArray: @[[adView.bottomAnchor constraintEqualToAnchor: layoutGuide.bottomAnchor],
                                                [adView.rightAnchor constraintEqualToAnchor: superview.rightAnchor]]];
        }
    }
    
    self.adViewConstraints[adUnitIdentifier] = constraints;
    
    [NSLayoutConstraint activateConstraints: constraints];
}

+ (UIViewController *)unityViewController
{
    return [[[UIApplication sharedApplication] keyWindow] rootViewController];
}

+ (void)forwardUnityEventWithArgs:(NSDictionary<NSString *, NSString *> *)args
{
#if !IS_TEST_APP
    NSString *serializedParameters = [self propsStrFromDictionary: args];
    UnitySendMessage("MaxSdkCallbacks", "ForwardEvent", serializedParameters.UTF8String);
#endif
}

+ (NSString *)propsStrFromDictionary:(NSDictionary<NSString *, NSString *> *)dict
{
    NSMutableString *result = [[NSMutableString alloc] initWithCapacity: 64];
    [dict enumerateKeysAndObjectsUsingBlock:^(NSString *key, NSString *obj, BOOL *stop)
     {
        [result appendString: key];
        [result appendString: @"="];
        [result appendString: obj];
        [result appendString: @"\n"];
    }];
    
    return result;
}

- (NSDictionary<NSString *, NSString *> *)deserializeParameters:(NSString *)serialized
{
    if ( serialized.length > 0 )
    {
        NSArray<NSString *> *keyValuePairs = [serialized componentsSeparatedByString: ALSerializeKeyValuePairSeparator]; // ["key-1<FS>value-1", "key-2<FS>value-2", "key-3<FS>value-3"]
        NSMutableDictionary<NSString *, NSString *> *deserialized = [NSMutableDictionary dictionaryWithCapacity: keyValuePairs.count];
        
        for ( NSString *keyValuePair in keyValuePairs )
        {
            NSArray<NSString *> *splitPair = [keyValuePair componentsSeparatedByString: ALSerializeKeyValueSeparator];
            if ( splitPair.count == 2 )
            {
                NSString *key = splitPair[0];
                NSString *value = splitPair[1];
                
                // Store in deserialized dictionary
                deserialized[key] = value;
            }
        }
        
        return deserialized;
    }
    else
    {
        return @{};
    }
}

- (ALSdkSettings *)generateSDKSettingsForAdUnitIdentifiers:(NSString *)serializedAdUnitIdentifiers metaData:(NSString *)serializedMetaData
{
    ALSdkSettings *settings = [[ALSdkSettings alloc] init];
    settings.initializationAdUnitIdentifiers = [serializedAdUnitIdentifiers componentsSeparatedByString: @","];
    
    NSDictionary<NSString *, NSString *> *unityMetaData = [self deserializeParameters: serializedMetaData];
    
    // Set the meta data to settings.
    if ( ALSdk.versionCode >= 61201 )
    {
        NSMutableDictionary<NSString *, NSString *> *metaDataDict = [settings valueForKey: @"metaData"];
        for ( NSString *key in unityMetaData )
        {
            metaDataDict[key] = unityMetaData[key];
        }
        
        return settings;
    }
    
    return settings;
}

+ (CGSize)adViewSizeForAdFormat:(MAAdFormat *)adFormat
{
    if ( MAAdFormat.leader == adFormat )
    {
        return CGSizeMake(728.0f, 90.0f);
    }
    else if ( MAAdFormat.banner == adFormat )
    {
        return CGSizeMake(320.0f, 50.0f);
    }
    else if ( MAAdFormat.mrec == adFormat )
    {
        return CGSizeMake(300.0f, 250.0f);
    }
    else
    {
        [NSException raise: NSInvalidArgumentException format: @"Invalid ad format"];
        return CGSizeZero;
    }
}

#pragma mark - Variable Service (Deprecated)

- (void)loadVariables
{
    [self.sdk.variableService loadVariables];
}

- (void)variableService:(ALVariableService *)variableService didUpdateVariables:(NSDictionary<NSString *, id> *)variables
{
    [MAUnityAdManager forwardUnityEventWithArgs: @{@"name": @"OnVariablesUpdatedEvent"}];
}

@end
