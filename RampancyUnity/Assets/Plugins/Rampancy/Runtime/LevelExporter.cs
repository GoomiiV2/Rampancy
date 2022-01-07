using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Plugins.Rampancy.Runtime
{
    public static class LevelExporter
    {
        public static void ExportLevel(string path, bool tJunctionFix = true)
        {
            var meshData = GetRcsgMesh();
            var mesh     = meshData.mesh;

            // Fix up t junction by splitting the edges
            if (tJunctionFix) {
                mesh = FixTJunctionsV2(mesh);
            }
            
            var jms      = JmsConverter.MeshToJms(mesh, meshData.matNames);
            jms.Save(path);
        }

        // Fix T-Junctions by converting to a wingedMesh, finding, fixing and back to a unity mesh
        public static Mesh FixTJunctionsV2(Mesh mesh)
        {
            var wingedMesh = new WingedMesh();
            wingedMesh.FromUnityMesh(mesh);
            var tJunctions = wingedMesh.FindTJunctions();
            wingedMesh.FixTJunctions(tJunctions);
            var outputMesh = wingedMesh.ToUnityMesh();

            return outputMesh;
        }
        
        // Export just the RCSG collision mesh
        public static void ExportLevelCollision(string path)
        {
            var cMesh = GetRcsgCollisionMesh();
            var jms      = JmsConverter.MeshToJms(cMesh, new[] { "none" });
            jms.Save(path);
        }
        
        public static (Mesh mesh, string[] matNames) GetRcsgMesh()
        {
            var baseMesh = GameObject.Find("Frame/LevelGeo/[generated-meshes]");

            var mesh     = new Mesh();
            var combines = new List<CombineInstance>(baseMesh.transform.childCount - 1);
            var matNames = new List<string>();
            for (int i = 0; i < baseMesh.transform.childCount; i++) {
                var childMesh = baseMesh.transform.GetChild(i);
                AddMeshCombiner(childMesh);
            }

            mesh.CombineMeshes(combines.ToArray(), false);
            mesh.Optimize();
            
            void AddMeshCombiner(Transform childMesh)
            {
                if (childMesh.name != "[generated-collider-mesh]") {
                    var mf          = childMesh.GetComponent<MeshFilter>();
                    var mr          = childMesh.GetComponent<MeshRenderer>();
                    var trimmedName = mr.sharedMaterial.name.Replace(" (Instance)", "").Replace("_mat", "");
                    if (trimmedName.ToLower() != "skip" && trimmedName != "transparentSpecialSurface_hidden" && trimmedName != "Default-Diffuse") {
                        var combine = new CombineInstance
                        {
                            mesh      = mf.sharedMesh,
                            transform = childMesh.transform.localToWorldMatrix
                        };

                        matNames.Add(trimmedName);
                        combines.Add(combine);
                    }
                }
            }
            
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
