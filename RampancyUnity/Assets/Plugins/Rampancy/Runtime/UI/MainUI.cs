using System.IO;
using UnityEditor;
using UnityEngine;

namespace Rampancy.UI
{
    public class LevelUI : EditorWindow
    {
        [MenuItem("Rampancy/Create New Level", false, 0)]
        public static void CreateNewLevel()
        {
            var window = CreateInstance<CreateLevelPopup>();
            window.position = new Rect(Screen.width, Screen.height, 300, 100);
            window.ShowPopup();
        }

    #region Launch

        [MenuItem("Rampancy/Launch/Sapien", false, 1)]
        public static void LaunchSapien() => RunExeIfExists(Rampancy.Cfg.ActiveGameConfig.SapienPath);

        [MenuItem("Rampancy/Launch/Guerilla", false, 2)]
        public static void LaunchGuerilla() => RunExeIfExists(Rampancy.Cfg.ActiveGameConfig.GuerillaPath);

        [MenuItem("Rampancy/Launch/TagTest", false, 3)]
        public static void LaunchTagTest() => RunExeIfExists(Rampancy.Cfg.ActiveGameConfig.TagTestPath);

        [MenuItem("Rampancy/Launch/Tool CMD", false, 4)]
        public static void LaunchToolCmd() => Rampancy.LaunchCMD("");

        [MenuItem("Rampancy/Launch/Open level in Tag Test _F5", false, 5)]
        public static void OpenInTagTest()
        {
            var rs   = RampancySentinel.GetOrCreateInScene();
            var path = $@"{rs.DataDir}\{rs.LevelName}".Replace("/", @"\");
            Actions.H1_LaunchTagTest(path);
        }

        public static void RunExeIfExists(string exePath)
        {
            if (File.Exists(exePath)) {
                Rampancy.LaunchProgram(exePath, "");
            }
        }

    #endregion

    #region Compile

        // Export the jms and compile to a bsp
        [MenuItem("Rampancy/Compile/Structure", false, 2)]
        public static void CompileStructure()
        {
            Actions.H1_CompileStructure();
        }

        [MenuItem("Rampancy/Compile/Preview lightmaps", false, 2)]
        public static void CompilePreviewLightmaps() => Actions.H1_CompileToolLightmaps(true, 0.1f);

        [MenuItem("Rampancy/Compile/Structure and Preview lightmaps _F6", false, 2)]
        public static void CompileStructureAndPreviewLightmaps()
        {
            ToolOutput.Clear();
            Actions.H1_CompileStructure();
            CompilePreviewLightmaps();
        }

    #endregion

    #region Import / Export

        [MenuItem("Rampancy/Import-Export/Import Jms", false, 3)]
        public static void ImportJms() => Actions.ImportJmsDialog();
        
        [MenuItem("Rampancy/Import-Export/Import Ass", false, 3)]
        public static void ImportAss() => Actions.H3_ImportAssDialog();
        
        [MenuItem("Rampancy/Import-Export/Export Ass", false, 3)]
        public static void ExportAss() => Actions.H3_ExportAss();

        [MenuItem("Rampancy/Import-Export/Export Jms", false, 3)]
        public static void ExportJms() => Actions.ExportLevelJmsDialog();

        [MenuItem("Rampancy/Import-Export/Export Jms Collision", false, 3)]
        public static void ExportJmsCollision() => Actions.ExportLevelCollisionJmsDialog();

    #endregion

    #region Help
        [MenuItem("Rampancy/Help/Rampancy Docs", false, 4)]
        public static void HelpRampancyDocs() => Application.OpenURL("https://github.com/GoomiiV2/Rampancy/wiki");
        
        [MenuItem("Rampancy/Help/Realtime CSG Docs", false, 4)]
        public static void HelpRealtimeCsgDocs() => Application.OpenURL("https://realtimecsg.com");
    #endregion
    }
}