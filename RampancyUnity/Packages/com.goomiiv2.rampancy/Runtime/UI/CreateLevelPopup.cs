using UnityEditor;
using UnityEngine;

namespace Rampancy.UI
{
    public class CreateLevelPopup : EditorWindow
    {
        public string LevelName;
        public bool   IsSP = true;

        private void OnGUI()
        {
            LevelName = EditorGUILayout.TextField("Level Name", LevelName);
            IsSP      = EditorGUILayout.Toggle("Single Player", IsSP);

            if (GUILayout.Button($"Create new {Rampancy.Cfg.GameVersion} level")) {
                Rampancy.CurrentGameImplementation.CreateNewScene(LevelName, IsSP);
                Close();
            }

            if (GUILayout.Button("Cancel"))
                Close();
        }
    }
}