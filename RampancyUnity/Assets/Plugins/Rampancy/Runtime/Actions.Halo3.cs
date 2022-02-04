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
            AssConverter.ImportToScene(ass, name);
        }

        // Read the  shader_collections.txt and get all the .shaders paths in those dirs
        public static List<string> H3_GetLevelShaders()
        {
            var shaderPaths = new List<string>();

            var shaderCollectionPath = Path.Combine(Rampancy.Cfg.Halo3MccGameConfig.TagsPath, "levels/shader_collections.txt");
            var shaderCollection     = new ShaderCollection(shaderCollectionPath, true);

            foreach (var dirPath in shaderCollection.Mapping.Values) {
                var diFullPath = Path.Combine(Rampancy.Cfg.Halo3MccGameConfig.TagsPath, dirPath);
                if (!Directory.Exists(diFullPath)) continue;

                var dirShaderPaths = Directory.GetFiles(diFullPath, "*.shader", SearchOption.AllDirectories);
                shaderPaths.AddRange(dirShaderPaths);
            }

            return shaderPaths;
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
            var bitmapPaths = H3_GetLevelBitmaps();
            var diffuseBitmaps = H3_FilterBitmapsDiffuseOnly(bitmapPaths); // Use conventions for the names, for now
            var numBitmaps = diffuseBitmaps.Count();

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

        public static void H3_ExportBitmapToTga(string tagPath, string outPath)
        {
            Rampancy.RunProgram(Rampancy.Cfg.Halo3MccGameConfig.ToolFastPath, $"export-bitmap-tga \"{tagPath}\" \"{outPath}\"", true, true);
        }
    }
}