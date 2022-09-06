using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rampancy.Common;
using RampantC20;
using RealtimeCSG.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Rampancy
{
    // Common interface for game integrations to extend from
    public class GameImplementationBase
    {
        public virtual string GetUnityBasePath()     => Path.Combine("Assets", $"{GameVersions.Halo1Mcc}");
        public virtual string GetUnityTempPathFull() => Path.Combine(Environment.CurrentDirectory, "Temp", $"{GameVersions.Halo1Mcc}");

        public    ImportedAssetDb   ImportedDB;
        protected bool              IsImportingAssets = false;
        protected PostponableAction PostponableAssetImportingEnd;
        public    SceneMatInfo[]    SceneMats { get; private set; } = Array.Empty<SceneMatInfo>();

        public GameImplementationBase()
        {
            var importedDbPath = Path.Combine(GetUnityBasePath(), "ImportedAssetsDB.json");
            ImportedDB = new(importedDbPath);
            ImportedDB.Load();

            PostponableAssetImportingEnd = new(TimeSpan.FromSeconds(2), RealStopEditingAssets);
        }

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

        public virtual void CreateNewScene(string name, string location, GameVersions gameVersion, Action customAction = null)
        {
            if (DoesSceneExist(name, location, gameVersion)) return;

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

            var baseDir   = $"{Path.Combine("Assets", $"{gameVersion}", Statics.SrcLevelsName)}/{location}/{name}";
            var scenePath = $"{baseDir}/{name}.unity";
            Directory.CreateDirectory(baseDir);
            Directory.CreateDirectory(Path.Combine(baseDir, "mats"));
            Directory.CreateDirectory(Path.Combine(baseDir, "instances"));

            customAction?.Invoke();

            EditorSceneManager.SaveScene(scene, scenePath);
            SceneManager.SetActiveScene(currentScene);
        }

        public virtual bool DoesSceneExist(string name, string location, GameVersions gameVersion)
        {
            var              baseDir   = $"{Path.Combine("Assets", $"{gameVersion}", Statics.SrcLevelsName)}/{location}/{name}";
            var              scenePath = $"{baseDir}/{name}.unity";

            var exists = File.Exists(scenePath);
            return exists;
        }

        public virtual void ImportScene(string path = null)
        {
        }

        public virtual void ExportScene(string path = null)
        {
        }

        // When a tag change has been detected
        public virtual void OnTagChanged(string path, string ext, AssetDb.TagChangedType type)
        {
        }

        public virtual void ImportMaterial(string path, string collection, ImportedAssetDb.ImportedAsset parentAssetRecord = null)
        {
        }

        public virtual void ImportBitmap(string path, ImportedAssetDb.ImportedAsset parentAssetRecord = null)
        {
        }

        public virtual void SyncMaterials()
        {
        }

        public virtual void GetMatsInScene()
        {
            var allRcsgModels = Object.FindObjectsOfType<CSGModel>();
            var matSets       = new HashSet<SceneMatInfo>();

            try {
                foreach (var model in allRcsgModels.Where(x => x.name != "[default-CSGModel]")) {
                    foreach (var mesh in model.generatedMeshes.MeshInstances.Where(x => x.name == "[generated-render-mesh]")) {
                        var mat = mesh.RenderMaterial;
                        if (mat != null) {
                            var matInfo = CreateSceneMatInfo();
                            matInfo.Name    = mat.name;
                            matInfo.Mat     = mat;
                            matInfo.MatPath = AssetDatabase.GetAssetPath(mat);

                            if (PrefabUtility.IsPartOfPrefabInstance(model)) {
                                var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model);
                                matInfo.IsInPrefab = true;
                                matInfo.PrefabPath = prefabPath;
                            }

                            matSets.Add(matInfo);
                        }
                    }
                }
            }
            catch (Exception e) {
                
            }

            SceneMats = matSets.ToArray();
        }

        protected virtual SceneMatInfo CreateSceneMatInfo() => new SceneMatInfo();

        protected void StartImportingAssets()
        {
            if (IsImportingAssets) return;

            AssetDatabase.StartAssetEditing();
            IsImportingAssets = true;

            Debug.Log("Starting importing assets");
        }

        protected void StopImportingAssets()
        {
            PostponableAssetImportingEnd.Invoke();
        }

        private void RealStopEditingAssets()
        {
            EditorApplication.delayCall += () =>
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                IsImportingAssets = false;

                Debug.Log("Stopping importing assets");
            };
        }

        public void DeleteAsset(string unityPath, string tagType)
        {
            if (File.Exists(unityPath))
                File.Delete(unityPath);

            var unityMetaPath = $"{unityPath}.meta";
            if (File.Exists(unityMetaPath))
                File.Delete(unityMetaPath);

            var metaPath = $"{unityPath}_meta.json";
            if (File.Exists(metaPath))
                File.Delete(metaPath);

            var tagPath = Path.Combine(Path.GetDirectoryName(unityPath), Path.GetFileNameWithoutExtension(unityPath));
            tagPath = tagPath.Replace($"{Path.Combine(GetUnityBasePath(), "TagData")}\\", "");

            if (tagPath.EndsWith("_mat")) {
                tagPath = tagPath[..^4];
            }

            ImportedDB.Remove(tagPath, tagType);

            var importedDbAsset = ImportedDB.Get(tagPath, tagType);
            if (importedDbAsset != null) {
                // If nothing else references the asset delete it too
                foreach (var assetRef in importedDbAsset.Refs) {
                    if (!ImportedDB.IsAssetReferanced(assetRef.TagType, assetRef.TagPath)) {
                        DeleteAsset(Path.Combine(GetUnityBasePath(), assetRef.FullTagPath), assetRef.TagType);
                    }
                }
            }

            Debug.Log($"Deleted asset: {unityPath}");
        }
    }
}