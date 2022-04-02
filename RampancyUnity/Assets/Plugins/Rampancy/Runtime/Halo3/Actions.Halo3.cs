using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Plugins.Rampancy.Runtime;
using Rampancy.Halo3;
using RampantC20;
using RampantC20.Halo3;
using UnityEditor;
using UnityEngine;

namespace Rampancy
{
    public static partial class Actions
    {
        public static void H3_ImportAssDialog()
        {
            var filePath = EditorUtility.OpenFilePanel("Import Ass file", "", "ass");
            if (!string.IsNullOrEmpty(filePath)) H3_ImportAss(filePath);
        }

        public static void H3_ImportAss(string path)
        {
            Debug.Log("Importing Ass :D");

            var ass  = Ass.Load(path);
            var name = Path.GetFileNameWithoutExtension(path);

            var converter = new AssConverter();
            converter.ImportToScene(ass, name, path);
        }

        public static void H3_ExportAss()
        {
            var filePath = EditorUtility.OpenFilePanel("Import ASS file", "", "ass");
            if (filePath == null) return;
            
            var exporter = new Halo3LevelExporter();
            exporter.Export(filePath);
        }

        public static void H3_CompileStructure()
        {
            var rs = RampancySentinel.GetOrCreateInScene();

            var exportPath = $"{Rampancy.Cfg.ActiveGameConfig.DataPath}/{rs.DataDir}/structure/{rs.LevelName}.ass";
            var exporter      = new Halo3LevelExporter();
            exporter.Export(exportPath);

            var cmd = $"structure {rs.DataDir}\\structure\\{rs.LevelName}.ass";
            Rampancy.RunToolCommand(cmd);

            Debug.Log("Compiled Halo 3 structure");
        }

        public static ShaderCollection H3_GetShaderCollection(bool onlyLevelShaders = true)
        {
            var shaderCollectionPath = Path.Combine(Rampancy.Cfg.Halo3MccGameConfig.TagsPath, "levels/shader_collections.txt");
            var shaderCollection     = new ShaderCollection(shaderCollectionPath, onlyLevelShaders);
            return shaderCollection;
        }

        // Read the  shader_collections.txt and get all the .shaders paths in those dirs
        public static Dictionary<string, List<string>> H3_GetLevelShaders()
        {
            var shaderGroupings  = new Dictionary<string, List<string>>();
            var shaderCollection = H3_GetShaderCollection();
            ;

            foreach (var (key, dirPath) in shaderCollection.Mapping) {
                var diFullPath = Path.Combine(Rampancy.Cfg.Halo3MccGameConfig.TagsPath, dirPath);
                if (!Directory.Exists(diFullPath)) continue;

                var shaderTypes = new string[] {"*.shader", "*.shader_terrain"};
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

        public static List<string> H3_GetLevelBitmaps()
        {
            var shaderPaths = new List<string>();

            var shaderCollectionPath = Path.Combine(Rampancy.Cfg.Halo3MccGameConfig.TagsPath, "levels/shader_collections.txt");
            var shaderCollection     = new ShaderCollection(shaderCollectionPath, true);

            foreach (var dirPath in shaderCollection.Mapping.Values) {
                var diFullPath = Path.Combine(Rampancy.Cfg.Halo3MccGameConfig.TagsPath, dirPath, "bitmaps");
                if (!Directory.Exists(diFullPath)) continue;

                var dirShaderPaths = Directory.GetFiles(diFullPath, "*.bitmap", SearchOption.AllDirectories);
                shaderPaths.AddRange(dirShaderPaths);
            }

            return shaderPaths;
        }

        public static void H3_ImportShaders()
        {
            var progressId = Progress.Start("Importing shaders...", "This could take a while :<");
            Progress.ShowDetails(false);
            var currentIdx = 0;

            var shaderPaths         = H3_GetLevelShaders();
            var flattendShaderPaths = shaderPaths.SelectMany(x => x.Value.Select(y => (x.Key, y))).ToArray();
            var shaderDatas         = new Dictionary<string, Halo3.ShaderData>(shaderPaths.Count);
            var tasksCount          = flattendShaderPaths.Count() + 1;
            var matDataList         = new List<(string, string, string)>();

            var task = Task.Factory.StartNew(() =>
            {
                // Import the textures
                Parallel.ForEach(flattendShaderPaths, (collectionAndPath) =>
                {
                    Progress.Report(progressId, currentIdx++, tasksCount);
                    Progress.SetDescription(progressId, collectionAndPath.y);

                    var path       = collectionAndPath.y;
                    var shaderData = Halo3.ShaderData.GetDataFromScan(path);
                    shaderData.Collection = collectionAndPath.Key;

                    var doImport = false;
                    lock (shaderDatas) {
                        if (shaderDatas.TryAdd(path, shaderData))
                            doImport = true;
                        else
                            Debug.LogWarning($"Duplicate shader, not importing copy: {path}");
                    }

                    if (doImport) H3_ImportShaderTextures(shaderData);
                    var shaderGuid = H3_CreateMaterialForShader(shaderData);

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
                };
            });
        }

        // Import the textures referanced by this shader
        public static void H3_ImportShaderTextures(Halo3.ShaderData shaderData)
        {
            // Export the diffuse just for now
            if (shaderData is Halo3.BasicShaderData basicShader) {
                if (basicShader.DiffuseTex != null) H3_ExportBitmapToTga(basicShader.DiffuseTex);
                /*if (basicShader.DetailTex     != null) H3_ExportBitmapToTga(basicShader.DetailTex);
                if (basicShader.BumpTex       != null) H3_ExportBitmapToTga(basicShader.BumpTex);
                if (basicShader.BumpDetailTex != null) H3_ExportBitmapToTga(basicShader.BumpDetailTex);
                if (basicShader.AlphaTestMap  != null) H3_ExportBitmapToTga(basicShader.AlphaTestMap);*/
            }
            else if (shaderData is Halo3.TerrainShaderData terrainShader) {
            }
        }

        // Create a material to repsrent this shader
        public static string H3_CreateMaterialForShader(Halo3.ShaderData shaderData)
        {
            var shaderTagRelPath  = shaderData.TagPath.Replace("/", "\\").Replace(Rampancy.Cfg.Halo3MccGameConfig.TagsPath.Replace("/", "\\"), "");
            var shaderProjectPath = Utils.GetProjectRelPath(shaderTagRelPath.Substring(1), GameVersions.Halo3);
            var shaderDirPath     = Path.GetDirectoryName(shaderProjectPath);
            var shaderPath        = Path.Combine(shaderDirPath, $"{Path.GetFileNameWithoutExtension(shaderProjectPath)}_mat.asset");
            Directory.CreateDirectory(shaderDirPath);

            var shaderTypeStr = Path.GetExtension(shaderData.TagPath).Substring(1);
            var tags          = new List<string>() {"mat", nameof(GameVersions.Halo3), shaderTypeStr, shaderData.Collection}; // some tags to add to the mats to help when searching

            if (shaderData is Halo3.BasicShaderData basicShader) {
                if (basicShader.DiffuseTex != null) {
                    if (basicShader.IsAlphaTested) tags.Add("AlphaTested");

                    var guid       = TagPathHash.H3MccPathHash(basicShader.DiffuseTex);
                    var shaderGuid = TagPathHash.H3MccPathHash(basicShader.TagPath.Replace("/", "\\"));
                    FastMatCreate.CreateBasicMat(guid, shaderPath, shaderGuid, tags.ToArray(), basicShader.IsAlphaTested, basicShader.BaseMapScale);

                    return shaderGuid;
                }
            }
            else if (shaderData is Halo3.TerrainShaderData terrainShader) {
            }

            return null;
        }

        public static void H3_ImportBitmaps()
        {
            var bitmapPaths    = H3_GetLevelBitmaps();
            var diffuseBitmaps = H3_FilterBitmapsDiffuseOnly(bitmapPaths); // Use conventions for the names, for now
            var numBitmaps     = diffuseBitmaps.Count();

            var progressId = Progress.Start("Importing bitmaps...", "This could take a while :<", Progress.Options.Synchronous);
            Progress.ShowDetails(false);
            var currentIdx = 0;

            var task = Task.Factory.StartNew(() =>
            {
                Parallel.ForEach(diffuseBitmaps, new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = 40
                    },
                    (string path, ParallelLoopState loopState, long idx) =>
                    {
                        try {
                            var relPath  = Utils.GetTagRelPath(path, Rampancy.Cfg.Halo3MccGameConfig.TagsPath).Replace(".bitmap", "");
                            var destPath = Utils.GetProjectRelPath(path, GameVersions.Halo3, Environment.CurrentDirectory);
                            var dirPath  = Path.GetDirectoryName(destPath);
                            Directory.CreateDirectory(dirPath);

                            Progress.Report(progressId, currentIdx++, numBitmaps);
                            Progress.SetDescription(progressId, relPath);

                            H3_ExportBitmapToTga(relPath, $"{dirPath}/");
                        }
                        catch (Exception e) {
                        }
                    });

                Progress.Remove(progressId);
            });
        }

        // TODO: Get a way that doesn't create temp files to then be read and deleted
        public static string H3_GetTagAsXml(string path, int tempNameId = 0)
        {
            const string TEMP_TAG_PATH  = "RampancyTempTgExport.xml"; // No standard output for halo 3 :<
            var          toolOutputPath = Path.Combine(Environment.CurrentDirectory, "tagsXml", $"{Path.GetFileName(path)}.xml");
            Rampancy.RunProgram(Rampancy.Cfg.Halo3MccGameConfig.ToolPath, $"export-tag-to-xml \"{path}\" \"{toolOutputPath}\"", true);
            var xmlStr = File.ReadAllText(toolOutputPath);
            File.Delete(toolOutputPath);

            return xmlStr;
        }

        public static (string diffuse, string bump) H3_GetBitmapNamesFromShaderXml(string xml)
        {
            var document = XDocument.Parse(xml);

            var diffuse = "";
            var bump    = "";

            var bitmapBlocks = document.Root.Elements().Where(x => x.Attribute("name").Value.EndsWith("render_method_parameter_block"));
            foreach (var block in bitmapBlocks) {
                var name       = block.Elements().First(x => x.Attribute("name").Value == "parameter name").Attribute("value").Value;
                var bitMapname = block.Elements().First(x => x.Attribute("name").Value == "bitmap").Attribute("value").Value;

                switch (name) {
                    case "base_map":
                        diffuse = bitMapname;
                        break;

                    case "bump_map":
                        bump = bitMapname;
                        break;
                }
            }

            return (diffuse, bump);
        }

        public static List<string> H3_GetDiffuseBitmapsForDir(string dir)
        {
            var bitmaps = new List<string>();

            var bitmapPaths = Directory.GetFiles(dir, "*.bitmap", SearchOption.AllDirectories);
            foreach (var bitmapPath in bitmapPaths) {
                var nonDiffuseEndings = new string[] {"bump", "illum", "bump", "bump"};

                if (!nonDiffuseEndings.Any(x => bitmapPath.EndsWith($"{x}.bitmap"))) bitmaps.Add(bitmapPath);
            }

            return bitmaps;
        }

        public static IEnumerable<string> H3_FilterBitmapsDiffuseOnly(List<string> paths)
        {
            var nonDiffuseEndings = new string[] {"bump", "illum", "albedo", "spec", "cubemaps"};
            var diffuseOnly       = paths.Where(x => !nonDiffuseEndings.Any(y => x.EndsWith($"{y}.bitmap") && !x.Contains("lightmap") && !x.Contains("lp_array"))).ToList();

            return diffuseOnly;
        }

        public static void H3_ExportBitmapToTga(string tagPath, string outPath = null)
        {
            if (outPath == null) {
                var destPath = Utils.GetProjectRelPath(tagPath, GameVersions.Halo3, Environment.CurrentDirectory);
                var dirPath  = Path.GetDirectoryName(destPath);
                Directory.CreateDirectory(dirPath);
                outPath = $"{dirPath}/";
            }

            var fullPath = outPath + $"{Path.GetFileName(tagPath)}_00.tga";
            if (!File.Exists(fullPath)) Rampancy.RunProgram(Rampancy.Cfg.Halo3MccGameConfig.ToolFastPath, $"export-bitmap-tga \"{tagPath}\" \"{outPath}\"", true, true);
        }
    }
}