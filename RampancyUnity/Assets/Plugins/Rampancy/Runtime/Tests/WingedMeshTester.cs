using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Rampancy.Tests
{
    [RequireComponent(typeof(MeshCollider))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteInEditMode]
    public class WingedMeshTester : MonoBehaviour
    {
        public Mesh       SourceMesh;
        public WingedMesh WingedMesh;

        public bool Process          = true;
        public bool ShowFaceHandles  = true;
        public bool ShowFaceCenters  = false;
        public bool AddTestTriToMesh = false;
        public bool ShowOpenEdges    = false;
        public bool ShowTJunctions   = true;
        public bool FixTJunctions    = false;

        private int                                             FaceIdxToDrawInfo = 0;
        private List<WingedMesh.EdgeRef>                        OpenEdges         = new();
        private Dictionary<int, List<WingedMesh.TJunctionInfo>> TJunctions        = new();

        public WingedMeshTester()
        {
            Process = true;
        }

        public void OnEnable()
        {
            Process = true;
        }

        public void OnDrawGizmos()
        {
            if (Process) {
                Process = false;

                ProcessMesh();
            }

            if (ShowFaceHandles) {
                DrawFaceHandles(ShowFaceCenters);
            }
        }

        private unsafe void ProcessMesh()
        {
            WingedMesh = new WingedMesh();
            WingedMesh.FromUnityMesh(SourceMesh);

            // Add a tri
            if (AddTestTriToMesh) {
                AddTestTri();
            }
            
            OpenEdges  = WingedMesh.FindOpenTris();
            TJunctions = WingedMesh.FindTJunctions();
            
            if (FixTJunctions) WingedMesh.FixTJunctions(TJunctions);

            var mf = GetComponent<MeshFilter>();
            mf.sharedMesh = WingedMesh.ToUnityMesh();

            var collider = GetComponent<MeshCollider>();
            collider.sharedMesh = mf.sharedMesh;

            /*Debug.Log($"Mesh: {name}");
            for (int i = 0; i < OpenEdges.Count; i++) {
                var tri = WingedMesh.Triangles.Array[OpenEdges[i].FaceIdx];
                Debug.Log($"Edge: Face: {OpenEdges[i].FaceIdx}, Vert1: {tri.VertIdx[OpenEdges[i].Vert1Idx]}, Vert2: {tri.VertIdx[OpenEdges[i].Vert2Idx]}");
            }*/
            
            Debug.Log($"Mesh: {name}");
            foreach (var kvp in TJunctions) {
                foreach (var tj in kvp.Value) {
                    Debug.Log($"TJunction: Face: {tj.FaceIdx}, Edge: {tj.Edge.Vert1Idx} {tj.Edge.Vert2Idx}, Spliting Vert: {tj.SplitingVertIdx}");
                }
            }
            
            /*for (int i = 0; i < TJunctions.Count; i++) {
                var tj = TJunctions[i];
                Debug.Log($"TJunction: Face: {tj.FaceIdx}, Edge: {tj.Edge.Vert1Idx} {tj.Edge.Vert2Idx}, Spliting Vert: {tj.SplitingVertIdx}");
            }*/
        }

        private void AddTestTri()
        {
            var vertIdx1 = WingedMesh.AddVert(WingedMesh.Vert_Positions[11], Vector3.up, Vector2.zero);
            //var vertIdx2 = WingedMesh.AddVert(new Vector3(22, -1.00071f, 2), Vector3.up, Vector2.zero);
            var vertIdx3 = WingedMesh.AddVert(new Vector3(23, -1.00071f, 2), Vector3.up, Vector2.zero);
            WingedMesh.AddTri(vertIdx1, 10, vertIdx3, 0);
        }

        private unsafe void DrawFaceHandles(bool showFaceCenters = true)
        {
            if (WingedMesh == null) return;

            if (ShowFaceHandles) {
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                if (Physics.Raycast(ray, out var hit)) {
                    FaceIdxToDrawInfo = hit.triangleIndex;

                    if (hit.transform != transform) {
                        FaceIdxToDrawInfo = -1;
                    }
                }

                if (FaceIdxToDrawInfo != -1 && FaceIdxToDrawInfo < WingedMesh.Triangles.Count) {
                    var tri    = WingedMesh.Triangles.Array[FaceIdxToDrawInfo];
                    var center = transform.position + tri.GetCenter(WingedMesh);

                    var height = 0.5f;
                    if (tri.NextTriIdx[0] != -1) DrawFaceLink(tri.Id, tri.NextTriIdx[0], Color.magenta, height);
                    if (tri.NextTriIdx[1] != -1) DrawFaceLink(tri.Id, tri.NextTriIdx[1], Color.yellow, height);
                    if (tri.NextTriIdx[2] != -1) DrawFaceLink(tri.Id, tri.NextTriIdx[2], Color.green, height);

                    for (int i = 0; i < 3; i++) {
                        Handles.Label(WingedMesh.Vert_Positions[tri.VertIdx[i]], $"{tri.VertIdx[i]}");
                    }

                    var screenPos = HandleUtility.WorldToGUIPoint(center);

                    Handles.BeginGUI();
                    GUI.Label(new Rect(screenPos, new Vector2(100, 100)), $"F: {tri.Id}");
                    GUI.contentColor = Color.magenta;
                    GUI.Label(new Rect(screenPos + new Vector2(0, 20), new Vector2(200, 100)), $"NT1: {tri.NextTriIdx[0]}");
                    GUI.contentColor = Color.yellow;
                    GUI.Label(new Rect(screenPos + new Vector2(0, 40), new Vector2(200, 100)), $"NT2: {tri.NextTriIdx[1]}");
                    GUI.contentColor = Color.green;
                    GUI.Label(new Rect(screenPos + new Vector2(0, 60), new Vector2(200, 100)), $"NT3: {tri.NextTriIdx[2]}");

                    GUI.contentColor = Color.white;
                    GUI.Label(new Rect(screenPos + new Vector2(0, 80), new Vector2(100, 100)), $"Verts: {tri.VertIdx[0]}, {tri.VertIdx[1]}, {tri.VertIdx[2]}");
                    Handles.EndGUI();
                }
            }

            if (showFaceCenters) {
                for (var index = 0; index < WingedMesh.Triangles.Count; index++) {
                    var tri    = WingedMesh.Triangles.Array[index];
                    var center = transform.position + tri.GetCenter(WingedMesh);

                    Handles.color = index == FaceIdxToDrawInfo ? Color.blue : Color.white;
                    Handles.DrawWireCube(center, Vector3.one * 0.005f);
                }
            }

            if (ShowOpenEdges) {
                for (int i = 0; i < OpenEdges.Count; i++) {
                    DrawEdge(OpenEdges[i]);
                }
            }

            if (ShowTJunctions) {
                foreach (var kvp in TJunctions) {
                    foreach (var tj in kvp.Value) {
                        DrawTJunction(tj);
                    }
                }
            }
        }

        private void DrawFaceLink(int face1Idx, int face2Idx, Color color, float height)
        {
            var tri1 = WingedMesh.Triangles.Array[face1Idx];
            var tri2 = WingedMesh.Triangles.Array[face2Idx];

            var center1 = transform.position + tri1.GetCenter(WingedMesh);
            var center2 = transform.position + tri2.GetCenter(WingedMesh);

            Handles.DrawBezier(center1, center2, center1 + (tri1.GetNormal(WingedMesh) * height), center2 + (tri2.GetNormal(WingedMesh) * height), color, Texture2D.grayTexture, 4);
        }

        private unsafe void DrawEdge(WingedMesh.EdgeRef edgeRef)
        {
            var tri = WingedMesh.Triangles.Array[edgeRef.FaceIdx];
            var (v1, v2) = WingedMesh.GetFullEdgePositions(edgeRef);
            Gizmos.DrawSphere(transform.position + v1, 0.02f);
            Gizmos.DrawSphere(transform.position + v2, 0.02f);
            Gizmos.DrawLine(transform.position  + v1, transform.position + v2);
        }

        private void DrawTJunction(WingedMesh.TJunctionInfo tJunctionInfo)
        {
            Gizmos.color = Color.green;
            DrawEdge(tJunctionInfo.Edge);
            
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position + WingedMesh.Vert_Positions[tJunctionInfo.SplitingVertIdx], 0.02f);
        }
    }
}