using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Plugins.Rampancy.Runtime
{
    // A helper to run stuff on the main thread
    // its abit crude atm
    public static class DoMain
    {
        public static Queue<DoAction> MainActions = new();

        static DoMain()
        {
            EditorApplication.update += () =>
            {
                Process();
            };
        }

        public static void Queue(Action action)
        {
            var act = new DoAction()
            {
                Action = action
            };

            lock (MainActions) {
                MainActions.Enqueue(act);
            }

            // Block untill done
            while (!act.IsDone) { }
        }

        public static void Process()
        {
            lock (MainActions) {
                while (MainActions.Count > 0) {
                    var action = MainActions.Dequeue();
                    action.Action();
                    action.IsDone = true;
                }
            }
        }

        public class DoAction
        {
            public Action Action;
            public bool   IsDone;
        }
    }
}