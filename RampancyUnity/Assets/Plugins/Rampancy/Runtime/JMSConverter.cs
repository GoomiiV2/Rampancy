using System;
using System.Collections.Generic;
using System.Linq;
using Plugins.Rampancy.RampantC20;
using Rampancy.RampantC20;
using UnityEngine;

namespace Plugins.Rampancy.Runtime
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
                    Normal     = hasNormals ? new Vector3(normals[i].x, normals[i].z, normals[i].y) : Vector3.one,
                    NodeIdx    = 0,
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
    }
}