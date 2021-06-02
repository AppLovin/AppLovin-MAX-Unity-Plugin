//
//  MAUnityPlugin.mm
//  AppLovin MAX Unity Plugin
//

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"

#import "MAUnityAdManager.h"

#define NSSTRING(_X) ( (_X != NULL) ? [NSString stringWithCString: _X encoding: NSStringEncodingConversionAllowLossy] : nil)

UIView* UnityGetGLView();

// When native code plugin is implemented in .mm / .cpp file, then functions
// should be surrounded with extern "C" block to conform C function naming rules
extern "C"
{
    static NSString *const TAG = @"MAUnityPlugin";
    static NSString *const ARRAY_ITEM_SEPARATOR = @",\n";
    
    static ALSdk *_sdk;
    static MAUnityAdManager *_adManager;
    static bool _isPluginInitialized = false;
    static bool _isSdkInitialized = false;
    static ALSdkConfiguration *_sdkConfiguration;
    
    // Store these values if pub attempts to set it before calling _MaxInitializeSdk()
    static NSString *_userIdentifierToSet;
    static NSString *_userSegmentNameToSet;
    static NSArray<NSString *> *_testDeviceIdentifiersToSet;
    static NSNumber *_verboseLoggingToSet;
    static NSNumber *_creativeDebuggerEnabledToSet;
    static NSNumber *_exceptionHandlerEnabledToSet;
    
    // Helper method to create C string copy
    static const char * cStringCopy(NSString *string);
    
    bool isPluginInitialized()
    {
        return _isPluginInitialized;
    }
    
    void maybeInitializePlugin()
    {
        if ( isPluginInitialized() ) return;
        
        _adManager = [MAUnityAdManager shared];
        _isPluginInitialized = true;
    }
    
    void _MaxSetSdkKey(const char *sdkKey)
    {
        maybeInitializePlugin();
        
        if (!sdkKey) return;
        
        NSString *sdkKeyStr = [NSString stringWithUTF8String: sdkKey];
        
        NSDictionary *infoDict = [[NSBundle mainBundle] infoDictionary];
        [infoDict setValue: sdkKeyStr forKey: @"AppLovinSdkKey"];
    }
    
    void _MaxInitializeSdk(const char *serializedAdUnitIdentifiers, const char *serializedMetaData)
    {
        maybeInitializePlugin();
        
        _sdk = [_adManager initializeSdkWithAdUnitIdentifiers: NSSTRING(serializedAdUnitIdentifiers)
                                                     metaData: NSSTRING(serializedMetaData)
                                         andCompletionHandler:^(ALSdkConfiguration *configuration) {
            _sdkConfiguration = configuration;
            _isSdkInitialized = true;
        }];
        
        if ( _userIdentifierToSet )
        {
            _sdk.userIdentifier = _userIdentifierToSet;
            _userIdentifierToSet = nil;
        }
        
        if ( _userSegmentNameToSet )
        {
            _sdk.userSegment.name = _userSegmentNameToSet;
            _userSegmentNameToSet = nil;
        }
        
        if ( _testDeviceIdentifiersToSet )
        {
            _sdk.settings.testDeviceAdvertisingIdentifiers = _testDeviceIdentifiersToSet;
            _testDeviceIdentifiersToSet = nil;
        }
        
        if ( _verboseLoggingToSet )
        {
            _sdk.settings.isVerboseLogging = _verboseLoggingToSet.boolValue;
            _verboseLoggingToSet = nil;
        }

        if ( _creativeDebuggerEnabledToSet )
        {
            _sdk.settings.creativeDebuggerEnabled = _creativeDebuggerEnabledToSet.boolValue;
            _creativeDebuggerEnabledToSet = nil;
        }

        if ( _exceptionHandlerEnabledToSet )
        {
            _sdk.settings.exceptionHandlerEnabled = _exceptionHandlerEnabledToSet.boolValue;
            _exceptionHandlerEnabledToSet = nil;
        }
    }
    
    bool _MaxIsInitialized()
    {
        return _isPluginInitialized && _isSdkInitialized;
    }

    const char * _MaxGetAvailableMediatedNetworks()
    {
        if ( !_sdk )
        {
            NSLog(@"[%@] Failed to get available mediated networks - please ensure the AppLovin MAX Unity Plugin has been initialized by calling 'MaxSdk.InitializeSdk();'!", TAG);
            return cStringCopy(@"");
        }
        
        NSArray<MAMediatedNetworkInfo *> *availableMediatedNetworks = [_sdk availableMediatedNetworks];
        
        // Create array of serialized network strings
        NSMutableArray<NSString *> *serializedNetworks = [NSMutableArray arrayWithCapacity: availableMediatedNetworks.count];
        for ( MAMediatedNetworkInfo *mediatedNetwork in availableMediatedNetworks )
        {
            NSString *result = [MAUnityAdManager propsStrFromDictionary: @{@"name" : mediatedNetwork.name,
                                                                           @"adapterClassName" : mediatedNetwork.adapterClassName,
                                                                           @"adapterVersion" : mediatedNetwork.adapterVersion,
                                                                           @"sdkVersion" : mediatedNetwork.sdkVersion}];
            [serializedNetworks addObject: result];
        }
        
        return cStringCopy([serializedNetworks componentsJoinedByString: ARRAY_ITEM_SEPARATOR]);
    }
    
    void _MaxShowMediationDebugger()
    {
        if ( !_sdk )
        {
            NSLog(@"[%@] Failed to show mediation debugger - please ensure the AppLovin MAX Unity Plugin has been initialized by calling 'MaxSdk.InitializeSdk();'!", TAG);
            return;
        }
        
        [_sdk showMediationDebugger];
    }

    void _MaxShowConsentDialog()
    {
        if ( !_sdk )
        {
            NSLog(@"[%@] Failed to show consent dialog - please ensure the AppLovin MAX Unity Plugin has been initialized by calling 'MaxSdk.InitializeSdk();'!", TAG);
            return;
        }

        [_sdk.userService showConsentDialogWithCompletionHandler:^{
            [_adManager didDismissUserConsentDialog];
        }];
    }
    
    int _MaxConsentDialogState()
    {
        if (!isPluginInitialized()) return ALConsentDialogStateUnknown;
        
        return (int) _sdkConfiguration.consentDialogState;
    }
    
    void _MaxSetUserId(const char *userId)
    {
        if ( _sdk )
        {
            _sdk.userIdentifier = NSSTRING(userId);
        }
        else
        {
            _userIdentifierToSet = NSSTRING(userId);
        }
    }
    
    void _MaxSetUserSegmentField(const char *serializedKey, const char *serializedValue)
    {
        // NSString *key = NSSTRING(serializedKey); // To be ignored until we add more properties
        NSString *value = NSSTRING(serializedValue);
        
        if ( _sdk )
        {
            _sdk.userSegment.name = value;
        }
        else
        {
            _userSegmentNameToSet = value;
        }
    }

    const char * _MaxGetSdkConfiguration()
    {
        if ( !_sdk )
        {
            NSLog(@"[%@] Failed to get SDK configuration - please ensure the AppLovin MAX Unity Plugin has been initialized by calling 'MaxSdk.InitializeSdk();'!", TAG);
            return cStringCopy(@"");
        }
        
        NSString *consentDialogStateStr = @(_sdk.configuration.consentDialogState).stringValue;
        NSString *appTrackingStatus = @(_sdk.configuration.appTrackingTransparencyStatus).stringValue; // Deliberately name it `appTrackingStatus` to be a bit more generic (in case Android introduces a similar concept)

        return cStringCopy([MAUnityAdManager propsStrFromDictionary: @{@"consentDialogState" : consentDialogStateStr,
                                                                       @"countryCode" : _sdk.configuration.countryCode,
                                                                       @"appTrackingStatus" : appTrackingStatus}]);
    }
    
    void _MaxSetHasUserConsent(bool hasUserConsent)
    {
        [ALPrivacySettings setHasUserConsent: hasUserConsent];
    }
    
    bool _MaxHasUserConsent()
    {
        return [ALPrivacySettings hasUserConsent];
    }
    
    void _MaxSetIsAgeRestrictedUser(bool isAgeRestrictedUser)
    {
        [ALPrivacySettings setIsAgeRestrictedUser: isAgeRestrictedUser];
    }
    
    bool _MaxIsAgeRestrictedUser()
    {
        return [ALPrivacySettings isAgeRestrictedUser];
    }
    
    void _MaxSetDoNotSell(bool doNotSell)
    {
        [ALPrivacySettings setDoNotSell: doNotSell];
    }
    
    bool _MaxIsDoNotSell()
    {
        return [ALPrivacySettings isDoNotSell];
    }
    
    void _MaxCreateBanner(const char *adUnitIdentifier, const char *bannerPosition)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager createBannerWithAdUnitIdentifier: NSSTRING(adUnitIdentifier) atPosition: NSSTRING(bannerPosition)];
    }

    void _MaxCreateBannerXY(const char *adUnitIdentifier, const float x, const float y)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager createBannerWithAdUnitIdentifier: NSSTRING(adUnitIdentifier) x: x y: y];
    }
    
    void _MaxSetBannerBackgroundColor(const char *adUnitIdentifier, const char *hexColorCode)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager setBannerBackgroundColorForAdUnitIdentifier: NSSTRING(adUnitIdentifier) hexColorCode: NSSTRING(hexColorCode)];
    }
    
    void _MaxSetBannerPlacement(const char *adUnitIdentifier, const char *placement)
    {
        [_adManager setBannerPlacement: NSSTRING(placement) forAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxSetBannerExtraParameter(const char *adUnitIdentifier, const char *key, const char *value)
    {
        [_adManager setBannerExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                           key: NSSTRING(key)
                                                         value: NSSTRING(value)];
    }

    void _MaxSetBannerWidth(const char *adUnitIdentifier, const float width)
    {
        [_adManager setBannerWidth: width forAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxUpdateBannerPosition(const char *adUnitIdentifier, const char *bannerPosition)
    {
        [_adManager updateBannerPosition: NSSTRING(bannerPosition) forAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }

    void _MaxUpdateBannerPositionXY(const char *adUnitIdentifier, const float x, const float y)
    {
        [_adManager updateBannerPosition: x y: y forAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxShowBanner(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager showBannerWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxDestroyBanner(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager destroyBannerWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxHideBanner(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager hideBannerWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    const char * _MaxGetBannerLayout(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return cStringCopy(@"");
        
        return cStringCopy([_adManager bannerLayoutForAdUnitIdentifier: NSSTRING(adUnitIdentifier)]);
    }
    
    void _MaxCreateMRec(const char *adUnitIdentifier, const char *mrecPosition)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager createMRecWithAdUnitIdentifier: NSSTRING(adUnitIdentifier) atPosition: NSSTRING(mrecPosition)];
    }
    
    void _MaxCreateMRecXY(const char *adUnitIdentifier, const float x, const float y)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager createMRecWithAdUnitIdentifier: NSSTRING(adUnitIdentifier) x: x y: y];
    }
    
    void _MaxSetMRecPlacement(const char *adUnitIdentifier, const char *placement)
    {
        [_adManager setMRecPlacement: NSSTRING(placement) forAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxUpdateMRecPosition(const char *adUnitIdentifier, const char *mrecPosition)
    {
        [_adManager updateMRecPosition: NSSTRING(mrecPosition) forAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxUpdateMRecPositionXY(const char *adUnitIdentifier, const float x, const float y)
    {
        [_adManager updateMRecPosition: x y: y forAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxShowMRec(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager showMRecWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxDestroyMRec(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager destroyMRecWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxHideMRec(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager hideMRecWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }

    void _MaxSetMRecExtraParameter(const char *adUnitIdentifier, const char *key, const char *value)
    {
        [_adManager setMRecExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                         key: NSSTRING(key)
                                                       value: NSSTRING(value)];
    }

    const char * _MaxGetMRecLayout(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return cStringCopy(@"");
        
        return cStringCopy([_adManager mrecLayoutForAdUnitIdentifier: NSSTRING(adUnitIdentifier)]);
    }

    void _MaxCreateCrossPromoAd(const char *adUnitIdentifier, const float x, const float y, const float width, const float height, const float rotation)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager createCrossPromoAdWithAdUnitIdentifier: NSSTRING(adUnitIdentifier) x: x y: y width: width height: height rotation: rotation];
    }

    void _MaxSetCrossPromoAdPlacement(const char *adUnitIdentifier, const char *placement)
    {
        [_adManager setCrossPromoAdPlacement: NSSTRING(placement) forAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }

    void _MaxUpdateCrossPromoAdPosition(const char *adUnitIdentifier, const float x, const float y, const float width, const float height, const float rotation)
    {
        [_adManager updateCrossPromoAdPositionForAdUnitIdentifier: NSSTRING(adUnitIdentifier) x: x y: y width: width height: height rotation: rotation];
    }

    void _MaxShowCrossPromoAd(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager showCrossPromoAdWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }

    void _MaxDestroyCrossPromoAd(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager destroyCrossPromoAdWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }

    void _MaxHideCrossPromoAd(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager hideCrossPromoAdWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }

    const char * _MaxGetCrossPromoAdLayout(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return cStringCopy(@"");
        
        return cStringCopy([_adManager crossPromoAdLayoutForAdUnitIdentifier: NSSTRING(adUnitIdentifier)]);
    }
    
    void _MaxLoadInterstitial(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager loadInterstitialWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxSetInterstitialExtraParameter(const char *adUnitIdentifier, const char *key, const char *value)
    {
        [_adManager setInterstitialExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                                 key: NSSTRING(key)
                                                               value: NSSTRING(value)];
    }
    
    bool _MaxIsInterstitialReady(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return false;
        
        return [_adManager isInterstitialReadyWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxShowInterstitial(const char *adUnitIdentifier, const char *placement)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager showInterstitialWithAdUnitIdentifier: NSSTRING(adUnitIdentifier) placement: NSSTRING(placement)];
    }
    
    void _MaxLoadRewardedAd(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager loadRewardedAdWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxSetRewardedAdExtraParameter(const char *adUnitIdentifier, const char *key, const char *value)
    {
        [_adManager setRewardedAdExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                               key: NSSTRING(key)
                                                             value: NSSTRING(value)];
    }
    
    bool _MaxIsRewardedAdReady(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return false;
        
        return [_adManager isRewardedAdReadyWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxShowRewardedAd(const char *adUnitIdentifier, const char *placement)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager showRewardedAdWithAdUnitIdentifier: NSSTRING(adUnitIdentifier) placement: NSSTRING(placement)];
    }
    
    void _MaxLoadRewardedInterstitialAd(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager loadRewardedInterstitialAdWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxSetRewardedInterstitialAdExtraParameter(const char *adUnitIdentifier, const char *key, const char *value)
    {
        [_adManager setRewardedInterstitialAdExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                                           key: NSSTRING(key)
                                                                         value: NSSTRING(value)];
    }
    
    bool _MaxIsRewardedInterstitialAdReady(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return false;
        
        return [_adManager isRewardedInterstitialAdReadyWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxShowRewardedInterstitialAd(const char *adUnitIdentifier, const char *placement)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager showRewardedInterstitialAdWithAdUnitIdentifier: NSSTRING(adUnitIdentifier) placement: NSSTRING(placement)];
    }
    
    void _MaxTrackEvent(const char *event, const char *parameters)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager trackEvent: NSSTRING(event) parameters: NSSTRING(parameters)];
    }
        
    bool _MaxGetBool(const char *key, bool defaultValue)
    {
        if ( !_sdk ) return defaultValue;
        
        return [_sdk.variableService boolForKey: NSSTRING(key) defaultValue: defaultValue];
    }
    
    const char * _MaxGetString(const char *key, const char *defaultValue)
    {
        if ( !_sdk ) return defaultValue;
        
        return cStringCopy([_sdk.variableService stringForKey: NSSTRING(key) defaultValue: NSSTRING(defaultValue)]);
    }
    
    bool _MaxIsTablet()
    {
        return [UIDevice currentDevice].userInterfaceIdiom == UIUserInterfaceIdiomPad;
    }
    
    static const char * cStringCopy(NSString *string)
    {
        const char *value = string.UTF8String;
        return value ? strdup(value) : NULL;
    }
    
    void _MaxSetMuted(bool muted)
    {
        if ( !_sdk ) return;
        
        _sdk.settings.muted = muted;
    }
    
    bool _MaxIsMuted()
    {
        if ( !_sdk ) return false;
        
        return _sdk.settings.muted;
    }
    
    float _MaxScreenDensity()
    {
        return [UIScreen.mainScreen scale];
    }
    
    const char * _MaxGetAdInfo(const char *adUnitIdentifier)
    {
        return cStringCopy([_adManager adInfoForAdUnitIdentifier: NSSTRING(adUnitIdentifier)]);
    }
    
    const char * _MaxGetAdValue(const char *adUnitIdentifier, const char *key)
    {
        return cStringCopy([_adManager adValueForAdUnitIdentifier: NSSTRING(adUnitIdentifier) withKey: NSSTRING(key)]);
    }

    void _MaxSetVerboseLogging(bool enabled)
    {
        if ( _sdk )
        {
            _sdk.settings.isVerboseLogging = enabled;
            _verboseLoggingToSet = nil;
        }
        else
        {
            _verboseLoggingToSet = @(enabled);
        }
    }
    
    bool _MaxIsVerboseLoggingEnabled()
    {
        if ( _sdk )
        {
            return _sdk.settings.isVerboseLogging;
        }
        else if ( _verboseLoggingToSet )
        {
            return _verboseLoggingToSet;
        }

        return false;
    }

    void _MaxSetTestDeviceAdvertisingIdentifiers(char **advertisingIdentifiers, int size)
    {
        NSMutableArray<NSString *> *advertisingIdentifiersArray = [NSMutableArray arrayWithCapacity: size];
        for (int i = 0; i < size; i++)
        {
            [advertisingIdentifiersArray addObject: NSSTRING(advertisingIdentifiers[i])];
        }
        
        if ( _sdk )
        {
            _sdk.settings.testDeviceAdvertisingIdentifiers = advertisingIdentifiersArray;
            _testDeviceIdentifiersToSet = nil;
        }
        else
        {
            _testDeviceIdentifiersToSet = advertisingIdentifiersArray;
        }
    }

    void _MaxSetCreativeDebuggerEnabled(bool enabled)
    {
        if ( _sdk )
        {
            _sdk.settings.creativeDebuggerEnabled = enabled;
            _creativeDebuggerEnabledToSet = nil;
        }
        else
        {
            _creativeDebuggerEnabledToSet = @(enabled);
        }
    }
    
    void _MaxSetExceptionHandlerEnabled(bool enabled)
    {
        if ( _sdk )
        {
            _sdk.settings.exceptionHandlerEnabled = enabled;
            _exceptionHandlerEnabledToSet = nil;
        }
        else
        {
            _exceptionHandlerEnabledToSet = @(enabled);
        }
    }

    float _MaxGetAdaptiveBannerHeight(const float width)
    {
        return [MAUnityAdManager adaptiveBannerHeightForWidth: width];
    }

    [[deprecated("This API has been deprecated. Please use our SDK's initialization callback to retrieve variables instead.")]]
    void _MaxLoadVariables()
    {
        if (!isPluginInitialized()) return;
        
        [_adManager loadVariables];
    }
}

#pragma clang diagnostic pop
