using Plugins.Rampancy.RampantC20;
using Plugins.Rampancy.Runtime;
using UnityEditor;
using UnityEngine;

namespace Plugins.Rampancy.Editor.Scripts.UI
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
            Runtime.Rampancy.Config ??= Config.Load();

            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Active Game Version");
                Runtime.Rampancy.Config.GameVersion = (GameVersions) EditorGUILayout.EnumPopup("", Runtime.Rampancy.Config.GameVersion, GUILayout.Width(150));
            }
            EditorGUILayout.EndHorizontal();
            
            // Game configs
            DrawGameConfig("Halo 1 MCC", Runtime.Rampancy.Config.Halo1MccGameConfig);
            DrawGameConfig("Halo 3 MCC", Runtime.Rampancy.Config.Halo3MccGameConfig);

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            if (GUILayout.Button("Save")) {
                Runtime.Rampancy.Config.Save();
            }
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