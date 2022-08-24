using System;
using System.Collections.Generic;
using System.Linq;
using Rampancy.Common;
using Rampancy.Halo3;
using RampantC20;
using RampantC20.Halo3;
using UnityEditor;
using UnityEngine;

namespace Rampancy.UI
{
    public partial class RampancyLevelUI : EditorWindow
    {
        private int ActiveTab = 0;

        private RampancySentinel _Sentinel;

        private Dictionary<GameVersions, MaterialRenderHandlers> MaterialEditFunctions = new()
        {
            {GameVersions.Halo3, new MaterialRenderHandlers {BasicView = DrawMaterialInfoHalo3Basic, AdvView = DrawMaterialInfoHalo3Adv}}
        };

        private Dictionary<SceneMatInfo, (bool ShowAdvanced, bool Placeholder)> MaterialStates = new();
        private Vector2                                                         ScrollPos      = Vector2.zero;

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
            /*if (GUILayout.Button($"Sync materials from {Rampancy.Cfg.GameVersion}"))
                SyncMats();

            if (Sentinel == null) return;
            */

            if (GUILayout.Button("Refresh")) {
                Rampancy.CurrentGameImplementation.GetMatsInScene();
            }

            bool HasDrawHandlers = MaterialEditFunctions.TryGetValue(Rampancy.ActiveGameVersion, out var drawHandlers);
            if (HasDrawHandlers) {
                ScrollPos = GUILayout.BeginScrollView(ScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
                foreach (var matInfo in Rampancy.CurrentGameImplementation.SceneMats) {
                    if (ShouldIgnore(matInfo)) continue;
                    
                    DrawMaterialInfo(matInfo, drawHandlers);
                    GUILayout.Space(15);
                }

                GUILayout.EndScrollView();
            }
        }

        private void DrawMaterialInfo(SceneMatInfo matInfo, MaterialRenderHandlers handlers)
        {
            const int THUMBNAIL_HEIGHT   = 100;

            if (!MaterialStates.TryGetValue(matInfo, out var toggleState)) {
                MaterialStates.Add(matInfo, new(false, false));
            }

            // @formatter:off
            GUILayout.BeginVertical();
                DrawTitle(matInfo.GetDisplayName());
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                    var icon = AssetPreview.GetAssetPreview(matInfo.Mat);
                    GUILayout.Label(icon, GUILayout.Height(THUMBNAIL_HEIGHT), GUILayout.Width(THUMBNAIL_HEIGHT));
                    GUILayout.BeginVertical(GUILayout.Width(THUMBNAIL_HEIGHT));
                        if (!handlers.BasicView(matInfo)) {
                            DrawMaterialCopyOptions(matInfo);
                        }
                        else {
                            toggleState.ShowAdvanced = EditorGUILayout.Foldout(toggleState.ShowAdvanced, "Advanced");
                        }
                    GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            // @formatter:on 

            if (toggleState.ShowAdvanced) {
                handlers.AdvView(matInfo);
            }

            MaterialStates[matInfo] = toggleState;
        }

        private static void DrawMaterialCopyOptions(SceneMatInfo matInfo)
        {
            if (GUILayout.Button("Make editable copy")) {
                CreateCopyOfMaterial(matInfo);
            }
            
            if (GUILayout.Button("Make editable copy and replace", GUILayout.ExpandWidth(true))) {
                var mat = CreateCopyOfMaterial(matInfo);
                if (mat != null) {
                    Utils.ReplaceMaterailOnBrushesInScene(matInfo.Mat, mat);
                }
            }
        }

        private static Material CreateCopyOfMaterial(SceneMatInfo matInfo)
        {
            var name = EditorInputDialog.Show( "Name", "New material copy name", matInfo.Name );
            if (!string.IsNullOrEmpty(name)) {
                if (matInfo.DoesCopyExist(name)) {
                    EditorUtility.DisplayDialog("Error", "A material with that name already exists, try another name.", "Ok");
                }
                else {
                    var mat = matInfo.MakeCopy(name);
                    if (mat != null) {
                        EditorUtility.DisplayDialog("Copy created", $"A copy of the material was created at: {AssetDatabase.GetAssetPath(mat)}", "Ok");
                        Rampancy.CurrentGameImplementation.GetMatsInScene();
                        
                        return mat;
                    }
                    else {
                        EditorUtility.DisplayDialog("Error", "Couldn't create a copy of the material ;^;", "Ok");
                    }
                }
            }
            else {
                EditorUtility.DisplayDialog("Error", "No name was given, please give me a name for the copy D:", "Ok");
            }
            
            return null;
        }

        private static void DrawTitle(string title, int fontSize = 14)
        {
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            {
                var style = EditorStyles.boldLabel;
                style.fontSize  = fontSize;
                style.fontStyle = FontStyle.BoldAndItalic;
                GUILayout.Label(title, style);

                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.Height(16f));
            }
            GUILayout.EndHorizontal();
        }

        private static System.Numerics.Vector3 ColorField(string name, string tooltip, System.Numerics.Vector3 color)
        {
            var inColor  = new Color(color.X, color.Y, color.Z);
            var outColor = EditorGUILayout.ColorField(new GUIContent(name, tooltip), inColor).ToNumerics();

            return outColor;
        }

        // If this material should be hidden from the UI, things like tool textures, +sky, etc
        private static bool ShouldIgnore(SceneMatInfo matInfo)
        {
            var inbuiltNames = new [] { "+portal" };
            return matInfo.Name.StartsWith("+sky") || inbuiltNames.Any(x => x == matInfo.Name);
        }
        
        private static void SyncMats()
        {
            Rampancy.CurrentGameImplementation.SyncMaterials();
        }

    #endregion

        protected class MaterialRenderHandlers
        {
            public Func<SceneMatInfo, bool> BasicView;
            public Action<SceneMatInfo>     AdvView;
        }
    }
}