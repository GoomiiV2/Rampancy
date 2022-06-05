using System.IO;
using Newtonsoft.Json;
using RampantC20.Halo3;

namespace Rampancy.Halo3
{
    public class MatInfo
    {
        public string            Collection;
        public string            Name;
        public string            TagPath; // SDK relative tag path for the shader this is based on
        public bool              IsLevelMat = false; // If true then this is a cloned mat with settings for use as an instance in a level
        public Ass.BmFlags       NameFlags;
        public Ass.BmFlags       Flags; // Figure out if these are the same and if all are usable
        public Ass.LmRes         LmRes         = null;
        public Ass.LmBasic       LmBasic       = null;
        public Ass.LmAttenuation LmAttenuation = null;
        public Ass.LmFrustum     LmFrustum     = null;

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
    }
}