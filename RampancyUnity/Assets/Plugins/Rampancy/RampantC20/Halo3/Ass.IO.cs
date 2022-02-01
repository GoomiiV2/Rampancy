using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Newtonsoft.Json.Serialization;

namespace RampantC20.Halo3
{
    public partial class Ass
    {
        public static Ass Load(string filePath)
        {
            var ass = new Ass();
            var sr  = new StreamReader(filePath);

            try {
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
            }
            catch (Exception e) {
                Console.WriteLine(e);
                return null;
            }
        }

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
            var mat = new Material
            {
                Name  = sr.ReadValidLine(),
                Group = sr.ReadValidLine()
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
                LightmapTransparency  = int.Parse(args[6]),
                AdditiveTint          = new Vector3(float.Parse(args[7]), float.Parse(args[8]), float.Parse(args[9])),
                UseShaderGel          = int.Parse(args[10]) == 1,
                IngoreDefaultResScale = int.Parse(args[11]) == 1
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
                LightShape           = sr.ReadValidLine(),
                AspectRatio          = float.Parse(sr.ReadValidLine())
            };

            return light;
        }

        protected static Vertex ParseVertex(StreamReader sr)
        {
            var vert = new Vertex
            {
                Position  = ParseVector3(sr),
                Normal  = ParseVector3(sr),
                Color = ParseVector3(sr)
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
                Rotation         = ParseQuaternionUnity(sr),
                Position         = ParseVector3Unity(sr),
                Scale            = float.Parse(sr.ReadValidLine()),
                PivotRotation    = ParseQuaternionUnity(sr),
                PivotPosition    = ParseVector3Unity(sr),
                PivotScale       = float.Parse(sr.ReadValidLine())
            };

            return inst;
        }

        protected static Vector3 ParseVector3(StreamReader sr)
        {
            var line = sr.ReadValidLine();
            var nums = line.Split('\t');
            var vec3 = new Vector3(float.Parse(nums[0]), float.Parse(nums[1]), float.Parse(nums[2]));
            return vec3;
        }
        
        protected static UnityEngine.Vector3 ParseVector3Unity(StreamReader sr)
        {
            var line = sr.ReadValidLine();
            var nums = line.Split('\t');
            var vec3 = new UnityEngine.Vector3(float.Parse(nums[0]), float.Parse(nums[1]), float.Parse(nums[2]));
            return vec3;
        }

        protected static Quaternion ParseQuaternion(StreamReader sr)
        {
            var line = sr.ReadValidLine();
            var nums = line.Split('\t');
            var quat = new Quaternion(float.Parse(nums[0]), float.Parse(nums[1]), float.Parse(nums[2]), float.Parse(nums[3]));
            return quat;
        }
        
        protected static UnityEngine.Quaternion ParseQuaternionUnity(StreamReader sr)
        {
            var line = sr.ReadValidLine();
            var nums = line.Split('\t');
            var quat = new UnityEngine.Quaternion(float.Parse(nums[0]), float.Parse(nums[1]), float.Parse(nums[2]), float.Parse(nums[3]));
            return quat;
        }
    }
}