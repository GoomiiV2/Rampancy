using System;
using Plugins.Rampancy.Editor.Scripts.UI;
using Plugins.Rampancy.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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

            EditorSceneManager.sceneSaving += (scene, path) =>
            {
                var rampancySentinelGO = GameObject.Find(RampancySentinel.NAME);
                if (rampancySentinelGO != null) {
                    var rampancySentinel = rampancySentinelGO.GetComponent<RampancySentinel>();
                    rampancySentinel.BuildMatIdToPathList();
                }
            };

            EditorSceneManager.sceneOpened += (scene, mode) =>
            {
                Actions.UpdateSceneMatRefs();
            };
        }
    }
}