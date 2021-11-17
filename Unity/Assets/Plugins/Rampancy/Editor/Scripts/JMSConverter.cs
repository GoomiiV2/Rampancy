using System;
using System.Collections.Generic;
using System.Linq;
using InternalRealtimeCSG;
using Plugins.Rampancy.RampantC20;
using Rampancy.RampantC20;
using UnityEditor;
//using UnityEditor.ProBuilder;
using UnityEngine;
//using UnityEngine.ProBuilder;
//using UnityEngine.ProBuilder.MeshOperations;
//using EditorUtility = UnityEditor.ProBuilder.EditorUtility;

namespace Plugins.Rampancy.Editor.Scripts
{
    public static class JmsConverter
    {
        // Simple JMS to Unity mesh for viewing
        public static Mesh JmsToMesh(JMS jms)
        {
            var mesh = new Mesh();

            // skip node stuff, all one mesh for this

            var rot   = Quaternion.Euler(new Vector3(-90, 0, 0));
            var scale = new Vector3(Statics.ExportScale, -Statics.ExportScale, Statics.ExportScale);
            var verts = new Vector3[jms.Verts.Count];
            var norms = new Vector3[jms.Verts.Count];
            var uvs   = new Vector2[jms.Verts.Count];
            for (int i = 0; i < jms.Verts.Count; i++) {
                var jmsVert = jms.Verts[i];
                verts[i] = rot * Vector3.Scale(scale, jmsVert.Position);
                norms[i] = rot * jmsVert.Normal;
                uvs[i]   = jmsVert.UV;
            }

            // A submesh per mat
            var subMeshes = new List<int>[jms.Materials.Count];
            for (int i = 0; i < jms.Materials.Count; i++) {
                subMeshes[i] = new List<int>();
            }

            for (int i = 0; i < jms.Tris.Count(); i++) {
                var tri = jms.Tris[i];
                subMeshes[tri.MaterialIdx].AddRange(new[] {tri.VertIdx2, tri.VertIdx1, tri.VertIdx0});
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = subMeshes.Length;

            for (int i = 0; i < subMeshes.Length; i++) {
                mesh.SetTriangles(subMeshes[i].ToArray(), i);
            }

            return mesh;
        }

        public static void AddMatsToRender(MeshRenderer mr, JMS jms)
        {
            var names = jms.Materials.Select(x => x.Name).ToArray();
            LevelExporter.AddMatsToRender(mr, names);
        }

        public static JMS MeshToJms(Mesh mesh, string[] matNames)
        {
            var indices = new int[mesh.subMeshCount][];
            
            for (int i = 0; i < mesh.subMeshCount; i++) {
                var subMesh = mesh.GetSubMesh(i);
                indices[i] = new int[subMesh.indexCount];
                Array.Copy(mesh.triangles, subMesh.indexStart, indices[i], 0, subMesh.indexCount);
            }

            var jms = CreateJms(mesh.vertices, mesh.uv, mesh.normals, indices, matNames);

            return jms;
        }

        public static JMS CreateJms(Vector3[] positions, Vector2[] uvs, Vector3[] normals, int[][] indices, string[] matNames)
        {
            var jms   = new JMS();
            var rot   = Quaternion.Euler(new Vector3(90, 0, 0));
            var scale = new Vector3(Statics.ImportScale, Statics.ImportScale, -Statics.ImportScale);
            jms.Nodes.Add(new JMS.Node
            {
                Name       = "frame",
                ChildIdx   = -1,
                SiblingIdx = -1,
                Rotation   = Quaternion.identity,
                Position   = new Vector3(0, 0, 0)
            });

            jms.Regions.Add(new JMS.Region
            {
                Name = "unnamed"
            });

            foreach (var matName in matNames) {
                jms.Materials.Add(new JMS.Material
                {
                    Name = matName,
                    Path = "<none>"
                });
            }

            bool hasUvs     = uvs     != null && uvs.Length == positions.Length;
            bool hasNormals = normals != null && normals.Length == positions.Length;
            for (int i = 0; i < positions.Length; i++) {
                var vert = new JMS.Vert
                {
                    Position   = rot * Vector3.Scale(scale, positions[i]),
                    UV         = hasUvs ? uvs[i] : Vector2.zero,
                    Normal     = hasNormals ? rot * normals[i] : Vector3.one,
                    NodeIdx    = -1,
                    NodeWeight = 0f
                };

                jms.Verts.Add(vert);
            }

            for (int i = 0; i < indices.Length; i++) {
                for (int idx = 0; idx < indices[i].Length; idx += 3) {
                    var tri = new JMS.Tri
                    {
                        RegionIdx   = 0,
                        MaterialIdx = i,
                        VertIdx0    = indices[i][idx + 2],
                        VertIdx1    = indices[i][idx + 1],
                        VertIdx2    = indices[i][idx]
                    };

                    jms.Tris.Add(tri);
                }
            }

            return jms;
        }

        public static void ExportLevel(string path)
        {
            var frame = GameObject.Find("Frame");
            //frame.transform.localScale = new Vector3(1, 1, -1);
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

            var jms   = new JMS();
            var rot   = Quaternion.Euler(new Vector3(90, 0, 0));
            var scale = new Vector3(Statics.ImportScale, Statics.ImportScale, -Statics.ImportScale);
            jms.Nodes.Add(new JMS.Node
            {
                Name       = "frame",
                ChildIdx   = -1,
                SiblingIdx = -1,
                Rotation   = Quaternion.identity,
                Position   = new Vector3(0, 0, 0)
            });

            jms.Regions.Add(new JMS.Region
            {
                Name = "unnamed"
            });

            foreach (var matName in matNames) {
                jms.Materials.Add(new JMS.Material
                {
                    Name = matName,
                    Path = "<none>"
                });
            }

            for (int i = 0; i < mesh.vertexCount; i++) {
                var vert = new JMS.Vert
                {
                    Position   = rot * Vector3.Scale(scale, mesh.vertices[i]),
                    UV         = mesh.uv[i],
                    Normal     = (rot * mesh.normals[i]),
                    NodeIdx    = -1,
                    NodeWeight = 0f
                };

                jms.Verts.Add(vert);
            }

            for (int i = 0; i < mesh.subMeshCount; i++) {
                var subMesh = mesh.GetSubMesh(i);
                for (int idx = subMesh.indexStart; idx < subMesh.indexStart + subMesh.indexCount; idx += 3) {
                    var tri = new JMS.Tri
                    {
                        RegionIdx   = 0,
                        MaterialIdx = i,
                        VertIdx0    = mesh.triangles[idx + 2],
                        VertIdx1    = mesh.triangles[idx + 1],
                        VertIdx2    = mesh.triangles[idx]
                    };

                    jms.Tris.Add(tri);
                }
            }

            jms.Save(path);
        }

        /*public static void ExportLevel2(string path)
        {
            var frame = GameObject.Find("Frame");
            //frame.transform.localScale = new Vector3(1, 1, -1);
            var baseMeshs = GameObject.Find("Frame/LevelGeo/[generated-meshes]");

            var mesh     = new Mesh();
            var combines = new List<CombineInstance>(baseMeshs.transform.childCount - 1);
            var matNames = new List<string>();
            var mats     = new List<Material>();
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

                        if (!mats.Contains(mr.material)) {
                            mats.Add(mr.material);
                        }
                    }
                }
            }

            mesh.CombineMeshes(combines.ToArray(), false);
            mesh.Optimize();

            var meshImportSettings = new MeshImportSettings
            {
                quads     = true,
                smoothing = false
            };

            var tempGo         = new GameObject();
            var probuilderMesh = tempGo.AddComponent<ProBuilderMesh>();
            var meshImporter   = new MeshImporter(mesh, mats.ToArray(), probuilderMesh);
            meshImporter.Import(meshImportSettings);

            Debug.Log($"before Has {probuilderMesh.vertexCount} verts, {probuilderMesh.faceCount} faces");
            //probuilderMesh.Rebuild();
            probuilderMesh.Optimize();
            //probuilderMesh.Refresh();

            var welded = probuilderMesh.WeldVertices(probuilderMesh.faces.SelectMany(x => x.indexes), 1f);
            EditorUtility.SynchronizeWithMeshFilter(probuilderMesh);
            probuilderMesh.ToMesh();
            
            Debug.Log($"after Has {probuilderMesh.vertexCount} verts, {probuilderMesh.faceCount} faces, {welded} welded");

            var jms   = new JMS();
            var rot   = Quaternion.Euler(new Vector3(90, 0, 0));
            var scale = new Vector3(Statics.ImportScale, Statics.ImportScale, -Statics.ImportScale);
            jms.Nodes.Add(new JMS.Node
            {
                Name       = "frame",
                ChildIdx   = -1,
                SiblingIdx = -1,
                Rotation   = Quaternion.identity,
                Position   = new Vector3(0, 0, 0)
            });

            jms.Regions.Add(new JMS.Region
            {
                Name = "unnamed"
            });

            foreach (var matName in matNames) {
                jms.Materials.Add(new JMS.Material
                {
                    Name = matName,
                    Path = "<none>"
                });
            }

            var faces      = probuilderMesh.ToTriangles(probuilderMesh.faces);
            var allIndices = faces.SelectMany(x => x.indexes);
            var verts      = probuilderMesh.GetVertices(allIndices.ToArray());
            for (int i = 0; i < verts.Count(); i++) {
                var vert = new JMS.Vert
                {
                    Position   = rot * Vector3.Scale(scale, verts[i].position),
                    UV         = verts[i].uv0,
                    Normal     = (rot * verts[i].normal),
                    NodeIdx    = -1,
                    NodeWeight = 0f
                };

                jms.Verts.Add(vert);
            }

            for (int i = 0; i < faces.Length; i++) {
                var face = faces[i];
                //for (int idx = 0; idx < indices.Count(); idx += 3) {
                var tri = new JMS.Tri
                {
                    RegionIdx   = 0,
                    MaterialIdx = face.submeshIndex,
                    VertIdx0    = face.indexes[2],
                    VertIdx1    = face.indexes[1],
                    VertIdx2    = face.indexes[0]
                };

                jms.Tris.Add(tri);
            }

            jms.Save(path);
        }*/
    }
}