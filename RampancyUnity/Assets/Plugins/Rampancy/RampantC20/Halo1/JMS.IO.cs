using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RampantC20.Halo1
{
    public partial class JMS
    {
        public JMS()
        {
            Magic            = MAGIC;
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
            sb.AppendLine(Nodes.Count.ToString());
            foreach (var node in Nodes)
                FormatNode(sb, node);

            // Materials
            sb.AppendLine(Materials.Count.ToString());
            foreach (var mat in Materials)
                FormatMaterial(sb, mat);

            sb.AppendLine("0");

            // Regions
            sb.AppendLine(Regions.Count.ToString());
            foreach (var region in Regions)
                FormatRegion(sb, region);

            // Verts
            sb.AppendLine(Verts.Count.ToString());
            foreach (var vert in Verts)
                FormatVert(sb, vert);

            // Tris
            sb.AppendLine(Tris.Count.ToString());
            foreach (var tri in Tris)
                FormatTri(sb, tri);

            return sb.ToString();
        }

        public static JMS Load(string path)
        {
            try {
                var       model = new JMS();
                using var sr    = new StreamReader(path);            // No spans :<
                model.Magic            = uint.Parse(sr.ReadLine()!); // Just parse as if it fails well its a messed up file anyway
                model.NodeListChecksum = uint.Parse(sr.ReadLine()!);

                // Quick validations
                if (model.Magic != MAGIC) {
                    Console.WriteLine($"Error loading model from {path}, Magic was {model.Magic}, expected {MAGIC}");
                    return null;
                }

                // Load nodes
                var numNodes = int.Parse(sr.ReadLine()!);
                model.Nodes = new List<Node>(numNodes);
                for (int i = 0; i < numNodes; i++) {
                    var node = ParseNode(sr);
                    model.Nodes.Add(node);
                }

                // Materials
                var numMats = int.Parse(sr.ReadLine()!);
                model.Materials = new List<Material>(numMats);
                for (int i = 0; i < numMats; i++) {
                    var mat = ParseMaterial(sr);
                    model.Materials.Add(mat);
                }

                // Markers
                // skipping for now?
                sr.ReadLine();

                // Regions
                var numRegions = int.Parse(sr.ReadLine()!);
                model.Regions = new List<Region>(numRegions);
                for (int i = 0; i < numRegions; i++) {
                    var region = ParseRegion(sr);
                    model.Regions.Add(region);
                }

                // Verts
                var numVerts = int.Parse(sr.ReadLine()!);
                model.Verts = new List<Vert>(numVerts);
                for (int i = 0; i < numVerts; i++) {
                    var vert = ParseVert(sr);
                    model.Verts.Add(vert);
                }

                // Tris
                var numTris = int.Parse(sr.ReadLine()!);
                model.Tris = new List<Tri>(numTris);
                for (int i = 0; i < numTris; i++) {
                    var tri = ParseTri(sr);
                    model.Tris.Add(tri);
                }

                return model;
            }
            catch (Exception e) {
                Console.WriteLine(e);

                return null;
            }
        }

    #region Formatting

        public static void FormatVector3(StringBuilder    sb, Vector3    vec3) => sb.AppendLine($"{vec3.x:F6}\t{vec3.y:F6}\t{vec3.z:F6}");
        public static void FormatVector2(StringBuilder    sb, Vector2    vec2) => sb.AppendLine($"{vec2.x:F6}\n{vec2.y:F6}");
        public static void FormatQuaternion(StringBuilder sb, Quaternion quat) => sb.AppendLine($"{quat.x:F6}\t{quat.y:F6}\t{quat.z:F6}\t{quat.w:F6}");

        public static void FormatNode(StringBuilder sb, Node node)
        {
            sb.AppendLine(node.Name);
            sb.AppendLine(node.ChildIdx.ToString());
            sb.AppendLine(node.SiblingIdx.ToString());
            FormatQuaternion(sb, node.Rotation);
            FormatVector3(sb, node.Position);
        }

        public static void FormatMaterial(StringBuilder sb, Material mat)
        {
            sb.AppendLine(mat.Name);
            sb.AppendLine(mat.Path);
        }

        public static void FormatRegion(StringBuilder sb, Region region)
        {
            sb.AppendLine(region.Name);
        }

        public static void FormatVert(StringBuilder sb, Vert vert)
        {
            sb.AppendLine(vert.Unk.ToString());
            FormatVector3(sb, vert.Position);
            FormatVector3(sb, vert.Normal);
            sb.AppendLine(vert.NodeIdx.ToString());
            sb.AppendLine(vert.NodeWeight.ToString(CultureInfo.InvariantCulture));
            FormatVector2(sb, vert.UV);
            sb.AppendLine(vert.Unk2.ToString());
        }

        public static void FormatTri(StringBuilder sb, Tri tri)
        {
            sb.AppendLine(tri.RegionIdx.ToString());
            sb.AppendLine(tri.MaterialIdx.ToString());
            sb.AppendLine($"{tri.VertIdx0}\t{tri.VertIdx1}\t{tri.VertIdx2}");
        }

    #endregion

    #region Parsing

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

        public static Node ParseNode(StreamReader sr)
        {
            var node = new Node()
            {
                Name       = sr.ReadLine(),
                ChildIdx   = int.Parse(sr.ReadLine()!),
                SiblingIdx = int.Parse(sr.ReadLine()!),
                Rotation   = ParseQuaternion(sr),
                Position   = ParseVector3(sr)
            };

            return node;
        }

        public static Material ParseMaterial(StreamReader sr)
        {
            var mat = new Material
            {
                Name = sr.ReadLine(),
                Path = sr.ReadLine()
            };

            return mat;
        }

        public static Region ParseRegion(StreamReader sr)
        {
            var region = new Region
            {
                Name = sr.ReadLine()
            };

            return region;
        }

        public static Vert ParseVert(StreamReader sr)
        {
            var vert = new Vert
            {
                Unk        = int.Parse(sr.ReadLine()!),
                Position   = ParseVector3(sr),
                Normal     = ParseVector3(sr),
                NodeIdx    = int.Parse(sr.ReadLine()!),
                NodeWeight = float.Parse(sr.ReadLine()!),
                UV         = ParseVector2(sr),
                Unk2       = int.Parse(sr.ReadLine()!)
            };

            return vert;
        }

        public static Tri ParseTri(StreamReader sr)
        {
            var tri = new Tri
            {
                RegionIdx   = int.Parse(sr.ReadLine()!),
                MaterialIdx = int.Parse(sr.ReadLine()!)
            };

            var indicesLine = sr.ReadLine();
            var indices     = indicesLine.Split('\t');
            tri.VertIdx0 = int.Parse(indices[0]);
            tri.VertIdx1 = int.Parse(indices[1]);
            tri.VertIdx2 = int.Parse(indices[2]);

            return tri;
        }

    #endregion
    }
}