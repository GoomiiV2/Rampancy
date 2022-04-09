using System.Collections.Generic;
using System.IO;
using RampantC20;
using RampantC20.Halo1;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rampancy
{
    // Actions to be called from menus or shortcuts or scripts
    public static partial class Actions
    {
        // Quick and dirty fix up for guid mismatches
        public static void UpdateSceneMatRefs()
        {
            var scene = SceneManager.GetActiveScene();

            if (!File.Exists(scene.path)) return;

            var sceneFile = File.ReadAllText(scene.path);
            var sentinel  = Object.FindObjectOfType<RampancySentinel>();

            if (sentinel == null) return;
            var matIdLookup = sentinel.GetMatIdToPathLookup();
            var newGuids    = new Dictionary<string, string>();

            foreach (var matIdItem in matIdLookup) {
                var newGuid = AssetDatabase.AssetPathToGUID(matIdItem.Value);
                if (newGuid != matIdItem.Key && !newGuids.ContainsKey(newGuid)) newGuids.Add(newGuid, matIdItem.Key);
            }

            foreach (var newGuidKvp in newGuids) sceneFile = sceneFile.Replace(newGuidKvp.Value, newGuidKvp.Key);

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
    }
}