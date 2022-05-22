using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RampantC20;
using RampantC20.Halo3;
using UnityEditor;
using UnityEngine;

namespace Rampancy.Halo3
{
    public class Halo3Implementation : GameImplementationBase
    {
        public virtual GameVersions GameVersion => GameVersions.Halo3;
        
        public override bool CanOpenTagTest() => false;

        public override bool CanCompileLightmaps() => false;

        public override void CompileStructure()
        {
            var rs = RampancySentinel.GetOrCreateInScene();

            var tagRelPath = $"{rs.DataDir}\\structure\\{rs.LevelName}.ass";
            var exportPath = $"{Rampancy.Cfg.ActiveGameConfig.DataPath}/{tagRelPath}";
            ExportScene(exportPath);

            var cmd = $"structure {tagRelPath}";
            Rampancy.RunToolCommand(cmd);

            Debug.Log("Compiled Halo 3 structure");
        }

        public override void CreateNewScene(string name, bool isSinglePlayer = true, Action customAction = null)
        {
            base.CreateNewScene(name, isSinglePlayer, () =>
            {
                var rs = RampancySentinel.GetOrCreateInScene();
                rs.LevelName   = name;
                rs.DataDir     = isSinglePlayer ? $"levels/solo/{name}" : $"levels/multi/{name}";
                rs.GameVersion = GameVersion;
            });
        }

        public override void ImportScene(string path = null)
        {
            path ??= Utils.OpenFileDialog("Import Ass file", "ass");

            var ass  = Ass.Load(path);
            var name = Path.GetFileNameWithoutExtension(path);

            var converter = new AssConverter();
            converter.ImportToScene(ass, name, path);
        }

        public override void ExportScene(string path = null)
        {
            path ??= Utils.OpenFileDialog("Export Ass file", "ass");

            var exporter = new Halo3LevelExporter();
            exporter.Export(path);
        }

        public override void ImportMaterial(string path, string collection, ImportedAssetDb.ImportedAsset parentAssetRecord = null)
        {
            base.ImportMaterial(path, collection);
        }

        public override void SyncMaterials()
        {
            // TODO: actually sync, check for changes, setup a watcher etc
            ImportShaders();
        }

        public ShaderCollection GetShaderCollection(bool onlyLevelShaders = true)
        {
            var shaderCollectionPath = Path.Combine(Rampancy.Cfg.GetGameConfig(GameVersion).TagsPath, "levels/shader_collections.txt");
            var shaderCollection     = new ShaderCollection(shaderCollectionPath, onlyLevelShaders);
            return shaderCollection;
        }

        // Read the  shader_collections.txt and get all the .shaders paths in those dirs
        public Dictionary<string, List<string>> GetLevelShaders()
        {
            var shaderGroupings  = new Dictionary<string, List<string>>();
            var shaderCollection = GetShaderCollection();

            foreach (var (key, dirPath) in shaderCollection.Mapping) {
                var diFullPath = Path.Combine(Rampancy.Cfg.GetGameConfig(GameVersion).TagsPath, dirPath);
                if (!Directory.Exists(diFullPath)) continue;

                var shaderTypes = new[] {"*.shader", "*.shader_terrain"};
                var shaderPaths = new List<string>();
                foreach (var shaderType in shaderTypes) {
                    var sPaths     = Directory.GetFiles(diFullPath, shaderType, SearchOption.AllDirectories);
                    var fixedPaths = sPaths.Select(x => x.Replace("/", "\\"));
                    shaderPaths.AddRange(fixedPaths);
                }

                shaderGroupings.Add(key, shaderPaths);
            }

            return shaderGroupings;
        }

        public void ImportShaders()
        {
            var progressId = Progress.Start("Importing shaders...", "This could take a while :<");
            Progress.ShowDetails(false);
            var currentIdx = 0;

            var shaderPaths         = GetLevelShaders();
            var flattendShaderPaths = shaderPaths.SelectMany(x => x.Value.Select(y => (x.Key, y))).ToArray();
            var shaderDatas         = new Dictionary<string, ShaderData>(shaderPaths.Count);
            var tasksCount          = flattendShaderPaths.Length + 1;
            var matDataList         = new List<(string, string, string)>();

            AssetDatabase.StartAssetEditing();
            
            var task = Task.Factory.StartNew(() =>
            {
                // Import the textures
                Parallel.ForEach(flattendShaderPaths, (collectionAndPath) =>
                {
                    Progress.Report(progressId, currentIdx++, tasksCount);
                    Progress.SetDescription(progressId, collectionAndPath.y);

                    var path       = collectionAndPath.y;
                    var shaderData = ShaderData.GetDataFromScan(path);
                    shaderData.Collection = collectionAndPath.Key;

                    var doImport = false;
                    lock (shaderDatas) {
                        if (shaderDatas.TryAdd(path, shaderData))
                            doImport = true;
                        else
                            Debug.LogWarning($"Duplicate shader, not importing copy: {path}");
                    }

                    if (doImport) ImportShaderTextures(shaderData);
                    var shaderGuid = CreateMaterialForShader(shaderData);

                    matDataList.Add((shaderGuid, shaderData.Collection, shaderData.TagPath));
                });

                EditorApplication.delayCall += () =>
                {
                    // Unity importing of the textures
                    Progress.Report(progressId, currentIdx++, tasksCount);
                    Progress.SetDescription(progressId, "Waiting for Unity to import the textures");
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                    Progress.Report(progressId, currentIdx++, tasksCount);
                    Progress.SetDescription(progressId, "Adding data to materials");
                    foreach (var matData in matDataList) {
                        var assetPath = AssetDatabase.GUIDToAssetPath(matData.Item1);
                        var matAsset  = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

                        if (matAsset != null) {
                            var matInfo = ScriptableObject.CreateInstance<MatInfo>();
                            matInfo.Mat        = matAsset;
                            matInfo.Collection = matData.Item2;
                            matInfo.Name       = Path.GetFileNameWithoutExtension(matData.Item3);
                            matInfo.name       = "Info";
                            AssetDatabase.AddObjectToAsset(matInfo, matAsset);
                        }
                    }

                    AssetDatabase.Refresh();
                    AssetDatabase.SaveAssets();
                    Progress.Remove(progressId);
                    
                    AssetDatabase.StopAssetEditing();
                };
            });
        }

        // Create a material to represent this shader
        public string CreateMaterialForShader(ShaderData shaderData)
        {
            var shaderTagRelPath  = shaderData.TagPath.Replace("/", "\\").Replace(Rampancy.Cfg.GetGameConfig(GameVersion).TagsPath.Replace("/", "\\"), "");
            var shaderProjectPath = Utils.GetProjectRelPath(shaderTagRelPath.TrimStart('/', '\\'), GameVersion);
            var shaderDirPath     = Path.GetDirectoryName(shaderProjectPath);
            var shaderPath        = Path.Combine(shaderDirPath, $"{Path.GetFileNameWithoutExtension(shaderProjectPath)}_mat.asset");
            Directory.CreateDirectory(shaderDirPath);

            var shaderTypeStr = Path.GetExtension(shaderData.TagPath).Substring(1);
            var tags          = new List<string>() {"mat", nameof(GameVersion), shaderTypeStr, shaderData.Collection}; // some tags to add to the mats to help when searching

            if (shaderData is BasicShaderData basicShader) {
                if (basicShader.DiffuseTex != null) {
                    if (basicShader.IsAlphaTested) tags.Add("AlphaTested");

                    var guid       = TagPathHash.GetHash(basicShader.DiffuseTex, GameVersion);
                    var shaderGuid = TagPathHash.GetHash(basicShader.TagPath.Replace("/", "\\"), GameVersion);
                    FastMatCreate.CreateBasicMat(guid, shaderPath, shaderGuid, tags.ToArray(), basicShader.IsAlphaTested, basicShader.BaseMapScale);

                    return shaderGuid;
                }
            }
            else if (shaderData is TerrainShaderData terrainShader) {
            }

            return null;
        }

        // Import the textures needed for a shader
        public void ImportShaderTextures(ShaderData shaderData)
        {
            // Export the diffuse just for now
            if (shaderData is BasicShaderData basicShader) {
                if (basicShader.DiffuseTex != null) ExportBitmapToTga(basicShader.DiffuseTex);
                /*if (basicShader.DetailTex     != null) ExportBitmapToTga(basicShader.DetailTex);
                if (basicShader.BumpTex       != null) ExportBitmapToTga(basicShader.BumpTex);
                if (basicShader.BumpDetailTex != null) ExportBitmapToTga(basicShader.BumpDetailTex);
                if (basicShader.AlphaTestMap  != null) ExportBitmapToTga(basicShader.AlphaTestMap);*/
            }
            else if (shaderData is TerrainShaderData terrainShader) {
            }
        }

        public override void ImportBitmap(string path, ImportedAssetDb.ImportedAsset parentAssetRecord = null)
        {
            if (path == null) return;
            
            var progressId = Progress.Start($"Importing bitmap: {path}");
            ExportBitmapToTga(path);
            
            ImportedDB.Add(path, "bitmap");
            parentAssetRecord?.AddRef(path, "bitmap");
            ImportedDB.AddRefToEntry(parentAssetRecord, path, "bitmap");
            
            Progress.Remove(progressId);
        }

        // Use Tool to export a texture to a tga in the project dir
        public virtual void ExportBitmapToTga(string tagPath, string outPath = null)
        {
            if (outPath == null) {
                var destPath = Utils.GetProjectRelPath(tagPath, GameVersion, Environment.CurrentDirectory);
                var dirPath  = Path.GetDirectoryName(destPath);
                Directory.CreateDirectory(dirPath);
                outPath = $"{dirPath}/";
            }

            var fullPath = outPath + $"{Path.GetFileName(tagPath)}_00.tga";
            if (!File.Exists(fullPath)) Rampancy.RunProgram(((H3GameConfig)Rampancy.Cfg.GetGameConfig(GameVersion)).ToolFastPath, $"export-bitmap-tga \"{tagPath}\" \"{outPath}\"", true, true);
        }
        
        // New shader import pipeline
        // Gather list of shaders paths
    }
}