//
//  EventSystemChecker.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Jonathan Liu on 10/23/2022.
//  Copyright © 2022 AppLovin. All rights reserved.
//

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;

namespace AppLovinMax.Scripts
{
    /// <summary>
    /// A script to check and enable event system as needed for the AppLovin MAX ad prefabs.
    /// </summary>
    [RequireComponent(typeof(EventSystem))]
    public class MaxEventSystemChecker : MonoBehaviour
    {
        private void Awake()
        {
            // Enable the EventSystem if there is no other EventSystem in the scene
            var eventSystem = GetComponent<EventSystem>();
            var currentSystem = UnityEngine.EventSystems.EventSystem.current;
            if (currentSystem == null || currentSystem == eventSystem)
            {
                eventSystem.enabled = true;
            }
            else
            {
                eventSystem.enabled = false;
            }
        }
    }
}
#endif
