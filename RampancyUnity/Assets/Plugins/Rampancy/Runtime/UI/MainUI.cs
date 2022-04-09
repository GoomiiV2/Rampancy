using System;
using System.IO;
using RampantC20;
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

        [MenuItem("Rampancy/Launch/Sapien", true)]
        public static bool CanLaunchSapien()
        {
            return Rampancy.CurrentGameImplementation.CanOpenSapien();
        }

        [MenuItem("Rampancy/Launch/Sapien", false, 1)]
        public static void LaunchSapien()
        {
            Rampancy.CurrentGameImplementation.OpenSapien();
        }


        [MenuItem("Rampancy/Launch/Guerilla", true)]
        public static bool CanLaunchGuerilla()
        {
            return Rampancy.CurrentGameImplementation.CanOpenGuerilla();
        }

        [MenuItem("Rampancy/Launch/Guerilla", false, 2)]
        public static void LaunchGuerilla()
        {
            Rampancy.CurrentGameImplementation.OpenGuerilla();
        }

        [MenuItem("Rampancy/Launch/TagTest", true)]
        public static bool CanLaunchTagTest()
        {
            return Rampancy.CurrentGameImplementation.CanOpenTagTest();
        }

        [MenuItem("Rampancy/Launch/TagTest", false, 3)]
        public static void LaunchTagTest()
        {
            Rampancy.CurrentGameImplementation.OpenTagTest();
        }

        [MenuItem("Rampancy/Launch/Tool CMD", false, 4)]
        public static void LaunchToolCmd()
        {
            Rampancy.LaunchCMD("");
        }

        [MenuItem("Rampancy/Launch/Open level in Tag Test _F5", true)]
        public static bool CanOpenInTagTest()
        {
            return Rampancy.CurrentGameImplementation.CanOpenTagTest();
        }

        [MenuItem("Rampancy/Launch/Open level in Tag Test _F5", false, 5)]
        public static void OpenInTagTest()
        {
            Rampancy.CurrentGameImplementation.OpenInTagTest();
        }

    #endregion

    #region Compile

        // Export the jms and compile to a bsp
        [MenuItem("Rampancy/Compile/Structure _F4", true)]
        public static bool CanCompileStructure()
        {
            return Rampancy.CurrentGameImplementation.CanCompileStructure();
        }

        [MenuItem("Rampancy/Compile/Structure _F4", false, 2)]
        public static void CompileStructure()
        {
            Rampancy.CurrentGameImplementation.CompileStructure();
        }

        [MenuItem("Rampancy/Compile/Preview lightmaps", true)]
        public static bool CanCompilePreviewLightmaps()
        {
            return Rampancy.CurrentGameImplementation.CanCompileLightmaps();
        }

        [MenuItem("Rampancy/Compile/Preview lightmaps", false, 2)]
        public static void CompilePreviewLightmaps()
        {
            Rampancy.CurrentGameImplementation.CompileLightmaps();
        }

        [MenuItem("Rampancy/Compile/Structure and Preview lightmaps _F6", true)]
        public static bool CanCompileStructureAndPreviewLightmaps()
        {
            return Rampancy.CurrentGameImplementation.CanCompileStructure() && Rampancy.CurrentGameImplementation.CanCompileLightmaps();
        }

        [MenuItem("Rampancy/Compile/Structure and Preview lightmaps _F6", false, 2)]
        public static void CompileStructureAndPreviewLightmaps()
        {
            ToolOutput.Clear();
            CompileStructure();
            CompilePreviewLightmaps();
        }

    #endregion

    #region Import / Export

        [MenuItem("Rampancy/Import-Export/Import Scene", true)]
        public static bool CanImportScene()
        {
            return Rampancy.CurrentGameImplementation.CanImportScene();
        }

        [MenuItem("Rampancy/Import-Export/Import Scene", false, 3)]
        public static void ImportScene()
        {
            Rampancy.CurrentGameImplementation.ImportScene();
        }

        [MenuItem("Rampancy/Import-Export/Export Scene", true)]
        public static bool CanExportScene()
        {
            return Rampancy.CurrentGameImplementation.CanExportScene();
        }

        [MenuItem("Rampancy/Import-Export/Export Scene", false, 3)]
        public static void ExportScene()
        {
            Rampancy.CurrentGameImplementation.ExportScene();
        }

    #endregion

    #region Help

        [MenuItem("Rampancy/Help/Rampancy Docs", false, 4)]
        public static void HelpRampancyDocs()
        {
            Application.OpenURL("https://github.com/GoomiiV2/Rampancy/wiki");
        }

        [MenuItem("Rampancy/Help/Realtime CSG Docs", false, 4)]
        public static void HelpRealtimeCsgDocs()
        {
            Application.OpenURL("https://realtimecsg.com");
        }

    #endregion
    }
}