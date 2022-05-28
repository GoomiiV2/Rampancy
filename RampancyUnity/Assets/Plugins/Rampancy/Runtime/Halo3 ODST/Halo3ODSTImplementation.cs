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
    }
}