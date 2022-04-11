using System;
using System.IO;
using RampantC20;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rampancy
{
    // Common interface for game integrations to extend from
    public class GameImplementationBase
    {
        // To enable the menu options for these
        public virtual bool CanOpenSapien() => true;

        public virtual bool CanOpenGuerilla() => true;

        public virtual bool CanOpenTagTest() => true;

        public virtual bool CanCompileStructure() => true;

        public virtual bool CanCompileLightmaps() => true;

        public virtual bool CanCompileStructureAndLightmaps() => CanCompileStructure() && CanCompileLightmaps();

        public virtual bool CanImportScene() => true;

        public virtual bool CanExportScene() => true;

        public virtual void OpenSapien()
        {
            Utils.RunExeIfExists(Rampancy.Cfg.ActiveGameConfig.SapienPath);
        }

        public virtual void OpenGuerilla()
        {
            Utils.RunExeIfExists(Rampancy.Cfg.ActiveGameConfig.GuerillaPath);
        }

        public virtual void OpenTagTest()
        {
            Utils.RunExeIfExists(Rampancy.Cfg.ActiveGameConfig.TagTestPath);
        }

        public virtual void OpenInTagTest(string mapPath = null)
        {
        }

        public virtual void CompileStructure()
        {
        }

        public virtual void CompileLightmaps(bool preview = true, float quality = 1.0f)
        {
        }

        public virtual void CompileStructureAndLightmaps()
        {
        }

        public virtual void CreateNewScene(string name, bool isSinglePlayer = true, Action customAction = null)
        {
            if (DoesSceneExist(name)) return;
            
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = name;

            var currentScene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            var rs = RampancySentinel.GetOrCreateInScene();

            var frame    = new GameObject("Frame");
            var levelGeo = new GameObject("LevelGeo");
            levelGeo.transform.parent = frame.transform;

            var debugGeo = new GameObject("DebugGeo");
            debugGeo.transform.parent = frame.transform;

            var csgModel = levelGeo.AddComponent<CSGModel>();
            csgModel.Settings = ModelSettingsFlags.InvertedWorld | ModelSettingsFlags.NoCollider;

            var baseDir   = $"{Rampancy.SceneDir}/{name}";
            var scenePath = $"{baseDir}/{name}.unity";
            Directory.CreateDirectory(baseDir);
            Directory.CreateDirectory(Path.Combine(baseDir, "mats"));
            Directory.CreateDirectory(Path.Combine(baseDir, "instances"));
            
            customAction?.Invoke();

            EditorSceneManager.SaveScene(scene, scenePath);
            SceneManager.SetActiveScene(currentScene);
        }

        public virtual bool DoesSceneExist(string name)
        {
            var baseDir   = $"{Rampancy.SceneDir}/{name}";
            var scenePath = $"{baseDir}/{name}.unity";

            var exists = File.Exists(scenePath);
            return exists;
        }
        
        public virtual void ImportScene(string path = null)
        {
        }

        public virtual void ExportScene(string path = null)
        {
        }

        public virtual void ImportMaterial(string path)
        {
        }

        public virtual void SyncMaterials()
        {
        }
    }
}