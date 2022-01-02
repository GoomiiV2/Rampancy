using System.IO;
using RealtimeCSG.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugins.Rampancy.Runtime.UI
{
    public class CreateLevelPopup : EditorWindow
    {
        public string LevelName;
        public bool   IsSP = true;
        
        void OnGUI()
        {
            LevelName = EditorGUILayout.TextField("Level Name", LevelName);
            IsSP      = EditorGUILayout.Toggle("Single Player", IsSP);

            if (GUILayout.Button("Create new level")) {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                scene.name = LevelName;

                var currentScene = SceneManager.GetActiveScene();
                SceneManager.SetActiveScene(scene);
                var rs = RampancySentinel.GetOrCreateInScene();
                rs.LevelName = LevelName;
                rs.DataDir   = IsSP ? $"levels/{LevelName}" : $"levels/test/{LevelName}";

                var frame    = new GameObject("Frame");
                var levelGeo = new GameObject("LevelGeo");
                levelGeo.transform.parent = frame.transform;

                var csgModel = levelGeo.AddComponent<CSGModel>();
                csgModel.Settings = ModelSettingsFlags.InvertedWorld | ModelSettingsFlags.NoCollider;
                
                var scenePath = $"{Rampancy.SceneDir}/{LevelName}.unity";
                Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
                EditorSceneManager.SaveScene(scene, scenePath);
                SceneManager.SetActiveScene(currentScene);
                
                Close();
            }
 
            if (GUILayout.Button("Cancel"))
                Close();
        }
    }
}