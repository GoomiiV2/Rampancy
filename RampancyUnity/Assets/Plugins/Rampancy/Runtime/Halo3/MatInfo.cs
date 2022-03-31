using RampantC20.Halo3;
using UnityEngine;

namespace Rampancy.Halo3
{
    public class MatInfo : ScriptableObject
    {
        public Material          Mat;
        public string            Collection;
        public string            Name;
        public bool              IsLevelMat = false; // If true then this is a cloned mat with settings for use as an instance in a level
        public Ass.BmFlags       NameFlags;
        public Ass.BmFlags       Flags; // Figure out if these are the same and if all are usable
        public Ass.LmRes         LmRes         = null;
        public Ass.LmBasic       LmBasic       = null;
        public Ass.LmAttenuation LmAttenuation = null;
        public Ass.LmFrustum     LmFrustum     = null;
    }
}