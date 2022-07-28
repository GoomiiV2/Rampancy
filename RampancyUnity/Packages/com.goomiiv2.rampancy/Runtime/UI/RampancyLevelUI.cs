using System;
using Rampancy.Halo3;
using RampantC20;
using RampantC20.Halo3;
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
                if (_Sentinel == null) _Sentinel = GameObject.Find(RampancySentinel.NAME)?.GetComponent<RampancySentinel>();

                return _Sentinel;
            }
        }

        [MenuItem("Rampancy/Open Level UI")]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(RampancyLevelUI), false, "Rampancy Level UI");
            window.Show();
        }

        private void OnGUI()
        {
            ActiveTab = GUILayout.Toolbar(ActiveTab, new[] {"Materials", "Debug"});
            switch (ActiveTab) {
                case 0:
                    MaterialsTab();
                    break;
                case 1:
                    switch (Rampancy.Cfg.GameVersion) {
                        case GameVersions.Halo3:
                            Halo3Debug();
                            break;
                    }

                    if (GUILayout.Button("AssetDB check for changes")) {
                        Rampancy.AssetDBCheckForChanges();
                    }

                    break;
            }
        }

        private void Halo3Debug()
        {
            if (GUILayout.Button($"Test ASS export")) {
                var loadedAss = Ass.Load("E:/Games/Steam/steamapps/common/H3EK/data/levels/dlc/warehouse/structure/warehouse.ass");
                loadedAss.Save("E:/Games/Steam/steamapps/common/H3EK/data/levels/dlc/warehouse/structure/warehouse_out.ass");
            }

            if (GUILayout.Button($"Test Material data")) {
                var testPath = "Assets/Halo3/TagData/levels/multi/construct/shaders/panel_080_floor_mat.asset";
                var asset    = AssetDatabase.LoadMainAssetAtPath(testPath);

                //var info = ScriptableObject.CreateInstance<MatInfo>();
                //AssetDatabase.CreateAsset(material, "Assets/MyMateriala.mat");

                //AssetDatabase.AddObjectToAsset(info, asset);

                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset));
            }
        }

    #region Materials

        private void MaterialsTab()
        {
            if (GUILayout.Button($"Sync materials from {Rampancy.Cfg.GameVersion}"))
                SyncMats();

            if (Sentinel == null) return;

            foreach (var matInfo in Sentinel.MatIdToPathLookup_Paths) {
            }
        }

        private void DrawMaterialInfo()
        {
        }

        private static void SyncMats()
        {
            Rampancy.CurrentGameImplementation.SyncMaterials();
        }

    #endregion
    }
}