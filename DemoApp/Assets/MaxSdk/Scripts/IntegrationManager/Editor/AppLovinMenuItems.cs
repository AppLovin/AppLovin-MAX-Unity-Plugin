//
//  MaxMenuItems.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 5/27/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

using UnityEditor;
using UnityEngine;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    public class AppLovinMenuItems
    {
        /**
         * The special characters at the end represent a shortcut for this action.
         * 
         * % - ctrl on Windows, cmd on macOS
         * # - shift
         * & - alt
         * 
         * So, (shift + cmd/ctrl + i) will launch the integration manager
         */
        [MenuItem("AppLovin/Integration Manager %#i")]
        private static void IntegrationManager()
        {
            ShowIntegrationManager();
        }

        [MenuItem("AppLovin/Documentation")]
        private static void Documentation()
        {
            Application.OpenURL("https://dash.applovin.com/documentation/mediation/unity/getting-started");
        }

        [MenuItem("AppLovin/Contact Us")]
        private static void ContactUs()
        {
            Application.OpenURL("https://www.applovin.com/contact/");
        }

        [MenuItem("AppLovin/About")]
        private static void About()
        {
            Application.OpenURL("https://www.applovin.com/about/");
        }

        [MenuItem("Assets/AppLovin Integration Manager")]
        private static void AssetsIntegrationManager()
        {
            ShowIntegrationManager();
        }

        private static void ShowIntegrationManager()
        {
            AppLovinIntegrationManagerWindow.ShowManager();
        }
    }
}
