//
//  MaxUserSegment.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Thomas So on 10/31/20.
//  Copyright © 2020 AppLovin. All rights reserved.
//

/// <summary>
/// User segments allow us to serve ads using custom-defined rules based on which segment the user is in. For now, we only support a custom string 32 alphanumeric characters or less as the user segment.
/// </summary>
public class MaxUserSegment
{
    private string _name;
    
    public string Name
    {
        set
        {
            _name = value;

            MaxSdk.SetUserSegmentField("name", _name);
        }
        get { return _name; }
    }

    public override string ToString()
    {
        return "[MaxUserSegment Name: " + Name + "]";
    }
}
