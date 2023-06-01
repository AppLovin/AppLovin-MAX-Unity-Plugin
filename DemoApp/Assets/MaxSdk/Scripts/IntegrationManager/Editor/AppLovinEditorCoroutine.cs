//
//  AppLovinEditorCoroutine.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 7/25/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

using System.Collections;
using UnityEditor;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    /// <summary>
    /// A coroutine that can update based on editor application update.
    /// </summary>
    public class AppLovinEditorCoroutine
    {
        private readonly IEnumerator enumerator;

        private AppLovinEditorCoroutine(IEnumerator enumerator)
        {
            this.enumerator = enumerator;
        }

        /// <summary>
        /// Creates and starts a coroutine.
        /// </summary>
        /// <param name="enumerator">The coroutine to be started</param>
        /// <returns>The coroutine that has been started.</returns>
        public static AppLovinEditorCoroutine StartCoroutine(IEnumerator enumerator)
        {
            var coroutine = new AppLovinEditorCoroutine(enumerator);
            coroutine.Start();
            return coroutine;
        }

        private void Start()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        /// <summary>
        /// Stops the coroutine.
        /// </summary>
        public void Stop()
        {
            if (EditorApplication.update == null) return;

            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            // Coroutine has ended, stop updating.
            if (!enumerator.MoveNext())
            {
                Stop();
            }
        }
    }
}
