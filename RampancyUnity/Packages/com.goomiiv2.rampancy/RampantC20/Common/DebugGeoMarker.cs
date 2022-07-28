using System;
using System.Collections.Generic;
using UnityEngine;

namespace RampantC20
{
    public class DebugGeoMarker
    {
        public string        Name;
        public ItemFlags     Flags;
        public List<Vector3> Verts;
        public Color32       Color;
        public List<short>   Indices; // may not be needed

        public static DebugGeoMarker Create()
        {
            var data = new DebugGeoMarker
            {
                Name    = "Debug Geo",
                Flags   = ItemFlags.Line,
                Verts   = new(),
                Color   = new Color32(255, 255, 0, 0),
                Indices = new()
            };

            return data;
        }

        [Flags]
        public enum ItemFlags : byte
        {
            Line,
            Tri
        }
    }
}