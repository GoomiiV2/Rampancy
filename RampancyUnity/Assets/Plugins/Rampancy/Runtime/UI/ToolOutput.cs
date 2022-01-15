using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Rampancy.UI
{
    // TODO: Do this better :>
    public class ToolOutput : EditorWindow
    {
        public        Vector2      ScrollPos;
        public static List<string> Lines          = new();

        public static void LogInfo(string  line) => Lines.Add(line);
        public static void LogError(string line) => Lines.Add(line);

        [MenuItem("Rampancy/ToolOutput")]
        public static void ShowWindow()
        {
            EditorWindow window = GetWindow(typeof(ToolOutput));
            window.Show();
        }

        public static void Clear()
        {
            Lines.Clear();
        }

        void OnGUI()
        {
            GUILayout.BeginHorizontal();
            
            var clearBefore = Rampancy.Cfg.ToolOutputClearOnCompile;
            Rampancy.Cfg.ToolOutputClearOnCompile = GUILayout.Toggle(Rampancy.Cfg.ToolOutputClearOnCompile, "Clear on compile");
            if (clearBefore != Rampancy.Cfg.ToolOutputClearOnCompile) Rampancy.Cfg.Save();
            
            if (GUILayout.Button("Clear")) Clear();
            GUILayout.EndHorizontal();

            ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);
            foreach (var line in Lines) {
                GUILayout.Label(line);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}