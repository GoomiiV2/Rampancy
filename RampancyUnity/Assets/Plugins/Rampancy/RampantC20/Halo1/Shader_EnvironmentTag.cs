using System.IO;
using Plugins.Rampancy.RampantC20;

namespace RampantC20.Halo1
{
    // Will just skip and read the bitmap tag refs for now
    public class ShaderEnvironmentTag : ShaderTag
    {
        public TagRef BaseMap;
        public TagRef PrimaryDetailMap;
        public TagRef SecondaryDetailMap;
        public TagRef MicroDetailMap;
        public TagRef BumpMap;

        public override BinaryReader Read(AssetDb.TagInfo tagInfo)
        {
            var br = base.Read(tagInfo);

            br.BaseStream.Seek(98, SeekOrigin.Current);
            BaseMap = new(br);

            br.BaseStream.Seek(32, SeekOrigin.Current);
            PrimaryDetailMap = new(br);

            br.BaseStream.Seek(4, SeekOrigin.Current);
            SecondaryDetailMap = new(br);

            br.BaseStream.Seek(32, SeekOrigin.Current);
            MicroDetailMap = new(br);

            br.BaseStream.Seek(28, SeekOrigin.Current);
            BumpMap = new(br);

            // Now skip to the end of the tag file for the path strings
            br.BaseStream.Seek(900, SeekOrigin.Begin);

            if (BaseMap.PathLen > 0)
                BaseMap.ReadTagPath(br);

            if (PrimaryDetailMap.PathLen > 0)
                PrimaryDetailMap.ReadTagPath(br);

            if (SecondaryDetailMap.PathLen > 0)
                SecondaryDetailMap.ReadTagPath(br);

            if (MicroDetailMap.PathLen > 0)
                MicroDetailMap.ReadTagPath(br);

            if (BumpMap.PathLen > 0)
                BumpMap.ReadTagPath(br);

            return br;
        }
    }
}