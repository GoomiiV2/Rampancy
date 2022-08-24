using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Pfim;
using Rampancy;
using Rampancy.Common;
using RampantC20.Halo3;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using RampancyInst = Rampancy.Rampancy;

namespace RampantC20
{
    public static partial class Utils
    {
        public static Mesh TrisToMesh(List<DebugGeoMarker> items)
        {
            var           mesh    = new Mesh();
            List<Vector3> verts   = new();
            List<int>     indices = new();
            List<Color32> colors  = new();

            foreach (var item in items) {
                foreach (var vert in item.Verts) {
                    var scale = new Vector3(Statics.ExportScale, -Statics.ExportScale, Statics.ExportScale);
                    var rot   = Quaternion.Euler(new Vector3(-90, 0, 0));
                    verts.Add(rot * Vector3.Scale(scale, vert));
                }

                var nubIndices = indices.Count;
                var newIndices = item.Indices.Select(x => nubIndices + x).ToList();
                indices.AddRange(newIndices);
                for (var i = 0; i < item.Verts.Count; i++) colors.Add(item.Color);
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
            var tagBaseDir    = $"Assets/{RampancyInst.Cfg.GameVersion}/TagData";
            var assetBasePath = tagBaseDir + tagInfo.Path.Replace(Path.GetFullPath(RampancyInst.Cfg.ActiveGameConfig.TagsPath), "").TrimEnd('/', '\\');
            ;
            assetBasePath = Path.Combine(Path.GetDirectoryName(assetBasePath), Path.GetFileNameWithoutExtension(assetBasePath));
            return assetBasePath;
        }

        public static string GetProjectRelPath(string tagPath, GameVersions? version = null, string unityBaseDir = null)
        {
            var ver     = version ?? RampancyInst.Cfg.GameVersion;
            var gameCfg = RampancyInst.Cfg.GetGameConfig(ver);

            var tagBaseDir    = $"Assets/{ver}/TagData";
            var assetBasePath = unityBaseDir == null ? Path.Combine(tagBaseDir, GetTagRelPath(tagPath, gameCfg.TagsPath)) : Path.Combine(unityBaseDir, tagBaseDir, GetTagRelPath(tagPath, gameCfg.TagsPath));
            assetBasePath = Path.Combine(Path.GetDirectoryName(assetBasePath), Path.GetFileNameWithoutExtension(assetBasePath));
            return assetBasePath;
        }

        public static string GetTagRelPath(string tagPath, string tagBaseDir)
        {
            var tagBase = tagBaseDir.Replace("/", "\\");
            var relPath = tagPath.Replace(tagBase, "");
            return relPath;
        }

        public static string GetDataRelPath(string dataPath, string dataBaseDir)
        {
            var tagBase = dataBaseDir.Replace("/", "\\");
            var relPath = dataPath.Replace(tagBase, "");
            return relPath;
        }

        public static string GetDataToTagPath(string dataPath)
        {
            if (dataPath.StartsWith("data")) {
                var path = Path.Combine("tags", dataPath.Substring(4));
                return path;
            }

            return dataPath;
        }

        public static string GetTagToDataPath(string tagPath)
        {
            if (tagPath.StartsWith("tags")) {
                var path = Path.Combine("data", tagPath.Substring(4));
                return path;
            }

            return tagPath;
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
            var hash = vector3.x.GetHashCode();
            hash = CombineHashCodes(hash, vector3.y.GetHashCode());
            hash = CombineHashCodes(hash, vector3.z.GetHashCode());
            return hash;
        }

        public static int CombineHashCodes(int h1, int h2)
        {
            return ((h1 << 5) + h1) ^ h2;
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
            var         area      = CalcAreaOfTri(v1, v2, v3);

            if (area                               <= TOLERANCE ||
                Math.Abs(Vector3.Distance(v1, v2)) <= TOLERANCE ||
                Math.Abs(Vector3.Distance(v1, v3)) <= TOLERANCE ||
                Math.Abs(Vector3.Distance(v2, v3)) <= TOLERANCE)
                return true;

            return false;
        }

        public static string OpenFileDialog(string message, string fileExt, string startDir = "")
        {
            var path = EditorUtility.OpenFilePanel(message, startDir, fileExt);
            if (string.IsNullOrEmpty(path)) {
                Debug.LogWarning("Please give a path to load :>");
                return null;
            }

            return path;
        }

        public static void RunExeIfExists(string exePath)
        {
            if (File.Exists(exePath)) Rampancy.Rampancy.LaunchProgram(exePath, "");
        }

        // Import a DDS, convert to a Texture2d and save
        public static void ImportDDS(string importPath, string savePath)
        {
            if (File.Exists(importPath)) {
                using (var image = Pfim.Pfim.FromFile(importPath)) {
                    var format = image.Format switch
                    {
                        ImageFormat.Rgb24  => TextureFormat.RGB24,
                        ImageFormat.Rgba16 => TextureFormat.RGBA4444,
                        ImageFormat.Rgba32 => TextureFormat.BGRA32,
                        ImageFormat.Rgb8   => TextureFormat.R8
                    };

                    var tex = new Texture2D(image.Width, image.Height, format, true);
                    tex.SetPixelData(image.Data, 0);
                    tex.Apply();

                    if (image.Width % 4 == 0 && image.Height % 4 == 0)
                        tex.Compress(true);

                    AssetDatabase.CreateAsset(tex, savePath);
                }
            }
            else {
                Debug.LogWarning($"Error importing {importPath}, no file found");
            }
        }

        public static void ReplaceMaterailOnBrushesInScene(Material oldMat, Material newMat)
        {
            var brushes = Object.FindObjectsOfType<CSGBrush>();

            foreach (var brush in brushes) {
                foreach (var surface in brush.Shape.Surfaces) {
                    var texGen = brush.Shape.TexGens[surface.TexGenIndex];
                    if (texGen.RenderMaterial == oldMat) {
                        brush.Shape.TexGens[surface.TexGenIndex].RenderMaterial = newMat;
                    }
                }
            }
        }
    }
}