using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Plugins.Rampancy.Runtime;
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
            if (!string.IsNullOrEmpty(filePath)) {
                H3_ImportAss(filePath);
            }
        }

        public static void H3_ImportAss(string path)
        {
            Debug.Log("Importing Ass :D");

            var ass  = Ass.Load(path);
            var name = Path.GetFileNameWithoutExtension(path);

            var converter = new AssConverter();
            converter.ImportToScene(ass, name, path);
        }

        public static ShaderCollection H3_GetShaderCollection(bool onlyLevelShaders = true)
        {
            var shaderCollectionPath = Path.Combine(Rampancy.Cfg.Halo3MccGameConfig.TagsPath, "levels/shader_collections.txt");
            var shaderCollection = new ShaderCollection(shaderCollectionPath, onlyLevelShaders);
            return shaderCollection;
        }

        // Read the  shader_collections.txt and get all the .shaders paths in those dirs
        public static List<string> H3_GetLevelShaders()
        {
            var shaderPaths      = new List<string>();
            var shaderCollection = H3_GetShaderCollection(); ;

            foreach (var dirPath in shaderCollection.Mapping.Values) {
                var diFullPath = Path.Combine(Rampancy.Cfg.Halo3MccGameConfig.TagsPath, dirPath);
                if (!Directory.Exists(diFullPath)) continue;

                var shaderTypes = new string[] { "*.shader", "*.shader_terrain" };
                foreach (var shaderType in shaderTypes)
                {
                    var sPaths = Directory.GetFiles(diFullPath, shaderType, SearchOption.AllDirectories);
                    shaderPaths.AddRange(sPaths);
                }
            }

            var distinct = shaderPaths.Distinct();
            return distinct.ToList();
        }

        public static List<string> H3_GetLevelBitmaps()
        {
            var shaderPaths = new List<string>();

            var shaderCollectionPath = Path.Combine(Rampancy.Cfg.Halo3MccGameConfig.TagsPath, "levels/shader_collections.txt");
            var shaderCollection = new ShaderCollection(shaderCollectionPath, true);

            foreach (var dirPath in shaderCollection.Mapping.Values)
            {
                var diFullPath = Path.Combine(Rampancy.Cfg.Halo3MccGameConfig.TagsPath, dirPath, "bitmaps");
                if (!Directory.Exists(diFullPath)) continue;

                var dirShaderPaths = Directory.GetFiles(diFullPath, "*.bitmap", SearchOption.AllDirectories);
                shaderPaths.AddRange(dirShaderPaths);
            }

            return shaderPaths;
        }

        public static void H3_ImportShaders()
        {
            int progressId = Progress.Start("Importing shaders...", "This could take a while :<");
            Progress.ShowDetails(false);
            int currentIdx = 0;

            var shaderPaths = H3_GetLevelShaders();
            var shaderDatas = new Dictionary<string, Halo3.ShaderData>(shaderPaths.Count);
            var tasksCount  = (shaderPaths.Count * 2) + 1;
            var task = Task.Factory.StartNew(() =>
            {
                Parallel.ForEach(shaderPaths, (string path) =>
                {
                    Progress.Report(progressId, currentIdx++, tasksCount);
                    Progress.SetDescription(progressId, path);

                    var bytes = File.ReadAllBytes(path);
                    var shaderData = Halo3.ShaderData.GetBasicShaderDataFromScan(bytes);

                    lock (shaderDatas)
                    {
                        if (shaderDatas.TryAdd(path, shaderData))
                        {
                            Debug.LogWarning($"Duplicate sahder, not importing copy: {path}");
                        }
                    }

                    // Export the diffuse just for now
                    if (shaderData.DiffuseTex != null) H3_ExportBitmapToTga(shaderData.DiffuseTex);

                    //Debug.Log($"{path}\n{shaderData.ToString()}");
                });

                EditorApplication.delayCall += () =>
                {
                    // Unity importing of the textures
                    Progress.Report(progressId, currentIdx++, tasksCount);
                    Progress.SetDescription(progressId, "Waiting for Unity to import the textures");
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                    for (int i = 0; i < shaderPaths.Count; i++)
                    {
                        string path    = shaderPaths[i];
                        if (shaderDatas.TryGetValue(path, out var shaderData))
                        {
                            Progress.Report(progressId, currentIdx++, tasksCount);
                            Progress.SetDescription(progressId, path);

                            if (shaderData.DiffuseTex != null)
                            {
                                var diffuseTexPath    = $"{Utils.GetProjectRelPath(shaderData.DiffuseTex, GameVersions.Halo3)}_00.tga";
                                var diffuseTex        = AssetDatabase.LoadAssetAtPath<Texture2D>(diffuseTexPath);
                                var shaderTagRelPath  = path.Replace("/", "\\").Replace(Rampancy.Cfg.Halo3MccGameConfig.TagsPath.Replace("/", "\\"), "");
                                var shaderProjectPath = Utils.GetProjectRelPath(shaderTagRelPath.Substring(1), GameVersions.Halo3);
                                var shaderDirPath     = Path.GetDirectoryName(shaderProjectPath);
                                var shaderPath        = Path.Combine(shaderDirPath, Path.GetFileNameWithoutExtension(shaderProjectPath));
                                Directory.CreateDirectory(shaderDirPath);

                                var mat = CreateBasicMat(diffuseTex, shaderPath);
                                AssetDatabase.SetLabels(mat, new string[] { "mat", $"{GameVersions.Halo3}", "shader" });
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Couldn't create mat for shader: {path}");
                        }
                    }
                };

                Progress.Remove(progressId);
            });
        }

        public static void H3_ImportBitmaps()
        {
            var bitmapPaths    = H3_GetLevelBitmaps();
            var diffuseBitmaps = H3_FilterBitmapsDiffuseOnly(bitmapPaths); // Use conventions for the names, for now
            var numBitmaps     = diffuseBitmaps.Count();

            int progressId = Progress.Start("Importing bitmaps...", "This could take a while :<", options: Progress.Options.Synchronous);
            Progress.ShowDetails(false);
            int currentIdx = 0;

            var task = Task.Factory.StartNew(() =>
            {
                Parallel.ForEach(diffuseBitmaps, parallelOptions: new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 40
                },
                (string path, ParallelLoopState loopState, long idx) =>
                {
                    try
                    {
                        var relPath = Utils.GetTagRelPath(path, Rampancy.Cfg.Halo3MccGameConfig.TagsPath).Replace(".bitmap", "");
                        var destPath = Utils.GetProjectRelPath(path, GameVersions.Halo3, Environment.CurrentDirectory);
                        var dirPath = Path.GetDirectoryName(destPath);
                        Directory.CreateDirectory(dirPath);

                        Progress.Report(progressId, currentIdx++, numBitmaps);
                        Progress.SetDescription(progressId, relPath);

                        H3_ExportBitmapToTga(relPath, $"{dirPath}/");
                    }
                    catch (Exception e)
                    {

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
            foreach (var bitmapPath in bitmapPaths)
            {
                var nonDiffuseEndings = new string[] { "bump", "illum", "bump", "bump" };

                if (!nonDiffuseEndings.Any(x => bitmapPath.EndsWith($"{x}.bitmap"))) {
                    bitmaps.Add(bitmapPath);
                }
            }

            return bitmaps;
        }

        public static IEnumerable<string> H3_FilterBitmapsDiffuseOnly(List<string> paths)
        {
            var nonDiffuseEndings = new string[] { "bump", "illum", "albedo", "spec", "cubemaps" };
            var diffuseOnly = paths.Where(x => !nonDiffuseEndings.Any(y => x.EndsWith($"{y}.bitmap") && !x.Contains("lightmap") && !x.Contains("lp_array"))).ToList();

            return diffuseOnly;
        }

        public static void H3_ExportBitmapToTga(string tagPath, string outPath = null)
        {
            if (outPath == null)
            {
                var destPath = Utils.GetProjectRelPath(tagPath, GameVersions.Halo3, Environment.CurrentDirectory);
                var dirPath = Path.GetDirectoryName(destPath);
                Directory.CreateDirectory(dirPath);
                outPath = $"{dirPath}/";
            }

            Rampancy.RunProgram(Rampancy.Cfg.Halo3MccGameConfig.ToolFastPath, $"export-bitmap-tga \"{tagPath}\" \"{outPath}\"", true, true);
        }
    }
}