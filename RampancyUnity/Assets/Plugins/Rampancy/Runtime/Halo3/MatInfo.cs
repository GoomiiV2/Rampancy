using System.IO;
using Newtonsoft.Json;
using RampantC20.Halo3;
using UnityEngine;

namespace Rampancy.Halo3
{
    public class MatInfo : ScriptableObject
    {
        //public Material          Mat;
        public string            Collection;
        public string            Name;
        public bool              IsLevelMat = false; // If true then this is a cloned mat with settings for use as an instance in a level
        public Ass.BmFlags       NameFlags;
        public Ass.BmFlags       Flags; // Figure out if these are the same and if all are usable
        public Ass.LmRes         LmRes         = null;
        public Ass.LmBasic       LmBasic       = null;
        public Ass.LmAttenuation LmAttenuation = null;
        public Ass.LmFrustum     LmFrustum     = null;

        public static string GetMetaPath(string path) => $"{path}_meta.json";
        
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