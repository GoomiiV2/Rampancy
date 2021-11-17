using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// JMS importer and exporter
// Used https://github.com/c0rp3n/blender-halo-tools/blob/master/io_scene_blam/export_jms_model.py as a reference
namespace Plugins.Rampancy.RampantC20
{
    public class JMS
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

        public JMS()
        {
            Magic            = 8200;
            NodeListChecksum = NODE_LIST_CHECKSUM;

            Nodes     = new List<Node>();
            Materials = new List<Material>();
            Regions   = new List<Region>();
            Verts     = new List<Vert>();
            Tris      = new List<Tri>();
        }

        public void Save(string path)
        {
            var data = ToString();
            File.WriteAllText(path, data);
        }

        // Convert this JMS to a string representation   
        // the same as would be in the file on disk
        public override string ToString()
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine(Magic.ToString());
            sb.AppendLine(NodeListChecksum.ToString());

            // Nodes
            sb.AppendLine(Nodes.Count().ToString());
            foreach (var node in Nodes) node.Format(sb);

            // Materials
            sb.AppendLine(Materials.Count().ToString());
            foreach (var mat in Materials) mat.Format(sb);

            sb.AppendLine("0");

            // Regions
            sb.AppendLine(Regions.Count().ToString());
            foreach (var region in Regions) region.Format(sb);

            // Verts
            sb.AppendLine(Verts.Count().ToString());
            foreach (var vert in Verts) vert.Format(sb);

            // Tris
            sb.AppendLine(Tris.Count().ToString());
            foreach (var tri in Tris) tri.Format(sb);

            return sb.ToString();
        }

        public static JMS Load(string path)
        {
            try {
                var       model = new JMS();
                using var sr    = new StreamReader(path);           // No spans :<
                model.Magic            = uint.Parse(sr.ReadLine()); // Just parse as if it fails well its a messed up file anyway
                model.NodeListChecksum = uint.Parse(sr.ReadLine());

                // Quick validations
                if (model.Magic != MAGIC) {
                    Console.WriteLine($"Error loading model from {path}, Magic was {model.Magic}, expected {MAGIC}");
                    return null;
                }

                // Should I care?
                /*if (model.NodeListChecksum != NODE_LIST_CHECKSUM) {
                Console.WriteLine($"Error loading model from {path}, NodeListChecksum was {model.NodeListChecksum}, expected {NODE_LIST_CHECKSUM}");
                return null;
            }*/

                // Load nodes
                var numNodes = int.Parse(sr.ReadLine());
                model.Nodes = new List<Node>(numNodes);
                for (int i = 0; i < numNodes; i++) {
                    var node = Node.Parse(sr);
                    model.Nodes.Add(node);
                }

                // Materials
                var numMats = int.Parse(sr.ReadLine());
                model.Materials = new List<Material>(numMats);
                for (int i = 0; i < numMats; i++) {
                    var mat = Material.Parse(sr);
                    model.Materials.Add(mat);
                }

                // Markers
                // skipping for now?
                sr.ReadLine();

                // Regions
                var numRegions = int.Parse(sr.ReadLine());
                model.Regions = new List<Region>(numRegions);
                for (int i = 0; i < numRegions; i++) {
                    var region = Region.Parse(sr);
                    model.Regions.Add(region);
                }

                // Verts
                var numVerts = int.Parse(sr.ReadLine());
                model.Verts = new List<Vert>(numVerts);
                for (int i = 0; i < numVerts; i++) {
                    var vert = Vert.Parse(sr);
                    model.Verts.Add(vert);
                }

                // Tris
                var numTris = int.Parse(sr.ReadLine());
                model.Tris = new List<Tri>(numTris);
                for (int i = 0; i < numTris; i++) {
                    var tri = Tri.Parse(sr);
                    model.Tris.Add(tri);
                }

                return model;
            }
            catch (Exception e) {
                Console.WriteLine(e);

                return null;
            }
        }

        public static void FormatVector3(StringBuilder    sb, Vector3    vec3) => sb.AppendLine($"{vec3.x:F6}\t{vec3.y:F6}\t{vec3.z:F6}");
        public static void FormatVector2(StringBuilder    sb, Vector2    vec2) => sb.AppendLine($"{vec2.x:F6}\n{vec2.y:F6}");
        public static void FormatQuaternion(StringBuilder sb, Quaternion quat) => sb.AppendLine($"{quat.x:F6}\t{quat.y:F6}\t{quat.z:F6}\t{quat.w:F6}");

        public static Vector3 ParseVector3(StreamReader sr)
        {
            var line  = sr.ReadLine();
            var parts = line.Split('\t');
            var vec3 = new Vector3
            {
                x = float.Parse(parts[0]),
                y = float.Parse(parts[1]),
                z = float.Parse(parts[2])
            };

            return vec3;
        }

        public static Vector2 ParseVector2(StreamReader sr)
        {
            var line1 = sr.ReadLine();
            var line2 = sr.ReadLine();
            var vec2 = new Vector3
            {
                x = float.Parse(line1),
                y = float.Parse(line2)
            };

            return vec2;
        }

        public static Quaternion ParseQuaternion(StreamReader sr)
        {
            var line  = sr.ReadLine();
            var parts = line.Split('\t');
            var quat = new Quaternion
            {
                x = float.Parse(parts[0]),
                y = float.Parse(parts[1]),
                z = float.Parse(parts[2]),
                w = float.Parse(parts[3])
            };

            return quat;
        }

        public class Node
        {
            public string     Name;
            public int        ChildIdx;
            public int        SiblingIdx;
            public Quaternion Rotation;
            public Vector3    Position;

            public void Format(StringBuilder sb)
            {
                sb.AppendLine(Name);
                sb.AppendLine(ChildIdx.ToString());
                sb.AppendLine(SiblingIdx.ToString());
                FormatQuaternion(sb, Rotation);
                FormatVector3(sb, Position);
            }

            public static Node Parse(StreamReader sr)
            {
                var node = new Node()
                {
                    Name       = sr.ReadLine(),
                    ChildIdx   = int.Parse(sr.ReadLine()),
                    SiblingIdx = int.Parse(sr.ReadLine()),
                    Rotation   = ParseQuaternion(sr),
                    Position   = ParseVector3(sr)
                };

                return node;
            }
        }

        public class Material
        {
            public string Name;
            public string Path;

            public void Format(StringBuilder sb)
            {
                sb.AppendLine(Name);
                sb.AppendLine(Path);
            }

            public static Material Parse(StreamReader sr)
            {
                var mat = new Material
                {
                    Name = sr.ReadLine(),
                    Path = sr.ReadLine()
                };

                return mat;
            }
        }

        public class Region
        {
            public string Name;

            public void Format(StringBuilder sb)
            {
                sb.AppendLine(Name);
            }

            public static Region Parse(StreamReader sr)
            {
                var region = new Region
                {
                    Name = sr.ReadLine()
                };

                return region;
            }
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

            public void Format(StringBuilder sb)
            {
                sb.AppendLine(Unk.ToString());
                FormatVector3(sb, Position);
                FormatVector3(sb, Normal);
                sb.AppendLine(NodeIdx.ToString());
                sb.AppendLine(NodeWeight.ToString(CultureInfo.InvariantCulture));
                FormatVector2(sb, UV);
                sb.AppendLine(Unk2.ToString());
            }

        
            public static Vert Parse(StreamReader sr)
            {
                var vert = new Vert
                {
                    Unk        = int.Parse(sr.ReadLine()),
                    Position   = ParseVector3(sr),
                    Normal     = ParseVector3(sr),
                    NodeIdx    = int.Parse(sr.ReadLine()),
                    NodeWeight = float.Parse(sr.ReadLine()),
                    UV         = ParseVector2(sr),
                    Unk2       = int.Parse(sr.ReadLine())
                };

                return vert;
            }
        }

        public class Tri
        {
            public int RegionIdx;
            public int MaterialIdx;
            public int VertIdx0;
            public int VertIdx1;
            public int VertIdx2;

            public void Format(StringBuilder sb)
            {
                sb.AppendLine(RegionIdx.ToString());
                sb.AppendLine(MaterialIdx.ToString());
                sb.AppendLine($"{VertIdx0}\t{VertIdx1}\t{VertIdx2}");
            }

            public static Tri Parse(StreamReader sr)
            {
                var tri = new Tri
                {
                    RegionIdx   = int.Parse(sr.ReadLine()),
                    MaterialIdx = int.Parse(sr.ReadLine())
                };

                var indicesLine = sr.ReadLine();
                var indices     = indicesLine.Split('\t');
                tri.VertIdx0 = int.Parse(indices[0]);
                tri.VertIdx1 = int.Parse(indices[1]);
                tri.VertIdx2 = int.Parse(indices[2]);

                return tri;
            }
        }
    }
}