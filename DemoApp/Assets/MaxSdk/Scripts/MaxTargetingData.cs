//
//  MaxTargetingData.cs
//  AppLovin MAX Unity Plugin
//
// Created by Harry Arakkal on 11/19/21.
// Copyright © 2020 AppLovin. All rights reserved.
//

/// <summary>
/// This class allows you to provide user or app data that will improve how we target ads.
/// </summary>
public class MaxTargetingData
{
    /// <summary>
    /// This enumeration represents content ratings for the ads shown to users.
    /// They correspond to IQG Media Ratings.
    /// </summary>
     public enum AdContentRating
    {
        None,
        AllAudiences,
        EveryoneOverTwelve,
        MatureAudiences
    }

    /// <summary>
    /// This enumeration represents gender.
    /// </summary>
    public enum UserGender
    {
        Unknown,
        Female,
        Male,
        Other
    }

    /// <summary>
    /// The year of birth of the user.
    /// Set this property to <c>0</c> to clear this value.
    /// </summary>
    public int YearOfBirth
    {
        set
        {
            MaxSdk.SetTargetingDataYearOfBirth(value);
        }
    }

    /// <summary>
    /// The gender of the user.
    /// Set this property to <c>UserGender.Unknown</c> to clear this value.
    /// </summary>
    public UserGender Gender
    {
        set
        {
            string genderString = "";
            if ( value == UserGender.Female )
            {
                genderString = "F";
            }
            else if ( value == UserGender.Male )
            {
                genderString = "M";
            }
            else if ( value == UserGender.Other )
            {
                genderString = "O";
            }

            MaxSdk.SetTargetingDataGender(genderString);
        }
    }

    /// <summary>
    /// The maximum ad content rating to show the user.
    /// Set this property to <c>AdContentRating.None</c> to clear this value.
    /// </summary>
    public AdContentRating MaximumAdContentRating
    {
        set
        {
            MaxSdk.SetTargetingDataMaximumAdContentRating((int) value);
        }
    }

    /// <summary>
    /// The email of the user.
    /// Set this property to <c>null</c> to clear this value.
    /// </summary>
    public string Email
    {
        set
        {
            MaxSdk.SetTargetingDataEmail(value);
        }
    }

    /// <summary>
    /// The phone number of the user. Do not include the country calling code.
    /// Set this property to <c>null</c> to clear this value.
    /// </summary>
    public string PhoneNumber
    {
        set
        {
            MaxSdk.SetTargetingDataPhoneNumber(value);
        }
    }

    /// <summary>
    /// The keywords describing the application.
    /// Set this property to <c>null</c> to clear this value.
    /// </summary>
    public string[] Keywords
    {
        set
        {
            MaxSdk.SetTargetingDataKeywords(value);
        }
    }

    /// <summary>
    /// The interests of the user.
    /// Set this property to <c>null</c> to clear this value.
    /// </summary>
    public string[] Interests
    {
        set
        {
            MaxSdk.SetTargetingDataInterests(value);
        }
    }

    /// <summary>
    /// Clear all saved data from this class.
    /// </summary>
    public void ClearAll()
    {
        MaxSdk.ClearAllTargetingData();
    }
}
