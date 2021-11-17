using System.Collections.Generic;
using InternalRealtimeCSG;
using UnityEditor;
using UnityEngine;

namespace Plugins.Rampancy.Editor.Scripts
{
    public static class LevelExporter
    {
        public static void ExportLevel(string path)
        {
            var meshData = GetRcsgMesh();
            var jms      = JmsConverter.MeshToJms(meshData.mehs, meshData.matNames);
            jms.Save(path);
        }
        
        public static void ExportLevelCollision(string path)
        {
            var cMesh = GetRcsgCollisionMesh();
            var jms      = JmsConverter.MeshToJms(cMesh, new[] { "none" });
            jms.Save(path);
        }
        
        public static (Mesh mehs, string[] matNames) GetRcsgMesh()
        {
            var baseMeshs = GameObject.Find("Frame/LevelGeo/[generated-meshes]");

            var mesh     = new Mesh();
            var combines = new List<CombineInstance>(baseMeshs.transform.childCount - 1);
            var matNames = new List<string>();
            for (int i = 0; i < baseMeshs.transform.childCount; i++) {
                var childMesh = baseMeshs.transform.GetChild(i);
                if (childMesh.name != "[generated-collider-mesh]") {
                    var mf          = childMesh.GetComponent<MeshFilter>();
                    var mr          = childMesh.GetComponent<MeshRenderer>();
                    var trimmedName = mr.material.name.Replace(" (Instance)", "").Replace("_mat", "");
                    if (trimmedName != "Skip") {
                        var combine = new CombineInstance
                        {
                            mesh      = mf.mesh,
                            transform = childMesh.transform.localToWorldMatrix
                        };

                        matNames.Add(trimmedName);
                        combines.Add(combine);
                    }
                }
            }

            mesh.CombineMeshes(combines.ToArray(), false);
            mesh.Optimize();
            
            return (mesh, matNames.ToArray());
        }

        public static Mesh GetRcsgCollisionMesh()
        {
            var collisionMeshGO = GameObject.Find("Frame/LevelGeo/[generated-meshes]/[generated-collider-mesh]");
            var cMesh           = collisionMeshGO.GetComponent<MeshCollider>().sharedMesh;
            return cMesh;
        }
        
        public static void AddMatsToRender(MeshRenderer mr, string[] matNames)
        {
            var mats = new Material[matNames.Length];
            for (int i = 0; i < matNames.Length; i++) {
                var mat = AssetDatabase.FindAssets($"{matNames[i]}_mat");
                if (mat != null && mat.Length > 0) {
                    var assetPath = AssetDatabase.GUIDToAssetPath(mat[0]);
                    mats[i] = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                }
            }

            mr.material  = mr.materials[0];
            mr.materials = mats;
        }
    }
}
