using System.Collections.Generic;
using System.IO;
using Plugins.Rampancy.RampantC20;
using RampantC20.Halo1;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Plugins.Rampancy.Runtime
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
            var dir = Path.GetDirectoryName(jmsPath);
            Directory.CreateDirectory(dir);
            LevelExporter.ExportLevel(jmsPath);
        }

        public static void ExportLevelJmsDialog()
        {
            var path = EditorUtility.SaveFilePanel("Save the jms file", "", "export.jms", "jms");
            if (string.IsNullOrEmpty(path)) return;
            ExportLevelJms(path);
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
            if (string.IsNullOrEmpty(path)) return;
            ExportLevelCollisionJms(path);
        }

        public static void ImportBitmaps()
        {
            BitmapConverter.ImportBitmaps();
        }

        public static void LaunchTagTest(string map)
        {
            switch (Rampancy.Config.GameVersion) {
                case GameVersions.Halo1Mcc:
                    H1_LaunchTagTest(map);
                    break;
            }
        }

        // Quick and dirty fix up for guid mismatches
        public static void UpdateSceneMatRefs()
        {
            var scene = EditorSceneManager.GetActiveScene();

            if (!File.Exists(scene.path)) return;

            var sceneFile = File.ReadAllText(scene.path);
            var sentinel  = GameObject.FindObjectOfType<RampancySentinel>();

            if (sentinel == null) return;
            var matIdLookup = sentinel.GetMatIdToPathLookup();
            var newGuids    = new Dictionary<string, string>();

            foreach (var matIdItem in matIdLookup) {
                var newGuid = AssetDatabase.AssetPathToGUID(matIdItem.Value);
                if (newGuid != matIdItem.Key && !newGuids.ContainsKey(newGuid)) {
                    newGuids.Add(newGuid, matIdItem.Key);
                }
            }

            foreach (var newGuidKvp in newGuids) {
                sceneFile = sceneFile.Replace(newGuidKvp.Value, newGuidKvp.Key);
            }

            if (newGuids.Count == 0) return;
            Debug.Log("Material Ids didn't match, remapping from paths");

            //Back up
            var backupPath = $"{scene.path}.backup";
            File.Copy(scene.path, backupPath, true);

            File.WriteAllText(scene.path, sceneFile);
            EditorSceneManager.OpenScene(scene.path);

            File.Delete(backupPath);

            Debug.Log("Material IDs reassigned from paths");
        }

        public static void CreateBasicMat(Texture2D tex, string path)
        {
            var mat = new Material(Shader.Find("Legacy Shaders/Diffuse"));
            mat.mainTexture = tex;
            mat.name        = Path.GetFileNameWithoutExtension(path);

            AssetDatabase.CreateAsset(mat, $"{path}_mat.asset");
        }
    }
}