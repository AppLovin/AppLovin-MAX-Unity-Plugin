//
//  MAUnityAdManager.h
//  AppLovin MAX Unity Plugin
//

#import <Foundation/Foundation.h>
#import <AppLovinSDK/AppLovinSDK.h>

NS_ASSUME_NONNULL_BEGIN
typedef const void *MAUnityRef;
typedef void (*ALUnityBackgroundCallback)(const char* args);

@interface MAUnityAdManager : NSObject

- (void)initializeSdkWithConfiguration:(ALSdkInitializationConfiguration *)initConfig andCompletionHandler:(ALSdkInitializationCompletionHandler)completionHandler;

- (void)createBannerWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier atPosition:(nullable NSString *)bannerPosition;
- (void)createBannerWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier x:(CGFloat)xOffset y:(CGFloat)yOffset;
- (void)loadBannerWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)setBannerBackgroundColorForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier hexColorCode:(nullable NSString *)hexColorCode;
- (void)setBannerPlacement:(nullable NSString *)placement forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)startBannerAutoRefreshForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)stopBannerAutoRefreshForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)setBannerExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable NSString *)value;
- (void)setBannerLocalExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable id)value;
- (void)setBannerCustomData:(nullable NSString *)customData forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)setBannerWidth:(CGFloat)width forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)updateBannerPosition:(nullable NSString *)bannerPosition forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)updateBannerPosition:(CGFloat)xOffset y:(CGFloat)yOffset forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)showBannerWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)destroyBannerWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)hideBannerWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (NSString *)bannerLayoutForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
+ (CGFloat)adaptiveBannerHeightForWidth:(CGFloat)width;

- (void)createMRecWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier atPosition:(nullable NSString *)mrecPosition;
- (void)createMRecWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier x:(CGFloat)xOffset y:(CGFloat)yOffset;
- (void)loadMRecWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)setMRecPlacement:(nullable NSString *)placement forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)startMRecAutoRefreshForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)stopMRecAutoRefreshForAdUnitIdentifier:(nullable NSString *)adUnitIdentifer;
- (void)setMRecExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable NSString *)value;
- (void)setMRecLocalExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable id)value;
- (void)setMRecCustomData:(nullable NSString *)customData forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)showMRecWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)destroyMRecWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)hideMRecWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)updateMRecPosition:(nullable NSString *)mrecPosition forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)updateMRecPosition:(CGFloat)xOffset y:(CGFloat)yOffset forAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (NSString *)mrecLayoutForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;

- (void)loadInterstitialWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (BOOL)isInterstitialReadyWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)showInterstitialWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier placement:(nullable NSString *)placement customData:(nullable NSString *)customData;
- (void)setInterstitialExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable NSString *)value;
- (void)setInterstitialLocalExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable id)value;

- (void)loadAppOpenAdWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (BOOL)isAppOpenAdReadyWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)showAppOpenAdWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier placement:(nullable NSString *)placement customData:(nullable NSString *)customData;
- (void)setAppOpenAdExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable NSString *)value;
- (void)setAppOpenAdLocalExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable id)value;

- (void)loadRewardedAdWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (BOOL)isRewardedAdReadyWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)showRewardedAdWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier placement:(nullable NSString *)placement customData:(nullable NSString *)customData;
- (void)setRewardedAdExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable NSString *)value;
- (void)setRewardedAdLocalExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable id)value;

- (void)loadRewardedInterstitialAdWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (BOOL)isRewardedInterstitialAdReadyWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;
- (void)showRewardedInterstitialAdWithAdUnitIdentifier:(nullable NSString *)adUnitIdentifier placement:(nullable NSString *)placement customData:(nullable NSString *)customData;
- (void)setRewardedInterstitialAdExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable NSString *)value;
- (void)setRewardedInterstitialAdLocalExtraParameterForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier key:(nullable NSString *)key value:(nullable id)value;

// Event Tracking
- (void)trackEvent:(nullable NSString *)event parameters:(nullable NSString *)parameters;

// Ad Info
- (NSString *)adInfoForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier;

// Ad Value
- (NSString *)adValueForAdUnitIdentifier:(nullable NSString *)adUnitIdentifier withKey:(nullable NSString *)key;

// User Service
- (void)didDismissUserConsentDialog;

// CMP Service
- (void)showCMPForExistingUser;

// Utils
+ (NSString *)serializeParameters:(NSDictionary<NSString *, id> *)dict;
+ (NSDictionary<NSString *, id> *)deserializeParameters:(nullable NSString *)serialized;

+ (void)setUnityBackgroundCallback:(ALUnityBackgroundCallback)unityBackgroundCallback;

/**
 * Creates an instance of @c MAUnityAdManager if needed and returns the singleton instance.
 */
+ (instancetype)shared;

- (instancetype)init NS_UNAVAILABLE;

@end

NS_ASSUME_NONNULL_END
