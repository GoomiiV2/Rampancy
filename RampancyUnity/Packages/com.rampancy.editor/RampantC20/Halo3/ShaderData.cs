using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text;

namespace Rampancy.Halo3
{
    public class ShaderData
    {
        public string TagPath;
        public string Collection;

        protected static byte[] TR_SHADER_TEMPLATE = new byte[] { 0x32, 0x74, 0x6D, 0x72, 0x73, 0x68, 0x61, 0x64, 0x65, 0x72, 0x73, 0x5C };

        // Scan a hader tag and get out some usefull info, lie textures paths and if its alpha blended or not
        public static ShaderData GetDataFromScan(string tagPath, string collection = null)
        {
            var shaderType = Path.GetExtension(tagPath).Substring(1);
            var bytes      = File.ReadAllBytes(tagPath);

            ShaderData data = shaderType switch
            {
                "shader"         => BasicShaderData.GetDataFromScan(bytes),
                "shader_terrain" => TerrainShaderData.GetDataFromScan(bytes),
                _                => null
            };

            if (data != null)
            {
                data.TagPath    = tagPath;
                data.Collection = collection;
            }

            return data;
        }

        // Scan for a pattern, jump abit and retun the string path ref for the tag
        public static string GetBitmapPathForProperty(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> startPattern)
        {
            var startIdx    = bytes.IndexOf(startPattern);
            var strLenStart = startIdx + startPattern.Length + 8;
            if (startIdx != -1 && strLenStart < bytes.Length)
            {
                var lenBytes    = BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(strLenStart, 4)) - 4;
                if (lenBytes <= 0) return null;

                var pathStrStart = strLenStart + 8;
                if (pathStrStart < bytes.Length && pathStrStart + lenBytes < bytes.Length)
                {
                    var pathStr = UTF8Encoding.ASCII.GetString(bytes.Slice(pathStrStart, lenBytes));
                    return pathStr;
                }
            }

            return null;
        }

        public static string GetShaderTemplatePath(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> startPattern)
        {
            var startIdx = bytes.IndexOf(startPattern);
            if (startIdx != -1)
            {
                var len  = BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(startIdx - 4, 4));
                var path = UTF8Encoding.ASCII.GetString(bytes.Slice(startIdx + 4, len - 4));
                return path;
            }

            return null;
        }
    }

    public class BasicShaderData : ShaderData
    {
        public string DiffuseTex;
        public string DetailTex;
        public string BumpTex;
        public string BumpDetailTex;
        public string SelfIllum;
        public string AlphaTestMap;

        public bool IsAlphaTested = false;
        public float BaseMapScale = 1f;

        public override string ToString()
        {
            var str = $"DiffuseTex: {DiffuseTex}\nDetailTex: {DetailTex}\nBumpTex: {BumpTex}\nBumpDetailTex: {BumpDetailTex}\nSelfIllum: {SelfIllum}";
            return str;
        }

        // Baisc bytes for the property names to search on, not ideal but for now
        static byte[] TR_BUMP_MAP_START        = new byte[] { 0x08, 0x00, 0x00, 0x00, 0x62, 0x75, 0x6D, 0x70, 0x5F, 0x6D, 0x61, 0x70 };
        static byte[] TR_BUMP_DETAIL_MAP_START = new byte[] { 0x0F, 0x00, 0x00, 0x00, 0x62, 0x75, 0x6D, 0x70, 0x5F, 0x64, 0x65, 0x74, 0x61, 0x69, 0x6C, 0x5F, 0x6D, 0x61, 0x70 };
        static byte[] TR_BASE_MAP_START        = new byte[] { 0x08, 0x00, 0x00, 0x00, 0x62, 0x61, 0x73, 0x65, 0x5F, 0x6D, 0x61, 0x70 };
        static byte[] TR_DETAIL_MAP_START      = new byte[] { 0x0A, 0x00, 0x00, 0x00, 0x64, 0x65, 0x74, 0x61, 0x69, 0x6C, 0x5F, 0x6D, 0x61, 0x70 };
        static byte[] TR_SELF_ILLUM_MAP_START  = new byte[] { 0x0E, 0x00, 0x00, 0x00, 0x73, 0x65, 0x6C, 0x66, 0x5F, 0x69, 0x6C, 0x6C, 0x75, 0x6D, 0x5F, 0x6D, 0x61, 0x70 };
        static byte[] TR_ALPHA_TEST_MAP_START  = new byte[] { 0x0E, 0x00, 0x00, 0x00, 0x61, 0x6C, 0x70, 0x68, 0x61, 0x5F, 0x74, 0x65, 0x73, 0x74, 0x5F, 0x6D, 0x61, 0x70 };

        // Scan a file for tag refs for the bitmats, not ideal but faster than tool xml export atm for the textures atleast
        public static BasicShaderData GetDataFromScan(Span<byte> bytes)
        {
            var shaderData = new BasicShaderData
            {
                DiffuseTex    = GetBitmapPathForProperty(bytes, TR_BASE_MAP_START),
                DetailTex     = GetBitmapPathForProperty(bytes, TR_DETAIL_MAP_START),
                BumpTex       = GetBitmapPathForProperty(bytes, TR_BUMP_MAP_START),
                BumpDetailTex = GetBitmapPathForProperty(bytes, TR_BUMP_DETAIL_MAP_START),
                SelfIllum     = GetBitmapPathForProperty(bytes, TR_SELF_ILLUM_MAP_START),
                AlphaTestMap  = GetBitmapPathForProperty(bytes, TR_ALPHA_TEST_MAP_START)
            };

            var template     = GetShaderTemplatePath(bytes, TR_SHADER_TEMPLATE);
            if (template != null)
            {
                var templateName = Path.GetFileName(template);
                if (templateName.Count(x => x == '_') >= 7)
                {
                    var cats = templateName.Split('_', options: StringSplitOptions.RemoveEmptyEntries);

                    // Alpha test or blend mode set
                    shaderData.IsAlphaTested = (cats[2] == "1") || (cats[7] == "3");
                }
            }

            // Try get the base map scale, this is hacky :D
            var baseMapStart = bytes.IndexOf(TR_BASE_MAP_START);
            if (baseMapStart > -1)
            {
                var searchSpan  = bytes.Slice(baseMapStart, 200);
                var scaleOffset = searchSpan.IndexOf(new byte[] { 0x61, 0x64, 0x67, 0x74, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x01, 0x24, 0x00, 0x00 });
                if (scaleOffset > -1)
                {
                    var startIdx      = baseMapStart + scaleOffset + 16;
                    var detailOffset  = bytes.IndexOf(TR_DETAIL_MAP_START);
                    if (detailOffset == -1 || detailOffset != -1 && startIdx < detailOffset)
                    {
                        shaderData.BaseMapScale = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(startIdx, 4)));
                    }
                }
            }

            return shaderData;
        }
    }

    public class TerrainShaderData : ShaderData
    {
        public static TerrainShaderData GetDataFromScan(Span<byte> bytes)
        {
            var shader = new TerrainShaderData();
            return shader;
        }
    }
}
