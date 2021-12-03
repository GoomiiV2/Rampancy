using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Plugins.Rampancy.Editor.Scripts
{
    // A half edge mesh for doing edits on :>
    // This isn't a proper half edge mesh, its not fully linked twin edges and each vert with different attributes is separate
    //TODO: Do proper, but this works ok for now
    public class HalfMesh
    {
        public List<Vert> Verts = new();
        public List<Edge> Edges = new();
        public List<Face> Faces = new();

        public void MakeTest()
        {
        }

        public void FromUnityMesh(Mesh mesh)
        {
            Verts = new List<Vert>(mesh.vertexCount);
            for (int i = 0; i < mesh.vertexCount; i++) {
                var vert = new Vert
                {
                    Pos    = mesh.vertices[i],
                    Normal = mesh.normals[i],
                    Uv     = mesh.uv[i]
                };

                Verts.Add(vert);
            }

            var edgeMapping = new Dictionary<long, int>(mesh.triangles.Length);

            for (int i = 0; i < mesh.subMeshCount; i++) {
                var subMesh = mesh.GetSubMesh(i);
                for (var eye = subMesh.indexStart; eye < subMesh.indexStart + subMesh.indexCount; eye += 3) {
                    var idx1 = mesh.triangles[eye];
                    var idx2 = mesh.triangles[eye + 1];
                    var idx3 = mesh.triangles[eye + 2];

                    var edgeIdxBase = Edges.Count;

                    var faceIdx = Faces.Count;
                    var face = new Face
                    {
                        FaceIdx      = faceIdx,
                        SubMeshId    = (short) i,
                        VertIdx      = idx1,
                        EdgeStartIdx = edgeIdxBase
                    };
                    Faces.Add(face);

                    var edge1Id     = CreateEdgeId(idx1, idx2);
                    var edge1TwinId = CreateEdgeId(idx2, idx1);

                    var edge2Id     = CreateEdgeId(idx2, idx3);
                    var edge2TwinId = CreateEdgeId(idx3, idx2);

                    var edge3Id     = CreateEdgeId(idx3, idx1);
                    var edge3TwinId = CreateEdgeId(idx1, idx3);

                    /*
                    var strid1 = EdgeIdToStr(edge1Id);
                    var strid2 = EdgeIdToStr(edge1TwinId);
                    var strid3 = EdgeIdToStr(edge2Id);
                    var strid4 = EdgeIdToStr(edge2TwinId);
                    var strid5 = EdgeIdToStr(edge3Id);
                    var strid6 = EdgeIdToStr(edge3TwinId);
                    */

                    // Edge 1
                    var edge1 = new Edge
                    {
                        FaceIdx      = faceIdx,
                        Idx          = edgeIdxBase,
                        StartVertIdx = idx1,
                        NextEdgeIdx  = edgeIdxBase + 1,
                        PrevEdgeIdx  = edgeIdxBase + 2
                    };

                    Edges.Add(edge1);
                    edgeMapping.Add(edge1Id, edgeIdxBase);

                    // Edge 2
                    var edge2 = new Edge
                    {
                        FaceIdx      = faceIdx,
                        Idx          = edgeIdxBase + 1,
                        StartVertIdx = idx2,
                        NextEdgeIdx  = edgeIdxBase + 2,
                        PrevEdgeIdx  = edgeIdxBase
                    };

                    Edges.Add(edge2);
                    edgeMapping.Add(edge2Id, edgeIdxBase + 1);

                    // Edge 2
                    var edge3 = new Edge
                    {
                        FaceIdx      = faceIdx,
                        Idx          = edgeIdxBase + 2,
                        StartVertIdx = idx3,
                        NextEdgeIdx  = edgeIdxBase,
                        PrevEdgeIdx  = edgeIdxBase + 1
                    };

                    Edges.Add(edge3);
                    edgeMapping.Add(edge3Id, edgeIdxBase + 2);

                    if (edgeMapping.TryGetValue(edge1TwinId, out var edge1TwinIdx)) {
                        edge1.TwinIdx               = edge1TwinIdx;
                        Edges[edge1TwinIdx].TwinIdx = edgeIdxBase;
                    }

                    if (edgeMapping.TryGetValue(edge2TwinId, out var edge2TwinIdx)) {
                        edge2.TwinIdx               = edge2TwinIdx;
                        Edges[edge2TwinIdx].TwinIdx = edgeIdxBase + 1;
                    }

                    if (edgeMapping.TryGetValue(edge3TwinId, out var edge3TwinIdx)) {
                        edge3.TwinIdx               = edge3TwinIdx;
                        Edges[edge3TwinIdx].TwinIdx = edgeIdxBase + 2;
                    }
                }
            }
        }

        public Mesh ToMesh()
        {
            var mesh  = new Mesh();
            var verts = new Vector3[Verts.Count];
            var norms = new Vector3[Verts.Count];
            var uvs   = new Vector2[Verts.Count];

            for (int i = 0; i < Verts.Count; i++) {
                var vert = Verts[i];
                verts[i] = vert.Pos;
                norms[i] = vert.Normal;
                uvs[i]   = vert.Uv;
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);

            // Eh, will do for now
            var subMeshes = new Dictionary<int, List<int>>();
            for (int i = 0; i < Faces.Count; i++) {
                var face    = Faces[i];
                var edgeIdx = face.EdgeStartIdx;
                do {
                    var edge = Edges[edgeIdx];
                    if (!subMeshes.ContainsKey(face.SubMeshId)) subMeshes.Add(face.SubMeshId, new List<int>());

                    subMeshes[face.SubMeshId].Add(edge.StartVertIdx);

                    edgeIdx = edge.NextEdgeIdx;
                } while (edgeIdx != face.EdgeStartIdx);
            }

            mesh.subMeshCount = subMeshes.Keys.Count;
            foreach (var subMesh in subMeshes) {
                mesh.SetTriangles(subMesh.Value.ToArray(), subMesh.Key);
            }

            return mesh;
        }

        private long CreateEdgeId(int indiceFrom, int indiceToo)
        {
            return unchecked((long) (((ulong) indiceFrom << 32) | (uint) indiceToo));
        }

        public string EdgeIdToStr(long id)
        {
            int id1 = (int) (id & uint.MaxValue);
            int id2 = (int) (id >> 32);
            var str = $"{id1} -> {id2}";

            return str;
        }

        public List<(int edgeIdx, int vertIdx)> FindTJunctions(float tolerance = 0.001f)
        {
            // TODO: Look at some spatial lookups?
            var tJunctions = new List<(int, int)>();
            Parallel.ForEach(Edges, (edge) =>
            {
                for (int i = 0; i < Verts.Count; i++) {
                    var vert       = Verts[i];
                    var vert1      = Verts[edge.StartVertIdx].Pos;
                    var vert2      = Verts[Edges[edge.NextEdgeIdx].StartVertIdx].Pos;
                    var distToLine = HandleUtility.DistancePointLine(vert.Pos, vert1, vert2);
                    distToLine = Math.Abs(distToLine);
                    if (distToLine < tolerance && vert1 != vert.Pos && vert2 != vert.Pos) {
                        lock (tJunctions) {
                            var alreadyAdded = false;
                            for (int j = 0; j < tJunctions.Count; j++) {
                                if (tJunctions[j].Item1 == edge.Idx && Verts[tJunctions[j].Item2].Pos == vert.Pos) {
                                    alreadyAdded = true;
                                    break;
                                }
                            }

                            if (!alreadyAdded)
                                tJunctions.Add((edge.Idx, i));
                        }
                    }
                }
            });

            return tJunctions;
        }

        // Split a face by a point on an edge
        // Adjust the existing face and add another one
        public void SplitEdge(int edgeId, Vector3 pos)
        {
            var edge       = Edges[edgeId]; // E2
            var e1         = Edges[edge.PrevEdgeIdx];
            var e3         = Edges[edge.NextEdgeIdx];
            var sourceVert = Verts[Edges[edge.PrevEdgeIdx].StartVertIdx]; // V1
            var mainFace   = Faces[edge.FaceIdx];                         // F1

            // Get uv for point
            var distToPoint = (pos                        - sourceVert.Pos).sqrMagnitude;
            var edgeLength  = (Verts[e3.StartVertIdx].Pos - sourceVert.Pos).sqrMagnitude;
            var dist        = distToPoint / edgeLength;
            var uvDir       = (Verts[edge.StartVertIdx].Uv - sourceVert.Uv).normalized;
            var newUv       = Verts[e3.StartVertIdx].Uv + (uvDir * dist);

            // Add new vert
            var newVertIdx = Verts.Count;
            var newVert = new Vert // V4
            {
                Pos    = pos,
                Uv     = newUv,
                Normal = Verts[mainFace.VertIdx].Normal
            };
            Verts.Add(newVert);

            // Create new edges
            var middleEdgeIdx  = Edges.Count;
            var middleEdge     = new Edge(middleEdgeIdx, mainFace.FaceIdx, newVertIdx, e1.Idx, edge.Idx, middleEdgeIdx + 1);                                                                                                                      // E4
            var middleEdgeTwin = new Edge(middleEdgeIdx                                                                + 1, Faces.Count, Edges[edge.PrevEdgeIdx].StartVertIdx, middleEdgeIdx    + 2, Edges[edge.NextEdgeIdx].Idx, middleEdgeIdx); // E5
            var splitEdge      = new Edge(middleEdgeIdx                                                                + 2, Faces.Count, newVertIdx, Edges[edge.NextEdgeIdx].Idx, middleEdgeIdx + 1);                                             // E6

            Edges.Add(middleEdge);
            Edges.Add(middleEdgeTwin);
            Edges.Add(splitEdge);

            // Update edges
            Edges[e1.Idx].PrevEdgeIdx = middleEdge.Idx;
            Edges[e3.Idx].NextEdgeIdx = middleEdgeTwin.Idx;
            Edges[e3.Idx].PrevEdgeIdx = splitEdge.Idx;
            Edges[edgeId].NextEdgeIdx = middleEdge.Idx;

            // New face
            var newFace = new Face // F2
            {
                FaceIdx      = Faces.Count,
                SubMeshId    = mainFace.SubMeshId,
                VertIdx      = Edges[edge.PrevEdgeIdx].StartVertIdx,
                EdgeStartIdx = splitEdge.Idx
            };
            Faces.Add(newFace);

            // Update old face
            Faces[edge.FaceIdx].EdgeStartIdx = e1.Idx;
        }

        public class Edge
        {
            public int Idx;
            public int NextEdgeIdx;
            public int PrevEdgeIdx;
            public int TwinIdx;
            public int FaceIdx;
            public int StartVertIdx;

            public Edge()
            {
            }

            public Edge(int idx, int faceIdx, int startVertIdx, int nextEdgeIdx, int prevEdgeIdx, int twinIdx = -1)
            {
                Idx          = idx;
                NextEdgeIdx  = nextEdgeIdx;
                PrevEdgeIdx  = prevEdgeIdx;
                TwinIdx      = twinIdx;
                FaceIdx      = faceIdx;
                StartVertIdx = startVertIdx;
            }
        }

        public struct Vert
        {
            public Vector3 Pos;
            public Vector3 Normal;
            public Vector2 Uv;

            public short EdgeIdx;
        }

        public class Face
        {
            public int   FaceIdx;
            public short SubMeshId;
            public int   VertIdx;
            public int   EdgeStartIdx;
        }
    }
}