//
//  MaxCmpService.cs
//  AppLovin User Engagement Unity Plugin
//
//  Created by Santosh Bagadi on 10/1/23.
//  Copyright © 2023 AppLovin. All rights reserved.
//

using System;
using System.Collections.Generic;

#if UNITY_EDITOR
#elif UNITY_ANDROID
using UnityEngine;
#elif UNITY_IOS
using System.Runtime.InteropServices;
#endif

/// <summary>
/// This class provides direct APIs for interfacing with the Google-certified CMP installed, if any.
/// </summary>
public class MaxCmpService
{
    private static readonly MaxCmpService _instance = new MaxCmpService();

    private MaxCmpService() { }

    private static Action<MaxCmpError> OnCompletedAction;

#if UNITY_EDITOR
#elif UNITY_ANDROID
    private static readonly AndroidJavaClass MaxUnityPluginClass = new AndroidJavaClass("com.applovin.mediation.unity.MaxUnityPlugin");
#elif UNITY_IOS
    [DllImport("__Internal")]
    private static extern void _MaxShowCmpForExistingUser();

    [DllImport("__Internal")]
    private static extern bool _MaxHasSupportedCmp();
#endif

    internal static MaxCmpService Instance
    {
        get { return _instance; }
    }

    /// <summary>
    /// Shows the CMP flow to an existing user.
    /// Note that the user's current consent will be reset before the CMP alert is shown.
    /// </summary>
    /// <param name="onCompletedAction">Called when the CMP flow finishes showing.</param>
    public void ShowCmpForExistingUser(Action<MaxCmpError> onCompletedAction)
    {
        OnCompletedAction = onCompletedAction;

#if UNITY_EDITOR
        var errorProps = new Dictionary<string, object>
        {
            {"code", (int) MaxCmpError.ErrorCode.FormUnavailable},
            {"message", "CMP is not supported in Unity editor"}
        };

        NotifyCompletedIfNeeded(errorProps);
#elif UNITY_ANDROID
        MaxUnityPluginClass.CallStatic("showCmpForExistingUser");
#elif UNITY_IOS
        _MaxShowCmpForExistingUser();
#endif
    }

    /// <summary>
    /// Returns <code>true</code> if a supported CMP SDK is detected.
    /// </summary>
    public bool HasSupportedCmp
    {
        get
        {
#if UNITY_EDITOR
            return false;
#elif UNITY_ANDROID
            return MaxUnityPluginClass.CallStatic<bool>("hasSupportedCmp");
#elif UNITY_IOS
            return _MaxHasSupportedCmp();
#else
            return false;
#endif
        }
    }

    internal static void NotifyCompletedIfNeeded(Dictionary<string, object> errorProps)
    {
        if (OnCompletedAction == null) return;

        var error = (errorProps == null) ? null : MaxCmpError.Create(errorProps);
        OnCompletedAction(error);
    }
}

public class MaxCmpError
{
    public enum ErrorCode
    {
        /// <summary>
        /// Indicates that an unspecified error has occurred.
        /// </summary>
        Unspecified = -1,

        /// <summary>
        /// Indicates that the CMP has not been integrated correctly.
        /// </summary>
        IntegrationError = 1,

        /// <summary>
        /// Indicates that the CMP form is unavailable.
        /// </summary>
        FormUnavailable = 2,

        /// <summary>
        /// Indicates that the CMP form is not required.
        /// </summary>
        FormNotRequired = 3
    }

    public static MaxCmpError Create(IDictionary<string, object> error)
    {
        return new MaxCmpError()
        {
            Code = GetCode(MaxSdkUtils.GetIntFromDictionary(error, "code")),
            Message = MaxSdkUtils.GetStringFromDictionary(error, "message"),
            CmpCode = MaxSdkUtils.GetIntFromDictionary(error, "cmpCode", -1),
            CmpMessage = MaxSdkUtils.GetStringFromDictionary(error, "cmpMessage")
        };
    }

    private static ErrorCode GetCode(int code)
    {
        switch (code)
        {
            case 1:
                return ErrorCode.IntegrationError;
            case 2:
                return ErrorCode.FormUnavailable;
            case 3:
                return ErrorCode.FormNotRequired;
            default:
                return ErrorCode.Unspecified;
        }
    }

    private MaxCmpError() { }

    /// <summary>
    /// The error code for this error.
    /// </summary>
    public ErrorCode Code { get; private set; }

    /// <summary>
    /// The error message for this error.
    /// </summary>
    public string Message { get; private set; }

    /// <summary>
    /// The error code returned by the CMP.
    /// </summary>
    public int CmpCode { get; private set; }

    /// <summary>
    /// The error message returned by the CMP.
    /// </summary>
    public string CmpMessage { get; private set; }
}
