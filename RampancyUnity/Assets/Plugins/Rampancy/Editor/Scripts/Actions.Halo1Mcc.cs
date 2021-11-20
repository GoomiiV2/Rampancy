using System;
using System.IO;
using System.Text;
using Plugins.Rampancy.Runtime;
using UnityEngine;

namespace Plugins.Rampancy.Editor.Scripts
{
    public static partial class Actions
    {
        // Use tool to compile the bsp structure for the current scene
        public static void H1_CompileStructure()
        {
            var rs = RampancySentinel.GetOrCreateInScene();

            var exportJmsPath = $"{Runtime.Rampancy.Config.ActiveGameConfig.DataPath}/{rs.DataDir}/models/{rs.LevelName}.jms";
            ExportLevelJms(exportJmsPath);

            var cmd = $"structure {rs.DataDir} {rs.LevelName}";
            Runtime.Rampancy.RunToolCommand(cmd);
        }

        public static void H1_CompileToolLightmaps(bool preview, float quality)
        {
            var rs   = RampancySentinel.GetOrCreateInScene();
            var path = rs.DataDir.Replace("/", "\\");
            var cmd  = $@"lightmaps ""{path}\{rs.LevelName}"" {rs.LevelName} {(preview ? 0 : 1)} {quality}";
            Runtime.Rampancy.RunToolCommand(cmd);
        }
        
        public static void H1_LaunchTagTest(string map)
        {
            const string INIT_FILE_NAME = "rampancyInit.txt"; 
            
            try {
                var sb = new StringBuilder();

            var tagTestDir  = Runtime.Rampancy.Config.Halo1MccGameConfig.ToolBasePath;
            var initTxtPath = Path.Combine(tagTestDir, "init.txt");
            if (File.Exists(initTxtPath)) {
                var initTxt = File.ReadAllText(initTxtPath);
                sb.AppendLine(initTxt);
            }

            sb.AppendLine("framerate_throttle 1");
            sb.AppendLine($"map_name {map}");

            var rampancyInitPath = Path.Combine(tagTestDir, INIT_FILE_NAME);
            File.WriteAllText(rampancyInitPath, sb.ToString());
            
            Runtime.Rampancy.LaunchProgram(Runtime.Rampancy.Config.Halo1MccGameConfig.TagTestPath, $"-windowed -exec {INIT_FILE_NAME}");
            }
            catch (Exception e) {
                Debug.LogError($"Error launching tag test for Halo 1: {e}");
            }
        }
    }
}