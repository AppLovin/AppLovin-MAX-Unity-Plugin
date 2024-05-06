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
        private static MaxEventExecutor instance;
        private static List<MaxAction> adEventsQueue = new List<MaxAction>();

        private static volatile bool adEventsQueueEmpty = true;

        struct MaxAction
        {
            public Action action;
            public string eventName;

            public MaxAction(Action actionToExecute, string nameOfEvent)
            {
                action = actionToExecute;
                eventName = nameOfEvent;
            }
        }

        public static void InitializeIfNeeded()
        {
            if (instance != null) return;

            var executor = new GameObject("MaxEventExecutor");
            executor.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(executor);
            instance = executor.AddComponent<MaxEventExecutor>();
        }

        #region Public API

#if UNITY_EDITOR || !(UNITY_ANDROID || UNITY_IPHONE || UNITY_IOS)
        public static MaxEventExecutor Instance
        {
            get
            {
                InitializeIfNeeded();
                return instance;
            }
        }
#endif

        public static void ExecuteOnMainThread(Action action, string eventName)
        {
            lock (adEventsQueue)
            {
                adEventsQueue.Add(new MaxAction(action, eventName));
                adEventsQueueEmpty = false;
            }
        }

        public static void InvokeOnMainThread(UnityEvent unityEvent, string eventName)
        {
            ExecuteOnMainThread(() => unityEvent.Invoke(), eventName);
        }

        #endregion

        public void Update()
        {
            if (adEventsQueueEmpty) return;

            var actionsToExecute = new List<MaxAction>();
            lock (adEventsQueue)
            {
                actionsToExecute.AddRange(adEventsQueue);
                adEventsQueue.Clear();
                adEventsQueueEmpty = true;
            }

            foreach (var maxAction in actionsToExecute)
            {
                if (maxAction.action.Target != null)
                {
                    try
                    {
                        maxAction.action.Invoke();
                    }
                    catch (Exception exception)
                    {
                        MaxSdkLogger.UserError("Caught exception in publisher event: " + maxAction.eventName + ", exception: " + exception);
                        Debug.LogException(exception);
                    }
                }
            }
        }

        public void Disable()
        {
            instance = null;
        }
    }
}
