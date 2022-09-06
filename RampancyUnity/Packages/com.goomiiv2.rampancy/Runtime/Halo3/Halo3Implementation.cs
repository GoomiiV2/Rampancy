using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Rampancy.Common;
using Rampancy.RampantC20;
using RampantC20;
using RampantC20.Halo3;
using UnityEditor;
using UnityEngine;

namespace Rampancy.Halo3
{
    public class Halo3Implementation : GameImplementationBase
    {
        public virtual GameVersions GameVersion => GameVersions.Halo3;

        public override string GetUnityBasePath() => Path.Combine("Assets", $"{GameVersions.Halo3}");

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

        public override void CreateNewScene(string name, string location, GameVersions gameVersion, Action customAction = null)
        {
            base.CreateNewScene(name, location, gameVersion, () =>
            {
                var rs = RampancySentinel.GetOrCreateInScene();
                rs.LevelName   = name;
                rs.DataDir     =  $"levels/{location}/{name}";
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

        public override void ImportMaterial(string tagPath, string collection, ImportedAssetDb.ImportedAsset parentAssetRecord = null)
        {
            var progressId = Progress.Start($"Importing shader: {tagPath}");

            var tagsPath    = Rampancy.Cfg.GetGameConfig(GameVersion).TagsPath;
            var fullTagPath = Path.Combine(tagsPath, tagPath);
            var assetRecord = new ImportedAssetDb.ImportedAsset(tagPath);
            
            var tagType = Path.GetExtension(tagPath)[1..];
            ImportedDB.Add(assetRecord, tagType);

            if (collection == null) {
                var shaderCollection = GetShaderCollection();
                collection = shaderCollection.GetCollectionNameForShader(tagPath);
            }

            // Get info for the shader
            var shdData = ShaderData.GetDataFromScan(fullTagPath);
            if (shdData == null) return;
            
            shdData.TagPath    = tagPath;
            shdData.Collection = collection;

            if (shdData is BasicShaderData shdBasic) {
                // import the bitmaps needed
                Rampancy.ToolTaskRunner.Queue(new ToolTasker.ToolTask(() => ImportBitmap(shdBasic.DiffuseTex, assetRecord)));

                if (((H3GameConfig)Rampancy.Cfg.GetGameConfig(GameVersion)).CreateAdvancedShaders) {
                    Rampancy.ToolTaskRunner.Queue(new ToolTasker.ToolTask(() => ImportBitmap(shdBasic.BumpTex, assetRecord)));
                    Rampancy.ToolTaskRunner.Queue(new ToolTasker.ToolTask(() => ImportBitmap(shdBasic.DetailTex, assetRecord)));
                    Rampancy.ToolTaskRunner.Queue(new ToolTasker.ToolTask(() => ImportBitmap(shdBasic.SelfIllum, assetRecord)));
                    Rampancy.ToolTaskRunner.Queue(new ToolTasker.ToolTask(() => ImportBitmap(shdBasic.BumpDetailTex, assetRecord)));
                }

                parentAssetRecord?.AddRef(tagPath, "shader");
            }

            CreateMaterialForShader(shdData);

            Progress.Remove(progressId);
        }

        public override void SyncMaterials()
        {
            var shaderPaths = GetLevelShaders();

            var progressId = Progress.Start("Importing tags...", "This could take a while :<");
            Progress.ShowDetails(false);
            int currentIdx   = 0;
            int totalShaders = shaderPaths.SelectMany(x => x.Value.Select(y => y.Length)).Sum();

            //AssetDatabase.StartAssetEditing();
            {
                var changes = Rampancy.AssetDB.GetTagChanges(Rampancy.TagsDbPath);
                AssetDatabase.StartAssetEditing();
                foreach (var change in changes) {
                    var ext = Path.GetExtension(change.Path)[1..];
                    OnTagChanged(change.Path, ext, change.ChangeType);
                    Progress.Report(progressId, currentIdx, changes.Count, change.Path);
                }

                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();

                /*foreach (var (col, paths) in shaderPaths) {
                    foreach (var path in paths) {
                        Progress.Report(progressId, currentIdx++, totalShaders);
                        Progress.SetDescription(progressId, $"{path}");
                        ImportMaterial(path, col);
                    }
                }*/
            }
            //AssetDatabase.StopAssetEditing();

            Progress.Remove(progressId);
        }

        public ShaderCollection GetShaderCollection(bool onlyLevelShaders = true)
        {
            var shaderCollectionPath = Path.Combine(Rampancy.Cfg.GetGameConfig(GameVersion).TagsPath, "levels/shader_collections.txt");
            var shaderCollection     = new ShaderCollection(shaderCollectionPath, onlyLevelShaders);
            return shaderCollection;
        }

        public override void OnTagChanged(string path, string ext, AssetDb.TagChangedType type)
        {
            //StartImportingAssets();
            var tagPath  = path[..^Path.GetExtension(path).Length];
            var isShader = new[] {"shader", "shader_terrain"}.Any(x => x == ext);
            var isBitmap = new[] {"bitmap"}.Any(x => x                   == ext);

            if (type == AssetDb.TagChangedType.Deleted) {
                if (ImportedDB.IsImported(path, ext)) {
                    var unityPath = Path.Combine(GetUnityBasePath(), "TagData", tagPath);
                    if (isShader) {
                        var matPath = $"{unityPath}_mat.asset";
                        DeleteAsset(matPath, ext);
                    }
                    else if (isBitmap) {
                        DeleteAsset($"{unityPath}_00.tga", ext);
                    }
                }
            }
            else if (type is AssetDb.TagChangedType.Added or AssetDb.TagChangedType.Changed) {
                // Check if a shader
                if (isShader) {
                    // is a level shader, is in the collection
                    var shaderCollection = GetShaderCollection();
                    var collection       = shaderCollection.GetCollectionNameForShader(path);
                    if (collection != null) {
                        ImportMaterial(path, collection);
                    }
                }
                else if (isBitmap) { // texture
                    // Only if its an update to an imported one
                    if (ImportedDB.IsImported(path, ext)) {
                        Rampancy.ToolTaskRunner.Queue(new ToolTasker.ToolTask(() => ImportBitmap(path)));
                    }
                }
            }

            //StopImportingAssets();
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
            var tags          = new List<string>() {"mat", $"{GameVersion}", shaderTypeStr, shaderData.Collection}; // some tags to add to the mats to help when searching

            if (shaderData is BasicShaderData basicShader) {
                if (basicShader.DiffuseTex != null) {
                    if (basicShader.IsAlphaTested) tags.Add("AlphaTested");

                    //var guid       = TagPathHash.GetHash(basicShader.DiffuseTex, GameVersion);
                    //var shaderGuid = TagPathHash.GetHash(basicShader.TagPath.Replace("/", "\\"), GameVersion);
                    //FastMatCreate.CreateBasicMat(guid, shaderPath, shaderGuid, tags.ToArray(), basicShader.IsAlphaTested, basicShader.BaseMapScale);

                    var shaderGuid = FastMatCreate.CreateBasicMat(GameVersion, shaderPath, basicShader, tags.ToArray());
                    AddMetaDataToMat(shaderData.Collection, shaderPath, shaderData.TagPath);

                    return shaderGuid;
                }
            }
            else if (shaderData is TerrainShaderData terrainShader) {
            }

            return null;
        }


        public override void ImportBitmap(string path, ImportedAssetDb.ImportedAsset parentAssetRecord = null)
        {
            if (path == null) return;
            ExportBitmapToTga(path);

            ImportedDB.Add(path, "bitmap");
            parentAssetRecord?.AddRef(path, "bitmap");
            ImportedDB.AddRefToEntry(parentAssetRecord, path, "bitmap");
        }

        public override void GetMatsInScene()
        {
            base.GetMatsInScene();

            foreach (var sceneMat in SceneMats) {
                var h3MatInfo = (SceneMatInfoHalo3) sceneMat;
                h3MatInfo.LoadMatMeta();
            }
        }

        protected override SceneMatInfo CreateSceneMatInfo() => new SceneMatInfoHalo3();

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
            if (!File.Exists(fullPath)) Rampancy.RunProgram(((H3GameConfig) Rampancy.Cfg.GetGameConfig(GameVersion)).ToolFastPath, $"export-bitmap-tga \"{tagPath}\" \"{outPath}\"", true, true);
        }

        public void AddMetaDataToMat(string collection, string matPath, string tagPath)
        {
            var matInfo = MatInfo.Create(matPath, collection);
            matInfo.TagPath = tagPath;
            matInfo.Save(matPath);
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