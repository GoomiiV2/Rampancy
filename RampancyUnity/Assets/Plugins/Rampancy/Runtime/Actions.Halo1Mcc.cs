using System;
using System.IO;
using System.Text;
using RampantC20;
using RampantC20.Halo1;
using UnityEditor;
using UnityEngine;

namespace Rampancy
{
    public static partial class Actions
    {
        // Use tool to compile the bsp structure for the current scene
        public static void H1_CompileStructure()
        {
            var rs = RampancySentinel.GetOrCreateInScene();

            var exportJmsPath = $"{Rampancy.Cfg.ActiveGameConfig.DataPath}/{rs.DataDir}/models/{rs.LevelName}.jms";
            ExportLevelJms(exportJmsPath);

            var cmd = $"structure {rs.DataDir} {rs.LevelName}";
            Rampancy.RunToolCommand(cmd);
            
            Debug.Log("Compiled Halo 1 structure");
        }

        public static void H1_CompileToolLightmaps(bool preview, float quality)
        {
            var rs   = RampancySentinel.GetOrCreateInScene();
            var path = rs.DataDir.Replace("/", "\\");
            var cmd  = $@"lightmaps ""{path}\{rs.LevelName}"" {rs.LevelName} {(preview ? 0 : 1)} {quality}";
            Rampancy.RunToolCommand(cmd);
            
            Debug.Log("Compiled Halo 1 lightmap");
        }

        public static void H1_LaunchTagTest(string map)
        {
            const string INIT_FILE_NAME = "rampancyInit.txt";

            try {
                var sb = new StringBuilder();

                var tagTestDir  = Rampancy.Cfg.Halo1MccGameConfig.ToolBasePath;
                var initTxtPath = Path.Combine(tagTestDir, "init.txt");
                if (File.Exists(initTxtPath)) {
                    var initTxt = File.ReadAllText(initTxtPath);
                    sb.AppendLine(initTxt);
                }

                sb.AppendLine("framerate_throttle 1");
                sb.AppendLine($"map_name {map}");

                var rampancyInitPath = Path.Combine(tagTestDir, INIT_FILE_NAME);
                File.WriteAllText(rampancyInitPath, sb.ToString());

                Rampancy.LaunchProgram(Rampancy.Cfg.Halo1MccGameConfig.TagTestPath, $"-windowed -exec {INIT_FILE_NAME}");
                
                Debug.Log("Launched Halo 1 Tag Test");
            }
            catch (Exception e) {
                Debug.LogError($"Error launching tag test for Halo 1: {e}");
            }
        }

        public static void H1_SyncMaterials()
        {
            H1_ImportShaderEnvironments();
            
            Debug.Log("Synced Halo 1 shaders from tags");
        }

        public static Texture2D H1_ImportBitmap(AssetDb.TagInfo tagInfo, bool createBasicMat = false, bool convertToNormal = false)
        {
            var (width, height, pixels) = global::RampantC20.Utils.GetColorPlateFromBitMap(tagInfo).Value;

            if (convertToNormal)
                pixels = Utils.HeightmapToNormal(width, height, pixels, 1.0f);

            var tex = Utils.BitMapToTex2D(width, height, pixels);
            tex.name = tagInfo.Name;

            var path = Utils.GetProjectRelPath(tagInfo, Rampancy.AssetDB);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(tex, $"{path}.asset");

            if (createBasicMat)
                CreateBasicMat(tex, path);

            return tex;
        }

        public static Texture2D H1_ImportBitmapFromTagRef(string path, bool convertToNormal = false)
        {
            var baseTagInfo = Rampancy.AssetDB.FindTagByRelPath(path, "bitmap");
            if (baseTagInfo != null) return H1_ImportBitmap(baseTagInfo.Value, convertToNormal: convertToNormal);
            return null;
        }

        public static void H1_ImportShaderEnvironments()
        {
            var shaderInfos = Rampancy.AssetDB.TagsOfType("shader_environment");
            foreach (var tagInfo in shaderInfos) {
                H1_ImportShaderEnvironment(tagInfo);
            }
        }

        public static void H1_ImportShaderEnvironment(AssetDb.TagInfo tagInfo)
        {
            var       shader = new ShaderEnvironmentTag();
            using var br     = shader.Read(tagInfo);

            var mat = new Material(Shader.Find("Rampancy/Halo1_ShaderEnviroment"));
            mat.name = Path.GetFileNameWithoutExtension(tagInfo.Path);

            // Import the bitmaps
            if (!string.IsNullOrEmpty(shader.BaseMap.Path)) {
                var tex = H1_ImportBitmapFromTagRef(shader.BaseMap.Path);
                mat.SetTexture("_MainTex", tex);
            }
            
            // The extra maps, can add support to the shader proper for these later
            if (!string.IsNullOrEmpty(shader.BumpMap.Path)) {
                var tex = H1_ImportBitmapFromTagRef(shader.BumpMap.Path);
                mat.SetTexture("_BumpMap", tex);
            }
            
            if (!string.IsNullOrEmpty(shader.MicroDetailMap.Path)) {
                var tex = H1_ImportBitmapFromTagRef(shader.MicroDetailMap.Path);
                mat.SetTexture("_MicroDetailMap", tex);
            }
            
            if (!string.IsNullOrEmpty(shader.PrimaryDetailMap.Path)) {
                var tex = H1_ImportBitmapFromTagRef(shader.PrimaryDetailMap.Path);
                mat.SetTexture("_PrimaryDetailMap", tex);
            }
            
            if (!string.IsNullOrEmpty(shader.SecondaryDetailMap.Path)) {
                var tex = H1_ImportBitmapFromTagRef(shader.SecondaryDetailMap.Path);
                mat.SetTexture("_SecondaryDetailMap", tex);
            }

            if ((shader.ShaderFlags & 0x1) == 1) {
                mat.SetFloat("_UseBumpAlpha", -1);
            }

            var path = Utils.GetProjectRelPath(tagInfo, Rampancy.AssetDB);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(mat, $"{path}_mat.asset");
        }
    }
}