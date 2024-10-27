//
//  MAUnityAdManager.m
//  AppLovin MAX Unity Plugin
//

#import "MAUnityAdManager.h"

#define KEY_WINDOW [UIApplication sharedApplication].keyWindow
#define DEVICE_SPECIFIC_ADVIEW_AD_FORMAT ([[UIDevice currentDevice] userInterfaceIdiom] == UIUserInterfaceIdiomPad) ? MAAdFormat.leader : MAAdFormat.banner
#define IS_VERTICAL_BANNER_POSITION(_POS) ( [@"center_left" isEqual: adViewPosition] || [@"center_right" isEqual: adViewPosition] )
#define DEGREES_TO_RADIANS(angle) ((angle) / 180.0 * M_PI)

#ifdef __cplusplus
extern "C" {
#endif
    
    extern bool max_unity_should_disable_all_logs(void);  // Forward declaration

    // UnityAppController.mm
    UIViewController* UnityGetGLViewController(void);
    UIWindow* UnityGetMainWindow(void);
    
    // life cycle management
    int UnityIsPaused(void);
    void UnityPause(int pause);
    
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

@interface MAUnityAdManager()<MAAdDelegate, MAAdViewAdDelegate, MARewardedAdDelegate, MAAdRevenueDelegate, MAAdReviewDelegate>

// Parent Fields
@property (nonatomic, weak) ALSdk *sdk;

// Fullscreen Ad Fields
@property (nonatomic, strong) NSMutableDictionary<NSString *, MAInterstitialAd *> *interstitials;
@property (nonatomic, strong) NSMutableDictionary<NSString *, MAAppOpenAd *> *appOpenAds;
@property (nonatomic, strong) NSMutableDictionary<NSString *, MARewardedAd *> *rewardedAds;
@property (nonatomic, strong) NSMutableDictionary<NSString *, MARewardedInterstitialAd *> *rewardedInterstitialAds;

// AdView Fields
@property (nonatomic, strong) NSMutableDictionary<NSString *, MAAdView *> *adViews;
@property (nonatomic, strong) NSMutableDictionary<NSString *, MAAdFormat *> *adViewAdFormats;
@property (nonatomic, strong) NSMutableDictionary<NSString *, NSString *> *adViewPositions;
@property (nonatomic, strong) NSMutableDictionary<NSString *, NSValue *> *adViewOffsets;
@property (nonatomic, strong) NSMutableDictionary<NSString *, NSNumber *> *adViewWidths;
@property (nonatomic, strong) NSMutableDictionary<NSString *, MAAdFormat *> *verticalAdViewFormats;
@property (nonatomic, strong) NSMutableDictionary<NSString *, NSArray<NSLayoutConstraint *> *> *adViewConstraints;
@property (nonatomic, strong) NSMutableDictionary<NSString *, NSMutableDictionary<NSString *, NSString *> *> *adViewExtraParametersToSetAfterCreate;
@property (nonatomic, strong) NSMutableDictionary<NSString *, NSMutableDictionary<NSString *, id> *> *adViewLocalExtraParametersToSetAfterCreate;
@property (nonatomic, strong) NSMutableDictionary<NSString *, NSString *> *adViewCustomDataToSetAfterCreate;
@property (nonatomic, strong) NSMutableArray<NSString *> *adUnitIdentifiersToShowAfterCreate;
@property (nonatomic, strong) NSMutableSet<NSString *> *disabledAdaptiveBannerAdUnitIdentifiers;
@property (nonatomic, strong) NSMutableSet<NSString *> *disabledAutoRefreshAdViewAdUnitIdentifiers;
@property (nonatomic, strong) UIView *safeAreaBackground;
@property (nonatomic, strong, nullable) UIColor *publisherBannerBackgroundColor;

@property (nonatomic, strong) NSMutableDictionary<NSString *, MAAd *> *adInfoDict;
@property (nonatomic, strong) NSObject *adInfoDictLock;

@property (nonatomic, strong) NSOperationQueue *backgroundCallbackEventsQueue;
@property (nonatomic, assign) BOOL resumeUnityAfterApplicationBecomesActive;

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
static ALUnityBackgroundCallback backgroundCallback;

#pragma mark - Initialization

- (instancetype)init
{
    self = [super init];
    if ( self )
    {
        self.interstitials = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.appOpenAds = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.rewardedAds = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.rewardedInterstitialAds = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.adViews = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.adViewAdFormats = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.adViewPositions = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.adViewOffsets = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.adViewWidths = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.verticalAdViewFormats = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.adViewConstraints = [NSMutableDictionary dictionaryWithCapacity: 2];
        self.adViewExtraParametersToSetAfterCreate = [NSMutableDictionary dictionaryWithCapacity: 1];
        self.adViewLocalExtraParametersToSetAfterCreate = [NSMutableDictionary dictionaryWithCapacity: 1];
        self.adViewCustomDataToSetAfterCreate = [NSMutableDictionary dictionaryWithCapacity: 1];
        self.adUnitIdentifiersToShowAfterCreate = [NSMutableArray arrayWithCapacity: 2];
        self.disabledAdaptiveBannerAdUnitIdentifiers = [NSMutableSet setWithCapacity: 2];
        self.disabledAutoRefreshAdViewAdUnitIdentifiers = [NSMutableSet setWithCapacity: 2];
        self.adInfoDict = [NSMutableDictionary dictionary];
        self.adInfoDictLock = [[NSObject alloc] init];
        
        self.backgroundCallbackEventsQueue = [[NSOperationQueue alloc] init];
        self.backgroundCallbackEventsQueue.maxConcurrentOperationCount = 1;
        
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
        
        [[NSNotificationCenter defaultCenter] addObserver: self
                                                 selector: @selector(applicationPaused:)
                                                     name: UIApplicationDidEnterBackgroundNotification
                                                   object: nil];
        
        [[NSNotificationCenter defaultCenter] addObserver: self
                                                 selector: @selector(applicationResumed:)
                                                     name: UIApplicationDidBecomeActiveNotification
                                                   object: nil];
        
        [[NSNotificationCenter defaultCenter] addObserverForName: UIApplicationDidBecomeActiveNotification
                                                          object: nil
                                                           queue: [NSOperationQueue mainQueue]
                                                      usingBlock:^(NSNotification *notification) {
            
#if !IS_TEST_APP
            if ( self.resumeUnityAfterApplicationBecomesActive && UnityIsPaused() )
            {
                UnityPause(NO);
            }
#endif
            
            self.backgroundCallbackEventsQueue.suspended = NO;
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

+ (void)setUnityBackgroundCallback:(ALUnityBackgroundCallback)unityBackgroundCallback
{
    backgroundCallback = unityBackgroundCallback;
}

#pragma mark - Plugin Initialization

- (void)initializeSdkWithConfiguration:(ALSdkInitializationConfiguration *)initConfig andCompletionHandler:(ALSdkInitializationCompletionHandler)completionHandler;
{
    self.sdk = [ALSdk shared];
    [self.sdk initializeWithConfiguration: initConfig completionHandler:^(ALSdkConfiguration *configuration) {
        dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
            
            // Note: internal state should be updated first
            completionHandler( configuration );
            
            NSString *consentFlowUserGeographyStr = @(configuration.consentFlowUserGeography).stringValue;
            NSString *consentDialogStateStr = @(configuration.consentDialogState).stringValue;
            NSString *appTrackingStatus = @(configuration.appTrackingTransparencyStatus).stringValue; // Deliberately name it `appTrackingStatus` to be a bit more generic (in case Android introduces a similar concept)
            [self forwardUnityEventWithArgs: @{@"name" : @"OnSdkInitializedEvent",
                                               @"consentFlowUserGeography" : consentFlowUserGeographyStr,
                                               @"consentDialogState" : consentDialogStateStr,
                                               @"countryCode" : configuration.countryCode,
                                               @"appTrackingStatus" : appTrackingStatus,
                                               @"isSuccessfullyInitialized" : @([self.sdk isInitialized]),
                                               @"isTestModeEnabled" : @([configuration isTestModeEnabled])}];
        });
    }];
}

#pragma mark - Banners

- (void)createBannerWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier atPosition:(nullable NSString *)bannerPosition
{
    [self createAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier] atPosition: bannerPosition withOffset: CGPointZero];
}

- (void)createBannerWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier x:(CGFloat)xOffset y:(CGFloat)yOffset
{
    [self createAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier] atPosition: DEFAULT_AD_VIEW_POSITION withOffset: CGPointMake(xOffset, yOffset)];
}

- (void)loadBannerWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self loadAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (void)setBannerBackgroundColorForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier hexColorCode:(nullable NSString *)hexColorCode
{
    [self setAdViewBackgroundColorForAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier] hexColorCode: hexColorCode];
}

- (void)setBannerPlacement:(nullable NSString *)placement forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self setAdViewPlacement: placement forAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (void)startBannerAutoRefreshForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self startAdViewAutoRefreshForAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (void)stopBannerAutoRefreshForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self stopAdViewAutoRefreshForAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (void)setBannerWidth:(CGFloat)width forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self setAdViewWidth: width forAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (void)updateBannerPosition:(nullable NSString *)bannerPosition forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self updateAdViewPosition: bannerPosition withOffset: CGPointZero forAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (void)updateBannerPosition:(CGFloat)xOffset y:(CGFloat)yOffset forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self updateAdViewPosition: DEFAULT_AD_VIEW_POSITION withOffset: CGPointMake(xOffset, yOffset) forAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (void)setBannerExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable NSString *)value
{
    [self setAdViewExtraParameterForAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier] key: key value: value];
}

- (void)setBannerLocalExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable id)value
{
    if ( !key )
    {
        [self log: @"Failed to set local extra parameter: No key specified"];
        return;
    }
    
    [self setAdViewLocalExtraParameterForAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier] key: key value: value];
}

- (void)setBannerCustomData:(nullable NSString *)customData forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self setAdViewCustomData: customData forAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (void)showBannerWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self showAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (void)hideBannerWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self hideAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (NSString *)bannerLayoutForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    return [self adViewLayoutForAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

- (void)destroyBannerWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self destroyAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: [self adViewAdFormatForAdUnitIdentifier: adUnitIdentifier]];
}

+ (CGFloat)adaptiveBannerHeightForWidth:(CGFloat)width
{
    return [DEVICE_SPECIFIC_ADVIEW_AD_FORMAT adaptiveSizeForWidth: width].height;
}

#pragma mark - MRECs

- (void)createMRecWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier atPosition:(nullable NSString *)mrecPosition
{
    [self createAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec atPosition: mrecPosition withOffset: CGPointZero];
}

- (void)createMRecWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier x:(CGFloat)xOffset y:(CGFloat)yOffset
{
    [self createAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec atPosition: DEFAULT_AD_VIEW_POSITION withOffset: CGPointMake(xOffset, yOffset)];
}

- (void)loadMRecWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self loadAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

- (void)setMRecPlacement:(nullable NSString *)placement forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self setAdViewPlacement: placement forAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

- (void)startMRecAutoRefreshForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self startAdViewAutoRefreshForAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

- (void)stopMRecAutoRefreshForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self stopAdViewAutoRefreshForAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

- (void)updateMRecPosition:(nullable NSString *)mrecPosition forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self updateAdViewPosition: mrecPosition withOffset: CGPointZero forAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

- (void)updateMRecPosition:(CGFloat)xOffset y:(CGFloat)yOffset forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self updateAdViewPosition: DEFAULT_AD_VIEW_POSITION withOffset: CGPointMake(xOffset, yOffset) forAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

- (void)setMRecExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable NSString *)value
{
    [self setAdViewExtraParameterForAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec key: key value: value];
}

- (void)setMRecLocalExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable id)value
{
    if ( !key )
    {
        [self log: @"Failed to set local extra parameter: No key specified"];
        return;
    }
    
    [self setAdViewLocalExtraParameterForAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec key: key value: value];
}

- (void)setMRecCustomData:(nullable NSString *)customData forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
{
    [self setAdViewCustomData: customData forAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

- (void)showMRecWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self showAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

- (void)destroyMRecWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self destroyAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

- (void)hideMRecWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    [self hideAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

- (NSString *)mrecLayoutForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    return [self adViewLayoutForAdUnitIdentifier: adUnitIdentifier adFormat: MAAdFormat.mrec];
}

#pragma mark - Interstitials

- (void)loadInterstitialWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    MAInterstitialAd *interstitial = [self retrieveInterstitialForAdUnitIdentifier: adUnitIdentifier];
    [interstitial loadAd];
}

- (BOOL)isInterstitialReadyWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    MAInterstitialAd *interstitial = [self retrieveInterstitialForAdUnitIdentifier: adUnitIdentifier];
    return [interstitial isReady];
}

- (void)showInterstitialWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier placement:(nullable NSString *)placement customData:(nullable NSString *)customData
{
    MAInterstitialAd *interstitial = [self retrieveInterstitialForAdUnitIdentifier: adUnitIdentifier];
    [interstitial showAdForPlacement: placement customData: customData];
}

- (void)setInterstitialExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable NSString *)value
{
    MAInterstitialAd *interstitial = [self retrieveInterstitialForAdUnitIdentifier: adUnitIdentifier];
    [interstitial setExtraParameterForKey: key value: value];
}

- (void)setInterstitialLocalExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable id)value
{
    if ( !key )
    {
        [self log: @"Failed to set local extra parameter: No key specified"];
        return;
    }
    
    MAInterstitialAd *interstitial = [self retrieveInterstitialForAdUnitIdentifier: adUnitIdentifier];
    [interstitial setLocalExtraParameterForKey: key value: value];
}

#pragma mark - App Open Ads

- (void)loadAppOpenAdWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    MAAppOpenAd *appOpenAd = [self retrieveAppOpenAdForAdUnitIdentifier: adUnitIdentifier];
    [appOpenAd loadAd];
}

- (BOOL)isAppOpenAdReadyWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    MAAppOpenAd *appOpenAd = [self retrieveAppOpenAdForAdUnitIdentifier: adUnitIdentifier];
    return [appOpenAd isReady];
}

- (void)showAppOpenAdWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier placement:(nullable NSString *)placement customData:(nullable NSString *)customData
{
    MAAppOpenAd *appOpenAd = [self retrieveAppOpenAdForAdUnitIdentifier: adUnitIdentifier];
    [appOpenAd showAdForPlacement: placement customData: customData];
}

- (void)setAppOpenAdExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable NSString *)value
{
    MAAppOpenAd *appOpenAd = [self retrieveAppOpenAdForAdUnitIdentifier: adUnitIdentifier];
    [appOpenAd setExtraParameterForKey: key value: value];
}

- (void)setAppOpenAdLocalExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable id)value
{
    if ( !key )
    {
        [self log: @"Failed to set local extra parameter: No key specified"];
        return;
    }
    
    MAAppOpenAd *appOpenAd = [self retrieveAppOpenAdForAdUnitIdentifier: adUnitIdentifier];
    [appOpenAd setLocalExtraParameterForKey: key value: value];
}

#pragma mark - Rewarded

- (void)loadRewardedAdWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    MARewardedAd *rewardedAd = [self retrieveRewardedAdForAdUnitIdentifier: adUnitIdentifier];
    [rewardedAd loadAd];
}

- (BOOL)isRewardedAdReadyWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    MARewardedAd *rewardedAd = [self retrieveRewardedAdForAdUnitIdentifier: adUnitIdentifier];
    return [rewardedAd isReady];
}

- (void)showRewardedAdWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier placement:(nullable NSString *)placement customData:(nullable NSString *)customData
{
    MARewardedAd *rewardedAd = [self retrieveRewardedAdForAdUnitIdentifier: adUnitIdentifier];
    [rewardedAd showAdForPlacement: placement customData: customData];
}

- (void)setRewardedAdExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable NSString *)value
{
    MARewardedAd *rewardedAd = [self retrieveRewardedAdForAdUnitIdentifier: adUnitIdentifier];
    [rewardedAd setExtraParameterForKey: key value: value];
}

- (void)setRewardedAdLocalExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable id)value;
{
    if ( !key )
    {
        [self log: @"Failed to set local extra parameter: No key specified"];
        return;
    }
    
    MARewardedAd *rewardedAd = [self retrieveRewardedAdForAdUnitIdentifier: adUnitIdentifier];
    [rewardedAd setLocalExtraParameterForKey: key value: value];
}

#pragma mark - Rewarded Interstitials

- (void)loadRewardedInterstitialAdWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    MARewardedInterstitialAd *rewardedInterstitialAd = [self retrieveRewardedInterstitialAdForAdUnitIdentifier: adUnitIdentifier];
    [rewardedInterstitialAd loadAd];
}

- (BOOL)isRewardedInterstitialAdReadyWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    MARewardedInterstitialAd *rewardedInterstitialAd = [self retrieveRewardedInterstitialAdForAdUnitIdentifier: adUnitIdentifier];
    return [rewardedInterstitialAd isReady];
}

- (void)showRewardedInterstitialAdWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier placement:(nullable NSString *)placement customData:(nullable NSString *)customData
{
    MARewardedInterstitialAd *rewardedInterstitialAd = [self retrieveRewardedInterstitialAdForAdUnitIdentifier: adUnitIdentifier];
    [rewardedInterstitialAd showAdForPlacement: placement customData: customData];
}

- (void)setRewardedInterstitialAdExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable NSString *)value
{
    MARewardedInterstitialAd *rewardedInterstitialAd = [self retrieveRewardedInterstitialAdForAdUnitIdentifier: adUnitIdentifier];
    [rewardedInterstitialAd setExtraParameterForKey: key value: value];
}

- (void)setRewardedInterstitialAdLocalExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable id)value
{
    if ( !key )
    {
        [self log: @"Failed to set local extra parameter: No key specified"];
        return;
    }
    
    MARewardedInterstitialAd *rewardedInterstitialAd = [self retrieveRewardedInterstitialAdForAdUnitIdentifier: adUnitIdentifier];
    [rewardedInterstitialAd setLocalExtraParameterForKey: key value: value];
}

#pragma mark - Event Tracking

- (void)trackEvent:(nullable NSString *)event parameters:(nullable NSString *)parameters
{
    NSDictionary<NSString *, id> *deserializedParameters = [MAUnityAdManager deserializeParameters: parameters];
    [self.sdk.eventService trackEvent: event parameters: deserializedParameters];
}

#pragma mark - Ad Info

- (NSString *)adInfoForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier
{
    if ( ![adUnitIdentifier al_isValidString] ) return @"";
    
    MAAd *ad = [self adWithAdUnitIdentifier: adUnitIdentifier];
    if ( !ad ) return @"";
    
    return [MAUnityAdManager serializeParameters: [self adInfoForAd: ad]];
}

- (NSDictionary<NSString *, id> *)adInfoForAd:(MAAd *)ad
{
    return @{@"adUnitId" : ad.adUnitIdentifier,
             @"adFormat" : ad.format.label,
             @"networkName" : ad.networkName,
             @"networkPlacement" : ad.networkPlacement,
             @"creativeId" : ad.creativeIdentifier ?: @"",
             @"placement" : ad.placement ?: @"",
             @"revenue" : [@(ad.revenue) stringValue],
             @"revenuePrecision" : ad.revenuePrecision,
             @"waterfallInfo" : [self createAdWaterfallInfo: ad.waterfall],
             @"latencyMillis" : [self requestLatencyMillisFromRequestLatency: ad.requestLatency],
             @"dspName" : ad.DSPName ?: @""};
}

#pragma mark - Waterfall Information

- (NSDictionary<NSString *, id> *)createAdWaterfallInfo:(MAAdWaterfallInfo *)waterfallInfo
{
    NSMutableDictionary<NSString *, NSObject *> *waterfallInfoDict = [NSMutableDictionary dictionary];
    if ( !waterfallInfo ) return waterfallInfoDict;
    
    waterfallInfoDict[@"name"] = waterfallInfo.name;
    waterfallInfoDict[@"testName"] = waterfallInfo.testName;
    
    NSMutableArray<NSDictionary<NSString *, NSObject *> *> *networkResponsesArray = [NSMutableArray arrayWithCapacity: waterfallInfo.networkResponses.count];
    for ( MANetworkResponseInfo *response in  waterfallInfo.networkResponses )
    {
        [networkResponsesArray addObject: [self createNetworkResponseInfo: response]];
    }
    
    waterfallInfoDict[@"networkResponses"] = networkResponsesArray;
    waterfallInfoDict[@"latencyMillis"] = [self requestLatencyMillisFromRequestLatency: waterfallInfo.latency];
    
    return waterfallInfoDict;
}

- (NSDictionary<NSString *, id> *)createNetworkResponseInfo:(MANetworkResponseInfo *)response
{
    NSMutableDictionary<NSString *, NSObject *> *networkResponseDict = [NSMutableDictionary dictionary];
    
    networkResponseDict[@"adLoadState"] = @(response.adLoadState).stringValue;
    
    MAMediatedNetworkInfo *mediatedNetworkInfo = response.mediatedNetwork;
    if ( mediatedNetworkInfo )
    {
        NSMutableDictionary <NSString *, NSObject *> *networkInfoObject = [NSMutableDictionary dictionary];
        networkInfoObject[@"name"] = response.mediatedNetwork.name;
        networkInfoObject[@"adapterClassName"] = response.mediatedNetwork.adapterClassName;
        networkInfoObject[@"adapterVersion"] = response.mediatedNetwork.adapterVersion;
        networkInfoObject[@"sdkVersion"] = response.mediatedNetwork.sdkVersion;
        
        networkResponseDict[@"mediatedNetwork"] = networkInfoObject;
    }
    
    networkResponseDict[@"credentials"] = response.credentials;
    networkResponseDict[@"isBidding"] = @([response isBidding]);
    
    MAError *error = response.error;
    if ( error )
    {
        NSMutableDictionary<NSString *, NSObject *> *errorObject = [NSMutableDictionary dictionary];
        errorObject[@"errorMessage"] = error.message;
        errorObject[@"adLoadFailure"] = error.adLoadFailureInfo;
        errorObject[@"errorCode"] = @(error.code).stringValue;
        errorObject[@"latencyMillis"] = [self requestLatencyMillisFromRequestLatency: error.requestLatency];
        
        networkResponseDict[@"error"] = errorObject;
    }
    
    networkResponseDict[@"latencyMillis"] = [self requestLatencyMillisFromRequestLatency: response.latency];
    
    return networkResponseDict;
}

#pragma mark - Ad Value

- (NSString *)adValueForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier withKey:(nullable NSString *)key
{
    if ( adUnitIdentifier == nil || adUnitIdentifier.length == 0 ) return @"";
    if ( key == nil || key.length == 0 ) return @"";
    
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
    else if ( MAAdFormat.appOpen == adFormat )
    {
        name = @"OnAppOpenAdLoadedEvent";
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
    
    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        
        @synchronized ( self.adInfoDictLock )
        {
            self.adInfoDict[ad.adUnitIdentifier] = ad;
        }
        
        NSDictionary<NSString *, id> *args = [self defaultAdEventParametersForName: name withAd: ad];
        [self forwardUnityEventWithArgs: args];
    });
}

- (void)didFailToLoadAdForAdUnitIdentifier:(NSString *)adUnitIdentifier withError:(MAError *)error
{
    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        
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
            else
            {
                name = @"OnBannerAdLoadFailedEvent";
            }
        }
        else if ( self.interstitials[adUnitIdentifier] )
        {
            name = @"OnInterstitialLoadFailedEvent";
        }
        else if ( self.appOpenAds[adUnitIdentifier] )
        {
            name = @"OnAppOpenAdLoadFailedEvent";
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
        
        [self forwardUnityEventWithArgs: @{@"name" : name,
                                           @"adUnitId" : adUnitIdentifier,
                                           @"errorCode" : [@(error.code) stringValue],
                                           @"errorMessage" : error.message,
                                           @"waterfallInfo" : [self createAdWaterfallInfo: error.waterfall],
                                           @"adLoadFailureInfo" : error.adLoadFailureInfo ?: @"",
                                           @"latencyMillis" : [self requestLatencyMillisFromRequestLatency: error.requestLatency]}];
    });
}

- (void)didClickAd:(MAAd *)ad
{
    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        
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
        else if ( MAAdFormat.appOpen == adFormat )
        {
            name = @"OnAppOpenAdClickedEvent";
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
        
        NSDictionary<NSString *, id> *args = [self defaultAdEventParametersForName: name withAd: ad];
        [self forwardUnityEventWithArgs: args];
    });
}

- (void)didDisplayAd:(MAAd *)ad
{
    // BMLs do not support [DISPLAY] events in Unity
    MAAdFormat *adFormat = ad.format;
    if ( ![adFormat isFullscreenAd] ) return;
    
#if !IS_TEST_APP
    // UnityPause needs to be called on the main thread.
    UnityPause(YES);
#endif
    
    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        
        NSString *name;
        if ( MAAdFormat.interstitial == adFormat )
        {
            name = @"OnInterstitialDisplayedEvent";
        }
        else if ( MAAdFormat.appOpen == adFormat )
        {
            name = @"OnAppOpenAdDisplayedEvent";
        }
        else if ( MAAdFormat.rewarded == adFormat )
        {
            name = @"OnRewardedAdDisplayedEvent";
        }
        else // rewarded inters
        {
            name = @"OnRewardedInterstitialAdDisplayedEvent";
        }
        
        NSDictionary<NSString *, id> *args = [self defaultAdEventParametersForName: name withAd: ad];
        [self forwardUnityEventWithArgs: args];
    });
}

- (void)didFailToDisplayAd:(MAAd *)ad withError:(MAError *)error
{
    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        
        // BMLs do not support [DISPLAY] events in Unity
        MAAdFormat *adFormat = ad.format;
        if ( ![adFormat isFullscreenAd] ) return;
        
        NSString *name;
        if ( MAAdFormat.interstitial == adFormat )
        {
            name = @"OnInterstitialAdFailedToDisplayEvent";
        }
        else if ( MAAdFormat.appOpen == adFormat )
        {
            name = @"OnAppOpenAdFailedToDisplayEvent";
        }
        else if ( MAAdFormat.rewarded == adFormat )
        {
            name = @"OnRewardedAdFailedToDisplayEvent";
        }
        else // rewarded inters
        {
            name = @"OnRewardedInterstitialAdFailedToDisplayEvent";
        }
        
        NSMutableDictionary<NSString *, id> *args = [self defaultAdEventParametersForName: name withAd: ad];
        args[@"errorCode"] = [@(error.code) stringValue];
        args[@"errorMessage"] = error.message;
        args[@"mediatedNetworkErrorCode"] = [@(error.mediatedNetworkErrorCode) stringValue];
        args[@"mediatedNetworkErrorMessage"] = error.mediatedNetworkErrorMessage;
        args[@"waterfallInfo"] = [self createAdWaterfallInfo: error.waterfall];
        args[@"latencyMillis"] = [self requestLatencyMillisFromRequestLatency: error.requestLatency];
        [self forwardUnityEventWithArgs: args];
    });
}

- (void)didHideAd:(MAAd *)ad
{
    // BMLs do not support [HIDDEN] events in Unity
    MAAdFormat *adFormat = ad.format;
    if ( ![adFormat isFullscreenAd] ) return;
    
#if !IS_TEST_APP
    extern bool _didResignActive;
    if ( _didResignActive )
    {
        // If the application is not active, we should wait until application becomes active to resume unity.
        self.resumeUnityAfterApplicationBecomesActive = YES;
    }
    else
    {
        // UnityPause needs to be called on the main thread.
        UnityPause(NO);
    }
#endif
    
    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        
        NSString *name;
        if ( MAAdFormat.interstitial == adFormat )
        {
            name = @"OnInterstitialHiddenEvent";
        }
        else if ( MAAdFormat.appOpen == adFormat )
        {
            name = @"OnAppOpenAdHiddenEvent";
        }
        else if ( MAAdFormat.rewarded == adFormat )
        {
            name = @"OnRewardedAdHiddenEvent";
        }
        else // rewarded inters
        {
            name = @"OnRewardedInterstitialAdHiddenEvent";
        }
        
        NSDictionary<NSString *, id> *args = [self defaultAdEventParametersForName: name withAd: ad];
        [self forwardUnityEventWithArgs: args];
    });
}

- (void)didExpandAd:(MAAd *)ad
{
    MAAdFormat *adFormat = ad.format;
    if ( ![adFormat isAdViewAd] )
    {
        [self logInvalidAdFormat: adFormat];
        return;
    }
    
#if !IS_TEST_APP
    // UnityPause needs to be called on the main thread.
    UnityPause(YES);
#endif
    
    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        
        NSString *name;
        if ( MAAdFormat.mrec == adFormat )
        {
            name = @"OnMRecAdExpandedEvent";
        }
        else
        {
            name = @"OnBannerAdExpandedEvent";
        }
        
        NSDictionary<NSString *, id> *args = [self defaultAdEventParametersForName: name withAd: ad];
        [self forwardUnityEventWithArgs: args];
    });
}

- (void)didCollapseAd:(MAAd *)ad
{
    MAAdFormat *adFormat = ad.format;
    if ( ![adFormat isAdViewAd] )
    {
        [self logInvalidAdFormat: adFormat];
        return;
    }
    
#if !IS_TEST_APP
    extern bool _didResignActive;
    if ( _didResignActive )
    {
        // If the application is not active, we should wait until application becomes active to resume unity.
        self.resumeUnityAfterApplicationBecomesActive = YES;
    }
    else
    {
        // UnityPause needs to be called on the main thread.
        UnityPause(NO);
    }
#endif
    
    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        
        NSString *name;
        if ( MAAdFormat.mrec == adFormat )
        {
            name = @"OnMRecAdCollapsedEvent";
        }
        else
        {
            name = @"OnBannerAdCollapsedEvent";
        }
        
        NSDictionary<NSString *, id> *args = [self defaultAdEventParametersForName: name withAd: ad];
        [self forwardUnityEventWithArgs: args];
    });
}

- (void)didRewardUserForAd:(MAAd *)ad withReward:(MAReward *)reward
{
    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        
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
        
        
        NSMutableDictionary<NSString *, id> *args = [self defaultAdEventParametersForName: name withAd: ad];
        args[@"rewardLabel"] = rewardLabel;
        args[@"rewardAmount"] = rewardAmount;
        [self forwardUnityEventWithArgs: args];
    });
}

- (void)didPayRevenueForAd:(MAAd *)ad
{
    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        
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
        else if ( MAAdFormat.interstitial == adFormat )
        {
            name = @"OnInterstitialAdRevenuePaidEvent";
        }
        else if ( MAAdFormat.appOpen == adFormat )
        {
            name = @"OnAppOpenAdRevenuePaidEvent";
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
        
        NSMutableDictionary<NSString *, id> *args = [self defaultAdEventParametersForName: name withAd: ad];
        args[@"keepInBackground"] = @([adFormat isFullscreenAd]);
        [self forwardUnityEventWithArgs: args];
    });
}

- (void)didGenerateCreativeIdentifier:(NSString *)creativeIdentifier forAd:(MAAd *)ad
{
    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        
        NSString *name;
        MAAdFormat *adFormat = ad.format;
        if ( MAAdFormat.banner == adFormat || MAAdFormat.leader == adFormat )
        {
            name = @"OnBannerAdReviewCreativeIdGeneratedEvent";
        }
        else if ( MAAdFormat.mrec == adFormat )
        {
            name = @"OnMRecAdReviewCreativeIdGeneratedEvent";
        }
        else if ( MAAdFormat.interstitial == adFormat )
        {
            name = @"OnInterstitialAdReviewCreativeIdGeneratedEvent";
        }
        else if ( MAAdFormat.rewarded == adFormat )
        {
            name = @"OnRewardedAdReviewCreativeIdGeneratedEvent";
        }
        else if ( MAAdFormat.rewardedInterstitial == adFormat )
        {
            name = @"OnRewardedInterstitialAdReviewCreativeIdGeneratedEvent";
        }
        else
        {
            [self logInvalidAdFormat: adFormat];
            return;
        }
        
        NSMutableDictionary<NSString *, id> *args = [self defaultAdEventParametersForName: name withAd: ad];
        args[@"adReviewCreativeId"] = creativeIdentifier;
        args[@"keepInBackground"] = @([adFormat isFullscreenAd]);
        
        // Forward the event in background for fullscreen ads so that the user gets the callback even while the ad is playing.
        [self forwardUnityEventWithArgs: args];
    });
}

- (NSMutableDictionary<NSString *, id> *)defaultAdEventParametersForName:(NSString *)name withAd:(MAAd *)ad
{
    NSMutableDictionary<NSString *, id> *args = [[self adInfoForAd: ad] mutableCopy];
    args[@"name"] = name;
    
    return args;
}

#pragma mark - Internal Methods

- (void)createAdViewWithAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat atPosition:(NSString *)adViewPosition withOffset:(CGPoint)offset
{
    max_unity_dispatch_on_main_thread(^{
        [self log: @"Creating %@ with ad unit identifier \"%@\" and position: \"%@\"", adFormat, adUnitIdentifier, adViewPosition];
        
        if ( self.adViews[adUnitIdentifier] )
        {
            [self log: @"Trying to create a %@ that was already created. This will cause the current ad to be hidden.", adFormat.label];
        }
        
        // Retrieve ad view from the map
        MAAdView *adView = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat atPosition: adViewPosition withOffset: offset];
        adView.hidden = YES;
        self.safeAreaBackground.hidden = YES;
        
        // Position ad view immediately so if publisher sets color before ad loads, it will not be the size of the screen
        self.adViewAdFormats[adUnitIdentifier] = adFormat;
        [self positionAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
        
        NSDictionary<NSString *, NSString *> *extraParameters = self.adViewExtraParametersToSetAfterCreate[adUnitIdentifier];
        
        // Enable adaptive banners by default for banners and leaders.
        if ( [adFormat isBannerOrLeaderAd] )
        {
            // Check if there is already a pending setting for adaptive banners. If not, enable them.
            if ( !extraParameters[@"adaptive_banner"] )
            {
                [adView setExtraParameterForKey: @"adaptive_banner" value: @"true"];
            }
        }
        
        // Handle initial extra parameters if publisher sets it before creating ad view
        if ( extraParameters )
        {
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
        
        // Handle initial local extra parameters if publisher sets it before creating ad view
        if ( self.adViewLocalExtraParametersToSetAfterCreate[adUnitIdentifier] )
        {
            NSDictionary<NSString *, NSString *> *localExtraParameters = self.adViewLocalExtraParametersToSetAfterCreate[adUnitIdentifier];
            for ( NSString *key in localExtraParameters )
            {
                [adView setLocalExtraParameterForKey: key value: localExtraParameters[key]];
            }
            
            [self.adViewLocalExtraParametersToSetAfterCreate removeObjectForKey: adUnitIdentifier];
        }
        
        // Handle initial custom data if publisher sets it before creating ad view
        if ( self.adViewCustomDataToSetAfterCreate[adUnitIdentifier] )
        {
            NSString *customData = self.adViewCustomDataToSetAfterCreate[adUnitIdentifier];
            adView.customData = customData;
            
            [self.adViewCustomDataToSetAfterCreate removeObjectForKey: adUnitIdentifier];
        }
        
        [adView loadAd];
        
        // Disable auto-refresh if publisher sets it before creating the ad view.
        if ( [self.disabledAutoRefreshAdViewAdUnitIdentifiers containsObject: adUnitIdentifier] )
        {
            [adView stopAutoRefresh];
        }
        
        // The publisher may have requested to show the banner before it was created. Now that the banner is created, show it.
        if ( [self.adUnitIdentifiersToShowAfterCreate containsObject: adUnitIdentifier] )
        {
            [self showAdViewWithAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
            [self.adUnitIdentifiersToShowAfterCreate removeObject: adUnitIdentifier];
        }
    });
}

- (void)loadAdViewWithAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    max_unity_dispatch_on_main_thread(^{
        MAAdView *adView = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
        if ( !adView )
        {
            [self log: @"%@ does not exist for ad unit identifier \"%@\".", adFormat.label, adUnitIdentifier];
            return;
        }
        
        if ( ![self.disabledAutoRefreshAdViewAdUnitIdentifiers containsObject: adUnitIdentifier] )
        {
            if ( [adView isHidden] )
            {
                [self log: @"Auto-refresh will resume when the %@ ad is shown. You should only call LoadBanner() or LoadMRec() if you explicitly pause auto-refresh and want to manually load an ad.", adFormat.label];
                return;
            }
            
            [self log: @"You must stop auto-refresh if you want to manually load %@ ads.", adFormat.label];
            return;
        }
        
        [adView loadAd];
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

- (void)startAdViewAutoRefreshForAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    max_unity_dispatch_on_main_thread(^{
        [self log: @"Starting %@ auto refresh for ad unit identifier \"%@\"", adFormat.label, adUnitIdentifier];
        
        [self.disabledAutoRefreshAdViewAdUnitIdentifiers removeObject: adUnitIdentifier];
        
        MAAdView *adView = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
        if ( !adView )
        {
            [self log: @"%@ does not exist for ad unit identifier %@.", adFormat.label, adUnitIdentifier];
            return;
        }
        
        [adView startAutoRefresh];
    });
}

- (void)stopAdViewAutoRefreshForAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    max_unity_dispatch_on_main_thread(^{
        [self log: @"Stopping %@ auto refresh for ad unit identifier \"%@\"", adFormat.label, adUnitIdentifier];
        
        [self.disabledAutoRefreshAdViewAdUnitIdentifiers addObject: adUnitIdentifier];
        
        MAAdView *adView = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
        if ( !adView )
        {
            [self log: @"%@ does not exist for ad unit identifier %@.", adFormat.label, adUnitIdentifier];
            return;
        }
        
        [adView stopAutoRefresh];
    });
}

- (void)setAdViewWidth:(CGFloat)width forAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    max_unity_dispatch_on_main_thread(^{
        [self log: @"Setting width %f for \"%@\" with ad unit identifier \"%@\"", width, adFormat, adUnitIdentifier];
        
        BOOL isBannerOrLeader = adFormat.isBannerOrLeaderAd;
        
        // Banners and leaders need to be at least 320pts wide.
        CGFloat minWidth = isBannerOrLeader ? MAAdFormat.banner.size.width : adFormat.size.width;
        if ( width < minWidth )
        {
            [self log: @"The provided width: %f is smaller than the minimum required width: %f for ad format: %@. Automatically setting width to %f.", width, minWidth, adFormat, minWidth];
        }
        
        CGFloat widthToSet = MAX( minWidth, width );
        self.adViewWidths[adUnitIdentifier] = @(widthToSet);
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
            [self log: @"%@ does not exist for ad unit identifier \"%@\". Saving extra parameter to be set when it is created.", adFormat, adUnitIdentifier];
            
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

- (void)setAdViewLocalExtraParameterForAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat key:(NSString *)key value:(nullable id)value
{
    max_unity_dispatch_on_main_thread(^{
        [self log: @"Setting %@ local extra with key: \"%@\" value: \"%@\"", adFormat, key, value];
        
        MAAdView *adView = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
        if ( adView )
        {
            [adView setLocalExtraParameterForKey: key value: value];
        }
        else
        {
            [self log: @"%@ does not exist for ad unit identifier \"%@\". Saving local extra parameter to be set when it is created.", adFormat, adUnitIdentifier];
            
            // The adView has not yet been created. Store the loca extra parameters, so that they can be added once the adview has been created.
            NSMutableDictionary<NSString *, id> *localExtraParameters = self.adViewLocalExtraParametersToSetAfterCreate[adUnitIdentifier];
            if ( !localExtraParameters )
            {
                localExtraParameters = [NSMutableDictionary dictionaryWithCapacity: 1];
                self.adViewLocalExtraParametersToSetAfterCreate[adUnitIdentifier] = localExtraParameters;
            }
            
            localExtraParameters[key] = value;
        }
    });
}

- (void)setAdViewCustomData:(nullable NSString *)customData forAdUnitIdentifier:(NSString *)adUnitIdentifier adFormat:(MAAdFormat *)adFormat
{
    max_unity_dispatch_on_main_thread(^{
        
        MAAdView *adView = [self retrieveAdViewForAdUnitIdentifier: adUnitIdentifier adFormat: adFormat];
        if ( adView )
        {
            adView.customData = customData;
        }
        else
        {
            [self log: @"%@ does not exist for ad unit identifier %@. Saving custom data to be set when it is created.", adFormat, adUnitIdentifier];
            
            // The adView has not yet been created. Store the custom data, so that they can be added once the AdView has been created.
            self.adViewCustomDataToSetAfterCreate[adUnitIdentifier] = customData;
        }
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
        
        if ( ![self.disabledAutoRefreshAdViewAdUnitIdentifiers containsObject: adUnitIdentifier] )
        {
            [view startAutoRefresh];
        }
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
    return [MAUnityAdManager serializeParameters: @{@"origin_x" : [NSString stringWithFormat: @"%f", adViewFrame.origin.x],
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
        view.adReviewDelegate = nil;
        
        [view removeFromSuperview];
        
        [self.adViews removeObjectForKey: adUnitIdentifier];
        [self.adViewAdFormats removeObjectForKey: adUnitIdentifier];
        [self.adViewPositions removeObjectForKey: adUnitIdentifier];
        [self.adViewOffsets removeObjectForKey: adUnitIdentifier];
        [self.adViewWidths removeObjectForKey: adUnitIdentifier];
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
    if (max_unity_should_disable_all_logs()) return;
    
    va_list valist;
    va_start(valist, format);
    NSString *message = [[NSString alloc] initWithFormat: format arguments: valist];
    va_end(valist);
    
    NSLog(@"[%@] [%@] %@", SDK_TAG, TAG, message);
}

+ (void)log:(NSString *)format, ...
{
    if (max_unity_should_disable_all_logs()) return;

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
        result.adReviewDelegate = self;
        
        self.interstitials[adUnitIdentifier] = result;
    }
    
    return result;
}

- (MAAppOpenAd *)retrieveAppOpenAdForAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    MAAppOpenAd *result = self.appOpenAds[adUnitIdentifier];
    if ( !result )
    {
        result = [[MAAppOpenAd alloc] initWithAdUnitIdentifier: adUnitIdentifier sdk: self.sdk];
        result.delegate = self;
        result.revenueDelegate = self;
        
        self.appOpenAds[adUnitIdentifier] = result;
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
        result.adReviewDelegate = self;
        
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
        result.adReviewDelegate = self;
        
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
        result.adReviewDelegate = self;
        
        self.adViews[adUnitIdentifier] = result;
        self.adViewPositions[adUnitIdentifier] = adViewPosition;
        self.adViewOffsets[adUnitIdentifier] = [NSValue valueWithCGPoint: offset];
        
        UIViewController *rootViewController = [self unityViewController];
        [rootViewController.view addSubview: result];
        
        // Allow pubs to pause auto-refresh immediately, by default.
        [result setExtraParameterForKey: @"allow_pause_auto_refresh_immediately" value: @"true"];
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
        
        if ( (adFormat == MAAdFormat.banner || adFormat == MAAdFormat.leader) && !isAdaptiveBannerDisabled )
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
        
        UILayoutGuide *layoutGuide = superview.safeAreaLayoutGuide;
        
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
        
        self.adViewConstraints[adUnitIdentifier] = constraints;
        
        [NSLayoutConstraint activateConstraints: constraints];
    });
}

- (UIViewController *)unityViewController
{
    // Handle edge case where `UnityGetGLViewController()` returns nil
    return UnityGetGLViewController() ?: UnityGetMainWindow().rootViewController ?: [KEY_WINDOW rootViewController];
}

- (void)forwardUnityEventWithArgs:(NSDictionary<NSString *, id> *)args
{
#if !IS_TEST_APP
    extern bool _didResignActive;
    // We should not call any script callbacks when application is not active. Suspend the callback queue if resign is active.
    // We'll resume the queue once the application becomes active again.
    self.backgroundCallbackEventsQueue.suspended = _didResignActive;
#endif
    
    [self.backgroundCallbackEventsQueue addOperationWithBlock:^{
        NSString *serializedParameters = [MAUnityAdManager serializeParameters: args];
        backgroundCallback(serializedParameters.UTF8String);
    }];
}

+ (NSString *)serializeParameters:(NSDictionary<NSString *, id> *)dict
{
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject: dict options: 0 error: nil];
    return [[NSString alloc] initWithData: jsonData encoding: NSUTF8StringEncoding];
}

+ (NSDictionary<NSString *, id> *)deserializeParameters:(nullable NSString *)serialized
{
    if ( serialized.length > 0 )
    {
        NSError *error;
        NSDictionary<NSString *, id> *deserialized = [NSJSONSerialization JSONObjectWithData: [serialized dataUsingEncoding:NSUTF8StringEncoding]
                                                                                     options: 0
                                                                                       error: &error];
        if ( error )
        {
            [self log: @"Failed to deserialize (%@) with error %@", serialized, error];
            return @{};
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

- (NSString *)requestLatencyMillisFromRequestLatency:(NSTimeInterval)requestLatency
{
    // Convert latency from seconds to milliseconds to match Android.
    long requestLatencyMillis = requestLatency * 1000;
    return @(requestLatencyMillis).stringValue;
}

#pragma mark - User Service

- (void)didDismissUserConsentDialog
{
    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        [self forwardUnityEventWithArgs: @{@"name" : @"OnSdkConsentDialogDismissedEvent"}];
    });
}

#pragma mark - CMP Service

- (void)showCMPForExistingUser
{
    [self.sdk.cmpService showCMPForExistingUserWithCompletion:^(ALCMPError * _Nullable error) {
        
        dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
            NSMutableDictionary<NSString *, id> *args = [NSMutableDictionary dictionaryWithCapacity: 2];
            args[@"name"] = @"OnCmpCompletedEvent";
            
            if ( error )
            {
                args[@"error"] = @{@"code": @(error.code),
                                   @"message": error.message,
                                   @"cmpCode": @(error.cmpCode),
                                   @"cmpMessage": error.cmpMessage,
                                   @"keepInBackground": @(YES)};
            }
            
            [self forwardUnityEventWithArgs: args];
        });
    }];
}

#pragma mark - Application

- (void)applicationPaused:(NSNotification *)notification
{
    [self notifyApplicationStateChangedEventForPauseState: YES];
}

- (void)applicationResumed:(NSNotification *)notification
{
    [self notifyApplicationStateChangedEventForPauseState: NO];
}

- (void)notifyApplicationStateChangedEventForPauseState:(BOOL)isPaused
{
    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        [self forwardUnityEventWithArgs: @{@"name": @"OnApplicationStateChanged",
                                           @"isPaused": @(isPaused)}];
    });
}

- (MAAd *)adWithAdUnitIdentifier:(NSString *)adUnitIdentifier
{
    @synchronized ( self.adInfoDictLock )
    {
        return self.adInfoDict[adUnitIdentifier];
    }
}

@end
