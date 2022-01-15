using System;
using RampantC20;
using UnityEditor;
using UnityEngine;

namespace Rampancy.UI
{
    public class RampancyLevelUI : EditorWindow
    {
        private int ActiveTab = 0;

        private RampancySentinel _Sentinel;

        private RampancySentinel Sentinel
        {
            get
            {
                if (_Sentinel == null) {
                    _Sentinel = GameObject.Find(RampancySentinel.NAME).GetComponent<RampancySentinel>();
                }

                return _Sentinel;
            }
        }

        [MenuItem("Rampancy/Open Level UI")]
        public static void ShowWindow()
        {
            EditorWindow window = GetWindow(typeof(RampancyLevelUI), false, "Rampancy Level UI");
            window.Show();
        }

        void OnGUI()
        {
            ActiveTab = GUILayout.Toolbar(ActiveTab, new[] {"Materials", "Debug"});
            switch (ActiveTab) {
                case 0:
                    MaterialsTab();
                    break;
            }
        }

    #region Materials

        private void MaterialsTab()
        {
            if (Sentinel == null) return;

            if (GUILayout.Button($"Sync materials from {Rampancy.Cfg.GameVersion}"))
                SyncMats();

            foreach (var matInfo in Sentinel.MatIdToPathLookup_Paths) {
            }
        }

        private void DrawMaterialInfo()
        {
        }

        private static void SyncMats()
        {
            Action func = Rampancy.Cfg.GameVersion switch
            {
                GameVersions.Halo1Mcc => Actions.H1_SyncMaterials,
                _ => null
            };

            func?.Invoke();
        }

    #endregion
    }
}