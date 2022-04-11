using System;
using System.IO;
using System.Text;
using RampantC20;
using RampantC20.Halo1;
using RealtimeCSG.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rampancy.Halo1
{
    public class Halo1Implementation : GameImplementationBase
    {
        public override bool CanOpenTagTest()
        {
            return true;
        }

        public override void OpenTagTest()
        {
            var rs = RampancySentinel.GetOrCreateInScene();
            OpenInTagTest(rs.LevelName);
        }

        public override void OpenInTagTest(string mapPath = null)
        {
            const string INIT_FILE_NAME = "rampancyInit.txt";

            if (mapPath == null) {
                var rs = RampancySentinel.GetOrCreateInScene();
                mapPath = $@"{rs.DataDir}\{rs.LevelName}".Replace("/", @"\");;
            }

            try {
                var sb = new StringBuilder();

                var tagTestDir  = Rampancy.Cfg.Halo1MccGameConfig.ToolBasePath;
                var initTxtPath = Path.Combine(tagTestDir, "init.txt");
                if (File.Exists(initTxtPath)) {
                    var initTxt = File.ReadAllText(initTxtPath);
                    sb.AppendLine(initTxt);
                }

                sb.AppendLine("framerate_throttle 1");
                sb.AppendLine($"map_name {mapPath}");

                var rampancyInitPath = Path.Combine(tagTestDir, INIT_FILE_NAME);
                File.WriteAllText(rampancyInitPath, sb.ToString());

                Rampancy.LaunchProgram(Rampancy.Cfg.Halo1MccGameConfig.TagTestPath, $"-windowed -exec {INIT_FILE_NAME}");

                Debug.Log("Launched Halo 1 Tag Test");
            }
            catch (Exception e) {
                Debug.LogError($"Error launching tag test for Halo 1: {e}");
            }
        }

        // Use tool to compile the bsp structure for the current scene
        public override void CompileStructure()
        {
            var rs            = RampancySentinel.GetOrCreateInScene();
            var exportJmsPath = $"{Rampancy.Cfg.ActiveGameConfig.DataPath}/{rs.DataDir}/models/{rs.LevelName}.jms";
            ExportScene(exportJmsPath);

            var cmd = $"structure {rs.DataDir} {rs.LevelName}";
            Rampancy.RunToolCommand(cmd);

            Debug.Log("Compiled Halo 1 structure");
        }

        public override void CompileLightmaps(bool preview = true, float quality = 1)
        {
            var rs   = RampancySentinel.GetOrCreateInScene();
            var path = rs.DataDir.Replace("/", "\\");
            var cmd  = $@"lightmaps ""{path}\{rs.LevelName}"" {rs.LevelName} {(preview ? 0 : 1)} {quality}";
            Rampancy.RunToolCommand(cmd);

            Debug.Log("Compiled Halo 1 lightmap");
        }

        public override void CompileStructureAndLightmaps()
        {
            CompileStructure();
            CompileLightmaps();
        }

        public override void CreateNewScene(string name, bool isSinglePlayer = true, Action customAction = null)
        {
            base.CreateNewScene(name, isSinglePlayer, () =>
            {
                var rs = RampancySentinel.GetOrCreateInScene();
                rs.LevelName   = name;
                rs.DataDir     = isSinglePlayer ? $"levels/{name}" : $"levels/test/{name}";
                rs.GameVersion = GameVersions.Halo1Mcc;
            });
        }

        public override void ImportScene(string path = null)
        {
            path ??= Utils.OpenFileDialog("Export jms file", "jms");

            if (path != null) {
                var jmsModel   = JMS.Load(path);
                var name       = Path.GetFileNameWithoutExtension(path);
                var testGo     = new GameObject(name);
                var meshFiler  = testGo.AddComponent<MeshFilter>();
                var meshRender = testGo.AddComponent<MeshRenderer>();

                meshFiler.mesh = JmsConverter.JmsToMesh(jmsModel);
                JmsConverter.AddMatsToRender(meshRender, jmsModel);

                Debug.Log("Imported JMS");
            }
        }

        public override void ExportScene(string path = null)
        {
            path ??= Utils.OpenFileDialog("Export jms file", "jms");

            var dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(dir);
            LevelExporter.ExportLevel(path);
        }

        public override void SyncMaterials()
        {
            ImportShaderEnvironments();
            Debug.Log("Synced Halo 1 shaders from tags");
        }

    #region Helpers

        public void ImportShaderEnvironments()
        {
            var shaderInfos = Rampancy.AssetDB.TagsOfType("shader_environment");
            foreach (var tagInfo in shaderInfos) ImportShaderEnvironment(tagInfo);
        }

        public void ImportShaderEnvironment(AssetDb.TagInfo tagInfo)
        {
            var       shader = new ShaderEnvironmentTag();
            using var br     = shader.Read(tagInfo);

            var mat = new Material(Shader.Find("Rampancy/Halo1_ShaderEnviroment"));
            mat.name = Path.GetFileNameWithoutExtension(tagInfo.Path);

            // Import the bitmaps
            if (!string.IsNullOrEmpty(shader.BaseMap.Path)) {
                var tex = ImportBitmapFromTagRef(shader.BaseMap.Path);
                mat.SetTexture("_MainTex", tex);
            }

            // The extra maps, can add support to the shader proper for these later
            if (!string.IsNullOrEmpty(shader.BumpMap.Path)) {
                var tex = ImportBitmapFromTagRef(shader.BumpMap.Path);
                mat.SetTexture("_BumpMap", tex);
            }

            if (!string.IsNullOrEmpty(shader.MicroDetailMap.Path)) {
                var tex = ImportBitmapFromTagRef(shader.MicroDetailMap.Path);
                mat.SetTexture("_MicroDetailMap", tex);
            }

            if (!string.IsNullOrEmpty(shader.PrimaryDetailMap.Path)) {
                var tex = ImportBitmapFromTagRef(shader.PrimaryDetailMap.Path);
                mat.SetTexture("_PrimaryDetailMap", tex);
            }

            if (!string.IsNullOrEmpty(shader.SecondaryDetailMap.Path)) {
                var tex = ImportBitmapFromTagRef(shader.SecondaryDetailMap.Path);
                mat.SetTexture("_SecondaryDetailMap", tex);
            }

            if ((shader.ShaderFlags & 0x1) == 1) mat.SetFloat("_UseBumpAlpha", -1);

            var path = Utils.GetProjectRelPath(tagInfo, Rampancy.AssetDB);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(mat, $"{path}_mat.asset");
        }

        public Texture2D ImportBitmap(AssetDb.TagInfo tagInfo, bool createBasicMat = false, bool convertToNormal = false)
        {
            var (width, height, pixels) = Utils.GetColorPlateFromBitMap(tagInfo).Value;

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

        public Texture2D ImportBitmapFromTagRef(string path, bool convertToNormal = false)
        {
            var baseTagInfo = Rampancy.AssetDB.FindTagByRelPath(path, "bitmap");
            if (baseTagInfo != null) return ImportBitmap(baseTagInfo.Value, convertToNormal: convertToNormal);
            return null;
        }

        public Material CreateBasicMat(Texture2D tex, string path)
        {
            var mat = new Material(Shader.Find("Legacy Shaders/Diffuse"));
            mat.mainTexture = tex;
            mat.name        = Path.GetFileNameWithoutExtension(path);

            AssetDatabase.CreateAsset(mat, $"{path}_mat.asset");

            return mat;
        }

        public Material CreateBasicTransparentMat(Texture2D tex, string path)
        {
            var mat = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
            mat.mainTexture = tex;
            mat.name        = Path.GetFileNameWithoutExtension(path);

            AssetDatabase.CreateAsset(mat, $"{path}_mat.asset");

            return mat;
        }

    #endregion
    }
}