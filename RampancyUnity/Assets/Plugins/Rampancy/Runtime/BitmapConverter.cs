using RampantC20.Halo1;
using UnityEngine;

namespace Rampancy
{
    public static class BitmapConverter
    {
        public static void ImportBitmaps()
        {
            var bitmapTagsInfos = Rampancy.AssetDB.TagsOfType("bitmap");
            Texture.allowThreadedTextureCreation = true;

            // Unitys texture creation is from the main thread only, so this is slow, for now
            // TODO: look into batching the texture creating and uploads
            foreach (var tagInfo in bitmapTagsInfos) Actions.H1_ImportBitmap(tagInfo);
        }

        public static void ImportShaders()
        {
            var bitmapTagsInfos = Rampancy.AssetDB.TagsOfType(
                "shader_environment"
                /*"shader_transparent_generic",
                "shader_transparent_chicago",
                "shader_transparent_chicago_extended",
                "shader_transparent_glass",
                "shader_transparent_water"*/
            );

            foreach (var tagInfo in bitmapTagsInfos) {
                //ImportBitmap(tagInfo);

                var shader = new ShaderEnvironmentTag();
                shader.Read(tagInfo);
            }
        }

        public static void SetTexData(Texture2D tex, byte[] pixels)
        {
            tex.SetPixelData(pixels, 0);
            tex.Compress(true);
            tex.Apply(true);
        }
    }
}