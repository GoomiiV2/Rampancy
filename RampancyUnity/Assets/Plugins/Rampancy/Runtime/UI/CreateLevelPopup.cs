using System.IO;
using RampantC20;
using RealtimeCSG.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            var activeConfig = Rampancy.Cfg.ActiveGameConfig;

            if (GUILayout.Button($"Create new {Rampancy.Cfg.GameVersion} level")) {
                switch (Rampancy.Cfg.GameVersion) {
                    case GameVersions.Halo1Mcc:
                        CreateHalo1Scene();
                        break;
                    case GameVersions.Halo3:
                        CreateHalo3Scene();
                        break;
                }

                Close();
            }

            if (GUILayout.Button("Cancel"))
                Close();
        }

        private void CreateHalo1Scene()
        {
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

            var debugGeo = new GameObject("DebugGeo");
            debugGeo.transform.parent = frame.transform;

            var csgModel = levelGeo.AddComponent<CSGModel>();
            csgModel.Settings = ModelSettingsFlags.InvertedWorld | ModelSettingsFlags.NoCollider;

            var baseDir   = $"{Rampancy.SceneDir}/{LevelName}";
            var scenePath = $"{baseDir}/{LevelName}.unity";
            Directory.CreateDirectory(baseDir);
            Directory.CreateDirectory(Path.Combine(baseDir, "mats"));
            Directory.CreateDirectory(Path.Combine(baseDir, "instances"));

            EditorSceneManager.SaveScene(scene, scenePath);
            SceneManager.SetActiveScene(currentScene);
        }

        private void CreateHalo3Scene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = LevelName;

            var currentScene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            var rs = RampancySentinel.GetOrCreateInScene();
            rs.LevelName = LevelName;
            rs.DataDir   = IsSP ? $"levels/solo/{LevelName}" : $"levels/multi/{LevelName}";

            var frame    = new GameObject("Frame");
            var levelGeo = new GameObject("LevelGeo");
            levelGeo.transform.parent = frame.transform;

            var debugGeo = new GameObject("DebugGeo");
            debugGeo.transform.parent = frame.transform;

            var csgModel = levelGeo.AddComponent<CSGModel>();
            csgModel.Settings = ModelSettingsFlags.InvertedWorld | ModelSettingsFlags.NoCollider;

            var baseDir   = $"{Rampancy.SceneDir}/{LevelName}";
            var scenePath = $"{baseDir}/{LevelName}.unity";
            Directory.CreateDirectory(baseDir);
            Directory.CreateDirectory(Path.Combine(baseDir, "mats"));
            Directory.CreateDirectory(Path.Combine(baseDir, "instances"));

            EditorSceneManager.SaveScene(scene, scenePath);
            SceneManager.SetActiveScene(currentScene);
        }
    }
}