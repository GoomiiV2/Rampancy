using Plugins.Rampancy.Runtime;
using UnityEditor;
using UnityEngine;

namespace Plugins.Rampancy.Editor.Scripts.UI
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
            ActiveTab = GUILayout.Toolbar(ActiveTab, new [] {"Materials", "Debug"});
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

            GUILayout.Button($"Sync materials from {Rampancy.Runtime.Rampancy.Config.GameVersion}");
            
            foreach (var matInfo in Sentinel.MatIdToPathLookup_Paths) {
                
            }
        }

        private void DrawMaterialInfo()
        {
            
        }
    #endregion
    }
}