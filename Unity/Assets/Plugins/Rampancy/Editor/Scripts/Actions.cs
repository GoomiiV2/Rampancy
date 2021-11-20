using System.IO;
using Plugins.Rampancy.RampantC20;
using Plugins.Rampancy.Runtime;
using UnityEditor;
using UnityEngine;

namespace Plugins.Rampancy.Editor.Scripts
{
    // Actions to be called from menus or shortcuts or scripts
    public static partial class Actions
    {
        // Import a jms file and add it to the scene from the path
        public static void ImportJms(string jmsPath)
        {
            if (!string.IsNullOrEmpty(jmsPath)) {
                var jmsModel   = JMS.Load(jmsPath);
                var name       = Path.GetFileNameWithoutExtension(jmsPath);
                var testGo     = new GameObject(name);
                var meshFiler  = testGo.AddComponent<MeshFilter>();
                var meshRender = testGo.AddComponent<MeshRenderer>();

                meshFiler.mesh = JmsConverter.JmsToMesh(jmsModel);
                JmsConverter.AddMatsToRender(meshRender, jmsModel);
            }
        }

        // Import a jms file and add it to the scene from a file picker
        public static void ImportJmsDialog()
        {
            var filePath = EditorUtility.OpenFilePanel("Import Jms file", "", "jms");
            if (!string.IsNullOrEmpty(filePath)) {
                ImportJms(filePath);
            }
        }

        public static void ExportLevelJms(string jmsPath)
        { 
            var dir           = Path.GetDirectoryName(jmsPath);
            Directory.CreateDirectory(dir);
            //JMSConverter.ExportLevel(jmsPath);
            
            LevelExporter.ExportLevel(jmsPath);
        }

        public static void ExportLevelJmsDialog()
        {
            var path = EditorUtility.SaveFilePanel("Save the jms file", "", "export.jms", "jms");
            if (!string.IsNullOrEmpty(path)) {
                ExportLevelJms(path);
            }
        }
        
        public static void ExportLevelCollisionJms(string jmsPath)
        { 
            var dir = Path.GetDirectoryName(jmsPath);
            Directory.CreateDirectory(dir);
            LevelExporter.ExportLevelCollision(jmsPath);
        }

        public static void ExportLevelCollisionJmsDialog()
        {
            var path = EditorUtility.SaveFilePanel("Save the jms file", "", "export_collision.jms", "jms");
            if (!string.IsNullOrEmpty(path)) {
                ExportLevelCollisionJms(path);
            }
        }

        public static void ImportBitmaps()
        {
            BitmapConverter.ImportBitmaps();
        }

        public static void LaunchTagTest(string map)
        {
            switch (Runtime.Rampancy.Config.GameVersion) {
                case GameVersions.Halo1Mcc:
                    H1_LaunchTagTest(map);
                    break;
            }
        }
    }
}