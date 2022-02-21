using RampantC20;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Rampancy.AssetProcessors
{
    public class TextureGuidSetter : AssetPostprocessor
    {
        private void OnPostprocessTexture(Texture2D texture)
        {
        }

        public static string TrimPath(string path)
        {
            path = path.Replace($"Assets/{nameof(GameVersions.Halo1Mcc)}/TagData/", "")
                       .Replace($"Assets/{nameof(GameVersions.Halo3)}/TagData/", "");

            return path;
        }

        // Replace guids with path based ones
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var imported in importedAssets)
                if (imported.Contains($"{nameof(GameVersions.Halo3)}/TagData") && imported.EndsWith(".tga"))
                    try {
                        var metaPath = $"{imported}.meta";
                        var trimed   = TrimPath(imported);
                        var metaTxt  = File.ReadAllText(metaPath);

                        var texPath = trimed.Replace("/", "\\").Replace("_00.tga", ""); // To match the tag path
                        var guid    = TagPathHash.H3MccPathHash(texPath);
                        metaTxt = Regex.Replace(metaTxt, @"guid: ([\d|\w]+)", $"guid: {guid}");
                        File.WriteAllText(metaPath, metaTxt);
                    }
                    catch (Exception) {
                    }
        }
    }
}