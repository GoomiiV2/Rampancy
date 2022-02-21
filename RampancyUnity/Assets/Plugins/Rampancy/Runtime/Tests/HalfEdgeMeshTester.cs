using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Rampancy.Tests
{
    public class HalfEdgeMeshTester : MonoBehaviour
    {
        public Mesh     Mesh;
        public HalfMesh HalfEdgeMesh;

        public float LabelDrawDist = 10.0f;

        public int  EdgeToDraw   = 0;
        public bool DrawNextEdge = false;
        public bool DrawPrevEdge = false;
        public bool GoToTwinEdge = false;

        public int FaceToDraw = 0;

        public bool             RefreshTJunctionFinder = false;
        public bool             DrawTJunctions         = true;
        public List<(int, int)> TJunctonsList          = new();

        public void Awake()
        {
        }

        private void OnDrawGizmos() //OnRenderObject()
        {
            /*for (var index = 0; index < HalfEdgeMesh.Verts.Count; index++) {
                var vert = HalfEdgeMesh.Verts[index];
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(vert.Pos, 0.1f);
                Handles.Label(vert.Pos, $"{index}");
            }*/

            /*for (var index = 0; index < HalfEdgeMesh.Edges.Count; index++) {
                var halfEdge = HalfEdgeMesh.Edges[index];
                
                Gizmos.color = Color.red;
                Gizmos.DrawLine(HalfEdgeMesh.Verts[halfEdge.StartVertIdx].Pos, HalfEdgeMesh.Verts[HalfEdgeMesh.Edges[halfEdge.NextEdgeIdx].StartVertIdx].Pos);
            }*/

            if (HalfEdgeMesh == null) return;

            if (EdgeToDraw != -1) {
                Gizmos.color = Color.red;
                var edge     = HalfEdgeMesh.Edges[EdgeToDraw];
                var nextEdge = HalfEdgeMesh.Edges[edge.NextEdgeIdx];
                var fromVert = HalfEdgeMesh.Verts[edge.StartVertIdx];
                var toVert   = HalfEdgeMesh.Verts[nextEdge.StartVertIdx];
                Gizmos.DrawLine(fromVert.Pos, toVert.Pos);

                Handles.Label(fromVert.Pos, $"{edge.StartVertIdx}");
                Handles.Label(toVert.Pos, $"{nextEdge.StartVertIdx}");
            }

            DrawFace(FaceToDraw);

            if (DrawNextEdge && EdgeToDraw != -1) {
                //StartCoroutine(DrawFaceEdges());

                var edge = HalfEdgeMesh.Edges[EdgeToDraw];
                EdgeToDraw   = edge.NextEdgeIdx;
                DrawNextEdge = false;
            }

            if (DrawPrevEdge && EdgeToDraw != -1) {
                var edge = HalfEdgeMesh.Edges[EdgeToDraw];
                EdgeToDraw   = edge.PrevEdgeIdx;
                DrawPrevEdge = false;
            }

            if (GoToTwinEdge) {
                EdgeToDraw   = HalfEdgeMesh.Edges[EdgeToDraw].TwinIdx;
                GoToTwinEdge = false;
            }

            if (RefreshTJunctionFinder) {
                TJunctonsList.Clear();

                var tJunctionTime = Stopwatch.StartNew();
                TJunctonsList = HalfEdgeMesh.FindTJunctions();
                tJunctionTime.Stop();
                Debug.Log($"Finding T-Junctions took: {tJunctionTime.Elapsed}");

                RefreshTJunctionFinder = false;

                foreach (var tJunction in TJunctonsList) {
                    Debug.Log($"E: {tJunction.Item1}, V: {tJunction.Item2}");

                    HalfEdgeMesh.SplitEdge(tJunction.Item1, HalfEdgeMesh.Verts[tJunction.Item2].Pos);
                }
            }

            if (DrawTJunctions)
                foreach (var tJunction in TJunctonsList) {
                    var tVert = HalfEdgeMesh.Verts[tJunction.Item2];
                    var tEdge = HalfEdgeMesh.Edges[tJunction.Item1];
                    Gizmos.DrawSphere(tVert.Pos, 0.01f);
                    DrawEdge(tEdge, false);
                    var camPos = SceneView.currentDrawingSceneView.camera.transform.position;
                    if (Vector3.Distance(camPos, tVert.Pos) < LabelDrawDist) Handles.Label(tVert.Pos, $"E: {tJunction.Item1}, v: {tJunction.Item2}\nPos: {tVert.Pos}\nNorm: {tVert.Normal}\nUv: {tVert.Uv}\nEdgeIdx: {tVert.EdgeIdx}");
                }
        }

        public void DrawFace(int faceIdx)
        {
            var face       = HalfEdgeMesh.Faces[faceIdx];
            var edgeIdx    = face.EdgeStartIdx;
            var faceCenter = Vector3.zero;
            var numEdges   = 0;
            do {
                var edge = HalfEdgeMesh.Edges[edgeIdx];
                Gizmos.color = Color.blue;
                DrawEdge(edge);
                faceCenter += HalfEdgeMesh.Verts[edge.StartVertIdx].Pos;
                edgeIdx    =  HalfEdgeMesh.Edges[edgeIdx].NextEdgeIdx;
                numEdges++;
            } while (edgeIdx != face.EdgeStartIdx);

            faceCenter = faceCenter / numEdges;

            var camPos = SceneView.currentDrawingSceneView.camera.transform.position;
            if (Vector3.Distance(camPos, faceCenter) < LabelDrawDist) Handles.Label(faceCenter, $"F: {faceIdx}\nEidx: {edgeIdx}\nVidx: {face.VertIdx}");
        }

        public void DrawEdge(HalfMesh.Edge edge, bool drawLabel = true)
        {
            var nextEdge = HalfEdgeMesh.Edges[edge.NextEdgeIdx];
            var fromVert = HalfEdgeMesh.Verts[edge.StartVertIdx];
            var toVert   = HalfEdgeMesh.Verts[nextEdge.StartVertIdx];
            Gizmos.DrawLine(fromVert.Pos, toVert.Pos);

            var center = (fromVert.Pos + toVert.Pos) / 2;
            var camPos = SceneView.currentDrawingSceneView.camera.transform.position;
            if (drawLabel && Vector3.Distance(camPos, center) < LabelDrawDist) Handles.Label(center, $"E: {nextEdge.PrevEdgeIdx}");
        }
    }
}