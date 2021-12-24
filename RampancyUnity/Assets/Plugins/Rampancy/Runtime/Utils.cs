using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Plugins.Rampancy.RampantC20;
using UnityEngine;
using Color = System.Drawing.Color;

namespace Plugins.Rampancy.Runtime
{
    public static class Utils
    {
        public static Mesh TrisToMesh(List<DebugGeoData.Item> items)
        {
            Mesh          mesh    = new Mesh();
            List<Vector3> verts   = new();
            List<int>     indices = new();
            List<Color32> colors  = new();

            foreach (var item in items) {
                verts.AddRange(item.Verts);
                var nubIndices = indices.Count;
                var newIndices = item.Indices.Select(x => nubIndices + x).ToList();
                indices.AddRange(newIndices);
                for (int i = 0; i < item.Verts.Count; i++) {
                    colors.Add(item.Color);
                }
            }

            mesh.subMeshCount = 1;
            mesh.SetVertices(verts);
            //mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.SetTriangles(indices, 0);
            mesh.SetColors(colors);

            mesh.RecalculateNormals();

            return mesh;
        }

        public static int GetHashCodeForVector3(Vector3 vector3)
        {
            int hash = vector3.x.GetHashCode();
            hash = CombineHashCodes(hash, vector3.y.GetHashCode());
            hash = CombineHashCodes(hash, vector3.z.GetHashCode());
            return hash;
        }

        public static int CombineHashCodes(int h1, int h2)
        {
            return (((h1 << 5) + h1) ^ h2);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetTriNormal(Vector3 vi1, Vector3 vi2, Vector3 vi3)
        {
            var v0 = vi2 - vi1;
            var v1 = vi3 - vi1;
            var n  = Vector3.Cross(v0, v1);

            return n.normalized;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalcAreaOfTri(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            var l1 = Vector3.Distance(v1, v2);
            var l2 = Vector3.Distance(v1, v3);
            var l3 = Vector3.Distance(v2, v3);

            var semiPerm          = (l1 + l2 + l3) / 2;
            var areaToBeSqrRooted = semiPerm       * (semiPerm - l1) * (semiPerm - l2) * (semiPerm - l3);
            var area              = Mathf.Sqrt(areaToBeSqrRooted);

            return area;
        }
    }
}