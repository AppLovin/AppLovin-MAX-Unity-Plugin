//
//  MAUnityPlugin.mm
//  AppLovin MAX Unity Plugin
//

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"

#import "MAUnityAdManager.h"

#define NSSTRING(_X) ( (_X != NULL) ? [NSString stringWithCString: _X encoding: NSStringEncodingConversionAllowLossy].al_stringByTrimmingWhitespace : nil)

@interface NSString (ALUtils)
@property (nonatomic, copy, readonly) NSString *al_stringByTrimmingWhitespace;
@property (assign, readonly, getter=al_isValidString) BOOL al_validString;
@end

// When native code plugin is implemented in .mm / .cpp file, then functions
// should be surrounded with extern "C" block to conform C function naming rules
extern "C"
{
    static NSString *const TAG = @"MAUnityPlugin";

    UIView* UnityGetGLView();
    
    static ALSdk *_sdk;
    static MAUnityAdManager *_adManager;
    static bool _isPluginInitialized = false;
    static bool _isSdkInitialized = false;
    static ALSdkConfiguration *_sdkConfiguration;
    
    // Store these values if pub attempts to set it before calling _MaxInitializeSdk()
    static NSString *_userIdentifierToSet;
    static NSString *_userSegmentNameToSet;
    static NSArray<NSString *> *_testDeviceIdentifiersToSet;
    static NSNumber *_mutedToSet;
    static NSNumber *_verboseLoggingToSet;
    static NSNumber *_creativeDebuggerEnabledToSet;
    static NSNumber *_exceptionHandlerEnabledToSet;
    static NSNumber *_locationCollectionEnabledToSet;
    static NSNumber *_targetingYearOfBirth;
    static NSString *_targetingGender;
    static NSNumber *_targetingMaximumAdContentRating;
    static NSString *_targetingEmail;
    static NSString *_targetingPhoneNumber;
    static NSArray<NSString *> *_targetingKeywords;
    static NSArray<NSString *> *_targetingInterests;
    static NSMutableDictionary<NSString *, NSString *> *_extraParametersToSet = [NSMutableDictionary dictionary];
    static NSObject *_extraParametersToSetLock = [[NSObject alloc] init];
    
    // Helper method to create C string copy
    static const char * cStringCopy(NSString *string);
    // Helper method to log errors
    void logUninitializedAccessError(const char *callingMethod);

    int getConsentStatusValue(NSNumber *consentStatus)
    {
        if ( consentStatus )
        {
            return consentStatus.intValue;
        }
        else
        {
            return -1;
        }
    }
    
    bool isPluginInitialized()
    {
        return _isPluginInitialized;
    }

    bool isReadyToInteractWithSdk()
    {
        return isPluginInitialized() && _sdk;
    }
    
    void maybeInitializePlugin()
    {
        if ( isPluginInitialized() ) return;
        
        _adManager = [MAUnityAdManager shared];
        _isPluginInitialized = true;
    }

    id getLocalExtraParameterValue(const char *json)
    {
        NSData *jsonData = [NSSTRING(json) dataUsingEncoding: NSUTF8StringEncoding];
        NSError *error;
        NSDictionary *jsonDict = [NSJSONSerialization JSONObjectWithData: jsonData
                                                                 options: 0
                                                                   error: &error];
        
        if ( error )
        {
            return nil;
        }
        else
        {
            return jsonDict[@"value"];
        }
    }

    NSArray<NSString *> * toStringArray(char **arrayPointer, int size)
    {
        NSMutableArray<NSString *> *array = [NSMutableArray arrayWithCapacity: size];
        for ( int i = 0; i < size; i++ )
        {
            NSString *element = NSSTRING(arrayPointer[i]);
            if ( element )
            {
                [array addObject: element];
            }
        }
        
        return array;
    }
    
    void setPendingExtraParametersIfNeeded(ALSdkSettings *settings)
    {
        NSDictionary *extraParameters;
        @synchronized ( _extraParametersToSetLock )
        {
            if ( _extraParametersToSet.count <= 0 ) return;
            
            extraParameters = [NSDictionary dictionaryWithDictionary: _extraParametersToSet];
            [_extraParametersToSet removeAllObjects];
        }
        
        for ( NSString *key in extraParameters.allKeys )
        {
            [settings setExtraParameterForKey: key value: extraParameters[key]];
        }
    }
    
    ALSdkSettings * generateSDKSettings(const char *serializedAdUnitIdentifiers, const char *serializedMetaData)
    {
        ALSdkSettings *settings = [[ALSdkSettings alloc] init];
        
        setPendingExtraParametersIfNeeded( settings );
        
        if ( _testDeviceIdentifiersToSet )
        {
            settings.testDeviceAdvertisingIdentifiers = _testDeviceIdentifiersToSet;
            _testDeviceIdentifiersToSet = nil;
        }
        
        if ( _mutedToSet != nil)
        {
            settings.muted = _mutedToSet.boolValue;
            _mutedToSet = nil;
        }
        
        if ( _verboseLoggingToSet != nil )
        {
            settings.verboseLoggingEnabled = _verboseLoggingToSet.boolValue;
            _verboseLoggingToSet = nil;
        }

        if ( _creativeDebuggerEnabledToSet != nil )
        {
            settings.creativeDebuggerEnabled = _creativeDebuggerEnabledToSet.boolValue;
            _creativeDebuggerEnabledToSet = nil;
        }

        if ( _exceptionHandlerEnabledToSet != nil )
        {
            settings.exceptionHandlerEnabled = _exceptionHandlerEnabledToSet.boolValue;
            _exceptionHandlerEnabledToSet = nil;
        }
        
        if ( _locationCollectionEnabledToSet != nil )
        {
            settings.locationCollectionEnabled = _locationCollectionEnabledToSet.boolValue;
            _locationCollectionEnabledToSet = nil;
        }
        
        settings.initializationAdUnitIdentifiers = [[NSString stringWithUTF8String: serializedAdUnitIdentifiers] componentsSeparatedByString: @","];
        
        NSDictionary<NSString *, id> *unityMetaData = [MAUnityAdManager deserializeParameters: [NSString stringWithUTF8String: serializedMetaData]];
        
        // Set the meta data to settings.
        NSMutableDictionary<NSString *, NSString *> *metaDataDict = [settings valueForKey: @"metaData"];
        for ( NSString *key in unityMetaData )
        {
            metaDataDict[key] = unityMetaData[key];
        }
        
        return settings;
    }

    ALGender getAppLovinGender(NSString *genderString)
    {
        if ( [@"F" al_isEqualToStringIgnoringCase: genderString] )
        {
            return ALGenderFemale;
        }
        else if ( [@"M" al_isEqualToStringIgnoringCase: genderString] )
        {
            return ALGenderMale;
        }
        else if ( [@"O" al_isEqualToStringIgnoringCase: genderString] )
        {
            return ALGenderOther;
        }
        
        return ALGenderUnknown;
    }

    ALAdContentRating getAppLovinAdContentRating(int maximumAdContentRating)
    {
        if ( maximumAdContentRating == 1 )
        {
            return ALAdContentRatingAllAudiences;
        }
        else if ( maximumAdContentRating == 2 )
        {
            return ALAdContentRatingEveryoneOverTwelve;
        }
        else if ( maximumAdContentRating == 3 )
        {
            return ALAdContentRatingMatureAudiences;
        }
        
        return ALAdContentRatingNone;
    }
    
    void _MaxSetSdkKey(const char *sdkKey)
    {
        maybeInitializePlugin();
        
        if (!sdkKey) return;
        
        NSString *sdkKeyStr = [NSString stringWithUTF8String: sdkKey];
        
        NSDictionary *infoDict = [[NSBundle mainBundle] infoDictionary];
        [infoDict setValue: sdkKeyStr forKey: @"AppLovinSdkKey"];
    }
    
    void _MaxInitializeSdk(const char *serializedAdUnitIdentifiers, const char *serializedMetaData, ALUnityBackgroundCallback backgroundCallback)
    {
        maybeInitializePlugin();
        
        _sdk = [_adManager initializeSdkWithSettings: generateSDKSettings(serializedAdUnitIdentifiers, serializedMetaData)
                                  backgroundCallback: backgroundCallback
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
        
        if ( _targetingYearOfBirth != nil )
        {
            _sdk.targetingData.yearOfBirth = _targetingYearOfBirth.intValue <= 0 ? nil : _targetingYearOfBirth;
            _targetingYearOfBirth = nil;
        }
        
        if ( _targetingGender )
        {
            _sdk.targetingData.gender = getAppLovinGender(_targetingGender);
            _targetingGender = nil;
        }
        
        if ( _targetingMaximumAdContentRating != nil )
        {
            _sdk.targetingData.maximumAdContentRating = getAppLovinAdContentRating(_targetingMaximumAdContentRating.intValue);
            _targetingMaximumAdContentRating = nil;
        }
        
        if ( _targetingEmail )
        {
            _sdk.targetingData.email = _targetingEmail;
            _targetingEmail = nil;
        }
        
        if ( _targetingPhoneNumber )
        {
            _sdk.targetingData.phoneNumber = _targetingPhoneNumber;
            _targetingPhoneNumber = nil;
        }
        
        if ( _targetingKeywords )
        {
            _sdk.targetingData.keywords = _targetingKeywords;
            _targetingKeywords = nil;
        }
        
        if ( _targetingInterests )
        {
            _sdk.targetingData.interests = _targetingInterests;
            _targetingInterests = nil;
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
        NSMutableArray<NSDictionary<NSString *, NSString *> *> *serializedNetworks = [NSMutableArray arrayWithCapacity: availableMediatedNetworks.count];
        for ( MAMediatedNetworkInfo *mediatedNetwork in availableMediatedNetworks )
        {
            NSDictionary<NSString *, NSString *> *mediatedNetworkDictionary = @{@"name" : mediatedNetwork.name,
                                                                               @"adapterClassName" : mediatedNetwork.adapterClassName,
                                                                               @"adapterVersion" : mediatedNetwork.adapterVersion,
                                                                               @"sdkVersion" : mediatedNetwork.sdkVersion};
            [serializedNetworks addObject: mediatedNetworkDictionary];
        }
        
        NSData *jsonData = [NSJSONSerialization dataWithJSONObject: serializedNetworks options: 0 error: nil];
        return cStringCopy([[NSString alloc] initWithData: jsonData encoding: NSUTF8StringEncoding]);
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

    void _MaxShowCreativeDebugger()
    {
        if ( !_sdk )
        {
            NSLog(@"[%@] Failed to show creative debugger - please ensure the AppLovin MAX Unity Plugin has been initialized by calling 'MaxSdk.InitializeSdk();'!", TAG);
            return;
        }
        
        [_sdk showCreativeDebugger];
    }
    
    void _MaxShowConsentDialog()
    {
        NSLog(@"[%@] Failed to show consent dialog - Unavailable on iOS, please use the consent flow: https://developers.applovin.com/en/unity/overview/terms-and-privacy-policy-flow", TAG);
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

    void _MaxSetTargetingDataYearOfBirth(const int yearOfBirth)
    {
        if ( !_sdk )
        {
            _targetingYearOfBirth = @(yearOfBirth);
            return;
        }
        
        _sdk.targetingData.yearOfBirth = yearOfBirth <= 0 ? nil : @(yearOfBirth);
    }

    void _MaxSetTargetingDataGender(char *gender)
    {
        if ( !_sdk )
        {
            _targetingGender = NSSTRING(gender);
            return;
        }
        
        NSString *genderString = NSSTRING(gender);
        _sdk.targetingData.gender = getAppLovinGender(genderString);
    }

    void _MaxSetTargetingDataMaximumAdContentRating(const int maximumAdContentRating)
    {
        if ( !_sdk )
        {
            _targetingMaximumAdContentRating = @(maximumAdContentRating);
            return;
        }
        
        _sdk.targetingData.maximumAdContentRating = getAppLovinAdContentRating(maximumAdContentRating);
    }

    void _MaxSetTargetingDataEmail(char *email)
    {
        if ( !_sdk )
        {
            _targetingEmail = NSSTRING(email);
            return;
        }
        
        _sdk.targetingData.email = NSSTRING(email);
    }

    void _MaxSetTargetingDataPhoneNumber(char *phoneNumber)
    {
        if ( !_sdk )
        {
            _targetingPhoneNumber = NSSTRING(phoneNumber);
            return;
        }
        
        _sdk.targetingData.phoneNumber = NSSTRING(phoneNumber);
    }

    void _MaxSetTargetingDataKeywords(char **keywords, int size)
    {
        NSArray<NSString *> *keywordsArray = keywords ? toStringArray(keywords, size) : nil;
        if ( !_sdk )
        {
            _targetingKeywords = keywordsArray;
            return;
        }
        
        _sdk.targetingData.keywords = keywordsArray;
    }

    void _MaxSetTargetingDataInterests(char **interests, int size)
    {
        NSArray<NSString *> *interestsArray = interests ? toStringArray(interests, size) : nil;
        if ( !_sdk )
        {
            _targetingInterests = interestsArray;
            return;
        }
        
        _sdk.targetingData.interests = interestsArray;
    }

    void _MaxClearAllTargetingData()
    {
        if ( !_sdk )
        {
            _targetingYearOfBirth = nil;
            _targetingGender = nil;
            _targetingMaximumAdContentRating = nil;
            _targetingEmail = nil;
            _targetingPhoneNumber = nil;
            _targetingKeywords = nil;
            _targetingInterests = nil;
            return;
        }
        
        [_sdk.targetingData clearAll];
    }

    const char * _MaxGetSdkConfiguration()
    {
        if ( !_sdk )
        {
            logUninitializedAccessError("_MaxGetSdkConfiguration");
            return cStringCopy(@"");
        }
        
        NSString *consentFlowUserGeographyStr = @(_sdk.configuration.consentFlowUserGeography).stringValue;
        NSString *consentDialogStateStr = @(_sdk.configuration.consentDialogState).stringValue;
        NSString *appTrackingStatus = @(_sdk.configuration.appTrackingTransparencyStatus).stringValue; // Deliberately name it `appTrackingStatus` to be a bit more generic (in case Android introduces a similar concept)

        return cStringCopy([MAUnityAdManager serializeParameters: @{@"consentFlowUserGeography" : consentFlowUserGeographyStr,
                                                                    @"consentDialogState" : consentDialogStateStr,
                                                                    @"countryCode" : _sdk.configuration.countryCode,
                                                                    @"appTrackingStatus" : appTrackingStatus,
                                                                    @"isSuccessfullyInitialized" : @([_sdk isInitialized]),
                                                                    @"isTestModeEnabled" : @([_sdk.configuration isTestModeEnabled])}]);
    }
    
    void _MaxSetHasUserConsent(bool hasUserConsent)
    {
        [ALPrivacySettings setHasUserConsent: hasUserConsent];
    }
    
    bool _MaxHasUserConsent()
    {
        return [ALPrivacySettings hasUserConsent];
    }
    
    bool _MaxIsUserConsentSet()
    {
        return [ALPrivacySettings isUserConsentSet];
    }

    void _MaxSetIsAgeRestrictedUser(bool isAgeRestrictedUser)
    {
        [ALPrivacySettings setIsAgeRestrictedUser: isAgeRestrictedUser];
    }
    
    bool _MaxIsAgeRestrictedUser()
    {
        return [ALPrivacySettings isAgeRestrictedUser];
    }

    bool _MaxIsAgeRestrictedUserSet()
    {
        return [ALPrivacySettings isAgeRestrictedUserSet];
    }
    
    void _MaxSetDoNotSell(bool doNotSell)
    {
        [ALPrivacySettings setDoNotSell: doNotSell];
    }
    
    bool _MaxIsDoNotSell()
    {
        return [ALPrivacySettings isDoNotSell];
    }

    bool _MaxIsDoNotSellSet()
    {
        return [ALPrivacySettings isDoNotSellSet];
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
    
   void _MaxLoadBanner(const char *adUnitIdentifier)
   {
       if (!isPluginInitialized()) return;

       [_adManager loadBannerWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
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
    
    void _MaxStartBannerAutoRefresh(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager startBannerAutoRefreshForAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxStopBannerAutoRefresh(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager stopBannerAutoRefreshForAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxSetBannerExtraParameter(const char *adUnitIdentifier, const char *key, const char *value)
    {
        [_adManager setBannerExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                           key: NSSTRING(key)
                                                         value: NSSTRING(value)];
    }
    
    void _MaxSetBannerLocalExtraParameter(const char *adUnitIdentifier, const char *key, MAUnityRef value)
    {
        if ( !isReadyToInteractWithSdk() )
        {
            logUninitializedAccessError("_MaxSetBannerLocalExtraParameter");
        }
        
        [_adManager setBannerLocalExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                                key: NSSTRING(key)
                                                              value: (__bridge id) value];
    }

    void _MaxSetBannerLocalExtraParameterJSON(const char *adUnitIdentifier, const char *key, const char *json)
    {
        if ( !isReadyToInteractWithSdk() )
        {
            logUninitializedAccessError("_MaxSetBannerLocalExtraParameter");
        }
        
        id value = getLocalExtraParameterValue(json);
        [_adManager setBannerLocalExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                                key: NSSTRING(key)
                                                              value: value];
    }
    
    void _MaxSetBannerCustomData(const char *adUnitIdentifier, const char *customData)
    {
        [_adManager setBannerCustomData: NSSTRING(customData) forAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
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
    
   void _MaxLoadMRec(const char *adUnitIdentifier)
   {
       if (!isPluginInitialized()) return;

       [_adManager loadMRecWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
   }
    
    void _MaxSetMRecPlacement(const char *adUnitIdentifier, const char *placement)
    {
        [_adManager setMRecPlacement: NSSTRING(placement) forAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxStartMRecAutoRefresh(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager startMRecAutoRefreshForAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxStopMRecAutoRefresh(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager stopMRecAutoRefreshForAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
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
    
    void _MaxSetMRecLocalExtraParameter(const char *adUnitIdentifier, const char *key, MAUnityRef value)
    {
        if ( !isReadyToInteractWithSdk() )
        {
            logUninitializedAccessError("_MaxSetMRecLocalExtraParameter");
        }
        
        [_adManager setMRecLocalExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                              key: NSSTRING(key)
                                                            value: (__bridge id)value];
    }

    void _MaxSetMRecLocalExtraParameterJSON(const char *adUnitIdentifier, const char *key, const char *json)
    {
        if ( !isReadyToInteractWithSdk() )
        {
            logUninitializedAccessError("_MaxSetMRecLocalExtraParameter");
        }
        
        id value = getLocalExtraParameterValue(json);
        [_adManager setMRecLocalExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                              key: NSSTRING(key)
                                                            value: value];
    }
    
    void _MaxSetMRecCustomData(const char *adUnitIdentifier, const char *customData)
    {
        [_adManager setMRecCustomData: NSSTRING(customData) forAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }

    const char * _MaxGetMRecLayout(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return cStringCopy(@"");
        
        return cStringCopy([_adManager mrecLayoutForAdUnitIdentifier: NSSTRING(adUnitIdentifier)]);
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
    
    void _MaxSetInterstitialLocalExtraParameter(const char *adUnitIdentifier, const char *key, MAUnityRef value)
    {
        if ( !isReadyToInteractWithSdk() )
        {
            logUninitializedAccessError("_MaxSetInterstitialLocalExtraParameter");
        }
        
        [_adManager setInterstitialLocalExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                                      key: NSSTRING(key)
                                                                    value: (__bridge id)value];
    }

    void _MaxSetInterstitialLocalExtraParameterJSON(const char *adUnitIdentifier, const char *key, const char *json)
    {
        if ( !isReadyToInteractWithSdk() )
        {
            logUninitializedAccessError("_MaxSetInterstitialLocalExtraParameter");
        }
        
        id value = getLocalExtraParameterValue(json);
        [_adManager setInterstitialLocalExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                                      key: NSSTRING(key)
                                                                    value: value];
    }

    bool _MaxIsInterstitialReady(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return false;
        
        return [_adManager isInterstitialReadyWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxShowInterstitial(const char *adUnitIdentifier, const char *placement, const char *customData)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager showInterstitialWithAdUnitIdentifier: NSSTRING(adUnitIdentifier) placement: NSSTRING(placement) customData: NSSTRING(customData)];
    }
    
    void _MaxLoadAppOpenAd(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager loadAppOpenAdWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxSetAppOpenAdExtraParameter(const char *adUnitIdentifier, const char *key, const char *value)
    {
        [_adManager setAppOpenAdExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                              key: NSSTRING(key)
                                                            value: NSSTRING(value)];
    }
    
    void _MaxSetAppOpenAdLocalExtraParameter(const char *adUnitIdentifier, const char *key, MAUnityRef value)
    {
        if ( !isReadyToInteractWithSdk() )
        {
            logUninitializedAccessError("_MaxSetAppOpenAdLocalExtraParameter");
        }
        
        [_adManager setAppOpenAdLocalExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                                   key: NSSTRING(key)
                                                                 value: (__bridge id)value];
    }

    void _MaxSetAppOpenAdLocalExtraParameterJSON(const char *adUnitIdentifier, const char *key, const char *json)
    {
        if ( !isReadyToInteractWithSdk() )
        {
            logUninitializedAccessError("_MaxSetAppOpenAdLocalExtraParameter");
        }
        
        id value = getLocalExtraParameterValue(json);
        [_adManager setAppOpenAdLocalExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                                   key: NSSTRING(key)
                                                                 value: value];
    }
    
    bool _MaxIsAppOpenAdReady(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return false;
        
        return [_adManager isAppOpenAdReadyWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxShowAppOpenAd(const char *adUnitIdentifier, const char *placement, const char *customData)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager showAppOpenAdWithAdUnitIdentifier: NSSTRING(adUnitIdentifier) placement: NSSTRING(placement) customData: NSSTRING(customData)];
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
    
    void _MaxSetRewardedAdLocalExtraParameter(const char *adUnitIdentifier, const char *key, MAUnityRef value)
    {
        if ( !isReadyToInteractWithSdk() )
        {
            logUninitializedAccessError("_MaxSetRewardedAdLocalExtraParameter");
        }
        
        [_adManager setRewardedAdLocalExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                                    key: NSSTRING(key)
                                                                  value: (__bridge id)value];
    }

    void _MaxSetRewardedAdLocalExtraParameterJSON(const char *adUnitIdentifier, const char *key, const char *json)
    {
        if ( !isReadyToInteractWithSdk() )
        {
            logUninitializedAccessError("_MaxSetRewardedAdLocalExtraParameter");
        }
        
        id value = getLocalExtraParameterValue(json);
        [_adManager setRewardedAdLocalExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                                    key: NSSTRING(key)
                                                                  value: value];
    }

    bool _MaxIsRewardedAdReady(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return false;
        
        return [_adManager isRewardedAdReadyWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxShowRewardedAd(const char *adUnitIdentifier, const char *placement, const char *customData)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager showRewardedAdWithAdUnitIdentifier: NSSTRING(adUnitIdentifier) placement: NSSTRING(placement) customData: NSSTRING(customData)];
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
    
    void _MaxSetRewardedInterstitialAdLocalExtraParameter(const char *adUnitIdentifier, const char *key, MAUnityRef value)
    {
        if ( !isReadyToInteractWithSdk() )
        {
            logUninitializedAccessError("_MaxSetRewardedInterstitialAdLocalExtraParameter");
        }
        
        [_adManager setRewardedInterstitialAdLocalExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                                                key: NSSTRING(key)
                                                                              value: (__bridge id)value];
    }

    void _MaxSetRewardedInterstitialAdLocalExtraParameterJSON(const char *adUnitIdentifier, const char *key, const char *json)
    {
        if ( !isReadyToInteractWithSdk() )
        {
            logUninitializedAccessError("_MaxSetRewardedInterstitialAdLocalExtraParameter");
        }
        
        id value = getLocalExtraParameterValue(json);
        [_adManager setRewardedInterstitialAdLocalExtraParameterForAdUnitIdentifier: NSSTRING(adUnitIdentifier)
                                                                                key: NSSTRING(key)
                                                                              value: value];
    }

    bool _MaxIsRewardedInterstitialAdReady(const char *adUnitIdentifier)
    {
        if (!isPluginInitialized()) return false;
        
        return [_adManager isRewardedInterstitialAdReadyWithAdUnitIdentifier: NSSTRING(adUnitIdentifier)];
    }
    
    void _MaxShowRewardedInterstitialAd(const char *adUnitIdentifier, const char *placement, const char *customData)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager showRewardedInterstitialAdWithAdUnitIdentifier: NSSTRING(adUnitIdentifier) placement: NSSTRING(placement) customData: NSSTRING(customData)];
    }
    
    void _MaxTrackEvent(const char *event, const char *parameters)
    {
        if (!isPluginInitialized()) return;
        
        [_adManager trackEvent: NSSTRING(event) parameters: NSSTRING(parameters)];
    }
    
    bool _MaxIsTablet()
    {
        return [UIDevice currentDevice].userInterfaceIdiom == UIUserInterfaceIdiomPad;
    }

    bool _MaxIsPhysicalDevice()
    {
        return !ALUtils.simulator;
    }

    int _MaxGetTcfVendorConsentStatus(int vendorIdentifier)
    {
        NSNumber *consentStatus = [ALPrivacySettings tcfVendorConsentStatusForIdentifier: vendorIdentifier];
        return getConsentStatusValue(consentStatus);
    }

    int _MaxGetAdditionalConsentStatus(int atpIdentifier)
    {
        NSNumber *consentStatus = [ALPrivacySettings additionalConsentStatusForIdentifier: atpIdentifier];
        return getConsentStatusValue(consentStatus);
    }

    int _MaxGetPurposeConsentStatus(int purposeIdentifier)
    {
        NSNumber *consentStatus = [ALPrivacySettings purposeConsentStatusForIdentifier: purposeIdentifier];
        return getConsentStatusValue(consentStatus);
    }

    int _MaxGetSpecialFeatureOptInStatus(int specialFeatureIdentifier)
    {
        NSNumber *consentStatus = [ALPrivacySettings specialFeatureOptInStatusForIdentifier: specialFeatureIdentifier];
        return getConsentStatusValue(consentStatus);
    }
    
    static const char * cStringCopy(NSString *string)
    {
        const char *value = string.UTF8String;
        return value ? strdup(value) : NULL;
    }
    
    void _MaxSetMuted(bool muted)
    {
        if ( _sdk )
        {
            _sdk.settings.muted = muted;
            _mutedToSet = nil;
        }
        else
        {
            _mutedToSet = @(muted);
        }
    }
    
    bool _MaxIsMuted()
    {
        if ( _sdk )
        {
            return _sdk.settings.muted;
        }
        else if ( _mutedToSet != nil )
        {
            return _mutedToSet.boolValue;
        }
        
        return false;
    }
    
    float _MaxScreenDensity()
    {
        return [UIScreen.mainScreen nativeScale];
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
            _sdk.settings.verboseLoggingEnabled = enabled;
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
            return [_sdk.settings isVerboseLoggingEnabled];
        }
        else if ( _verboseLoggingToSet != nil )
        {
            return _verboseLoggingToSet.boolValue;
        }

        return false;
    }

    void _MaxSetTestDeviceAdvertisingIdentifiers(char **advertisingIdentifiers, int size)
    {
        NSArray<NSString *> *advertisingIdentifiersArray = toStringArray(advertisingIdentifiers, size);
        _testDeviceIdentifiersToSet = advertisingIdentifiersArray;
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

    void _MaxSetLocationCollectionEnabled(bool enabled)
    {
        if ( _sdk )
        {
            _sdk.settings.locationCollectionEnabled = enabled;
            _locationCollectionEnabledToSet = nil;
        }
        else
        {
            _locationCollectionEnabledToSet = @(enabled);
        }
    }

    void _MaxSetExtraParameter(const char *key, const char *value)
    {
        NSString *stringKey = NSSTRING(key);
        if ( ![stringKey al_isValidString] )
        {
            NSLog(@"[%@] Failed to set extra parameter for nil or empty key: %@", TAG, stringKey);
            return;
        }
        
        if ( _sdk )
        {
            ALSdkSettings *settings = _sdk.settings;
            [settings setExtraParameterForKey: stringKey value: NSSTRING(value)];
            setPendingExtraParametersIfNeeded( settings );
        }
        else
        {
            @synchronized ( _extraParametersToSetLock )
            {
                _extraParametersToSet[stringKey] = NSSTRING(value);
            }
        }
    }

    int * _MaxGetSafeAreaInsets()
    {
        UIEdgeInsets safeAreaInsets = UnityGetGLView().safeAreaInsets;
        static int insets[4] = {(int) safeAreaInsets.left, (int) safeAreaInsets.top, (int) safeAreaInsets.right, (int) safeAreaInsets.bottom};
        return insets;
    }
    
    void _MaxShowCmpForExistingUser()
    {
        if (!isPluginInitialized())
        {
            logUninitializedAccessError("_MaxShowCmpForExistingUser");
            return;
        }
        
        [_adManager showCMPForExistingUser];
    }
    
    bool _MaxHasSupportedCmp()
    {
        if (!isPluginInitialized())
        {
            logUninitializedAccessError("_MaxHasSupportedCmp");
            return false;
        }
        
        return [_sdk.cmpService hasSupportedCMP];
    }

    float _MaxGetAdaptiveBannerHeight(const float width)
    {
        return [MAUnityAdManager adaptiveBannerHeightForWidth: width];
    }

    void logUninitializedAccessError(const char *callingMethod)
    {
        NSLog(@"[%@] Failed to execute: %s - please ensure the AppLovin MAX Unity Plugin has been initialized by calling 'MaxSdk.InitializeSdk();'!", TAG, callingMethod);
    }
}

#pragma clang diagnostic pop
