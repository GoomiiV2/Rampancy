using System;
using System.IO;
using Rampancy.Halo3;
using Rampancy.RampantC20;
using RampantC20;

namespace Rampancy.Halo3_ODST
{
    public class Halo3ODSTImplementation : Halo3Implementation
    {
        public override string GetUnityBasePath() => Path.Combine("Assets", $"{GameVersions.Halo3ODST}");
        public virtual  string GetUnityTempPath() => Path.Combine(Environment.CurrentDirectory, "Temp", $"{GameVersions.Halo3ODST}");

        public override GameVersions GameVersion => GameVersions.Halo3ODST;

        // Urgh, Unity doesn't want to let you import a DDS, because it hates me :D
        // So, I have tool export a bitmap to a dds in a temp folder
        // Queue up a main thread task to load the dds (since texture creation needs the main thread), convert to a Unity texture
        // Save and delete the temp dds
        public override void ExportBitmapToTga(string tagPath, string outPath = null)
        {
            if (outPath == null) {
                var destPath = Utils.GetProjectRelPath(tagPath, GameVersion, Environment.CurrentDirectory);
                var dirPath  = Path.GetDirectoryName(destPath);
                Directory.CreateDirectory(dirPath);
                outPath = $"{dirPath}/";
            }

            Rampancy.ToolTaskRunner.Queue(new ToolTasker.ToolTask(() =>
                {
                    Directory.CreateDirectory(GetUnityTempPath());

                    var tempPath = Path.Combine(GetUnityTempPath(), $"{Path.GetFileName(tagPath)}_00.dds");
                    if (!File.Exists(tempPath))
                        Rampancy.RunProgram(Rampancy.Cfg.GetGameConfig(GameVersion).ToolPath, $"export-bitmap-dds \"{tagPath}\" \"{GetUnityTempPath()}/\"", true, true);

                    return (tempPath, outPath, tagPath);
                },
                (state) =>
                {
                    var (ddsPath, outPath, tagPath) = ((string, string, string)) state;

                    var outPathFull = $"{Path.Combine(outPath.Replace(Environment.CurrentDirectory, ""), Path.GetFileNameWithoutExtension(ddsPath))}.asset".TrimStart('\\');
                    Utils.ImportDDS(ddsPath, outPathFull);

                    File.Delete(ddsPath);
                }));
        }
    }
}