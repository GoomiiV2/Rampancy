using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using Ionic.Zlib;
using Plugins.Rampancy.RampantC20;

namespace RampantC20
{
    public partial class Utils
    {
        // Just grab and decompress the color plate from a bitmap
        public static (int width, int height, byte[] pixels)? GetColorPlateFromBitMap(AssetDb.TagInfo tagInfo)
        {
            using var br = tagInfo.GetReader();
            var (width, height) = GetH1BitmapDimensions(br);
            var byteSize = (int) SwapEndianness((uint) br.ReadInt32());
            br.BaseStream.Seek(80, SeekOrigin.Current);
            var plateBytes = br.ReadBytes(byteSize);

            var outSize = (width * height) * 4; // RGBA
            using (var stream = new BinaryReader(new ZlibStream(new MemoryStream(plateBytes), CompressionMode.Decompress, true))) {
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

        // Convert a height map to a normal map with a sobel edge filter like
        public static byte[] HeightmapToNormal(int width, int height, byte[] pixels, float normalStrength)
        {
            var norm = new byte[pixels.Length];

            for (int i = 0; i < pixels.Length; i += 4) {
                var upIdx                           = i - (width * 4);
                var downIdx                         = i + (width * 4);
                var leftIdx                         = i - 1;
                var rightIdx                        = i + 1;
                
                var up    = upIdx   > 0 ? RGBToGreyscale(pixels[upIdx], pixels[upIdx                 + 1], pixels[upIdx    + 2]) : 0;
                var down  = downIdx < pixels.Length ? RGBToGreyscale(pixels[downIdx], pixels[downIdx + 1], pixels[downIdx  + 2]) : 0;
                var left  = leftIdx > 0 ? RGBToGreyscale(pixels[leftIdx], pixels[leftIdx             + 1], pixels[leftIdx  + 2]) : 0;
                var right = rightIdx < pixels.Length ? RGBToGreyscale(pixels[rightIdx], pixels[rightIdx + 1], pixels[rightIdx + 2]) : 0;

                var dx     = new Vector3(1, 0, (right - left) * normalStrength);
                var dy     = new Vector3(0, 1, (down  - up)   * normalStrength);
                var normal = Vector3.Cross(dx, dy);
                normal = Vector3.Normalize(normal);

                byte r = (byte)((normal.X + 1.0f) * 127.5f);
                byte g = (byte)(255 - ((normal.Y + 1.0f) * 127.5f));

                norm[i]     = 255;                                                                                  // b
                norm[i + 1] = g;                                                                                    // g
                norm[i + 2] = r;                                                                                    // r
                norm[i + 3] = (byte) (256 / RGBToGreyscale(pixels[i], pixels[i + 1], pixels[i + 2]));          // a
            }

            return norm;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int To1DIndex(int x, int y, int width) => x + width * y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RGBToGreyscale(byte r, byte g, byte b) => ((r + b + g) / 3) / 255f;

        public static uint SwapEndianness(uint x)
        {
            x = (x >> 16) | (x << 16);
            return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
        }

        public static float SwapEndianness(float val) => SwapEndianness((uint) val);

        public static int SwapEndianness(int val) => (int) SwapEndianness((uint) val);

        public static ushort SwapEndianness(ushort x)
        {
            return (ushort) ((ushort) ((x & 0xff) << 8) | ((x >> 8) & 0xff));
        }

        /*public static float CalcAreaOfTri(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            double a = (p1 - p2).magnitude;
            double b = (p2 - p3).magnitude;
            double c = (p3 - p1).magnitude;
            double s = (a  + b + c) / 2;
            return Mathf.Sqrt((float)(s * (s -a) * (s -b) * (s -c)));
        }*/
    }
}