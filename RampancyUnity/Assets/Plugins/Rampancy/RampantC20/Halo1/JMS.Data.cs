using System.Collections.Generic;
using UnityEngine;

namespace RampantC20.Halo1
{
    public partial class JMS
    {
        public const uint MAGIC              = 8200;
        public const uint NODE_LIST_CHECKSUM = 3251; // Its static?

        public uint Magic;
        public uint NodeListChecksum;

        public List<Node>     Nodes;
        public List<Material> Materials;
        public List<Region>   Regions;
        public List<Vert>     Verts;
        public List<Tri>      Tris;
        
        public class Node
        {
            public string     Name;
            public int        ChildIdx;
            public int        SiblingIdx;
            public Quaternion Rotation;
            public Vector3    Position;
        }

        public class Material
        {
            public string Name;
            public string Path;
        }

        public class Region
        {
            public string Name;
        }

        public class Vert
        {
            public int     Unk = 0;
            public Vector3 Position;
            public Vector3 Normal;
            public int     NodeIdx;
            public float   NodeWeight;
            public Vector2 UV;
            public int     Unk2 = 0;
        }

        public class Tri
        {
            public int RegionIdx;
            public int MaterialIdx;
            public int VertIdx0;
            public int VertIdx1;
            public int VertIdx2;
        }
    }
}