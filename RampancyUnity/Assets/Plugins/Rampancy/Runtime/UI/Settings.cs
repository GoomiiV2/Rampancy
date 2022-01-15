using RampantC20;
using UnityEditor;
using UnityEngine;


namespace Rampancy.UI
{
    public class Settings : EditorWindow
    {
        [MenuItem("Rampancy/Settings")]
        public static void ShowWindow()
        {
            EditorWindow window = GetWindow(typeof(Settings), false, "Settings");
            window.Show();
        }
        
        void OnGUI()
        {
            Rampancy.Cfg ??= Config.Load();

            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Active Game Version");
                Rampancy.Cfg.GameVersion = (GameVersions) EditorGUILayout.EnumPopup("", Rampancy.Cfg.GameVersion, GUILayout.Width(150));
            }
            EditorGUILayout.EndHorizontal();
            
            // Game configs
            DrawGameConfig("Halo 1 MCC", Rampancy.Cfg.Halo1MccGameConfig);
            DrawGameConfig("Halo 3 MCC", Rampancy.Cfg.Halo3MccGameConfig);

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            if (GUILayout.Button("Save")) {
                Rampancy.Cfg.Save();
                
                // TODO: only do this is the path of a game changed or the game version
                Rampancy.AssetDB.ScanTags(Rampancy.Cfg.ActiveGameConfig.TagsPath);
            }

            ShowVersion();
        }

        private static void ShowVersion()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Version: ");
                GUILayout.Label(Statics.Version);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGameConfig(string name, GameConfig config)
        {
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            }
            EditorGUILayout.EndHorizontal();
            
            // Base dir 

            GUILayout.Label("Base Tools dir: ");
            EditorGUILayout.BeginHorizontal();
            {
                config.ToolBasePath = GUILayout.TextField(config.ToolBasePath ?? "");

                if (GUILayout.Button("...", GUILayout.Width(40))) {
                    var path = EditorUtility.OpenFolderPanel("Base dir", "", "");
                    if (path != "") {
                        config.ToolBasePath = path;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}