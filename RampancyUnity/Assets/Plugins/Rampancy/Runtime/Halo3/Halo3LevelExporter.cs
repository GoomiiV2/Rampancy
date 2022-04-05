using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using InternalRealtimeCSG;
using Rampancy.Halo3;
using RampantC20;
using RampantC20.Halo3;
using RealtimeCSG.Components;
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
        private        int                              MatIdx           = 0;
        private static int                              InstanceUniqueId = 0;
        private        Dictionary<string, MatInfoAndId> MatList          = new();

        public bool Export(string path)
        {
            var ass = new Ass();
            // Header
            ass.Head = new()
            {
                Version           = 7,
                ToolName          = Statics.NAME,
                ToolVersion       = Statics.Version,
                ExportUsername    = "",
                ExportMachineName = ""
            };


            var allRcsgModels  = GameObject.FindObjectsOfType<CSGModel>();
            var rcsgMeshLookup = new Dictionary<string, MeshReference>();

            // Get all the meshes and store with a unique name
            foreach (var model in allRcsgModels.Where(x => x.name != "[default-CSGModel]")) {
                if (!PrefabUtility.IsPartOfPrefabInstance(model)) {
                    rcsgMeshLookup.Add(model.gameObject.name, new MeshReference(model, model.gameObject));
                    continue;
                }
                
                // Is a prefab, so likley an instance
                var name = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model);
                var go   = PrefabUtility.GetNearestPrefabInstanceRoot(model).transform.parent.gameObject;
                if (rcsgMeshLookup.ContainsKey(name))
                    rcsgMeshLookup[name].InstanceData.Add(go);
                else
                    rcsgMeshLookup.Add(name, new MeshReference(model, go));
            }

            // Convert and combine
            foreach (var meshData in rcsgMeshLookup.Values) {
                if (meshData.Type == MeshReference.RefType.Rcsg) {
                    var meshAndMats = GetRcsgCombinedMesh(meshData.GetRcsgModel().generatedMeshes, meshData.InstanceData.FirstOrDefault().transform.position);
                    var fixedMesh   = LevelExporter.FixTJunctionsV2(meshAndMats.Mesh);
                    var assMesh     = UnityMeshToAssMesh(fixedMesh, meshAndMats.Mats);
                    ass.Objects.Add(assMesh);
                }
                else {
                    // not supported yet
                }
            }

            // Instances transforms
            ass.Instances.Add(CreateSceneRoot());

            var instIdx = 0;
            foreach (var entry in rcsgMeshLookup.Values) {
                foreach (var go in entry.InstanceData) {
                    var inst = CreateInstanceRef(go, instIdx);
                    ass.Instances.Add(inst);
                }

                instIdx++;
            }

            // Mats
            ass.Materials = new(MatList.Count);
            foreach (var mat in MatList.Values.OrderBy(x => x.Id)) {
                ass.Materials.Add(new Ass.Material
                {
                    Collection = mat.MatInfo?.Collection ?? "",
                    Name       = mat.GetName()
                });
            }

            ass.Save(path);

            return true;
        }

        private static Ass.Instance CreateInstanceRef(GameObject go, int instIdx)
        {
            var instComp   = go.GetComponent<Instance>();
            var isInstance = instComp != null;

            var inst = new Ass.Instance
            {
                ObjectIdx        = instIdx,
                Name             = isInstance ? instComp.GetName() : go.name,
                UniqueId         = InstanceUniqueId++,
                ParentId         = -1,
                InheritanceFlags = 0,
                Rotation         = go.transform.rotation.ToNumericsYUpToZUp(),
                Position         = ScalePos(go.transform.position).ToNumerics(),
                Scale            = go.transform.localScale.x, // Assume all are uniform
                PivotRotation    = new System.Numerics.Quaternion(0, 0, 0, 1),
                PivotScale       = 1
            };

            return inst;
        }

        private static Ass.Instance CreateSceneRoot()
        {
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
            return sceneRootInst;
        }

        private static MeshAndMats GetRcsgCombinedMesh(GeneratedMeshes meshes, UnityEngine.Vector3 offset)
        {
            var combines = new List<CombineInstance>(meshes.MeshInstances.Length);
            var mats     = new List<Material>(meshes.MeshInstances.Length);

            foreach (var mesh in meshes.MeshInstances.Where(x => x.name == "[generated-render-mesh]")) {
                var combineData = new CombineInstance
                {
                    mesh      = mesh.SharedMesh,
                    transform = Matrix4x4.Translate(-offset)
                };

                combines.Add(combineData);
                mats.Add(mesh.RenderMaterial);
            }

            var combinedMesh = new Mesh();
            combinedMesh.name = meshes.name;

            combinedMesh.CombineMeshes(combines.ToArray(), false);
            combinedMesh.Optimize();

            return new MeshAndMats(combinedMesh, mats.ToArray());
        }

        private static UnityEngine.Vector3 ScalePos(UnityEngine.Vector3 vec) => new UnityEngine.Vector3(vec.x, vec.z, vec.y) * Statics.ImportScale;

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
                    Normal   = Vector3.Normalize(new Vector3(srcVertsNorm[i].x, srcVertsNorm[i].z, srcVertsNorm[i].y)),
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

                    var tri = new Ass.Triangle
                    {
                        MatIndex = matId,
                        Vert1Idx = mesh.triangles[j + 2],
                        Vert2Idx = mesh.triangles[j + 1],
                        Vert3Idx = mesh.triangles[j]
                    };
                    assMesh.Tris.Add(tri);
                    
                    // scale the uvs
                    /*var matTiling = meshMats[i].mainTextureScale;
                    assMesh.Verts[tri.Vert1Idx].Uvws[0] = new(assMesh.Verts[tri.Vert1Idx].Uvws[0].X / matTiling.x, assMesh.Verts[tri.Vert1Idx].Uvws[0].Y / matTiling.y, 1);
                    assMesh.Verts[tri.Vert2Idx].Uvws[0] = new(assMesh.Verts[tri.Vert2Idx].Uvws[0].X / matTiling.x, assMesh.Verts[tri.Vert2Idx].Uvws[0].Y / matTiling.y, 1);
                    assMesh.Verts[tri.Vert3Idx].Uvws[0] = new(assMesh.Verts[tri.Vert3Idx].Uvws[0].X / matTiling.x, assMesh.Verts[tri.Vert3Idx].Uvws[0].Y / matTiling.y, 1);*/
                }
            }

            return assMesh;
        }

        private int GetMatId(Material mat)
        {
            var path = AssetDatabase.GetAssetPath(mat);
            if (!MatList.ContainsKey(path)) {
                var info = AssetDatabase.LoadAssetAtPath<MatInfo>(path);
                MatList.Add(path, new MatInfoAndId(MatIdx++, info, mat.name));
                return MatIdx - 1;
            }

            var matId = MatList[path].Id;
            return matId;
        }

        public class MeshReference
        {
            public RefType          Type;
            public object           Ref;
            public List<GameObject> InstanceData;

            public MeshReference(object @ref, GameObject go)
            {
                Ref          = @ref;
                InstanceData = new List<GameObject> {go};

                Type = Ref switch
                {
                    CSGModel => RefType.Rcsg,
                    Mesh     => RefType.UnityMesh,
                    _        => Type
                };
            }

            public CSGModel GetRcsgModel()  => Ref as CSGModel;
            public Mesh     GetUnityModel() => Ref as Mesh;

            public enum RefType
            {
                Rcsg,
                UnityMesh
            }
        }

        public class MeshAndMats
        {
            public Mesh       Mesh;
            public Material[] Mats;

            public MeshAndMats(Mesh mesh, Material[] mats)
            {
                Mesh = mesh;
                Mats = mats;
            }
        }

        public class MatInfoAndId
        {
            public int     Id;
            public MatInfo MatInfo;
            public string  Name;

            public string GetName() => MatInfo?.Name ?? Name;

            public MatInfoAndId(int id, MatInfo matInfo, string name)
            {
                Id      = id;
                MatInfo = matInfo;
                Name    = name;
            }
        }
    }
}