using System;
using System.Numerics;
using RampantC20;

namespace Rampancy.Halo3
{
    public class MaterialInfo
    {
        public string  Name;
        public string  Collection;
        public string  TagPath; // Tag path for the shader
        public string  DiffusePath;
        public string  BumpPath;
        public string  BumpDetailPath;
        public string  DetailPath;
        public string  AlphaPath;
        public bool    IsAlphaTested;
        public Vector2 Tiling;
    }
}