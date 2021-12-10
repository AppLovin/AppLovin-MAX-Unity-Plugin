//
//  MAUnityAdManager.m
//  AppLovin MAX Unity Plugin
//

#import "MAUnityAdManager.h"

#define VERSION @"4.3.12"

#define KEY_WINDOW [UIApplication sharedApplication].keyWindow
#define DEVICE_SPECIFIC_ADVIEW_AD_FORMAT ([[UIDevice currentDevice] userInterfaceIdiom] == UIUserInterfaceIdiomPad) ? MAAdFormat.leader : MAAdFormat.banner
#define IS_VERTICAL_BANNER_POSITION(_POS) ( [@"center_left" isEqual: adViewPosition] || [@"center_right" isEqual: adViewPosition] )
#define DEGREES_TO_RADIANS(angle) ((angle) / 180.0 * M_PI)

#ifdef __cplusplus
extern "C" {
#endif
    
    // UnityAppController.mm
    UIViewController* UnityGetGLViewController(void);
    UIWindow* UnityGetMainWindow(void);
    
    // life cycle management
    void UnityPause(int pause);
    void UnitySendMessage(const char* obj, const char* method, const char* msg);
    
    void max_unity_dispatch_on_main_thread(dispatch_block_t block)
    {
        if ( block )
        {
            if ( [NSThread isMainThread] )
            {
                block();
            }
            else
            {
                dispatch_async(dispatch_get_main_queue(), block);
            }
        }
    }
#ifdef __cplusplus
}
#endif

@interface MAUnityAdManager()<MAAdDelegate, MAAdViewAdDelegate, MARewardedAdDelegate, MAAdRevenueDelegate, ALVariableServiceDelegate>

// Parent Fields
@property (nonatomic, weak) ALSdk *sdk;

// Fullscreen Ad Fields
@property (nonatomic, strong) NSMutableDictionary<NSString *, MAInterstitialAd *> *interstitials;
@property (nonatomic, strong) NSMutableDictionary<NSString *, MARewardedAd *> *rewardedAds;
@property (nonatomic, strong) NSMutableDictionary<NSString *, MARewardedInterstitialAd *> *rewardedInterstitialAds;

// AdView Fields
@property (nonatomic, strong) NSMutableDictionary<NSString *, MAAdView *> *adViews;
@property (nonatomic, strong) NSMutableDictionary<NSString *, MAAdFormat *> *adViewAdFormats;
@property (nonatomic, strong) NSMutableDictionary<NSString *, NSString *> *adViewPositions;
@property (nonatomic, strong) NSMutableDictionary<NSString *, NSValue *> *adViewOffsets;
@property (nonatomic, strong) NSMutableDictionary<NSString *, NSNumber *> *adViewWidths;
@property (nonatomic, strong) NSMutableDictionary<NSString *, NSNumber *> *crossPromoAdViewHeights;
@property (nonatomic, strong) NSMutableDictionary<NSString *, NSNumber *> *crossPromoAdViewRotations;
@property (nonatomic, strong) NSMutableDictionary<NSString *, MAAdFormat *> *verticalAdViewFormats;
@property (nonatomic, strong) NSMutableDictionary<NSString *, NSArray<NSLayoutConstraint *> *> *adViewConstraints;
@property (nonatomic, strong) NSMutableDictionary<NSString *, NSMutableDictionary<NSString *, NSString *> *> *adViewExtraParametersToSetAfterCreate;
@property (nonatomic, strong) NSMutableArray<NSString *> *adUnitIdentifiersToShowAfterCreate;
@property (nonatomic, strong) NSMutableSet<NSString *> *disabledAdaptiveBannerAdUnitIdentifiers;
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

@interface NSString (ALUtils)
@property (assign, readonly, getter=al_isValidString) BOOL al_validString;
@end

@interface MAAdFormat (ALUtils)
@property (nonatomic, assign, readonly, getter=isFullscreenAd) BOOL fullscreenAd;
@property (nonatomic, assign, readonly, getter=isAdViewAd) BOOL adViewAd;
@end

@implementation MAUnityAdManager
static NSString *const SDK_TAG = @"AppLovinSdk";
static NSString *const TAG = @"MAUnityAdManager";
static NSString *const DEFAULT_AD_VIEW_POSITION = @"top_left";
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
        self.adViewOffsets = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.adViewWidths = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.crossPromoAdViewHeights = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.crossPromoAdViewRotations = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.verticalAdViewFormats = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.adViewConstraints = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.adViewExtraParametersToSetAfterCreate = [NSMutableDictionary dictionaryWithCapacity: 1];
        self.adUnitIdentifiersToShowAfterCreate = [NSMutableArray arrayWithCapacity: 2];
        self.disabledAdaptiveBannerAdUnitIdentifiers = [NSMutableSet setWithCapacity: 2];
        self.adInfoDict = [NSMutableDictionary dictionary];
        self.adInfoDictLock = [[NSObject alloc] init];
        
        max_unity_dispatch_on_main_thread(^{
            self.safeAreaBackground = [[UIView alloc] init];
            self.safeAreaBackground.hidden = YES;
            self.safeAreaBackground.backgroundColor = UIColor.clearColor;
            self.safeAreaBackground.translatesAutoresizingMaskIntoConstraints = NO;
            self.safeAreaBackground.userInteractionEnabled = NO;
            
            UIViewController *rootViewController = [self unityViewController];
            [rootViewController.view addSubview: self.safeAreaBackground];
        });
        
        // Enable orientation change listener, so that the position can be updated for vertical banners.
        [[NSNotificationCenter defaultCenter] addObserverForName: UIDeviceOrientationDidChangeNotification
                                                          object: nil
                                                           queue: [NSOperationQueue mainQueue]
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
        NSString *appTrackingStatus = @(configuration.appTrackingTransparencyStatus).stringValue; // Deliberately name it `appTrackingStatus` to be a bit more generic (in case Android introduces a similar concept)
        [MAUnityAdManager forwardUnityEventWithArgs: @{@"name" : @"OnSdkInitializedEvent",
                                                       @"consentDialogState" : consentDialogStateStr,
                                                       @"countryCode" : configuration.countryCode,
                                                       @"appTrackingStatus" : appTrackingStatus}];
    }];
    
    return self.sdk;
}

#pragma mark - Banners

- (void)createBannerWithAdUnitIdentifier:(NSString *)adUnitIdentifier atPosition:(NSString *)bannerPosition
{
    [self createAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier] atPosition: bannerPosition withOffset: CGPointZero];
}

- (void)createBannerWithAdUnitIdentifier:(NSString *)adUnitIdentifier x:(CGFloat)xOffset y:(CGFloat)yOffset
{
    [self createAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier] atPosition: DEFAULT_AD_VIEW_POSITION withOffset: CGPointMake(xOffset, yOffset)];
}

- (void)setBannerBackgroundColorForAdUnitIdentifier:(NSString *)adUnitIdentifier hexColorCode:(NSString *)hexColorCode
{
    [self setAdViewBackgroundColorForAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier] hexColorCode: hexColorCode];
}

- (void)setBannerPlacement:(nullable NSString *)placement forAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self setAdViewPlacement: placement forAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (void)setBannerWidth:(CGFloat)width forAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self setAdViewWidth: width forAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (void)updateBannerPosition:(NSString *)bannerPosition forAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self updateAdViewPosition: bannerPosition withOffset: CGPointZero forAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (void)updateBannerPosition:(CGFloat)xOffset y:(CGFloat)yOffset forAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self updateAdViewPosition: DEFAULT_AD_VIEW_POSITION withOffset: CGPointMake(xOffset, yOffset) forAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (void)setBannerExtraParameterForAdUnitIdentifier:(NSString *)adUnitIdentifier key:(NSString *)key value:(nullable NSString *)value
{
    [self setAdViewExtraParameterForAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier] key: key value: value];
}

- (void)showBannerWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self showAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (void)hideBannerWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self hideAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (NSString *)bannerLayoutForAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    return [self adViewLayoutForAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (void)destroyBannerWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self destroyAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

+ (CGFloat)adaptiveBannerHeightForWidth:(CGFloat)width
{
    return [DEVICE_SPECIFIC_ADVIEW_AD_FORMAT adaptiveSizeForWidth: width].height;
}

#pragma mark - MRECs

- (void)createMRecWithAdUnitIdentifier:(NSString *)adUnitIdentifier atPosition:(NSString *)mrecPosition
{
    [self createAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec atPosition: mrecPosition withOffset: CGPointZero];
}

- (void)createMRecWithAdUnitIdentifier:(NSString *)adUnitIdentifier x:(CGFloat)xOffset y:(CGFloat)yOffset
{
    [self createAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec atPosition: DEFAULT_AD_VIEW_POSITION withOffset: CGPointMake(xOffset, yOffset)];
}

- (void)setMRecPlacement:(nullable NSString *)placement forAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self setAdViewPlacement: placement forAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

- (void)updateMRecPosition:(NSString *)mrecPosition forAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self updateAdViewPosition: mrecPosition withOffset: CGPointZero forAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

- (void)updateMRecPosition:(CGFloat)xOffset y:(CGFloat)yOffset forAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self updateAdViewPosition: DEFAULT_AD_VIEW_POSITION withOffset: CGPointMake(xOffset, yOffset) forAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

- (void)setMRecExtraParameterForAdUnitIdentifier:(NSString *)adUnitIdentifier key:(NSString *)key value:(nullable NSString *)value
{
    [self setAdViewExtraParameterForAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec key: key value: value];
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

- (NSString *)mrecLayoutForAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    return [self adViewLayoutForAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

#pragma mark - Cross Promo Ads

- (void)createCrossPromoAdWithAdUnitIdentifier:(NSString *)adUnitIdentifier x:(CGFloat)xOffset y:(CGFloat)yOffset width:(CGFloat)width height:(CGFloat)height rotation:(CGFloat)rotation
{
    self.adViewWidths[adUnitIdentifier] = @(width);
    self.crossPromoAdViewHeights[adUnitIdentifier] = @(height);
    self.crossPromoAdViewRotations[adUnitIdentifier] = @(rotation);
    [self createAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.crossPromo atPosition: DEFAULT_AD_VIEW_POSITION withOffset: CGPointMake(xOffset, yOffset)];
}

- (void)setCrossPromoAdPlacement:(nullable NSString *)placement forAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self setAdViewPlacement: placement forAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.crossPromo];
}

- (void)updateCrossPromoAdPositionForAdUnitIdentifier:(NSString *)adUnitIdentifier x:(CGFloat)xOffset y:(CGFloat)yOffset width:(CGFloat)width height:(CGFloat)height rotation:(CGFloat)rotation
{
    self.adViewWidths[adUnitIdentifier] = @(width);
    self.crossPromoAdViewHeights[adUnitIdentifier] = @(height);
    self.crossPromoAdViewRotations[adUnitIdentifier] = @(rotation);
    [self updateAdViewPosition: DEFAULT_AD_VIEW_POSITION withOffset: CGPointMake(xOffset, yOffset) forAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.crossPromo];
}

- (void)showCrossPromoAdWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self showAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.crossPromo];
}

- (void)destroyCrossPromoAdWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self destroyAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.crossPromo];
}

- (void)hideCrossPromoAdWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    [self hideAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.crossPromo];
}

- (NSString *)crossPromoAdLayoutForAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    return [self adViewLayoutForAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.crossPromo];
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
    
    MAAd *ad = [self adWithAdUnitIdentifier: adUnitIdentifier];
    if ( !ad ) return @"";
    
    return [MAUnityAdManager propsStrFromDictionary: [self adInfoForAd: ad]];
}

- (NSDictionary<NSString *, NSString *> *)adInfoForAd:(MAAd *)ad
{
    return @{@"adUnitId" : ad.adUnitIdentifier,
             @"networkName" : ad.networkName,
             @"networkPlacement" : ad.networkPlacement,
             @"creativeId" : ad.creativeIdentifier ?: @"",
             @"placement" : ad.placement ?: @"",
             @"revenue" : [@(ad.revenue) stringValue]};
}

#pragma mark - Ad Value

- (NSString *)adValueForAdUnitIdentifier:(NSString *)adUnitIdentifier withKey:(NSString *)key
{
    if ( adUnitIdentifier.length == 0 ) return @"";
    
    MAAd *ad = [self adWithAdUnitIdentifier: adUnitIdentifier];
    if ( !ad ) return @"";
    
    return [ad adValueForKey: key];
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
        
        if ( MAAdFormat.mrec == adFormat )
        {
            name = @"OnMRecAdLoadedEvent";
        }
        else if ( MAAdFormat.crossPromo == adFormat )
        {
            name = @"OnCrossPromoAdLoadedEvent";
        }
        else
        {
            name = @"OnBannerAdLoadedEvent";
        }
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
    
    NSDictionary<NSString *, NSString *> *args = [self defaultAdEventParametersForName: name withAd: ad];
    [MAUnityAdManager forwardUnityEventWithArgs: args];
}

- (void)didFailToLoadAdForAdUnitIdentifier:(NSString *)adUnitIdentifier withError:(MAError *)error
{
    if ( !adUnitIdentifier )
    {
        [self log: @"adUnitIdentifier cannot be nil from %@", [NSThread callStackSymbols]];
        return;
    }
    
    NSString *name;
    if ( self.adViews[adUnitIdentifier] )
    {
        MAAdFormat *adFormat = self.adViewAdFormats[adUnitIdentifier];
        if ( MAAdFormat.mrec == adFormat )
        {
            name = @"OnMRecAdLoadFailedEvent";
        }
        else if ( MAAdFormat.crossPromo == adFormat )
        {
            name = @"OnCrossPromoAdLoadFailedEvent";
        }
        else
        {
            name = @"OnBannerAdLoadFailedEvent";
        }
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
    
    [MAUnityAdManager forwardUnityEventWithArgs: @{@"name" : name,
                                                   @"adUnitId" : adUnitIdentifier,
                                                   @"errorCode" : [@(error.code) stringValue],
                                                   @"errorMessage" : error.message,
                                                   @"adLoadFailureInfo" : error.adLoadFailureInfo ?: @""}];
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
    else if ( MAAdFormat.crossPromo == adFormat )
    {
        name = @"OnCrossPromoAdClickedEvent";
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
    
    NSDictionary<NSString *, NSString *> *args = [self defaultAdEventParametersForName: name withAd: ad];
    [MAUnityAdManager forwardUnityEventWithArgs: args];
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
    
    NSDictionary<NSString *, NSString *> *args = [self defaultAdEventParametersForName: name withAd: ad];
    [MAUnityAdManager forwardUnityEventWithArgs: args];
}

- (void)didFailToDisplayAd:(MAAd *)ad withError:(MAError *)error
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
    
    NSMutableDictionary<NSString *, NSString *> *args = [self defaultAdEventParametersForName: name withAd: ad];
    args[@"errorCode"] = [@(error.code) stringValue];
    args[@"errorMessage"] = error.message;
    [MAUnityAdManager forwardUnityEventWithArgs: args];
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
    
    NSDictionary<NSString *, NSString *> *args = [self defaultAdEventParametersForName: name withAd: ad];
    [MAUnityAdManager forwardUnityEventWithArgs: args];
}

- (void)didCollapseAd:(MAAd *)ad
{
    MAAdFormat *adFormat = ad.format;
    if ( ![adFormat isAdViewAd] )
    {
        [self logInvalidAdFormat: adFormat];
        return;
    }
    
    NSString *name;
    if ( MAAdFormat.mrec == adFormat )
    {
        name = @"OnMRecAdCollapsedEvent";
    }
    else if ( MAAdFormat.crossPromo == adFormat )
    {
        name = @"OnCrossPromoAdCollapsedEvent";
    }
    else
    {
        name = @"OnBannerAdCollapsedEvent";
    }
    
    NSDictionary<NSString *, NSString *> *args = [self defaultAdEventParametersForName: name withAd: ad];
    [MAUnityAdManager forwardUnityEventWithArgs: args];
}

- (void)didExpandAd:(MAAd *)ad
{
    MAAdFormat *adFormat = ad.format;
    if ( ![adFormat isAdViewAd] )
    {
        [self logInvalidAdFormat: adFormat];
        return;
    }
    
    NSString *name;
    if ( MAAdFormat.mrec == adFormat )
    {
        name = @"OnMRecAdExpandedEvent";
    }
    else if ( MAAdFormat.crossPromo == adFormat )
    {
        name = @"OnCrossPromoAdExpandedEvent";
    }
    else
    {
        name = @"OnBannerAdExpandedEvent";
    }
    
    NSDictionary<NSString *, NSString *> *args = [self defaultAdEventParametersForName: name withAd: ad];
    [MAUnityAdManager forwardUnityEventWithArgs: args];
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
    
    
    NSMutableDictionary<NSString *, NSString *> *args = [self defaultAdEventParametersForName: name withAd: ad];
    args[@"rewardLabel"] = rewardLabel;
    args[@"rewardAmount"] = rewardAmount;
    [MAUnityAdManager forwardUnityEventWithArgs: args];
}

- (void)didPayRevenueForAd:(MAAd *)ad
{
    NSString *name;
    MAAdFormat *adFormat = ad.format;
    if ( MAAdFormat.banner == adFormat || MAAdFormat.leader == adFormat )
    {
        name = @"OnBannerAdRevenuePaidEvent";
    }
    else if ( MAAdFormat.mrec == adFormat )
    {
        name = @"OnMRecAdRevenuePaidEvent";
    }
    else if ( MAAdFormat.crossPromo == adFormat )
    {
        name = @"OnCrossPromoAdRevenuePaidEvent";
    }
    else if ( MAAdFormat.interstitial == adFormat )
    {
        name = @"OnInterstitialAdRevenuePaidEvent";
    }
    else if ( MAAdFormat.rewarded == adFormat )
    {
        name = @"OnRewardedAdRevenuePaidEvent";
    }
    else if ( MAAdFormat.rewardedInterstitial == adFormat )
    {
        name = @"OnRewardedInterstitialAdRevenuePaidEvent";
    }
    else
    {
        [self logInvalidAdFormat: adFormat];
        return;
    }
    
    NSDictionary<NSString *, NSString *> *args = [self defaultAdEventParametersForName: name withAd: ad];
    [MAUnityAdManager forwardUnityEventWithArgs: args];
}

- (NSMutableDictionary<NSString *, NSString *> *)defaultAdEventParametersForName:(NSString *)name withAd:(MAAd *)ad
{
    NSMutableDictionary<NSString *, NSString *> *args = [[self adInfoForAd: ad] mutableCopy];
    args[@"name"] = name;
    
    return args;
}

#pragma mark - Internal Methods

- (void)createAdViewWithAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat atPosition:(NSString *)adViewPosition withOffset:(CGPoint)offset
{
    max_unity_dispatch_on_main_thread(^{
        [self log: @"Creating %@ with ad unit identifier \"%@\" and position: \"%@\"", adFormat, adUnitIdentifier, adViewPosition];
        
        // Retrieve ad view from the map
        MAAdView *adView = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat atPosition: adViewPosition withOffset: offset];
        adView.hidden = YES;
        self.safeAreaBackground.hidden = YES;
        
        // Position ad view immediately so if publisher sets color before ad loads, it will not be the size of the screen
        self.adViewAdFormats[adUnitIdentifier] = adFormat;
        [self positionAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
        
        // Handle initial extra parameters if publisher sets it before creating ad view
        if ( self.adViewExtraParametersToSetAfterCreate[adUnitIdentifier] )
        {
            NSDictionary<NSString *, NSString *> *extraParameters = self.adViewExtraParametersToSetAfterCreate[adUnitIdentifier];
            for ( NSString *key in extraParameters )
            {
                [adView setExtraParameterForKey: key value: extraParameters[key]];
                
                [self handleExtraParameterChangesIfNeededForAdUnitIdentifier: adUnitIdentifier
                                                                    adFormat: adFormat
                                                                         key: key
                                                                       value: extraParameters[key]];
            }
            
            [self.adViewExtraParametersToSetAfterCreate removeObjectForKey: adUnitIdentifier];
        }
        
        [adView loadAd];
        
        // The publisher may have requested to show the banner before it was created. Now that the banner is created, show it.
        if ( [self.adUnitIdentifiersToShowAfterCreate containsObject: adUnitIdentifier] )
        {
            [self showAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
            [self.adUnitIdentifiersToShowAfterCreate removeObject: adUnitIdentifier];
        }
    });
}

- (void)setAdViewBackgroundColorForAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat hexColorCode:(NSString *)hexColorCode
{
    max_unity_dispatch_on_main_thread(^{
        [self log: @"Setting %@ with ad unit identifier \"%@\" to color: \"%@\"", adFormat, adUnitIdentifier, hexColorCode];
        
        // In some cases, black color may get redrawn on each frame update, resulting in an undesired flicker
        NSString *hexColorCodeToUse = [hexColorCode containsString: @"FF000000"] ? @"FF000001" : hexColorCode;
        UIColor *convertedColor = [UIColor al_colorWithHexString: hexColorCodeToUse];
        
        MAAdView *view = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
        self.publisherBannerBackgroundColor = convertedColor;
        self.safeAreaBackground.backgroundColor = view.backgroundColor = convertedColor;
        
        // Position adView to ensure logic that depends on background color is properly run
        [self positionAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
    });
}

- (void)setAdViewPlacement:(nullable NSString *)placement forAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    max_unity_dispatch_on_main_thread(^{
        [self log: @"Setting placement \"%@\" for \"%@\" with ad unit identifier \"%@\"", placement, adFormat, adUnitIdentifier];
        
        MAAdView *adView = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
        adView.placement = placement;
    });
}

- (void)setAdViewWidth:(CGFloat)width forAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    max_unity_dispatch_on_main_thread(^{
        [self log: @"Setting width %f for \"%@\" with ad unit identifier \"%@\"", width, adFormat, adUnitIdentifier];
        
        CGFloat minWidth = adFormat.size.width;
        if ( width < minWidth )
        {
            [self log: @"The provided with: %f is smaller than the minimum required width: %f for ad format: %@. Please set the width higher than the minimum required.", width, minWidth, adFormat];
        }
        
        self.adViewWidths[adUnitIdentifier] = @(width);
        [self positionAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
    });
}

- (void)updateAdViewPosition:(NSString *)adViewPosition withOffset:(CGPoint)offset forAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    max_unity_dispatch_on_main_thread(^{
        self.adViewPositions[adUnitIdentifier] = adViewPosition;
        self.adViewOffsets[adUnitIdentifier] = [NSValue valueWithCGPoint: offset];
        [self positionAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
    });
}

- (void)setAdViewExtraParameterForAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat key:(NSString *)key value:(nullable NSString *)value
{
    max_unity_dispatch_on_main_thread(^{
        [self log: @"Setting %@ extra with key: \"%@\" value: \"%@\"", adFormat, key, value];
        
        MAAdView *adView = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
        if ( adView )
        {
            [adView setExtraParameterForKey: key value: value];
        }
        else
        {
            [self log: @"%@ does not exist for ad unit identifier %@. Saving extra parameter to be set when it is created.", adFormat, adUnitIdentifier];
            
            // The adView has not yet been created. Store the extra parameters, so that they can be added once the banner has been created.
            NSMutableDictionary<NSString *, NSString *> *extraParameters = self.adViewExtraParametersToSetAfterCreate[adUnitIdentifier];
            if ( !extraParameters )
            {
                extraParameters = [NSMutableDictionary dictionaryWithCapacity: 1];
                self.adViewExtraParametersToSetAfterCreate[adUnitIdentifier] = extraParameters;
            }
            
            extraParameters[key] = value;
        }
        
        // Certain extra parameters need to be handled immediately
        [self handleExtraParameterChangesIfNeededForAdUnitIdentifier: adUnitIdentifier
                                                            adFormat: adFormat
                                                                 key: key
                                                               value: value];
    });
}

- (void)handleExtraParameterChangesIfNeededForAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat key:(NSString *)key value:(nullable NSString *)value
{
    if ( MAAdFormat.mrec != adFormat )
    {
        if ( [@"force_banner" isEqualToString: key] )
        {
            BOOL shouldForceBanner = [NSNumber al_numberWithString: value].boolValue;
            MAAdFormat *forcedAdFormat = shouldForceBanner ? MAAdFormat.banner : DEVICE_SPECIFIC_ADVIEW_AD_FORMAT;
            
            self.adViewAdFormats[adUnitIdentifier] = forcedAdFormat;
            [self positionAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: forcedAdFormat];
        }
        else if ( [@"adaptive_banner" isEqualToString: key] )
        {
            BOOL shouldUseAdaptiveBanner = [NSNumber al_numberWithString: value].boolValue;
            if ( shouldUseAdaptiveBanner )
            {
                [self.disabledAdaptiveBannerAdUnitIdentifiers removeObject: adUnitIdentifier];
            }
            else
            {
                [self.disabledAdaptiveBannerAdUnitIdentifiers addObject: adUnitIdentifier];
            }
            
            [self positionAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
        }
    }
}

- (void)showAdViewWithAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    max_unity_dispatch_on_main_thread(^{
        [self log: @"Showing %@ with ad unit identifier \"%@\"", adFormat, adUnitIdentifier];
        
        MAAdView *view = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
        if ( !view )
        {
            [self log: @"%@ does not exist for ad unit identifier %@.", adFormat, adUnitIdentifier];
            
            // The adView has not yet been created. Store the ad unit ID, so that it can be displayed once the banner has been created.
            [self.adUnitIdentifiersToShowAfterCreate addObject: adUnitIdentifier];
        }
        else
        {
            // Check edge case where ad may be detatched from view controller
            if ( !view.window.rootViewController )
            {
                [self log: @"%@ missing view controller - re-attaching to %@...", adFormat, [self unityViewController]];
                
                UIViewController *rootViewController = [self unityViewController];
                [rootViewController.view addSubview: view];
                
                [self positionAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
            }
        }
        
        self.safeAreaBackground.hidden = NO;
        view.hidden = NO;
        
        [view startAutoRefresh];
    });
}

- (void)hideAdViewWithAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    max_unity_dispatch_on_main_thread(^{
        [self log: @"Hiding %@ with ad unit identifier \"%@\"", adFormat, adUnitIdentifier];
        [self.adUnitIdentifiersToShowAfterCreate removeObject: adUnitIdentifier];
        
        MAAdView *view = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
        view.hidden = YES;
        self.safeAreaBackground.hidden = YES;
        
        [view stopAutoRefresh];
    });
}

- (NSString *)adViewLayoutForAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    [self log: @"Getting %@ position with ad unit identifier \"%@\"", adFormat, adUnitIdentifier];
    
    MAAdView *view = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
    if ( !view )
    {
        [self log: @"%@ does not exist for ad unit identifier %@", adFormat, adUnitIdentifier];
        
        return @"";
    }
    
    CGRect adViewFrame = view.frame;
    return [MAUnityAdManager propsStrFromDictionary: @{@"origin_x" : [NSString stringWithFormat: @"%f", adViewFrame.origin.x],
                                                       @"origin_y" : [NSString stringWithFormat: @"%f", adViewFrame.origin.y],
                                                       @"width" : [NSString stringWithFormat: @"%f", CGRectGetWidth(adViewFrame)],
                                                       @"height" : [NSString stringWithFormat: @"%f", CGRectGetHeight(adViewFrame)]}];
}

- (void)destroyAdViewWithAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    max_unity_dispatch_on_main_thread(^{
        [self log: @"Destroying %@ with ad unit identifier \"%@\"", adFormat, adUnitIdentifier];
        
        MAAdView *view = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
        view.delegate = nil;
        view.revenueDelegate = nil;
        
        [view removeFromSuperview];
        
        [self.adViews removeObjectForKey: adUnitIdentifier];
        [self.adViewAdFormats removeObjectForKey: adUnitIdentifier];
        [self.adViewPositions removeObjectForKey: adUnitIdentifier];
        [self.adViewOffsets removeObjectForKey: adUnitIdentifier];
        [self.adViewWidths removeObjectForKey: adUnitIdentifier];
        [self.crossPromoAdViewHeights removeObjectForKey: adUnitIdentifier];
        [self.crossPromoAdViewRotations removeObjectForKey: adUnitIdentifier];
        [self.verticalAdViewFormats removeObjectForKey: adUnitIdentifier];
        [self.disabledAdaptiveBannerAdUnitIdentifiers removeObject: adUnitIdentifier];
    });
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
        result.revenueDelegate = self;
        
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
        result.revenueDelegate = self;
        
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
        result.revenueDelegate = self;
        
        self.rewardedInterstitialAds[adUnitIdentifier] = result;
    }
    
    return result;
}

- (MAAdView *)retrieveAdViewForAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    return [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat atPosition: nil withOffset: CGPointZero];
}

- (MAAdView *)retrieveAdViewForAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat atPosition:(NSString *)adViewPosition withOffset:(CGPoint)offset
{
    MAAdView *result = self.adViews[adUnitIdentifier];
    if ( !result && adViewPosition )
    {
        result = [[MAAdView alloc] initWithAdUnitIdentifier: adUnitIdentifier adFormat: adFormat sdk: self.sdk];
        // There is a Unity bug where if an empty UIView is on screen with user interaction enabled, and a user interacts with it, it just passes the events to random parts of the screen.
        result.userInteractionEnabled = NO;
        result.translatesAutoresizingMaskIntoConstraints = NO;
        result.delegate = self;
        result.revenueDelegate = self;
        
        self.adViews[adUnitIdentifier] = result;
        self.adViewPositions[adUnitIdentifier] = adViewPosition;
        self.adViewOffsets[adUnitIdentifier] = [NSValue valueWithCGPoint: offset];
        
        UIViewController *rootViewController = [self unityViewController];
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
    max_unity_dispatch_on_main_thread(^{
        MAAdView *adView = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
        NSString *adViewPosition = self.adViewPositions[adUnitIdentifier];
        NSValue *adViewPositionValue = self.adViewOffsets[adUnitIdentifier];
        CGPoint adViewOffset = [adViewPositionValue CGPointValue];
        BOOL isAdaptiveBannerDisabled = [self.disabledAdaptiveBannerAdUnitIdentifiers containsObject: adUnitIdentifier];
        BOOL isWidthPtsOverridden = self.adViewWidths[adUnitIdentifier] != nil;
        BOOL isCrossPromoHeightPtsOverridden = self.crossPromoAdViewHeights[adUnitIdentifier] != nil;
        BOOL isCrossPromoRotationOverridden = self.crossPromoAdViewRotations[adUnitIdentifier] != nil;
        
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
        self.safeAreaBackground.hidden = adView.hidden;
        
        //
        // Determine ad width
        //
        CGFloat adViewWidth;
        
        // Check if publisher has overridden width as points
        if ( isWidthPtsOverridden )
        {
            adViewWidth = self.adViewWidths[adUnitIdentifier].floatValue;
        }
        // Top center / bottom center stretches full screen
        else if ( [adViewPosition isEqual: @"top_center"] || [adViewPosition isEqual: @"bottom_center"] )
        {
            adViewWidth = CGRectGetWidth(KEY_WINDOW.bounds);
        }
        // Else use standard widths of 320, 728, or 300
        else
        {
            adViewWidth = adFormat.size.width;
        }
        
        //
        // Determine ad height
        //
        CGFloat adViewHeight;
        
        if ( isCrossPromoHeightPtsOverridden )
        {
            adViewHeight = self.crossPromoAdViewHeights[adUnitIdentifier].floatValue;
        }
        else if ( (adFormat == MAAdFormat.banner || adFormat == MAAdFormat.leader) && !isAdaptiveBannerDisabled )
        {
            adViewHeight = [adFormat adaptiveSizeForWidth: adViewWidth].height;
        }
        else
        {
            adViewHeight = adFormat.size.height;
        }
        
        CGSize adViewSize = CGSizeMake(adViewWidth, adViewHeight);
        
        // All positions have constant height
        NSMutableArray<NSLayoutConstraint *> *constraints = [NSMutableArray arrayWithObject: [adView.heightAnchor constraintEqualToConstant: adViewSize.height]];
        
        UILayoutGuide *layoutGuide;
        if ( @available(iOS 11.0, *) )
        {
            layoutGuide = superview.safeAreaLayoutGuide;
        }
        else
        {
            layoutGuide = superview.layoutMarginsGuide;
        }
        
        if ( [adViewPosition isEqual: @"top_center"] || [adViewPosition isEqual: @"bottom_center"] )
        {
            // Non AdMob banners will still be of 50/90 points tall. Set the auto sizing mask such that the inner ad view is pinned to the bottom or top according to the ad view position.
            if ( !isAdaptiveBannerDisabled )
            {
                adView.autoresizingMask = UIViewAutoresizingFlexibleWidth;
                
                if ( [@"top_center" isEqual: adViewPosition] )
                {
                    adView.autoresizingMask |= UIViewAutoresizingFlexibleBottomMargin;
                }
                else // bottom_center
                {
                    adView.autoresizingMask |= UIViewAutoresizingFlexibleTopMargin;
                }
            }
            
            // If publisher actually provided a banner background color
            if ( self.publisherBannerBackgroundColor && adFormat != MAAdFormat.mrec )
            {
                if ( isWidthPtsOverridden )
                {
                    [constraints addObjectsFromArray: @[[adView.widthAnchor constraintEqualToConstant: adViewWidth],
                                                        [adView.centerXAnchor constraintEqualToAnchor: layoutGuide.centerXAnchor],
                                                        [self.safeAreaBackground.widthAnchor constraintEqualToConstant: adViewWidth],
                                                        [self.safeAreaBackground.centerXAnchor constraintEqualToAnchor: layoutGuide.centerXAnchor]]];
                    
                    if ( [adViewPosition isEqual: @"top_center"] )
                    {
                        [constraints addObjectsFromArray: @[[adView.topAnchor constraintEqualToAnchor: layoutGuide.topAnchor],
                                                            [self.safeAreaBackground.topAnchor constraintEqualToAnchor: superview.topAnchor],
                                                            [self.safeAreaBackground.bottomAnchor constraintEqualToAnchor: adView.topAnchor]]];
                    }
                    else // bottom_center
                    {
                        [constraints addObjectsFromArray: @[[adView.bottomAnchor constraintEqualToAnchor: layoutGuide.bottomAnchor],
                                                            [self.safeAreaBackground.topAnchor constraintEqualToAnchor: adView.bottomAnchor],
                                                            [self.safeAreaBackground.bottomAnchor constraintEqualToAnchor: superview.bottomAnchor]]];
                    }
                }
                else
                {
                    [constraints addObjectsFromArray: @[[adView.leftAnchor constraintEqualToAnchor: superview.leftAnchor],
                                                        [adView.rightAnchor constraintEqualToAnchor: superview.rightAnchor],
                                                        [self.safeAreaBackground.leftAnchor constraintEqualToAnchor: superview.leftAnchor],
                                                        [self.safeAreaBackground.rightAnchor constraintEqualToAnchor: superview.rightAnchor]]];
                    
                    if ( [adViewPosition isEqual: @"top_center"] )
                    {
                        [constraints addObjectsFromArray: @[[adView.topAnchor constraintEqualToAnchor: layoutGuide.topAnchor],
                                                            [self.safeAreaBackground.topAnchor constraintEqualToAnchor: superview.topAnchor],
                                                            [self.safeAreaBackground.bottomAnchor constraintEqualToAnchor: adView.topAnchor]]];
                    }
                    else // bottom_center
                    {
                        [constraints addObjectsFromArray: @[[adView.bottomAnchor constraintEqualToAnchor: layoutGuide.bottomAnchor],
                                                            [self.safeAreaBackground.topAnchor constraintEqualToAnchor: adView.bottomAnchor],
                                                            [self.safeAreaBackground.bottomAnchor constraintEqualToAnchor: superview.bottomAnchor]]];
                    }
                }
            }
            // If pub does not have a background color set or this is not a banner
            else
            {
                self.safeAreaBackground.hidden = YES;
                
                [constraints addObjectsFromArray: @[[adView.widthAnchor constraintEqualToConstant: adViewWidth],
                                                    [adView.centerXAnchor constraintEqualToAnchor: layoutGuide.centerXAnchor]]];
                
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
                // If the publiser has a background color set - set the width to the height of the screen, to span the ad across the screen after it is rotated.
                if ( self.publisherBannerBackgroundColor )
                {
                    if ( isWidthPtsOverridden )
                    {
                        width = adViewWidth;
                    }
                    else
                    {
                        width = CGRectGetHeight(KEY_WINDOW.bounds);
                    }
                }
                // Otherwise - we shouldn't span the banner the width of the realm (there might be user-interactable UI on the sides)
                else
                {
                    width = adViewWidth;
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
                
                // If adaptive - make top flexible since we anchor with the bottom of the banner at the edge of the screen
                if ( !isAdaptiveBannerDisabled )
                {
                    adView.autoresizingMask = UIViewAutoresizingFlexibleWidth | UIViewAutoresizingFlexibleTopMargin;
                }
            }
        }
        // Otherwise, publisher will likely construct his own views around the adview
        else
        {
            self.safeAreaBackground.hidden = YES;
            
            [constraints addObject: [adView.widthAnchor constraintEqualToConstant: adViewWidth]];
            
            if ( [adViewPosition isEqual: @"top_left"] )
            {
                [constraints addObjectsFromArray: @[[adView.leftAnchor constraintEqualToAnchor: superview.leftAnchor constant: adViewOffset.x],
                                                    [adView.topAnchor constraintEqualToAnchor: layoutGuide.topAnchor constant: adViewOffset.y]]];
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
        
        if ( isCrossPromoRotationOverridden )
        {
            adView.transform = CGAffineTransformRotate(CGAffineTransformIdentity, DEGREES_TO_RADIANS(self.crossPromoAdViewRotations[adUnitIdentifier].floatValue));
        }
        
        self.adViewConstraints[adUnitIdentifier] = constraints;
        
        [NSLayoutConstraint activateConstraints: constraints];
    });
}

- (UIViewController *)unityViewController
{
    // Handle edge case where `UnityGetGLViewController()` returns nil
    return UnityGetGLViewController() ?: UnityGetMainWindow().rootViewController ?: [KEY_WINDOW rootViewController];
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

- (MAAdFormat *)adViewAdFormatForAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    if ( self.adViewAdFormats[adUnitIdentifier] )
    {
        return self.adViewAdFormats[adUnitIdentifier];
    }
    else
    {
        return DEVICE_SPECIFIC_ADVIEW_AD_FORMAT;
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

#pragma mark - User Service

- (void)didDismissUserConsentDialog
{
    [MAUnityAdManager forwardUnityEventWithArgs: @{@"name" : @"OnSdkConsentDialogDismissedEvent"}];
}

#pragma mark - Variable Service (Deprecated)

- (void)loadVariables
{
    [self.sdk.variableService loadVariables];
}

- (void)variableService:(ALVariableService *)variableService didUpdateVariables:(NSDictionary<NSString *, id> *)variables
{
    [MAUnityAdManager forwardUnityEventWithArgs: @{@"name" : @"OnVariablesUpdatedEvent"}];
}

- (MAAd *)adWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    @synchronized ( self.adInfoDictLock )
    {
        return self.adInfoDict[adUnitIdentifier];
    }
}

@end
