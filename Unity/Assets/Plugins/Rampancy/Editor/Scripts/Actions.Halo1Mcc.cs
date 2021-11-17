using Plugins.Rampancy.Runtime;

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
    }
}