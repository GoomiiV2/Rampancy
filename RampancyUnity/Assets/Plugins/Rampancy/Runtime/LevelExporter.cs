using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Rampancy
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
            
            Debug.Log($"Exported Level {Path.GetFileName(path)}");
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
            
            Debug.Log($"Exported Level Collision {Path.GetFileName(path)}");
        }
        
        public static (Mesh mesh, string[] matNames) GetRcsgMesh()
        {
            var rootGo     = GameObject.Find("Frame/LevelGeo");
            if (rootGo == null) Debug.LogError("No Frame/LevelGeo found, did you create this level from the menu?\nLevels should be created from the \"Rampancy > Create New Level\" UI menu");

            var allGameObjects = GameObject.FindObjectsOfType<GameObject>();
            if (allGameObjects == null) Debug.LogError("No GameObjects found, is the scene empty?");

            var renderMeshes = allGameObjects.Where(x => x.transform.IsChildOf(rootGo.transform) && x.name == "[generated-render-mesh]");
            if (!renderMeshes.Any()) Debug.LogError("No realtime csg meshes where found.\nPlease check you have at least one brush in the level");
            
            var rcsgMeshes   = renderMeshes.ToList();
            
            var mesh     = new Mesh();
            var combines = new List<CombineInstance>(rcsgMeshes.Count);
            var matNames = new List<string>();
            for (int i = 0; i < rcsgMeshes.Count; i++) {
                AddMeshCombiner(rcsgMeshes[i].transform);
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
                if (mat is {Length: > 0}) {
                    var assetPath = AssetDatabase.GUIDToAssetPath(mat[0]);
                    mats[i] = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                }
            }

            mr.material  = mr.materials[0];
            mr.materials = mats;
        }
    }
}
