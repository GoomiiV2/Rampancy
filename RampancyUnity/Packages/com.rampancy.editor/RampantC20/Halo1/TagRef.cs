using System;
using System.IO;
using System.Linq;

namespace RampantC20.Halo1
{
    public class TagRef
    {
        public uint TagCode;
        public uint Pointer;
        public int PathLen;
        public uint TagId;

        public string Path;

        public TagRef(BinaryReader br)
        {
            Read(br);
        }

        // Messy
        public string TagCodeToStr()
        {
            var bytes = BitConverter.GetBytes(TagCode);
            var str    = $"{(char)bytes[0]}{(char)bytes[1]}{(char)bytes[2]}{(char)bytes[3]}";
            return str;
        }

        public void Read(BinaryReader br)
        {
            TagCode = Utils.SwapEndianness(br.ReadUInt32());
            Pointer = Utils.SwapEndianness(br.ReadUInt32());
            PathLen = Utils.SwapEndianness(br.ReadInt32());
            TagId   = Utils.SwapEndianness(br.ReadUInt32());
        }

        public void ReadTagPath(BinaryReader br)
        {
            Path = new string(br.ReadChars(PathLen));
            br.BaseStream.Seek(1, SeekOrigin.Current);
        }
    }
}