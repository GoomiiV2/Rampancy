using System.IO;
using Rampancy.Common;
using RampantC20.Halo3;

namespace Rampancy.Halo3
{
    public class SceneMatInfoHalo3 : SceneMatInfo
    {
        public MatInfo MatMeta;

        public override string GetDisplayName()
        {
            if (MatMeta == null) return base.GetDisplayName();
            
            var flagsSymbols = Ass.MaterialSymbols.FlagToSymbols(MatMeta.Flags);
            return MatMeta != null ? $"{MatMeta.Collection ?? ""} - {Name ?? ""} {flagsSymbols}" : "Error";
        }

        public void LoadMatMeta(bool force = false)
        {
            if (MatMeta != null && !force)
                return;

            MatMeta = MatInfo.Load(MatPath);
        }

        protected override void CreateOrCopyBaseMetaProps(string path)
        {
            var matInfo = new MatInfo(MatMeta);
            matInfo.IsLevelMat = true; // Allow it to be editable
            
            matInfo.Save(path);
        }
    }
}