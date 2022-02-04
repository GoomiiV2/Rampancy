using System.Collections.Generic;
using System.Linq;
using RampantC20;
using RampantC20.Halo3;
using UnityEditor;
using UnityEngine;
using MeshAndMatIndexes = System.Tuple<UnityEngine.Mesh, int[]>;

namespace Plugins.Rampancy.Runtime
{
    public static class AssConverter
    {
        public static void ImportToScene(Ass ass, string name)
        {
            var rootGo = new GameObject(name);

            // Convert the meshes
            var meshes = new Dictionary<int, MeshAndMatIndexes>(ass.Objects.Count);
            for (int i = 0; i < ass.Objects.Count; i++) {
                var obj = ass.Objects[i];
                if (obj.Type == Ass.ObjectType.MESH) {
                    var mesh = AssMeshToMesh(obj as Ass.MeshObject);
                    meshes.Add(i, mesh);
                }
            }

            for (int i = 0; i < ass.Instances.Count; i++) {
                var inst = ass.Instances[i];

                if (inst.ObjectIdx != -1 && ass.Objects[inst.ObjectIdx].Type == Ass.ObjectType.MESH) {
                    var instanceGo = CreateInstanceMesh(inst, meshes, ass);
                    instanceGo.transform.parent = rootGo.transform;
                }
            }
            
            rootGo.transform.rotation   = Quaternion.Euler(-90, 0, 0);
            rootGo.transform.localScale = new Vector3(-1, 1, 1);
        }

        public static GameObject CreateInstanceMesh(Ass.Instance instance, Dictionary<int, MeshAndMatIndexes> meshLookup, Ass ass)
        {
            var go     = new GameObject($"Instance: {instance.Name}");
            var meshGo = new GameObject($"Mesh");

            var rot   = Quaternion.Euler(new Vector3(0, 0, 0));
            var scale = new Vector3(Statics.ExportScale, Statics.ExportScale, Statics.ExportScale);

            var localPosScaled = Vector3.Scale(scale, instance.Position.ToUnity());
            var pivotPosScaled = Vector3.Scale(scale, instance.PivotPosition.ToUnity());

            var localRot     = Quaternion.Euler(instance.Rotation.ToUnity().eulerAngles);
            var pivotRot     = Quaternion.Euler(instance.PivotRotation.ToUnity().eulerAngles);

            meshGo.transform.SetParent(go.transform);
            
            meshGo.transform.rotation   = pivotRot;
            meshGo.transform.position   = pivotPosScaled;
            meshGo.transform.localScale = Vector3.one * instance.PivotScale;

            go.transform.rotation   = localRot;
            go.transform.position   = localPosScaled;
            go.transform.localScale = Vector3.one * instance.Scale;

            var mr = meshGo.AddComponent<MeshRenderer>();
            var mf = meshGo.AddComponent<MeshFilter>();

            var meshData = meshLookup[instance.ObjectIdx];
            var matNames = meshData.Item2.Select(x => "").ToArray();

            var missingMatMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseData/uv Grid.mat");

            // TODO: Redo, temp
            var newMats = new Material[matNames.Length];
            for (int i = 0; i < newMats.Length; i++)
            {
                var matIdx = meshData.Item2[i];
                if (matIdx == -1)
                {
                    newMats[i] = missingMatMat;
                    continue;
                }

                var matName = ass.Materials[matIdx].Name;
                var shortName = matName.Contains(' ') ? matName.Split(' ')[1] : matName;
                var name = $"{shortName.Trim()}_mat";
                var mats = AssetDatabase.FindAssets(name);

                var mat = mats.FirstOrDefault();


                if (mat == null)
                {
                    newMats[i] = missingMatMat;
                    Debug.Log($"Couldn't find mat for: {name} ({matName})");
                }
                else
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(mat);
                    var matData = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                    newMats[i] = new Material(matData);
                }
            }

            mr.materials = newMats;

            mf.sharedMesh = meshData.Item1;

            return go;
        }

        public static GameObject CreateMeshGo(Mesh mesh, string name, string[] matNames)
        {
            var go = new GameObject(name ?? "Mesh");
            var mr = go.AddComponent<MeshRenderer>();
            var mf = go.AddComponent<MeshFilter>();

            var missingMatMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseData/uv Grid.mat");
            var mats = new Material[matNames.Length];

            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = missingMatMat;
            }

            mr.sharedMaterials = mats;

            mf.sharedMesh = mesh;

            return go;
        }

        public static MeshAndMatIndexes AssMeshToMesh(Ass.MeshObject assMesh)
        {
            var mesh = new Mesh();

            var verts  = new Vector3[assMesh.Verts.Count];
            var norms  = new Vector3[assMesh.Verts.Count];
            var colors = new Vector3[assMesh.Verts.Count];
            var uvs    = new Vector2[assMesh.Verts.Count];

            var rot   = Quaternion.Euler(new Vector3(-90, 0, 0));
            var scale = new Vector3(Statics.ExportScale, Statics.ExportScale, Statics.ExportScale);

            var centerPoint = Vector3.zero;
            for (int i = 0; i < assMesh.Verts.Count; i++) {
                centerPoint += assMesh.Verts[i].Position.ToUnity();
            }

            centerPoint /= assMesh.Verts.Count;

            for (int i = 0; i < assMesh.Verts.Count; i++) {
                var assVert = assMesh.Verts[i];
                verts[i]  = Vector3.Scale(scale, assVert.Position.ToUnity());
                norms[i]  = assVert.Normal.ToUnity();
                colors[i] = assVert.Color.ToUnity();

                if (assVert.Uvws.Count >= 1)
                    uvs[i] = assVert.Uvws[0].ToUnity(); // Assume only and at least 1
            }

            var subMeshes = new Dictionary<int, List<int>>();
            for (int i = 0; i < assMesh.Tris.Count; i++) {
                var tri = assMesh.Tris[i];

                if (!subMeshes.ContainsKey(tri.MatIndex)) subMeshes.Add(tri.MatIndex, new List<int>(50 * 3));
                subMeshes[tri.MatIndex].AddRange(new[] {tri.Vert1Idx, tri.Vert2Idx, tri.Vert3Idx});
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = subMeshes.Count;

            var subMeshIdx = 0;
            foreach (var submeshKvp in subMeshes) {
                mesh.SetTriangles(submeshKvp.Value.ToArray(), subMeshIdx++);
            }

            return new MeshAndMatIndexes(mesh, subMeshes.Keys.ToArray());
        }
    }
}