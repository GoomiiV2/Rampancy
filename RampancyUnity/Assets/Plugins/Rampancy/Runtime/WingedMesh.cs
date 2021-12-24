using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace Plugins.Rampancy.Runtime
{
    // This is abit flawed so i'd like to redo it at some point
    // Maybe have faces have an array of edges for the links
    public class WingedMesh
    {
        public string                    Name;
        public List<Vector3>             Vert_Positions;
        public List<Vector3>             Vert_Normals;
        public List<Vector2>             Vert_Uvs;
        public ResizeableArray<Triangle> Triangles;

        // Edge to tri mapping
        private Dictionary<Edge, int> ConnectivityMap = new();

        public unsafe void FromUnityMesh(Mesh mesh)
        {
            Profiler.BeginSample("Winged Mesh From Unity Mesh");
            var timer = Stopwatch.StartNew();

            ConnectivityMap = new(mesh.triangles.Length);

            // Verts
            Profiler.BeginSample("Winged Mesh From Unity Mesh, Verts");
            Vert_Positions = new(mesh.vertices);
            Vert_Normals   = new(mesh.normals);
            Vert_Uvs       = new(mesh.uv);

            Profiler.EndSample();
            Debug.Log($"Converting mesh verts: {timer.Elapsed}");

            // Tris
            Profiler.BeginSample("Winged Mesh From Unity Mesh, Triangles");
            var triIdxs = mesh.triangles;
            Triangles = new(triIdxs.Length / 3);
            var numSubMeshes = mesh.subMeshCount;
            int faceIdx      = 0;
            for (int subMeshIdx = 0; subMeshIdx < numSubMeshes; subMeshIdx++) {
                var subMesh = mesh.GetSubMesh(subMeshIdx);
                for (var triIdx = subMesh.indexStart; triIdx < subMesh.indexStart + subMesh.indexCount; triIdx += 3) {
                    var idx1 = triIdxs[triIdx];
                    var idx2 = triIdxs[triIdx + 1];
                    var idx3 = triIdxs[triIdx + 2];

                    var area = Utils.CalcAreaOfTri(Vert_Positions[idx1], Vert_Positions[idx2], Vert_Positions[idx3]);
                    if (area != 0f) {
                        ConnectivityMap.Add(Edge.Create(Vert_Positions[idx2], Vert_Positions[idx1]), faceIdx);
                        ConnectivityMap.Add(Edge.Create(Vert_Positions[idx3], Vert_Positions[idx2]), faceIdx);
                        ConnectivityMap.Add(Edge.Create(Vert_Positions[idx1], Vert_Positions[idx3]), faceIdx);

                        Triangles.Array[faceIdx].Id            = faceIdx;
                        Triangles.Array[faceIdx].SubMeshId     = (ushort) subMeshIdx;
                        Triangles.Array[faceIdx].VertIdx[0]    = idx1;
                        Triangles.Array[faceIdx].VertIdx[1]    = idx2;
                        Triangles.Array[faceIdx].VertIdx[2]    = idx3;
                        Triangles.Array[faceIdx].NextTriIdx[0] = ConnectivityMap.TryGetValue(Edge.Create(Vert_Positions[idx1], Vert_Positions[idx2]), out var connectedTri1) ? connectedTri1 : -1;
                        Triangles.Array[faceIdx].NextTriIdx[1] = ConnectivityMap.TryGetValue(Edge.Create(Vert_Positions[idx2], Vert_Positions[idx3]), out var connectedTri2) ? connectedTri2 : -1;
                        Triangles.Array[faceIdx].NextTriIdx[2] = ConnectivityMap.TryGetValue(Edge.Create(Vert_Positions[idx3], Vert_Positions[idx1]), out var connectedTri3) ? connectedTri3 : -1;

                        for (int i = 0; i < 3; i++) {
                            if (Triangles.Array[faceIdx].NextTriIdx[i] != -1) { // This face was connected
                                for (int j = 0; j < 3; j++) {
                                    if (Triangles.Array[Triangles.Array[faceIdx].NextTriIdx[i]].NextTriIdx[j] == -1) {
                                        Triangles.Array[Triangles.Array[faceIdx].NextTriIdx[i]].NextTriIdx[j] = Triangles.Array[faceIdx].Id;
                                        break;
                                    }
                                }
                            }
                        }

                        faceIdx++;
                    }
                    else {
                        Triangles.Array[faceIdx].Id = -1; // Blank
                    }
                }
            }

            timer.Stop();
            Profiler.EndSample();

            Debug.Log($"Converting mesh took: {timer.Elapsed}, {Vert_Positions.Count} Verts, {Triangles.Count} Tris");
            Profiler.EndSample();
        }

        public unsafe Mesh ToUnityMesh()
        {
            Profiler.BeginSample("Winged Mesh To Unity Mesh");
            var timer = Stopwatch.StartNew();
            var mesh  = new Mesh();

            mesh.SetVertices(Vert_Positions);
            mesh.SetNormals(Vert_Normals);
            mesh.SetUVs(0, Vert_Uvs);

            // Some gc should be ok here, this is going to be on exporting, I hope :>
            var subMeshes = Triangles.Array.Take(Triangles.Count).GroupBy(x => x.SubMeshId);

            foreach (var subMesh in subMeshes) {
                mesh.subMeshCount++;

                var indices    = new int[subMesh.Count() * 3];
                var indicesIdx = 0;
                foreach (var tri in subMesh) {
                    if (tri.Id != -1) {
                        indices[indicesIdx++] = tri.VertIdx[0];
                        indices[indicesIdx++] = tri.VertIdx[1];
                        indices[indicesIdx++] = tri.VertIdx[2];
                    }
                }

                mesh.SetTriangles(indices, subMesh.Key);
            }

            timer.Stop();
            Profiler.EndSample();
            Debug.Log($"Converting mesh to Unity took: {timer.Elapsed}");

            return mesh;
        }

        public int AddVert(Vector3 pos, Vector3 normal, Vector2 uv)
        {
            Vert_Positions.Add(pos);
            Vert_Normals.Add(normal);
            Vert_Uvs.Add(uv);

            return Vert_Positions.Count - 1;
        }

        public unsafe int AddTri(int v1, int v2, int v3, ushort subMeshId, int nextTri1 = -1, int nextTri2 = -1, int nextTri3 = -1, bool addEdgesToConnectivityMap = true)
        {
            var tri = new Triangle
            {
                Id        = Triangles.Count,
                SubMeshId = subMeshId
            };

            if (addEdgesToConnectivityMap) {
                ConnectivityMap.Add(Edge.Create(Vert_Positions[v2], Vert_Positions[v1]), tri.Id);
                ConnectivityMap.Add(Edge.Create(Vert_Positions[v3], Vert_Positions[v2]), tri.Id);
                ConnectivityMap.Add(Edge.Create(Vert_Positions[v1], Vert_Positions[v3]), tri.Id);
            }

            tri.VertIdx[0] = v1;
            tri.VertIdx[1] = v2;
            tri.VertIdx[2] = v3;

            tri.NextTriIdx[0] = nextTri1 == -1 ? (ConnectivityMap.TryGetValue(Edge.Create(Vert_Positions[v1], Vert_Positions[v2]), out var connectedTri1) ? connectedTri1 : -1) : nextTri1;
            tri.NextTriIdx[1] = nextTri2 == -1 ? (ConnectivityMap.TryGetValue(Edge.Create(Vert_Positions[v2], Vert_Positions[v3]), out var connectedTri2) ? connectedTri2 : -1) : nextTri2;
            tri.NextTriIdx[2] = nextTri3 == -1 ? (ConnectivityMap.TryGetValue(Edge.Create(Vert_Positions[v3], Vert_Positions[v1]), out var connectedTri3) ? connectedTri3 : -1) : nextTri3;

            for (int i = 0; i < 3; i++) {
                if (tri.NextTriIdx[i] != -1) {
                    for (int j = 0; j < 3; j++) {
                        if (Triangles.Array[tri.NextTriIdx[i]].NextTriIdx[j] == -1) {
                            Triangles.Array[tri.NextTriIdx[i]].NextTriIdx[j] = tri.Id;
                            break;
                        }
                    }
                }
            }

            Triangles.Add(tri);
            return tri.Id;
        }

        public unsafe (Vector3 V1, Vector3 V2) GetFullEdgePositions(EdgeRef edge)
        {
            return (Vert_Positions[Triangles.Array[edge.FaceIdx].VertIdx[edge.Vert1Idx]],
                Vert_Positions[Triangles.Array[edge.FaceIdx].VertIdx[edge.Vert2Idx]]);
        }

        // find any edge of a face that isn't linked ot an other face
        public unsafe List<EdgeRef> FindOpenTris()
        {
            var openEdges = new List<EdgeRef>();
            for (int i = 0; i < Triangles.Count; i++) { // All the tris
                for (int j = 0; j < 3; j++) {           // Connections
                    if (Triangles.Array[i].NextTriIdx[j] == -1) {
                        var edges = new EdgeRef[]
                        {
                            new() {FaceIdx = i, Vert1Idx = 0, Vert2Idx = 1},
                            new() {FaceIdx = i, Vert1Idx = 1, Vert2Idx = 2},
                            new() {FaceIdx = i, Vert1Idx = 2, Vert2Idx = 0}
                        };

                        for (int k = 0; k < edges.Length; k++) {
                            var vert1Idx = Triangles.Array[i].VertIdx[edges[k].Vert1Idx];
                            var vert2Idx = Triangles.Array[i].VertIdx[edges[k].Vert2Idx];
                            if (!ConnectivityMap.ContainsKey(Edge.Create(Vert_Positions[vert1Idx], Vert_Positions[vert2Idx]))) {
                                openEdges.Add(edges[k]);
                            }
                        }

                        break;
                    }
                }
            }

            return openEdges;
        }

        // Find an open edge that has a vert from another face sitting on it but not split
        // This can be improved
        public unsafe Dictionary<int, List<TJunctionInfo>> FindTJunctions(float tolerance = 0.01f)
        {
            var tJunctions = new List<TJunctionInfo>();
            var openEdges  = FindOpenTris();

            Parallel.ForEach(openEdges, (edge) =>
            {
                var edgePositions = GetFullEdgePositions(edge);
                for (int i = 0; i < Vert_Positions.Count; i++) {
                    var distToLine = HandleUtility.DistancePointLine(Vert_Positions[i], edgePositions.V1, edgePositions.V2);
                    if (distToLine < tolerance && (Vert_Positions[i] != edgePositions.V1 && Vert_Positions[i] != edgePositions.V2)) {
                        var tJunctionInfo = new TJunctionInfo
                        {
                            FaceIdx         = edge.FaceIdx,
                            Edge            = edge,
                            SplitingVertIdx = i
                        };

                        lock (tJunctions) {
                            tJunctions.Add(tJunctionInfo);
                        }
                    }
                }
            });

            Dictionary<int, List<TJunctionInfo>> groupedTJunctions = new();
            for (int i = 0; i < tJunctions.Count; i++) {
                if (!groupedTJunctions.ContainsKey(tJunctions[i].FaceIdx)) groupedTJunctions.Add(tJunctions[i].FaceIdx, new List<TJunctionInfo>());

                groupedTJunctions[tJunctions[i].FaceIdx].Add(tJunctions[i]);
            }

            var keys = groupedTJunctions.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++) {
                groupedTJunctions[keys[i]] = groupedTJunctions[keys[i]].DistinctBy(x => Vert_Positions[x.SplitingVertIdx])
                                                                       .OrderBy(x => x.Edge.Vert1Idx)
                                                                       .ThenBy(x => Vector3.Distance(Vert_Positions[Triangles.Array[x.FaceIdx].VertIdx[x.Edge.Vert1Idx]], Vert_Positions[x.SplitingVertIdx]))
                                                                       .ToList();
            }

            return groupedTJunctions;
        }

        // Breaks connectivity atm :<
        public unsafe void FixTJunctions(Dictionary<int, List<TJunctionInfo>> tJunctions)
        {
            foreach (var faceTJs in tJunctions) {
                var perEdge              = faceTJs.Value.GroupBy(x => x.Edge.Vert1Idx);
                var faceIdx              = faceTJs.Key;
                var fistFaceAdded        = -1;
                var edgeNum              = 0;
                var lastSplittingVertPos = Vector3.zero;
                foreach (var edgeGroup in perEdge) {
                    var tjs = edgeGroup;

                    if (edgeNum == 2) {
                        faceIdx = faceTJs.Key;
                    }

                    foreach (var tj in tjs) {
                        if (lastSplittingVertPos != Vert_Positions[tj.SplitingVertIdx]) {
                            //Debug.Log($"Fixing t-junction for face: {faceTJs.Key}, edge: {tj.Edge.Vert1Idx} -> {tj.Edge.Vert2Idx}, ({edgeNum})");
                            faceIdx = SplitFaceOnEdge(faceIdx, tj.Edge, tj.SplitingVertIdx, edgeNum == 2);
                            //Debug.Log($"New face: {faceIdx}, ({Triangles.Array[faceIdx].VertIdx[0]}, {Triangles.Array[faceIdx].VertIdx[1]}, {Triangles.Array[faceIdx].VertIdx[2]})");

                            if (fistFaceAdded == -1) fistFaceAdded = faceIdx;
                        }

                        lastSplittingVertPos = Vert_Positions[tj.SplitingVertIdx];
                    }

                    edgeNum++;
                }
            }
        }

        // Add a new vert at the splitting verts position
        // Adjust the existing face to move from one edge to the newly added vert
        // Create a new tri to fill the gap and return the idx of that face
        public unsafe int SplitFaceOnEdge(int face, EdgeRef edge, int splittingVert, bool flipFace = false)
        {
            var rootVertIdx = 0;
            if (edge.Vert1Idx      == 0 && edge.Vert2Idx == 1) rootVertIdx = 2;
            else if (edge.Vert1Idx == 1 && edge.Vert2Idx == 2) rootVertIdx = 0;
            else if (edge.Vert1Idx == 2 && edge.Vert2Idx == 0) rootVertIdx = 1;

            var originalFaceNorm = Triangles.Array[face].GetNormal(this);
            var oldVertIdx       = Triangles.Array[face].VertIdx[edge.Vert2Idx];
            var newUvs           = GetUvsForPointOnEdge(Triangles.Array[face].VertIdx[edge.Vert1Idx], Triangles.Array[face].VertIdx[edge.Vert2Idx], splittingVert);
            var newVertIdx       = AddVert(Vert_Positions[splittingVert], Vert_Normals[splittingVert], newUvs);

            // Update the existing face
            Triangles.Array[face].VertIdx[edge.Vert2Idx] = newVertIdx;

            // Add the new face to fill in the hole
            int[] indices = rootVertIdx switch
            {
                0 => new[] {Triangles.Array[face].VertIdx[rootVertIdx], newVertIdx, oldVertIdx},
                1 => new[] {newVertIdx, Triangles.Array[face].VertIdx[rootVertIdx], oldVertIdx},
                2 => new[] {newVertIdx, oldVertIdx, Triangles.Array[face].VertIdx[rootVertIdx]}
            };

            var newFaceN    = GetTriNormal(indices[0], indices[1], indices[2]);
            var normsDiffer = Math.Abs(Vector3.Distance(originalFaceNorm, newFaceN)) > 0.1f;

            if (normsDiffer) {
                (indices[0], indices[2]) = (indices[2], indices[0]);
            }

            var newTriArea = Utils.CalcAreaOfTri(Vert_Positions[indices[0]], Vert_Positions[indices[1]], Vert_Positions[indices[2]]);
            Debug.Log($"faceIdx: {face}, newTriArea: {newTriArea}, did norms differ: {normsDiffer}, edge: {edge.Vert1Idx}, {edge.Vert2Idx}, rootVertIdx: {rootVertIdx} splittingVert: {splittingVert} ({Vert_Positions[splittingVert]})");

            if (newTriArea == 0) {
                Triangles.Array[face].VertIdx[edge.Vert2Idx] = oldVertIdx;
                return face;
            }

            var newFaceIdx = AddTri(indices[0], indices[1], indices[2], Triangles.Array[face].SubMeshId, addEdgesToConnectivityMap: false);

            return newFaceIdx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetTriNormal(int vi1, int vi2, int vi3) => Utils.GetTriNormal(Vert_Positions[vi1], Vert_Positions[vi2], Vert_Positions[vi3]);

        public Vector2 GetUvsForPointOnEdge(int v1, int v2, int splittingVert)
        {
            var edgeLen     = Mathf.Abs(Vector3.Distance(Vert_Positions[v1], Vert_Positions[v2]));
            var distToPoint = Mathf.Abs(Vector3.Distance(Vert_Positions[v1], Vert_Positions[splittingVert]));
            var pct         = distToPoint / edgeLen;
            var newUv       = Vector2.Lerp(Vert_Uvs[v1], Vert_Uvs[v2], pct);

            return newUv;
        }

    #region Types

        public unsafe struct Triangle
        {
            public       int    Id;
            public       ushort SubMeshId;
            public fixed int    VertIdx[3];
            public fixed int    NextTriIdx[3];

            // Get the center point of the tri
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 GetCenter(WingedMesh mesh) => (mesh.Vert_Positions[VertIdx[0]] + mesh.Vert_Positions[VertIdx[1]] + mesh.Vert_Positions[VertIdx[2]]) / 3;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector3 GetNormal(WingedMesh mesh)
            {
                var v0 = mesh.Vert_Positions[VertIdx[1]] - mesh.Vert_Positions[VertIdx[0]];
                var v1 = mesh.Vert_Positions[VertIdx[2]] - mesh.Vert_Positions[VertIdx[0]];
                var n  = Vector3.Cross(v0, v1);

                return n.normalized;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetArea(WingedMesh mesh) => Utils.CalcAreaOfTri(mesh.Vert_Positions[VertIdx[0]], mesh.Vert_Positions[VertIdx[1]], mesh.Vert_Positions[VertIdx[2]]);
        }

        public struct Edge
        {
            public Vector3 V1;
            public Vector3 V2;

            public static Edge Create(Vector3 v1, Vector3 v2)
            {
                var edge = new Edge
                {
                    V1 = v1,
                    V2 = v2
                };

                return edge;
            }

            public override bool Equals(object obj)
            {
                const float TOLRANCE = 0.00000001f;
                if (obj is not Edge other) return false;
                var equal = Vector3.Distance(other.V1, V1) < TOLRANCE && Vector3.Distance(other.V2, V2) < TOLRANCE;
                return equal;
            }
        }

        public struct EdgeRef
        {
            public int FaceIdx;
            public int Vert1Idx;
            public int Vert2Idx;
        }

        public struct TJunctionInfo
        {
            public int     FaceIdx;
            public EdgeRef Edge;
            public int     SplitingVertIdx;
        }

    #endregion
    }
}