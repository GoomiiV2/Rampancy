using System.Collections.Generic;
using System.Linq;
using InternalRealtimeCSG;
using Plugins.Rampancy.Runtime;
using UnityEditor;
using UnityEngine;

namespace Plugins.Rampancy.Runtime
{
    public static class LevelExporter
    {
        public static void ExportLevel(string path, bool tJunctionFix = true)
        {
            var meshData = GetRcsgMesh();
            var mesh     = meshData.mehs;

            // Fix up t junction by splitting the edges
            if (tJunctionFix) {
                mesh = FixTJunctionsV2(mesh);
            }
            
            var jms      = JmsConverter.MeshToJms(mesh, meshData.matNames);
            jms.Save(path);
        }
        
        // Todo: Improve
        public static Mesh FixTJunctions(Mesh mesh)
        {
            var halfEdgeMesh = new HalfMesh();
            halfEdgeMesh.FromUnityMesh(mesh);
            var tJunctions = halfEdgeMesh.FindTJunctions();

            var groupedByEdge = tJunctions.GroupBy(x => x.edgeIdx);
            var maxLoops      = tJunctions.Count > 1 ? groupedByEdge.Select(x => x.Count()).Max() : 0;

            // Lazy fix for now
            for (int i = 0; i < maxLoops; i++) {
                tJunctions = halfEdgeMesh.FindTJunctions();
                tJunctions = tJunctions.GroupBy(x => x.edgeIdx).Select(x => x.First()).ToList();
                
                foreach (var (edgeIdx, vertIdx) in tJunctions) {
                    var vert = halfEdgeMesh.Verts[vertIdx];
                    halfEdgeMesh.SplitEdge(edgeIdx, vert.Pos);
                }
            }

            var fixedMesh = halfEdgeMesh.ToMesh();

            return fixedMesh;
        }

        public static Mesh FixTJunctionsV2(Mesh mesh)
        {
            var wingedMesh = new WingedMesh();
            wingedMesh.FromUnityMesh(mesh);
            var tJunctions = wingedMesh.FindTJunctions();
            wingedMesh.FixTJunctions(tJunctions);
            var outputMesh = wingedMesh.ToUnityMesh();

            return outputMesh;
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
                            mesh      = mf.mesh,
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
