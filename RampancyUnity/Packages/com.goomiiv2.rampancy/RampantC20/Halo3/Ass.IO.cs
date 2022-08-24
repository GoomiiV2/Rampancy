using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text;

namespace RampantC20.Halo3
{
    public partial class Ass
    {
        public static Ass Load(string filePath)
        {
            var ass = new Ass();
            var sr  = new StreamReader(filePath);

            //try {
            ass.Head = ParseHeader(sr);

            // Materials
            var numMats = int.Parse(sr.ReadValidLine());
            for (int i = 0; i < numMats; i++) {
                var mat = ParseMaterial(sr);
                ass.Materials.Add(mat);
            }

            // Objects
            var numObjects = int.Parse(sr.ReadValidLine());
            for (int i = 0; i < numObjects; i++) {
                var obj = ParseObject(sr);
                ass.Objects.Add(obj);
            }

            // Instances
            var numInstances = int.Parse(sr.ReadValidLine());
            for (int i = 0; i < numInstances; i++) {
                var inst = ParseInstance(sr);
                ass.Instances.Add(inst);
            }

            return ass;
            /*}
            catch (Exception e) {
                Console.WriteLine(e);
                return null;
            }*/
        }

        public void Save(string filepath)
        {
            var dirPath = Path.GetDirectoryName(filepath);
            Directory.CreateDirectory(dirPath);
            
            var data = ToString();
            File.WriteAllText(filepath, data);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"; Exported from Rampancy");
            sb.AppendLine();

            FormatHeader(sb, Head);
            sb.AppendLine();

            // Mats
            sb.AppendLine(";### MATERIALS ###");
            sb.AppendLine($"{Materials.Count}");
            sb.AppendLine();
            
            for (int i = 0; i < Materials.Count; i++) {
                FormatMaterial(sb, Materials[i], i);
            }

            // Objects, Meshes etc
            sb.AppendLine(";### OBJECTS ###");
            sb.AppendLine($"{Objects.Count}");
            sb.AppendLine();
            
            for (int i = 0; i < Objects.Count; i++) {
                FormatObject(sb, Objects[i], i);
            }
            
            // Instances
            sb.AppendLine(";### INSTANCES ###");
            sb.AppendLine($"{Instances.Count}");
            sb.AppendLine();
            
            for (int i = 0; i < Instances.Count; i++) {
                FormatInstance(sb, Instances[i], i);
            }

            return sb.ToString();
        }

    #region Import

        protected static Header ParseHeader(StreamReader sr)
        {
            var header = new Header
            {
                Version           = int.Parse(sr.ReadValidLine()),
                ToolName          = sr.ReadValidLine(),
                ToolVersion       = sr.ReadValidLine(),
                ExportUsername    = sr.ReadValidLine(),
                ExportMachineName = sr.ReadValidLine()
            };

            return header;
        }

        protected static Material ParseMaterial(StreamReader sr)
        {
            var name          = sr.ReadValidLine();
            var hasCollection = name.Contains(' ');
            var nameSplit     = hasCollection ? name.Split(' ') : null; // Skip properies for now
            var matName       = hasCollection ? nameSplit[1] : name;
            var mat = new Material
            {
                Collection = hasCollection ? nameSplit[0] : null,
                Name       = matName.Trim(MaterialSymbols.Symbols), // the flags should already be set
                Group      = sr.ReadValidLine()
            };

            var numExtras = int.Parse(sr.ReadValidLine());
            for (int i = 0; i < numExtras; i++) {
                var line = sr.ReadValidLine().Trim('"');
                var args = line.Split(' ');
                var key  = args[0];

                switch (key) {
                    case Material.BM_FLAGS:
                        mat.Flags = (BmFlags) Convert.ToInt32(args[1], 2);
                        break;
                    case Material.BM_LMRES:
                        mat.LmRes = ParseLmRes(args);
                        break;
                    case Material.BM_LIGHTING_BASIC:
                        mat.Basic = ParseLmBasic(args);
                        break;
                    case Material.BM_LIGHTING_ATTEN:
                        mat.Attenuation = ParseLmAttenuation(args);
                        break;
                    case Material.BM_LIGHTING_FRUS:
                        mat.Fustrum = ParseLmFrustum(args);
                        break;
                }
            }

            return mat;
        }

        protected static LmRes ParseLmRes(string[] args)
        {
            var lmRes = new LmRes
            {
                Res                   = float.Parse(args[1]),
                PhotonFidelity        = int.Parse(args[2]),
                TransparentTint       = new Vector3(float.Parse(args[3]), float.Parse(args[4]), float.Parse(args[5])),
                LightmapTransparency  = int.Parse(args[6]) == 1,
                AdditiveTint          = new Vector3(float.Parse(args[7]), float.Parse(args[8]), float.Parse(args[9])),
                UseShaderGel          = int.Parse(args[10]) == 1,
                IgnoreDefaultResScale = int.Parse(args[11]) == 1
            };

            return lmRes;
        }

        protected static LmBasic ParseLmBasic(string[] args)
        {
            var lmBasic = new LmBasic
            {
                Power         = float.Parse(args[1]),
                Color         = new Vector3(float.Parse(args[2]), float.Parse(args[3]), float.Parse(args[4])),
                Quality       = float.Parse(args[5]),
                PowerPerArea  = int.Parse(args[6]),
                EmissiveFocus = float.Parse(args[7])
            };

            return lmBasic;
        }

        protected static LmAttenuation ParseLmAttenuation(string[] args)
        {
            var lmAttenuation = new LmAttenuation
            {
                Enabled = int.Parse(args[1]) == 1,
                Falloff = float.Parse(args[2]),
                Cutoff  = float.Parse(args[3]),
            };

            return lmAttenuation;
        }

        protected static LmFrustum ParseLmFrustum(string[] args)
        {
            var lmFrustum = new LmFrustum
            {
                Blend   = float.Parse(args[1]),
                Falloff = float.Parse(args[2]),
                Cutoff  = float.Parse(args[3])
            };

            return lmFrustum;
        }

        protected static AssObject ParseObject(StreamReader sr)
        {
            var parsed = Enum.TryParse<ObjectType>(sr.ReadValidLine(), out var objType);
            if (parsed) {
                switch (objType) {
                    case ObjectType.MESH:
                        return ParseMeshObject(sr);
                    case ObjectType.GENERIC_LIGHT:
                        return ParseLightObject(sr);
                    case ObjectType.SPHERE:
                        return ParseSphereObject(sr);
                    case ObjectType.BOX:
                        return ParseBoxObject(sr);
                    default:
                        Debug.WriteLine($"Unsupported type: {objType}");
                        return null;
                }
            }

            return null;
        }

        protected static MeshObject ParseMeshObject(StreamReader sr)
        {
            var mesh = new MeshObject
            {
                Type     = ObjectType.MESH,
                Filepath = sr.ReadValidLine(),
                Name     = sr.ReadValidLine()
            };

            // Verts
            var numVerts = int.Parse(sr.ReadValidLine());
            mesh.Verts = new List<Vertex>(numVerts);
            for (int i = 0; i < numVerts; i++) {
                var vert = ParseVertex(sr);
                mesh.Verts.Add(vert);
            }

            // Tris
            var numTris = int.Parse(sr.ReadValidLine());
            mesh.Tris = new List<Triangle>(numTris);
            for (int i = 0; i < numTris; i++) {
                var tri = ParseTriangle(sr);
                mesh.Tris.Add(tri);
            }

            return mesh;
        }

        protected static SphereObject ParseSphereObject(StreamReader sr)
        {
            var sphere = new SphereObject
            {
                Type     = ObjectType.SPHERE,
                Filepath = sr.ReadValidLine(),
                Name     = sr.ReadValidLine(),
                MatIdx   = int.Parse(sr.ReadValidLine()),
                Radius   = float.Parse(sr.ReadValidLine())
            };

            return sphere;
        }

        protected static BoxObject ParseBoxObject(StreamReader sr)
        {
            var box = new BoxObject
            {
                Type     = ObjectType.BOX,
                Filepath = sr.ReadValidLine(),
                Name     = sr.ReadValidLine(),
                MatIdx   = int.Parse(sr.ReadValidLine()),
                Extents  = ParseVector3(sr)
            };

            return box;
        }

        protected static LightObject ParseLightObject(StreamReader sr)
        {
            var light = new LightObject
            {
                Type                 = ObjectType.GENERIC_LIGHT,
                Filepath             = sr.ReadValidLine(),
                Name                 = sr.ReadValidLine(),
                LightType            = (LightType) Enum.Parse(typeof(LightType), sr.ReadValidLine()),
                Color                = ParseVector3(sr),
                Intensity            = float.Parse(sr.ReadValidLine()),
                HotspotSize          = float.Parse(sr.ReadValidLine()),
                HotspotFalloff       = float.Parse(sr.ReadValidLine()),
                UseNearAttenuation   = int.Parse(sr.ReadValidLine()) == 1,
                NearAttenuationStart = float.Parse(sr.ReadValidLine()),
                NearAttenuationEnd   = float.Parse(sr.ReadValidLine()),
                UseFarAttenuation    = int.Parse(sr.ReadValidLine()) == 1,
                FarAttenuationStart  = float.Parse(sr.ReadValidLine()),
                FarAttenuationEnd    = float.Parse(sr.ReadValidLine()),
                LightShape           = (LightShape) Enum.Parse(typeof(LightShape), sr.ReadValidLine()),
                AspectRatio          = float.Parse(sr.ReadValidLine())
            };

            return light;
        }

        protected static Vertex ParseVertex(StreamReader sr)
        {
            var vert = new Vertex
            {
                Position = ParseVector3(sr),
                Normal   = ParseVector3(sr),
                Color    = ParseVector3(sr)
            };

            var numVertWeight = int.Parse(sr.ReadValidLine());
            vert.Weights = new List<VertexWeight>(numVertWeight);
            for (int i = 0; i < numVertWeight; i++) {
                var weight = ParseVertexWeight(sr);
                vert.Weights.Add(weight);
            }

            var numUvws = int.Parse(sr.ReadValidLine());
            vert.Uvws = new List<Vector3>(numUvws);
            for (int i = 0; i < numUvws; i++) {
                var uvw = ParseVector3(sr);
                vert.Uvws.Add(uvw);
            }

            return vert;
        }

        protected static VertexWeight ParseVertexWeight(StreamReader sr)
        {
            var weight = new VertexWeight
            {
                Index  = int.Parse(sr.ReadValidLine()),
                Weight = float.Parse(sr.ReadValidLine())
            };

            return weight;
        }

        protected static Triangle ParseTriangle(StreamReader sr)
        {
            var line  = sr.ReadValidLine();
            var split = line.Split(new[] {"\t\t", "\t"}, StringSplitOptions.None);

            var tri = new Triangle
            {
                MatIndex = int.Parse(split[0]),
                Vert1Idx = int.Parse(split[1]),
                Vert2Idx = int.Parse(split[2]),
                Vert3Idx = int.Parse(split[3])
            };

            return tri;
        }

        protected static Instance ParseInstance(StreamReader sr)
        {
            var inst = new Instance
            {
                ObjectIdx        = int.Parse(sr.ReadValidLine()),
                Name             = sr.ReadValidLine(),
                UniqueId         = int.Parse(sr.ReadValidLine()),
                ParentId         = int.Parse(sr.ReadValidLine()),
                InheritanceFlags = int.Parse(sr.ReadValidLine()),
                Rotation         = ParseQuaternion(sr),
                Position         = ParseVector3(sr),
                Scale            = float.Parse(sr.ReadValidLine()),
                PivotRotation    = ParseQuaternion(sr),
                PivotPosition    = ParseVector3(sr),
                PivotScale       = float.Parse(sr.ReadValidLine())
            };

            return inst;
        }

        protected static Vector3 ParseVector3(StreamReader sr)
        {
            var line = sr.ReadValidLine();
            var nums = line.Split('\t');
            var vec3 = new Vector3(
                float.TryParse(nums[0], out var x) ? x : 0,
                float.TryParse(nums[1], out var y) ? y : 0,
                float.TryParse(nums[2], out var z) ? z : 0
            );
            return vec3;
        }

    #if UNITY_5
        protected static UnityEngine.Vector3 ParseVector3Unity(StreamReader sr)
        {
            var line = sr.ReadValidLine();
            var nums = line.Split('\t');
            var vec3 = new UnityEngine.Vector3(float.Parse(nums[0]), float.Parse(nums[1]), float.Parse(nums[2]));
            return vec3;
        }
    #endif

        protected static Quaternion ParseQuaternion(StreamReader sr)
        {
            var line = sr.ReadValidLine();
            var nums = line.Split('\t');
            var quat = new Quaternion(float.Parse(nums[0]), float.Parse(nums[1]), float.Parse(nums[2]), float.Parse(nums[3]));
            return quat;
        }

    #if UNITY_5
        protected static UnityEngine.Quaternion ParseQuaternionUnity(StreamReader sr)
        {
            var line = sr.ReadValidLine();
            var nums = line.Split('\t');
            var quat = new UnityEngine.Quaternion(float.Parse(nums[0]), float.Parse(nums[1]), float.Parse(nums[2]), float.Parse(nums[3]));
            return quat;
        }
    #endif

    #endregion

    #region Export

        private static void FormatHeader(StringBuilder sb, Header head)
        {
            sb.AppendLine($";### HEADER ###");
            sb.AppendLine($"{head.Version}");
            sb.AppendLine($"\"{head.ToolName}\"");
            sb.AppendLine($"\"{head.ToolVersion}\"");
            sb.AppendLine($"\"{head.ExportUsername}\"");
            sb.AppendLine($"\"{head.ExportMachineName}\"");
        }

        private static string Vec3ToStr(Vector3 vec) => $"{vec.X:F10} {vec.Y:F10} {vec.Z:F10}";
        private static string BoolToStr(bool val) => val ? "1" : "0";

        private static void FormatVector3(StringBuilder sb, Vector3 vec) => sb.AppendLine($"{vec.X:F10}\t{vec.Y:F10}\t{vec.Z:F10}");
        private static void FormatQuat(StringBuilder sb, Quaternion quat) => sb.AppendLine($"{quat.X:F10}\t{quat.Y:F10}\t{quat.Z:F10}\t{quat.W:F10}");

        private static void FormatMaterial(StringBuilder sb, Material mat, int idx)
        {
            sb.AppendLine($";MATERIAL {idx}");
            sb.AppendLine($"\"{mat.Collection} {mat.Name}\"".Trim(' ')); // TODO: add symbols {(mat.Flags.HasValue ? MaterialSymbols.FlagToSymbols(mat.Flags.Value) : "")}
            sb.AppendLine($"\"{mat.Group}\"");
            
            sb.AppendLine($"{GetNumExtraLinesInMat(mat)}");
            if (mat.Flags       != null) sb.AppendLine($"\"{Material.BM_FLAGS} {Convert.ToString((uint)mat.Flags, 2).PadLeft(22, '0')}\"");
            if (mat.LmRes       != null) sb.AppendLine($"\"{Material.BM_LMRES} {FormatLmRes(mat.LmRes)}\"");
            if (mat.Basic       != null) sb.AppendLine($"\"{Material.BM_LIGHTING_BASIC} {FormatLmBasic(mat.Basic)}\"");
            if (mat.Attenuation != null) sb.AppendLine($"\"{Material.BM_LIGHTING_ATTEN} {FormatLmAttenuation(mat.Attenuation)}\"");
            if (mat.Fustrum     != null) sb.AppendLine($"\"{Material.BM_LIGHTING_FRUS} {FormatLmFrustum(mat.Fustrum)}\"");

            //sb.AppendLine($"; {mat.Flags}");
            
            sb.AppendLine();
        }

        private static int GetNumExtraLinesInMat(Material mat)
        {
            var numLines = 0;
            
            if (mat.Flags       != null) numLines++;
            if (mat.LmRes       != null) numLines++;
            if (mat.Basic       != null) numLines++;
            if (mat.Attenuation != null) numLines++;
            if (mat.Fustrum     != null) numLines++;

            return numLines;
        }

        private static string FormatLmRes(LmRes lmRes)
        {
            var str = $"{lmRes.Res:F10} {lmRes.PhotonFidelity} {Vec3ToStr(lmRes.TransparentTint)} {BoolToStr(lmRes.LightmapTransparency)} {Vec3ToStr(lmRes.AdditiveTint)} {BoolToStr(lmRes.UseShaderGel)} {BoolToStr(lmRes.IgnoreDefaultResScale)}";
            return str;
        }
        
        private static string FormatLmBasic(LmBasic lmBasic)
        {
            var str = $"{lmBasic.Power:F10} {Vec3ToStr(lmBasic.Color)} {lmBasic.Quality:F10} {lmBasic.PowerPerArea} {lmBasic.EmissiveFocus:F10}";
            return str;
        }
        
        private static string FormatLmAttenuation(LmAttenuation lmAtten)
        {
            var str = $"{BoolToStr(lmAtten.Enabled)} {lmAtten.Falloff:F10} {lmAtten.Cutoff:F10}";
            return str;
        }
        
        private static string FormatLmFrustum(LmFrustum lmFust)
        {
            var str = $"{lmFust.Blend:F10} {lmFust.Falloff:F10} {lmFust.Cutoff:F10}";
            return str;
        }

        private static void FormatObject(StringBuilder sb, AssObject obj, int objIdx)
        {
            sb.AppendLine($";OBJECT {objIdx}");
            sb.AppendLine($"\"{obj.Type}\"");
            sb.AppendLine($"\"{obj.Filepath}\"");
            sb.AppendLine($"\"{obj.Name}\"");
            
            switch (obj.Type) {
                case ObjectType.MESH:
                    FormatObjectMesh(sb, obj as MeshObject);
                    break;
                case ObjectType.SPHERE:
                    FormatObjectSphere(sb, obj as SphereObject);
                    break;
                case ObjectType.BOX:
                    FormatObjectBox(sb, obj as BoxObject);
                    break;
                case ObjectType.PILL:
                    FormatObjectPill(sb, obj as PillObject);
                    break;
                case ObjectType.GENERIC_LIGHT:
                    FormatObjectLight(sb, obj as LightObject);
                    break;
            }
        }

        private static void FormatObjectMesh(StringBuilder sb, MeshObject obj)
        {
            sb.AppendLine($"{obj.Verts.Count}");
            foreach (var vert in obj.Verts) {
                FormatVert(sb, vert);
            }
            
            sb.AppendLine($"{obj.Tris.Count}");
            foreach (var tri in obj.Tris) {
                FormatTri(sb, tri);
            }

            sb.AppendLine();
        }
        
        private static void FormatVert(StringBuilder sb, Vertex vert)
        {
            FormatVector3(sb, vert.Position);
            FormatVector3(sb, vert.Normal);
            FormatVector3(sb, vert.Color);

            sb.AppendLine($"{vert.Weights.Count}");
            foreach (var weight in vert.Weights) {
                sb.AppendLine($"{weight.Index}");
                sb.AppendLine($"{weight.Weight:F10}");
            }
            
            sb.AppendLine($"{vert.Uvws.Count}");
            foreach (var uvw in vert.Uvws) {
                FormatVector3(sb, uvw);
            }

            sb.AppendLine();
        }
        
        private static void FormatTri(StringBuilder sb, Triangle tri)
        {
            sb.AppendLine($"{tri.MatIndex}\t\t{tri.Vert1Idx}\t{tri.Vert2Idx}\t{tri.Vert3Idx}");
        }
        
        private static void FormatObjectSphere(StringBuilder sb, SphereObject obj)
        {
            sb.AppendLine($"{obj.MatIdx}");
            sb.AppendLine($"{obj.Radius:F10}");
        }

        private static void FormatObjectPill(StringBuilder sb, PillObject obj)
        {
            sb.AppendLine($"{obj.MatIdx}");
        }

        private static void FormatObjectBox(StringBuilder sb, BoxObject obj)
        {
            sb.AppendLine($"{obj.MatIdx}");
            FormatVector3(sb, obj.Extents);
        }
        
        private static void FormatObjectLight(StringBuilder sb, LightObject obj)
        {
            sb.AppendLine($"\"{obj.LightType}\"");
            FormatVector3(sb, obj.Color);
            sb.AppendLine($"{obj.Intensity:F10}");
            sb.AppendLine($"{obj.HotspotSize:F10}");
            sb.AppendLine($"{obj.HotspotFalloff:F10}");
            sb.AppendLine($"{BoolToStr(obj.UseNearAttenuation)}");
            sb.AppendLine($"{obj.NearAttenuationStart:F10}");
            sb.AppendLine($"{obj.NearAttenuationEnd:F10}");
            sb.AppendLine($"{BoolToStr(obj.UseFarAttenuation)}");
            sb.AppendLine($"{obj.FarAttenuationStart:F10}");
            sb.AppendLine($"{obj.FarAttenuationEnd:F10}");
            sb.AppendLine($"{(int)obj.LightShape}");
            sb.AppendLine($"{obj.AspectRatio:F10}");

            sb.AppendLine();
        }
        
        private void FormatInstance(StringBuilder sb, Instance inst, int idx)
        {
            sb.AppendLine($";INSTANCE {idx}");
            sb.AppendLine($"{inst.ObjectIdx}");
            sb.AppendLine($"\"{inst.Name}\"");
            sb.AppendLine($"{inst.UniqueId}");
            sb.AppendLine($"{inst.ParentId}");
            sb.AppendLine($"{inst.InheritanceFlags}");
            FormatQuat(sb, inst.Rotation);
            FormatVector3(sb, inst.Position);
            sb.AppendLine($"{inst.Scale:F10}");
            FormatQuat(sb, inst.PivotRotation);
            FormatVector3(sb, inst.PivotPosition);
            sb.AppendLine($"{inst.PivotScale:F10}");

            sb.AppendLine();
        }

    #endregion
    }
}