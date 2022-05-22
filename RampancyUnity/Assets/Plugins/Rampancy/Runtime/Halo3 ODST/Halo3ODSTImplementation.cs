using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rampancy.Halo3;
using Rampancy.RampantC20;
using RampantC20;
using UnityEditor;
using ShaderData = Rampancy.Halo3.ShaderData;

namespace Rampancy.Halo3_ODST
{
    public class Halo3ODSTImplementation : Halo3Implementation
    {
        public override string GetUnityBasePath() => Path.Combine("Assets", $"{GameVersions.Halo3ODST}");

        public override GameVersions GameVersion => GameVersions.Halo3ODST;

        /*public override void ExportBitmapToTga(string tagPath, string outPath = null)
        {
            if (outPath == null) {
                var destPath = Utils.GetProjectRelPath(tagPath, GameVersion, Environment.CurrentDirectory);
                var dirPath  = Path.GetDirectoryName(destPath);
                Directory.CreateDirectory(dirPath);
                outPath = $"{dirPath}/";
            }

            // hdds because unity has an importer for dds THAT DOESN"T IMPORT and blocks me for making my own for that extension, ofc
            var fullPath = outPath + $"{Path.GetFileName(tagPath)}_00.hdds";
            var fullPathDds = outPath + $"{Path.GetFileName(tagPath)}_00.dds";
            if (!File.Exists(fullPath)) {
                Rampancy.RunProgram(((H3GameConfig)Rampancy.Cfg.GetGameConfig(GameVersion)).ToolFastPath, $"export-bitmap-dds \"{tagPath}\" \"{outPath}\"", true, true);
                if (File.Exists(fullPathDds)) File.Move(fullPathDds , fullPath);
            }
        }*/

        public override void ExportBitmapToTga(string tagPath, string outPath = null)
        {
            if (outPath == null && tagPath != null) {
                var destPath = Utils.GetProjectRelPath(tagPath, GameVersion, Environment.CurrentDirectory);
                var dirPath  = Path.GetDirectoryName(destPath);
                Directory.CreateDirectory(dirPath);
                outPath = $"{dirPath}/";
            }

            var fullPath = outPath + $"{Path.GetFileName(tagPath)}_00.tga";
            if (!File.Exists(fullPath)) Rampancy.RunProgram(((H3ODSTGameConfig) Rampancy.Cfg.GetGameConfig(GameVersion)).H3_ToolFastPath, $"export-bitmap-tga \"{tagPath}\" \"{outPath}\"", true, true);
        }

        public override void SyncMaterials()
        {
            var shaderPaths = GetLevelShaders();

            var progressId = Progress.Start("Importing shaders...", "This could take a while :<");
            Progress.ShowDetails(false);
            int currentIdx   = 0;
            int totalShaders = shaderPaths.SelectMany(x => x.Value.Select(y => y.Length)).Sum();

            AssetDatabase.StartAssetEditing();
            {
                foreach (var (col, paths) in shaderPaths) {
                    foreach (var path in paths) {
                        Progress.Report(progressId, currentIdx++, totalShaders);
                        Progress.SetDescription(progressId, $"{path}");
                        ImportMaterial(path, col);
                    }
                }
            }
            AssetDatabase.StopAssetEditing();

            Progress.Remove(progressId);
        }

        public override void OnTagChanged(string path, string ext, AssetDb.TagChangedType type)
        {
            //StartImportingAssets();
            var tagPath = path[..^Path.GetExtension(path).Length];
            
            if (type == AssetDb.TagChangedType.Deleted) {
                if (ImportedDB.IsImported(path, ext)) {
                    var unityPath = Path.Combine(GetUnityBasePath(), tagPath);
                    AssetDatabase.DeleteAsset(unityPath);
                }
                
                ImportedDB.Remove(path, ext);
            }
            else if (type is AssetDb.TagChangedType.Added or AssetDb.TagChangedType.Changed) {
                // Check if a shader
                if (new[] {"shader", "shader_terrain"}.Any(x => x == ext)) {
                    // is a level shader, is in the collection
                    var shaderCollection = GetShaderCollection();
                    var collection = shaderCollection.GetCollectionNameForShader(path);
                    if (collection != null) {
                        ImportMaterial(path, collection);
                    }
                }
                else if (new[] {"bitmap"}.Any(x => x == ext)) { // texture
                    // Only if its an update to an imported one
                    if (ImportedDB.IsImported(path, ext)) {
                        Rampancy.ToolTaskRunner.Queue(new ToolTasker.ToolTask(() => ImportBitmap(path)));
                    }
                }
            }
            
            //StopImportingAssets();
        }

        public override void ImportMaterial(string tagPath, string collection, ImportedAssetDb.ImportedAsset parentAssetRecord = null)
        {
            var progressId    = Progress.Start($"Importing shader: {tagPath}");
            
            var tagsPath    = Rampancy.Cfg.GetGameConfig(GameVersion).TagsPath;
            var fullTagPath = Path.Combine(tagsPath, tagPath);
            var assetRecord = new ImportedAssetDb.ImportedAsset(tagPath);

            if (collection == null) {
                var shaderCollection = GetShaderCollection();
                collection = shaderCollection.GetCollectionNameForShader(tagPath);
            }

            // Get info for the shader
            var shdData = ShaderData.GetDataFromScan(fullTagPath);
            shdData.TagPath    = tagPath;
            shdData.Collection = collection;

            if (shdData is BasicShaderData shdBasic) {
                // import the bitmaps needed
                Rampancy.ToolTaskRunner.Queue(new ToolTasker.ToolTask(() => ImportBitmap(shdBasic.DiffuseTex, assetRecord)));
                
                parentAssetRecord?.AddRef(tagPath, "shader");
            }
            
            CreateMaterialForShader(shdData);

            var tagType = Path.GetExtension(tagPath)[1..];
            ImportedDB.Add(assetRecord, tagType);

            Progress.Remove(progressId);
        }

        // Read the shader_collections.txt and get all the .shaders paths in those dirs
        public Dictionary<string, List<string>> GetLevelShaders(bool tagRelPath = true)
        {
            var collectionsAndPaths = new Dictionary<string, List<string>>();
            var shaderCollection    = GetShaderCollection();
            var tagsDir             = Path.GetFullPath(Rampancy.Cfg.GetGameConfig(GameVersion).TagsPath);
            var shaderTypes         = new[] {"*.shader", "*.shader_terrain"}; // we only want these

            foreach (var (collection, colPath) in shaderCollection.Mapping) {
                var dirFullPath = Path.Combine(tagsDir, colPath);
                if (!Directory.Exists(dirFullPath)) continue;

                var shadersInCol = shaderTypes.SelectMany(x => Directory.GetFiles(dirFullPath, x, SearchOption.AllDirectories))
                                              .Select(Path.GetFullPath)
                                              .Select(x => tagRelPath ? x[(tagsDir.Length + 1)..] : x);

                collectionsAndPaths.Add(collection, shadersInCol.ToList());
            }

            return collectionsAndPaths;
        }
    }
}