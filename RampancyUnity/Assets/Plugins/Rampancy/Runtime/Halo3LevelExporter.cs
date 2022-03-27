using System;
using System.Collections.Generic;
using System.Linq;
using InternalRealtimeCSG;
using Rampancy.Halo3;
using RampantC20.Halo3;
using UnityEditor;
using UnityEngine;

namespace Rampancy
{
    public class Halo3LevelExporter
    {
        public static bool Export(string path)
        {
            var ass            = new Ass();
            var rootGo         = GameObject.Find("Frame");
            var allRcsgModels  = GameObject.FindObjectsOfType<RealtimeCSG.Components.CSGModel>();
            var rcsgMeshLookup = new Dictionary<string, RealtimeCSG.Components.CSGModel>();

            // Get all the meshes and store with a unique name
            foreach (var model in allRcsgModels) {
                if (PrefabUtility.IsPartOfPrefabInstance(model)) {
                    var name = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model);
                    rcsgMeshLookup.TryAdd(name, model);
                }
                else {
                    rcsgMeshLookup.Add(model.gameObject.name, model);
                }
            }

            // Convert and combine
            foreach (var meshData in rcsgMeshLookup) {
                var combined  = GetRcsgCombinedMesh(meshData.Value.generatedMeshes);
                var fixedMesh = LevelExporter.FixTJunctionsV2(combined);
                Debug.Log($"Model: {meshData.Key}, {meshData.Value.name}");
            }

            // Find unique meshes
            // Find instances
            // write unique meshes
            // write instances with references to meshes

            return true;
        }

        private static Int64 GetUniqueMeshHash(GeneratedMeshes meshes)
        {
            unchecked {
                int hash = 17;
                foreach (var mesh in meshes.MeshInstances) {
                    hash = hash * 23 + mesh.MeshDescription.geometryHashValue.GetHashCode();
                    hash = hash * 23 + mesh.MeshDescription.surfaceHashValue.GetHashCode();
                }

                return hash;
            }
        }

        private static Mesh GetRcsgCombinedMesh(GeneratedMeshes meshes)
        {
            var combines = new List<CombineInstance>(meshes.MeshInstances.Length);

            foreach (var mesh in meshes.MeshInstances.Where(x => x.name == "[generated-render-mesh]")) {
                var combineData = new CombineInstance
                {
                    mesh      = mesh.SharedMesh,
                    transform = mesh.transform.localToWorldMatrix
                };

                combines.Add(combineData);
            }

            var combinedMesh = new Mesh();
            combinedMesh.name = meshes.name;

            combinedMesh.CombineMeshes(combines.ToArray(), false);
            combinedMesh.Optimize();

            return combinedMesh;
        }
    }
}