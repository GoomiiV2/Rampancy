using System.IO;

namespace RampantC20.Halo1
{
    // Base tag file for Halo 1
    // TODO: Make a better tag reading system, mostly if I add support for placing and editing propertys in editor
    public class TagBase
    {
        public uint   TagId;
        public string TagName;
        public uint   TagClass;
        public uint   Crc;
        public uint   HeaderSize;
        public ulong  _Padding;
        public ushort Version;
        public ushort Marker;
        public uint   Blam;

        public virtual BinaryReader Read(AssetDb.TagInfo tagInfo)
        {
            var br = tagInfo.GetReader();
            br.BaseStream.Seek(12, SeekOrigin.Current);

            TagId      = Utils.SwapEndianness(br.ReadUInt32());
            TagName    = new string(br.ReadChars(20));
            TagClass   = Utils.SwapEndianness(br.ReadUInt32());
            Crc        = Utils.SwapEndianness(br.ReadUInt32());
            HeaderSize = Utils.SwapEndianness(br.ReadUInt32());
            _Padding   = br.ReadUInt64();
            Version    = Utils.SwapEndianness(br.ReadUInt16());
            Marker     = Utils.SwapEndianness(br.ReadUInt16());
            Blam       = Utils.SwapEndianness(br.ReadUInt32());

            return br;
        }
    }
}