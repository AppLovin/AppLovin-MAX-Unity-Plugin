//
//  MaxEventExecutor.cs
//  Max Unity Plugin
//
//  Created by Jonathan Liu on 1/22/2024.
//  Copyright © 2024 AppLovin. All rights reserved.
//

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AppLovinMax.Internal
{
    public class MaxEventExecutor : MonoBehaviour
    {
        private static MaxEventExecutor _instance;
        private static readonly List<MaxAction> AdEventsQueue = new List<MaxAction>();

        private static volatile bool _adEventsQueueEmpty = true;

        struct MaxAction
        {
            public readonly Action ActionToExecute;
            public readonly string EventName;

            public MaxAction(Action actionToExecute, string nameOfEvent)
            {
                ActionToExecute = actionToExecute;
                EventName = nameOfEvent;
            }
        }

        public static void InitializeIfNeeded()
        {
            if (_instance != null) return;

            var executor = new GameObject("MaxEventExecutor");
            executor.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(executor);
            _instance = executor.AddComponent<MaxEventExecutor>();
        }

        #region Public API

#if UNITY_EDITOR || !(UNITY_ANDROID || UNITY_IPHONE || UNITY_IOS)
        public static MaxEventExecutor Instance
        {
            get
            {
                InitializeIfNeeded();
                return _instance;
            }
        }
#endif

        public static void ExecuteOnMainThread(Action action, string eventName)
        {
            lock (AdEventsQueue)
            {
                AdEventsQueue.Add(new MaxAction(action, eventName));
                _adEventsQueueEmpty = false;
            }
        }

        public static void InvokeOnMainThread(UnityEvent unityEvent, string eventName)
        {
            ExecuteOnMainThread(() => unityEvent.Invoke(), eventName);
        }

        #endregion

        public void Update()
        {
            if (_adEventsQueueEmpty) return;

            var actionsToExecute = new List<MaxAction>();
            lock (AdEventsQueue)
            {
                actionsToExecute.AddRange(AdEventsQueue);
                AdEventsQueue.Clear();
                _adEventsQueueEmpty = true;
            }

            foreach (var maxAction in actionsToExecute)
            {
                if (maxAction.ActionToExecute.Target != null)
                {
                    try
                    {
                        maxAction.ActionToExecute.Invoke();
                    }
                    catch (Exception exception)
                    {
                        MaxSdkLogger.UserError("Caught exception in publisher event: " + maxAction.EventName + ", exception: " + exception);
                        MaxSdkLogger.LogException(exception);
                    }
                }
            }
        }

        public void Disable()
        {
            _instance = null;
        }
    }
}
