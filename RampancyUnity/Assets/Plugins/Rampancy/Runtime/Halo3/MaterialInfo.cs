using System;
using RampantC20;

namespace Rampancy.Halo3
{
    public class MaterialInfo
    {
        public Int64 MatGUID;
        public int   MatFlags;
        public int   Idx;

        public override int GetHashCode()
        {
            var hash = Utils.CombineHashCodes(MatGUID.GetHashCode(), MatFlags.GetHashCode());

            return hash;
        }
    }
}