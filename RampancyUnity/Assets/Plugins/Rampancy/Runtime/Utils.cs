using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Plugins.Rampancy.RampantC20;
using Rampancy.RampantC20;
using UnityEngine;

namespace RampantC20
{
    public static partial class Utils
    {
        public static Mesh TrisToMesh(List<DebugGeoData.Item> items)
        {
            Mesh          mesh    = new Mesh();
            List<Vector3> verts   = new();
            List<int>     indices = new();
            List<Color32> colors  = new();

            foreach (var item in items) {
                foreach (var vert in item.Verts) {
                    var scale = new Vector3(Statics.ExportScale, -Statics.ExportScale, Statics.ExportScale);
                    var rot   = Quaternion.Euler(new Vector3(-90, 0, 0));
                    verts.Add(rot * Vector3.Scale(scale,vert));
                }
                
                var nubIndices = indices.Count;
                var newIndices = item.Indices.Select(x => nubIndices + x).ToList();
                indices.AddRange(newIndices);
                for (int i = 0; i < item.Verts.Count; i++) {
                    colors.Add(item.Color);
                }
            }

            mesh.subMeshCount = 1;
            mesh.SetVertices(verts);
            mesh.SetTriangles(indices, 0);
            mesh.SetColors(colors);

            mesh.RecalculateNormals();

            return mesh;
        }
        
        public static string GetProjectRelPath(AssetDb.TagInfo tagInfo, AssetDb assetDb)
        {
            var basse         = $"Assets/{Plugins.Rampancy.Runtime.Rampancy.Config.GameVersion}/TagData";
            var assetBasePath = basse + assetDb.GetBaseTagPath(tagInfo);
            assetBasePath = Path.Combine(Path.GetDirectoryName(assetBasePath), Path.GetFileNameWithoutExtension(assetBasePath));
            return assetBasePath;
        }

        public static Texture2D BitMapToTex2D(int width, int height, byte[] pixels)
        {
            var tex = new Texture2D(width, height, TextureFormat.BGRA32, true);
            tex.SetPixelData(pixels, 0);
            //if (width % 4 == 0 && height % 4 == 0)
            //tex.Compress(true);
            tex.Apply(true);

            return tex;
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

        public static bool IsDegenerateTri(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            const float TOLERANCE = 0.0001f;
            var         area     = CalcAreaOfTri(v1, v2, v3);

            if (area <= TOLERANCE ||
                Math.Abs(Vector3.Distance(v1, v2)) <= TOLERANCE ||
                Math.Abs(Vector3.Distance(v1, v3)) <= TOLERANCE ||
                Math.Abs(Vector3.Distance(v2, v3)) <= TOLERANCE) {
                return true;
            }

            return false;
        }
    }
}