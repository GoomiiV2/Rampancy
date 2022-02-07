using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rampancy.Halo3
{
    public class ShaderData
    {
        public string DiffuseTex;
        public string DetailTex;
        public string BumpTex;
        public string BumpDetailTex;
        public string SelfIllum;

        public override string ToString()
        {
            var str = $"DiffuseTex: {DiffuseTex}\nDetailTex: {DetailTex}\nBumpTex: {BumpTex}\nBumpDetailTex: {BumpDetailTex}\nSelfIllum: {SelfIllum}";
            return str;
        }

        // Baisc bytes for the property names to search on, not ideal but for now
        static byte[] TR_BUMP_MAP_START          = new byte[] { 0x08, 0x00, 0x00, 0x00, 0x62, 0x75, 0x6D, 0x70, 0x5F, 0x6D, 0x61, 0x70 };
        static byte[] TR_BUMP_DETAIL_MAP_START   = new byte[] { 0x0F, 0x00, 0x00, 0x00, 0x62, 0x75, 0x6D, 0x70, 0x5F, 0x64, 0x65, 0x74, 0x61, 0x69, 0x6C, 0x5F,0x6D, 0x61, 0x70 };
        static byte[] TR_BASE_MAP_START          = new byte[] { 0x08, 0x00, 0x00, 0x00, 0x62, 0x61, 0x73, 0x65, 0x5F, 0x6D, 0x61, 0x70 };
        static byte[] TR_DETAIL_MAP_START        = new byte[] { 0x0A, 0x00, 0x00, 0x00, 0x64, 0x65, 0x74, 0x61, 0x69, 0x6C, 0x5F, 0x6D, 0x61, 0x70 };
        static byte[] TR_SELF_ILLUM_MAP_START    = new byte[] { 0x0E, 0x00, 0x00, 0x00, 0x73, 0x65, 0x6C, 0x66, 0x5F, 0x69, 0x6C, 0x6C, 0x75, 0x6D, 0x5F, 0x6D, 0x61, 0x70 };

        // Scan a file for tag refs for the bitmats, not ideal but faster than tool xml export atm for the textures atleast
        public static ShaderData GetBasicShaderDataFromScan(Span<byte> bytes)
        {
            var shaderData = new ShaderData
            {
                DiffuseTex    = GetBitmapPathForProperty(bytes, TR_BASE_MAP_START),
                DetailTex     = GetBitmapPathForProperty(bytes, TR_DETAIL_MAP_START),
                BumpTex       = GetBitmapPathForProperty(bytes, TR_BUMP_MAP_START),
                BumpDetailTex = GetBitmapPathForProperty(bytes, TR_BUMP_DETAIL_MAP_START),
                SelfIllum     = GetBitmapPathForProperty(bytes, TR_SELF_ILLUM_MAP_START)
            };

            return shaderData;
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
    }
}
