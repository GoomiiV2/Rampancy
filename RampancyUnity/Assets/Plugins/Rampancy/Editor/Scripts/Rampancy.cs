using System;
using Plugins.Rampancy.Editor.Scripts.UI;
using Plugins.Rampancy.Runtime;
using UnityEditor;

namespace Plugins.Rampancy.Editor.Scripts
{
    [InitializeOnLoad]
    public class Rampancy
    {
        static Rampancy()
        {
            Init();
        }

        public static void Init()
        {
            Runtime.Rampancy.Config = Config.Load();

            if (Runtime.Rampancy.Config == null) {
                Runtime.Rampancy.Config = new Config();

                EditorApplication.CallbackFunction showSettings = null;
                showSettings = () =>
                {
                    Settings.ShowWindow();
                    EditorApplication.update -= showSettings;
                };
                EditorApplication.update += showSettings;
            }
            else {
                Runtime.Rampancy.AssetDB.ScanTags();
            }
        }
    }
}