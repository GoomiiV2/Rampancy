using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using InternalRealtimeCSG;
using Rampancy.Halo3;
using RampantC20;
using RampantC20.Halo3;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = System.Numerics.Vector3;

namespace Rampancy
{
    public class Halo3LevelExporter
    {
        private int                                MatIdx  = 0;
        private Dictionary<string, (int, MatInfo)> MatList = new();

        public bool Export(string path)
        {
            var ass            = new Ass();
            var rootGo         = GameObject.Find("Frame");
            var allRcsgModels  = GameObject.FindObjectsOfType<RealtimeCSG.Components.CSGModel>();
            var rcsgMeshLookup = new Dictionary<string, (RealtimeCSG.Components.CSGModel, List<(Transform, Instance)>)>();

            // Get all the meshes and store with a unique name
            foreach (var model in allRcsgModels.Where(x => x.name != "[default-CSGModel]")) {
                if (PrefabUtility.IsPartOfPrefabInstance(model)) {
                    var name         = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model);
                    var instanceData = PrefabUtility.GetOutermostPrefabInstanceRoot(model).transform.parent.gameObject.GetComponent<Instance>();

                    if (rcsgMeshLookup.ContainsKey(name)) {
                        rcsgMeshLookup[name].Item2.Add((instanceData.transform, instanceData));
                    }
                    else {
                        rcsgMeshLookup.Add(name, (model, new List<(Transform, Instance)> {(model.gameObject.transform.parent, instanceData)}));
                    }
                }
                else {
                    rcsgMeshLookup.Add(model.gameObject.name, (model, new List<(Transform, Instance)> {(model.gameObject.transform, null)}));
                }
            }

            // Convert and combine
            foreach (var meshData in rcsgMeshLookup) {
                //var mats      = meshData.Value.generatedMeshes.MeshInstances.Select(x => x.RenderMaterial).ToArray();
                var combined  = GetRcsgCombinedMesh(meshData.Value.Item1.generatedMeshes);
                var fixedMesh = LevelExporter.FixTJunctionsV2(combined.mesh);
                var assMesh   = UnityMeshToAssMesh(fixedMesh, combined.mats);
                ass.Objects.Add(assMesh);

                /*Debug.Log($"Model: {meshData.Key}, {meshData.Value.Item1.name}");
                foreach (var mat in combined.mats) {
                    Debug.Log($"    MatName: {mat.name}");
                }*/
            }

            // Instances transforms
            var instIdx      = 0;
            var instUniqueId = 0;

            var sceneRootInst = new Ass.Instance
            {
                ObjectIdx        = -1,
                Name             = "Scene Root",
                UniqueId         = -1,
                ParentId         = -1,
                InheritanceFlags = 0,
                Rotation         = new System.Numerics.Quaternion(0, 0, 0, 1),
                Position         = new Vector3(),
                Scale            = 1,
                PivotScale       = 1,
                PivotRotation    = new System.Numerics.Quaternion(0, 0, 0, 1)
            };
            ass.Instances.Add(sceneRootInst);

            foreach (var entry in rcsgMeshLookup.Values) {
                foreach (var instance in entry.Item2) {
                    var rot = instance.Item1.rotation * Statics.ExportRotation;

                    var inst = new Ass.Instance
                    {
                        ObjectIdx        = instIdx,
                        Name             = instance.Item2?.name ?? instance.Item1.gameObject.name,
                        UniqueId         = instUniqueId++,
                        ParentId         = -1,
                        InheritanceFlags = 0,
                        Rotation         = instance.Item1.rotation.ToNumericsYUpToZUp(),
                        Position         = ScalePos(instance.Item1.position).ToNumerics(),
                        Scale            = instance.Item1.localScale.x, // Assume all are uniform
                        PivotRotation    = new System.Numerics.Quaternion(0, 0, 0, 1),
                        PivotScale       = 1
                    };

                    ass.Instances.Add(inst);
                }

                instIdx++;
            }

            // Header
            ass.Head = new()
            {
                Version           = 7,
                ToolName          = Statics.NAME,
                ToolVersion       = Statics.Version,
                ExportUsername    = "",
                ExportMachineName = ""
            };

            // Mats
            ass.Materials = new(MatList.Count);
            foreach (var mat in MatList.Values.OrderBy(x => x.Item1)) {
                ass.Materials.Add(new Ass.Material
                {
                    Collection = mat.Item2.Collection ?? "",
                    Name       = mat.Item2.Name
                });
            }

            // log debugs
            var matIdx = 0;
            foreach (var mat in ass.Materials) {
                Debug.Log($"Mat: {matIdx++} {mat.Collection} {mat.Name}");
            }

            var objectIdx = 0;
            foreach (var obj in ass.Objects) {
                Debug.Log($"Model: {objectIdx++} {obj.Type} {obj.Name}");
            }

            var instanceIdx = 0;
            foreach (var inst in ass.Instances) {
                // Obj: {ass.Objects[inst.ObjectIdx].Name}
                Debug.Log($"Instance: {instanceIdx++} {inst.Name}");
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

        private static (Mesh mesh, Material[] mats) GetRcsgCombinedMesh(GeneratedMeshes meshes)
        {
            var combines = new List<CombineInstance>(meshes.MeshInstances.Length);
            var matNames = new List<Material>(meshes.MeshInstances.Length);

            foreach (var mesh in meshes.MeshInstances.Where(x => x.name == "[generated-render-mesh]")) {
                var combineData = new CombineInstance
                {
                    mesh      = mesh.SharedMesh,
                    transform = Matrix4x4.identity
                };

                combines.Add(combineData);
                matNames.Add(mesh.RenderMaterial);
            }

            var combinedMesh = new Mesh();
            combinedMesh.name = meshes.name;

            combinedMesh.CombineMeshes(combines.ToArray(), false);
            combinedMesh.Optimize();

            return (combinedMesh, matNames.ToArray());
        }

        private static UnityEngine.Vector3 ScalePos(UnityEngine.Vector3 vec, bool scale = true)
        {
            var pos = scale ? (vec * Statics.ImportScale) : (vec);
            return new UnityEngine.Vector3(pos.x, pos.z, pos.y);
        }

        private static UnityEngine.Vector3 ScalePosRot(UnityEngine.Vector3 vec, bool scale = true)
        {
            var pos = scale ? (Statics.ExportRotation * vec * Statics.ImportScale) : (Statics.ExportRotation * vec);
            return new UnityEngine.Vector3(pos.x * -1, pos.y, pos.z);
        }

        private Ass.MeshObject UnityMeshToAssMesh(Mesh mesh, Material[] meshMats)
        {
            var numVerts = mesh.vertexCount;
            var assMesh = new Ass.MeshObject
            {
                Type  = Ass.ObjectType.MESH,
                Verts = new(numVerts),
                Tris  = new(mesh.triangles.Length / 3)
            };

            var srcVertsPos   = mesh.vertices;
            var srcVertsNorm  = mesh.normals;
            var srcVertsColor = mesh.colors;
            var srcVertsUvs1  = mesh.uv;
            for (int i = 0; i < numVerts; i++) {
                var vert = new Ass.Vertex
                {
                    Position = ScalePos(srcVertsPos[i]).ToNumerics(),
                    Normal   = new Vector3(srcVertsNorm[i].x, srcVertsNorm[i].z, srcVertsNorm[i].y),
                    Color    = srcVertsColor.Length > 0 ? new Vector3(srcVertsColor[i].r, srcVertsColor[i].g, srcVertsColor[i].b) : Vector3.Zero,
                    Uvws     = new(1),
                    Weights  = new()
                };

                // TODO: Other uv channels, lightmaps?
                vert.Uvws.Add(new Vector3(srcVertsUvs1[i].x, srcVertsUvs1[i].y, 1));

                assMesh.Verts.Add(vert);
            }

            //var mats = mesh.GameObject().GetComponent<MeshRenderer>()?.materials;
            for (int i = 0; i < mesh.subMeshCount; i++) {
                var subMesh = mesh.GetSubMesh(i);
                for (int j = subMesh.indexStart; j < subMesh.indexStart + subMesh.indexCount; j += 3) {
                    var mat   = meshMats[i];
                    var matId = GetMatId(mat);

                    assMesh.Tris.Add(new Ass.Triangle
                    {
                        MatIndex = matId,
                        Vert1Idx = mesh.triangles[j + 2],
                        Vert2Idx = mesh.triangles[j + 1],
                        Vert3Idx = mesh.triangles[j]
                    });
                }
            }

            return assMesh;
        }

        private int GetMatId(Material mat)
        {
            var path = AssetDatabase.GetAssetPath(mat);
            if (!MatList.ContainsKey(path)) {
                var info = AssetDatabase.LoadAssetAtPath<MatInfo>(path);

                if (info != null) {
                    MatList.Add(path, (MatIdx++, info));
                }
                else {
                    MatList.Add(path, (MatIdx++, new MatInfo
                    {
                        Name = mat.name
                    }));
                }

                return MatIdx - 1;
            }

            var matId = MatList[path].Item1;
            return matId;
        }
    }
}