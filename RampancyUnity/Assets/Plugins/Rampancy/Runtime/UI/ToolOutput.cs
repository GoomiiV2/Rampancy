using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Plugins.Rampancy.Runtime.UI
{
    // TODO: Do this better :>
    public class ToolOutput : EditorWindow
    {
        public Vector2      ScrollPos;
        public static List<string> Lines = new();

        public static void LogInfo(string line) => Lines.Add(line);
        public static void LogError(string line) => Lines.Add(line);
        
        [MenuItem("Rampancy/ToolOutput")]
        public static void ShowWindow()
        {
            EditorWindow window = GetWindow(typeof(ToolOutput));
            window.Show();
        }
        
        void OnGUI()
        {
            if (GUILayout.Button("Clear")) {
                Lines.Clear();
            }
            
            ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);

            var style = new GUIStyle
            {
                normal = new GUIStyleState()
            };
            
            foreach (var line in Lines) {
                //if (line.StartsWith("!")) style.normal.textColor = Color.red;
                
                GUILayout.Label(line);
            }
            
            EditorGUILayout.EndScrollView();
        }
    }
}