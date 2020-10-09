//
//  MAUnityAdManager.h
//  AppLovin MAX Unity Plugin
//

#import <Foundation/Foundation.h>
#import <AppLovinSDK/AppLovinSDK.h>

NS_ASSUME_NONNULL_BEGIN

@interface MAUnityAdManager : NSObject

- (ALSdk *)initializeSdkWithAdUnitIdentifiers:(NSString *)serializedAdUnitIdentifiers metaData:(NSString *)serializedMetaData andCompletionHandler:(ALSdkInitializationCompletionHandler)completionHandler;

- (void)createBannerWithAdUnitIdentifier:(NSString *)adUnitIdentifier atPosition:(NSString *)bannerPosition;
- (void)setBannerBackgroundColorForAdUnitIdentifier:(NSString *)adUnitIdentifier hexColorCode:(NSString *)hexColorCode;
- (void)setBannerPlacement:(nullable NSString *)placement forAdUnitIdentifier:(NSString *)adUnitIdentifier;
- (void)setBannerExtraParameterForAdUnitIdentifier:(NSString *)adUnitIdentifier key:(NSString *)key value:(nullable NSString *)value;
- (void)updateBannerPosition:(NSString *)bannerPosition forAdUnitIdentifier:(NSString *)adUnitIdentifier;
- (void)showBannerWithAdUnitIdentifier:(NSString *)adUnitIdentifier;
- (void)destroyBannerWithAdUnitIdentifier:(NSString *)adUnitIdentifier;
- (void)hideBannerWithAdUnitIdentifier:(NSString *)adUnitIdentifier;

- (void)createMRecWithAdUnitIdentifier:(NSString *)adUnitIdentifier atPosition:(NSString *)mrecPosition;
- (void)setMRecPlacement:(nullable NSString *)placement forAdUnitIdentifier:(NSString *)adUnitIdentifier;
- (void)updateMRecPosition:(NSString *)mrecPosition forAdUnitIdentifier:(NSString *)adUnitIdentifier;
- (void)showMRecWithAdUnitIdentifier:(NSString *)adUnitIdentifier;
- (void)destroyMRecWithAdUnitIdentifier:(NSString *)adUnitIdentifier;
- (void)hideMRecWithAdUnitIdentifier:(NSString *)adUnitIdentifier;

- (void)loadInterstitialWithAdUnitIdentifier:(NSString *)adUnitIdentifier;
- (BOOL)isInterstitialReadyWithAdUnitIdentifier:(NSString *)adUnitIdentifier;
- (void)showInterstitialWithAdUnitIdentifier:(NSString *)adUnitIdentifier placement:(NSString *)placement;
- (void)setInterstitialExtraParameterForAdUnitIdentifier:(NSString *)adUnitIdentifier key:(NSString *)key value:(nullable NSString *)value;

- (void)loadRewardedAdWithAdUnitIdentifier:(NSString *)adUnitIdentifier;
- (BOOL)isRewardedAdReadyWithAdUnitIdentifier:(NSString *)adUnitIdentifier;
- (void)showRewardedAdWithAdUnitIdentifier:(NSString *)adUnitIdentifier placement:(NSString *)placement;
- (void)setRewardedAdExtraParameterForAdUnitIdentifier:(NSString *)adUnitIdentifier key:(NSString *)key value:(nullable NSString *)value;

- (void)loadRewardedInterstitialAdWithAdUnitIdentifier:(NSString *)adUnitIdentifier;
- (BOOL)isRewardedInterstitialAdReadyWithAdUnitIdentifier:(NSString *)adUnitIdentifier;
- (void)showRewardedInterstitialAdWithAdUnitIdentifier:(NSString *)adUnitIdentifier placement:(NSString *)placement;
- (void)setRewardedInterstitialAdExtraParameterForAdUnitIdentifier:(NSString *)adUnitIdentifier key:(NSString *)key value:(nullable NSString *)value;

// Event Tracking
- (void)trackEvent:(NSString *)event parameters:(NSString *)parameters;

// Ad Info
- (NSString *)adInfoForAdUnitIdentifier:(NSString *)adUnitIdentifier;

/**
 * Creates an instance of @c MAUnityAdManager if needed and returns the singleton instance.
 */
+ (instancetype)shared;

- (instancetype)init NS_UNAVAILABLE;

@end

@interface MAUnityAdManager(ALDeprecated)
- (void)loadVariables __deprecated_msg("This API has been deprecated. Please use our SDK's initialization callback to retrieve variables instead.");
@end

NS_ASSUME_NONNULL_END
