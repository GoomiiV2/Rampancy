using System.IO;
using Plugins.Rampancy.RampantC20;
using UnityEditor;
using UnityEngine;

namespace Plugins.Rampancy.Runtime
{
    public static class BitmapConverter
    {
        public static void ImportBitmaps()
        {
            var bitmapTagsInfos = Rampancy.AssetDB.TagsOfType("bitmap");
            Texture.allowThreadedTextureCreation = true;
            
            // Unitys texture creation is from the main thread only, so this is slow, for now
            // TODO: look into batching the texture creating and uploads
            foreach (var tagInfo in bitmapTagsInfos) {
                ImportBitmap(tagInfo);
            }
        }

        public static void ImportShaders()
        {
            var bitmapTagsInfos = Rampancy.AssetDB.TagsOfType(
                "shader_environment",
                "shader_transparent_generic",
                "shader_transparent_chicago",
                "shader_transparent_chicago_extended",
                "shader_transparent_glass",
                "shader_transparent_water"
            );

            foreach (var tagInfo in bitmapTagsInfos) {
                //ImportBitmap(tagInfo);
            }
        }

        public static void ImportBitmap(AssetDb.TagInfo tagInfo)
        {
            var (width, height, pixels) = RampantC20.Utils.GetColorPlateFromBitMap(tagInfo).Value;
            var tex = BitMapToTex2D(width, height, pixels);
            tex.name = tagInfo.Name;

            var path = GetProjectRelPath(tagInfo, Rampancy.AssetDB);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(tex, $"{path}.asset");

            CreateBasicMat(tex, path);
        }

        public static string GetProjectRelPath(AssetDb.TagInfo tagInfo, AssetDb assetDb)
        {
            var basse         = $"Assets/{Rampancy.Config.GameVersion}/TagData";
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

        public static void SetTexData(Texture2D tex, byte[] pixels)
        {
            tex.SetPixelData(pixels, 0);
            tex.Compress(true);
            tex.Apply(true);
        }

        public static void CreateBasicMat(Texture2D tex, string path)
        {
            var mat = new Material(Shader.Find("Legacy Shaders/Diffuse"));
            mat.mainTexture = tex;
            mat.name        = Path.GetFileNameWithoutExtension(path);

            AssetDatabase.CreateAsset(mat, $"{path}_mat.asset");
        }
    }
}