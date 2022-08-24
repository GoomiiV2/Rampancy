using System;
using System.IO;
using Newtonsoft.Json;
using RampantC20.Halo3;
using UnityEditor.Experimental.GraphView;

namespace Rampancy.Halo3
{
    public class MatInfo
    {
        public string            Collection;
        public string            Name;
        public string            TagPath;            // SDK relative tag path for the shader this is based on
        public bool              IsLevelMat = false; // If true then this is a cloned mat with settings for use as an instance in a level
        public Ass.BmFlags       NameFlags;
        public Ass.BmFlags       Flags; // Figure out if these are the same and if all are usable
        public Ass.LmRes         LmRes         = null;
        public Ass.LmBasic       LmBasic       = null;
        public Ass.LmAttenuation LmAttenuation = null;
        public Ass.LmFrustum     LmFrustum     = null;
        public bool              IsDIrty       = false;

        private static int FlagsDefaultHash         = new Ass.BmFlags().GetHashCode();
        private static int LmResDefaultHash         = new Ass.LmRes().GetHashCode();
        private static int LmBasicDefaultHash       = new Ass.LmBasic().GetHashCode();
        private static int LmAttenuationDefaultHash = new Ass.LmAttenuation().GetHashCode();
        private static int LmFrustumDefaultHash     = new Ass.LmFrustum().GetHashCode();

        public MatInfo()
        {
        }

        public MatInfo(MatInfo matInfo)
        {
            Collection    = matInfo.Collection;
            Name          = matInfo.Name;
            TagPath       = matInfo.TagPath;
            IsLevelMat    = matInfo.IsLevelMat;
            NameFlags     = matInfo.NameFlags;
            Flags         = matInfo.Flags;
            LmRes         = matInfo.LmRes;
            LmBasic       = matInfo.LmBasic;
            LmAttenuation = matInfo.LmAttenuation;
            LmFrustum     = matInfo.LmFrustum;
        }

        public static string GetMetaPath(string path) => $"{path}_meta.json";

        public static MatInfo Create(string matPath, string collection)
        {
            var name                        = Path.GetFileNameWithoutExtension(matPath);
            if (name.EndsWith("_mat")) name = name[..^4];

            var info = new MatInfo
            {
                Name       = name,
                Collection = collection
            };

            return info;
        }

        public static MatInfo Load(string path)
        {
            var metadataPath = GetMetaPath(path);
            if (!File.Exists(metadataPath)) return null;

            var jsonTxt = File.ReadAllText(metadataPath);
            var matInfo = JsonConvert.DeserializeObject<MatInfo>(jsonTxt);

            return matInfo;
        }

        public void Save(string path)
        {
            var metadataPath = GetMetaPath(path);
            var jsonTxt      = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(metadataPath, jsonTxt);
        }

        public Ass.Material ToAssMat()
        {
            var mat = new Ass.Material
            {
                Collection = Collection,
                Name       = Name
            };

            if (Flags != null && Flags.GetHashCode() != FlagsDefaultHash) {
                mat.Flags = Flags;
                mat.LmRes = LmRes;
            }

            if (LmBasic != null) {
                mat.Flags       = Flags;
                mat.LmRes       = LmRes;
                mat.Basic       = LmBasic;
                mat.Attenuation = LmAttenuation;
                mat.Fustrum     = LmFrustum;
            }

            /*
             * Flags       = Flags         != null ? Flags.GetHashCode()         == FlagsDefaultHash ? null : Flags : null,
                LmRes       = LmRes         != null ? LmRes.GetHashCode()         == LmResDefaultHash ? null : LmRes : null,
                Basic       = LmBasic       != null ? LmBasic.GetHashCode()       == LmBasicDefaultHash ? null : LmBasic : null,
                Attenuation = LmAttenuation != null ? LmAttenuation.GetHashCode() == LmAttenuationDefaultHash ? null : LmAttenuation : null,
                Fustrum     = LmFrustum     != null ? LmFrustum.GetHashCode()     == LmFrustumDefaultHash ? null : LmFrustum : null
             */

            return mat;
        }

        protected bool Equals(MatInfo other)
        {
            return Collection == other.Collection && Name == other.Name                         && TagPath == other.TagPath && IsLevelMat == other.IsLevelMat && NameFlags == other.NameFlags && Flags == other.Flags && Equals(LmRes, other.LmRes) &&
                   Equals(LmBasic, other.LmBasic) && Equals(LmAttenuation, other.LmAttenuation) && Equals(LmFrustum, other.LmFrustum);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MatInfo) obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Collection);
            hashCode.Add(Name);
            hashCode.Add(TagPath);
            hashCode.Add(IsLevelMat);
            hashCode.Add((int) NameFlags);
            hashCode.Add((int) Flags);
            hashCode.Add(LmRes);
            hashCode.Add(LmBasic);
            hashCode.Add(LmAttenuation);
            hashCode.Add(LmFrustum);
            return hashCode.ToHashCode();
        }
    }
}