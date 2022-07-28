using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rampancy;
using RampantC20;
using RampantC20.Halo3;
using UnityEditor;
using UnityEngine;
using MeshAndMatIndexes = System.Tuple<UnityEngine.Mesh, int[]>;

namespace Rampancy
{
    public class AssConverter
    {
        public Ass        AssFile;
        public string     Name;
        public string     AssFilePath;
        public GameObject Root;

        private Dictionary<int, MeshData> MeshLookup       = new();
        private List<Material>            MatLookup        = new();
        private Material                  MissingMatMat    = null;
        private Vector3                   Scale            = new(Statics.ExportScale, Statics.ExportScale, Statics.ExportScale);
        private ShaderCollection          ShaderCollection = null;

        public void ImportToScene(Ass ass, string name, string assPath)
        {
            AssFile     = ass;
            Name        = name;
            AssFilePath = assPath;
            Root        = new GameObject(name);

            // Convert the meshes
            for (var i = 0; i < ass.Objects.Count; i++) {
                var obj = ass.Objects[i];
                if (obj.Type == Ass.ObjectType.MESH) {
                    var mesh = AssMeshToMesh(obj as Ass.MeshObject);
                    MeshLookup.Add(i, mesh);
                }
            }

            // Get the materials
            MissingMatMat    = AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseData/uv Grid.mat");
            ShaderCollection = Rampancy.Halo3Implementation.GetShaderCollection();
            for (var i = 0; i < ass.Materials.Count; i++) {
                var matData = ass.Materials[i];
                var mat     = GetMat(matData);
                MatLookup.Add(mat);
            }

            for (var i = 0; i < ass.Instances.Count; i++) {
                var inst = ass.Instances[i];

                if (inst.ObjectIdx != -1) {
                    var objType = ass.Objects[inst.ObjectIdx].Type;
                    var go = objType switch
                    {
                        Ass.ObjectType.MESH          => CreateInstanceMesh(inst),
                        Ass.ObjectType.GENERIC_LIGHT => CreateLightObject(inst),
                        Ass.ObjectType.SPHERE        => CreateSphereObject(inst),
                        Ass.ObjectType.BOX           => CreateBoxObject(inst),
                        _                            => new GameObject($"Unknown Object, Type: {objType}")
                    };
                }
            }

            Root.transform.rotation   = Quaternion.Euler(-90, 0, 0);
            Root.transform.localScale = new Vector3(-1, 1, 1);
        }

        public Material GetMat(Ass.Material matData)
        {
            // Special materials
            if (matData.Name.StartsWith("+portal")) return AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseData/+portal.mat");

            var relPath          = Utils.GetDataRelPath(Path.GetDirectoryName(AssFilePath), Rampancy.Cfg.Halo3MccGameConfig.DataPath);
            var tagRelPath       = Utils.GetDataToTagPath(relPath).Trim('\\').Replace("\\structure", "");
            var halo3TagDataPath = Path.Combine("Assets", $"{GameVersions.Halo3}", "TagData");

            var    assBasePath        = Path.Combine(halo3TagDataPath, tagRelPath, "shaders");
            string collectionBasePath = null;
            if (matData.Collection != null) // Has a collection
                if (ShaderCollection.Mapping.TryGetValue(matData.Collection, out var cBasePath))
                    collectionBasePath = Path.Combine(halo3TagDataPath, cBasePath, "shaders");

            var shaderName = $"{matData.Name}_mat";
            var foundAssets = AssetDatabase.FindAssets(shaderName, new string[] {collectionBasePath ?? assBasePath, assBasePath, halo3TagDataPath})
                                           .Select(x => AssetDatabase.GUIDToAssetPath(x));

            var mat = AssetDatabase.LoadAssetAtPath<Material>(foundAssets.FirstOrDefault());

            if (mat == null)
                Debug.LogWarning(
                    $"Couldn't find shader \"{matData.Name}\" {(matData.Collection != null ? $"in collection {matData.Collection}" : "")}, tried looking in: {collectionBasePath}, {assBasePath}, {halo3TagDataPath}, ({string.Join(",", foundAssets)})");

            return mat ?? MissingMatMat;
        }

        public GameObject CreateInstanceMesh(Ass.Instance instance)
        {
            var go     = new GameObject($"Instance: {instance.Name}");
            var meshGo = new GameObject($"Mesh");

            var rot   = Quaternion.Euler(new Vector3(0, 0, 0));
            var scale = new Vector3(Statics.ExportScale, Statics.ExportScale, Statics.ExportScale);

            var localPosScaled = Vector3.Scale(scale, instance.Position.ToUnity());
            var pivotPosScaled = Vector3.Scale(scale, instance.PivotPosition.ToUnity());

            var localRot = Quaternion.Euler(instance.Rotation.ToUnity().eulerAngles);
            var pivotRot = Quaternion.Euler(instance.PivotRotation.ToUnity().eulerAngles);

            meshGo.transform.SetParent(go.transform);

            meshGo.transform.rotation   = pivotRot;
            meshGo.transform.position   = pivotPosScaled;
            meshGo.transform.localScale = Vector3.one * instance.PivotScale;

            go.transform.rotation   = localRot;
            go.transform.position   = localPosScaled;
            go.transform.localScale = Vector3.one * instance.Scale;

            var mr = meshGo.AddComponent<MeshRenderer>();
            var mf = meshGo.AddComponent<MeshFilter>();

            var meshData = MeshLookup[instance.ObjectIdx];

            // TODO: Redo, temp
            var newMats = new Material[meshData.MatIds.Length];
            for (var i = 0; i < newMats.Length; i++) {
                var matIdx = meshData.MatIds[i];
                if (matIdx == -1) {
                    newMats[i] = MissingMatMat;
                    continue;
                }

                newMats[i] = MatLookup[matIdx];
            }

            mr.sharedMaterials = newMats;
            mf.sharedMesh      = meshData.Mesh;

            go.transform.parent = Root.transform;

            return go;
        }

        public GameObject CreateLightObject(Ass.Instance instance)
        {
            var objData = AssFile.Objects[instance.ObjectIdx] as Ass.LightObject;
            var light   = new GameObject("Light");
            light.transform.position = Vector3.Scale(Scale, instance.Position.ToUnity());
            light.transform.rotation = instance.Rotation.ToUnity();
            light.transform.parent   = Root.transform;

            return light;
        }

        public GameObject CreateSphereObject(Ass.Instance instance)
        {
            var objData = AssFile.Objects[instance.ObjectIdx] as Ass.SphereObject;
            var sphere  = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name                 = "Sphere";
            sphere.transform.position   = Vector3.Scale(Scale, instance.Position.ToUnity());
            sphere.transform.rotation   = instance.Rotation.ToUnity();
            sphere.transform.localScale = Vector3.one * objData.Radius;
            sphere.transform.parent     = Root.transform;

            return sphere;
        }

        public GameObject CreateBoxObject(Ass.Instance instance)
        {
            var objData = AssFile.Objects[instance.ObjectIdx] as Ass.SphereObject;
            var box     = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name                 = "Cube";
            box.transform.position   = Vector3.Scale(Scale, instance.Position.ToUnity());
            box.transform.rotation   = instance.Rotation.ToUnity();
            box.transform.localScale = Vector3.one * instance.Scale;
            box.transform.parent     = Root.transform;

            return box;
        }

        public static GameObject CreateMeshGo(Mesh mesh, string name, string[] matNames)
        {
            var go = new GameObject(name ?? "Mesh");
            var mr = go.AddComponent<MeshRenderer>();
            var mf = go.AddComponent<MeshFilter>();

            var missingMatMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseData/uv Grid.mat");
            var mats          = new Material[matNames.Length];

            for (var i = 0; i < mats.Length; i++) mats[i] = missingMatMat;

            mr.sharedMaterials = mats;

            mf.sharedMesh = mesh;

            return go;
        }

        public static MeshData AssMeshToMesh(Ass.MeshObject assMesh)
        {
            var mesh = new Mesh();

            var verts  = new Vector3[assMesh.Verts.Count];
            var norms  = new Vector3[assMesh.Verts.Count];
            var colors = new Vector3[assMesh.Verts.Count];
            var uvs    = new Vector2[assMesh.Verts.Count];

            var rot   = Quaternion.Euler(new Vector3(-90, 0, 0));
            var scale = new Vector3(Statics.ExportScale, Statics.ExportScale, Statics.ExportScale);

            var centerPoint                                           = Vector3.zero;
            for (var i = 0; i < assMesh.Verts.Count; i++) centerPoint += assMesh.Verts[i].Position.ToUnity();

            centerPoint /= assMesh.Verts.Count;

            for (var i = 0; i < assMesh.Verts.Count; i++) {
                var assVert = assMesh.Verts[i];
                verts[i]  = Vector3.Scale(scale, assVert.Position.ToUnity());
                norms[i]  = assVert.Normal.ToUnity();
                colors[i] = assVert.Color.ToUnity();

                if (assVert.Uvws.Count >= 1)
                    uvs[i] = assVert.Uvws[0].ToUnity(); // Assume only and at least 1
            }

            var subMeshes = new Dictionary<int, List<int>>();
            for (var i = 0; i < assMesh.Tris.Count; i++) {
                var tri = assMesh.Tris[i];

                if (!subMeshes.ContainsKey(tri.MatIndex)) subMeshes.Add(tri.MatIndex, new List<int>(50 * 3));
                subMeshes[tri.MatIndex].AddRange(new[] {tri.Vert1Idx, tri.Vert2Idx, tri.Vert3Idx});
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = subMeshes.Count;

            var subMeshIdx = 0;
            foreach (var submeshKvp in subMeshes) mesh.SetTriangles(submeshKvp.Value.ToArray(), subMeshIdx++);

            var meshData = new MeshData()
            {
                Mesh   = mesh,
                MatIds = subMeshes.Keys.ToArray()
            };

            return meshData;
        }

        public class MeshData
        {
            public Mesh  Mesh;
            public int[] MatIds;
        }
    }
}