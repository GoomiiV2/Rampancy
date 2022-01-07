using System.IO;
using Plugins.Rampancy.RampantC20;

namespace RampantC20.Halo1
{
    public class ShaderTag : TagBase
    {
        public ushort  ShaderFlags;
        public ushort  DetailLevel;
        public float   Power;
        public float[] LightColor;
        public float[] TintColor;
        public ushort  PhysicsFlags;
        public ushort  MaterialType;

        public ushort _Padding2;

        public override BinaryReader Read(AssetDb.TagInfo tagInfo)
        {
            var br = base.Read(tagInfo);

            ShaderFlags = Utils.SwapEndianness(br.ReadUInt16());
            DetailLevel = Utils.SwapEndianness(br.ReadUInt16());
            Power       = Utils.SwapEndianness(br.ReadSingle());
            LightColor = new[]
            {
                Utils.SwapEndianness(br.ReadSingle()),
                Utils.SwapEndianness(br.ReadSingle()),
                Utils.SwapEndianness(br.ReadSingle())
            };
            TintColor = new[]
            {
                Utils.SwapEndianness(br.ReadSingle()),
                Utils.SwapEndianness(br.ReadSingle()),
                Utils.SwapEndianness(br.ReadSingle())
            };
            PhysicsFlags = Utils.SwapEndianness(br.ReadUInt16());
            MaterialType = Utils.SwapEndianness(br.ReadUInt16());
            _Padding2    = Utils.SwapEndianness(br.ReadUInt16());

            return br;
        }
    }
}