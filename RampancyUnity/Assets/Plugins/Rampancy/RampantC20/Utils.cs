using System.IO;
using System.Runtime.InteropServices;
using Ionic.Zlib;
using Plugins.Rampancy.RampantC20;
using UnityEngine;

namespace RampantC20
{
    public class Utils
    {
        // Just grab and decompress the color plate from a bitmap
        public static (int width, int height, byte[] pixels)? GetColorPlateFromBitMap(AssetDb.TagInfo tagInfo)
        {
            using var br = tagInfo.GetReader();
            var (width, height) = GetH1BitmapDimensions(br);
            var byteSize = (int)SwapEndianness((uint)br.ReadInt32());
            br.BaseStream.Seek(80, SeekOrigin.Current);
            var plateBytes = br.ReadBytes(byteSize);

            var outSize = (width * height) * 4; // RGBA
            using (var stream = new BinaryReader(new ZlibStream(new MemoryStream(plateBytes), CompressionMode.Decompress, true)))
            {
                var pixels = stream.ReadBytes(outSize);
                return (width, height, pixels);
            }
        }

        public static (int width, int height) GetH1BitmapDimensions(BinaryReader br)
        {
            br.BaseStream.Seek(88, SeekOrigin.Begin);
            var width  = SwapEndianness(br.ReadUInt16());
            var height = SwapEndianness(br.ReadUInt16());
            
            return (width, height);
        }

        public static uint SwapEndianness(uint x)
        {
            x = (x >> 16) | (x << 16);
            return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
        }
        
        public static ushort SwapEndianness(ushort x)
        {
            return (ushort)((ushort)((x & 0xff) << 8) | ((x >> 8) & 0xff));
        }

        public static float CalcAreaOfTri(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            double a = (p1 - p2).magnitude;
            double b = (p2 - p3).magnitude;
            double c = (p3 - p1).magnitude;
            double s = (a  + b + c) / 2;
            return Mathf.Sqrt((float)(s * (s -a) * (s -b) * (s -c)));
        }
    }
}