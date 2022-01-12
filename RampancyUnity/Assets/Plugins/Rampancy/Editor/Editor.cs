using Plugins.Rampancy.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Plugins.Rampancy
{
    [InitializeOnLoad]
    public class Editor
    {
        static Editor()
        {
            Init();
        }
        
        public static void Init()
        {
            EditorSceneManager.sceneOpened += (scene, mode) =>
            {
                RealtimeCSG.CSGModelManager.ForceRebuild();
            };
        }
    }
}