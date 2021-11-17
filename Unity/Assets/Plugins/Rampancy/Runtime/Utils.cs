using System.Collections.Generic;
using System.Linq;
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
            List<Color32>   colors  = new();

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
    }
}