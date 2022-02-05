using System;
using System.Collections.Generic;
using UnityEngine;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;

namespace RampantC20.Halo3
{
    public partial class Ass
    {
        public Header          Head      = new();
        public List<Material>  Materials = new();
        public List<AssObject> Objects   = new();
        public List<Instance>  Instances = new();

        public class Header
        {
            public int    Version;
            public string ToolName;
            public string ToolVersion;
            public string ExportUsername;
            public string ExportMachineName;
        }

        public class Material
        {
            public const string BM_FLAGS          = "BM_FLAGS";
            public const string BM_LMRES          = "BM_LMRES";
            public const string BM_LIGHTING_BASIC = "BM_LIGHTING_BASIC";
            public const string BM_LIGHTING_ATTEN = "BM_LIGHTING_ATTEN";
            public const string BM_LIGHTING_FRUS  = "BM_LIGHTING_FRUS";

            public string Name;
            public string Group;

            public BmFlags?      Flags       = null;
            public LmRes         LmRes       = null;
            public LmBasic       Basic       = null;
            public LmAttenuation Attenuation = null;
            public LmFrustum     Fustrum     = null;
        }

        [Flags]
        public enum BmFlags : int
        {
            None,
            TwoSided,
            TransparentOneSided,
            TransparentTwoSided,
            RenderOnly,
            CollisionOnly,
            SphereCollisionOnly,
            FogPlane,
            Ladder,
            Breakable,
            AiDefeaning,
            NoShadow,
            ShadowOnly,
            LightmapOnly,
            Precise,
            Conveyor,
            PortalOneWay,
            PortalDoor,
            PortalVisBlocker,
            IngoredByLightmaps,
            BlocksSound,
            DecalOffset,
            SlipSurface
        }

        public class LmRes
        {
            public float   Res;
            public int     PhotonFidelity;
            public Vector3 TransparentTint;
            public int     LightmapTransparency;
            public Vector3 AdditiveTint;
            public bool    UseShaderGel;
            public bool    IngoreDefaultResScale;
        }

        public class LmBasic
        {
            public float   Power;
            public Vector3 Color;
            public float   Quality;
            public int     PowerPerArea;
            public float   EmissiveFocus;
        }

        public class LmAttenuation
        {
            public bool  Enabled;
            public float Falloff;
            public float Cutoff;
        }

        public class LmFrustum
        {
            public float Blend;
            public float Falloff;
            public float Cutoff;
        }

        public class AssObject
        {
            public ObjectType Type;
            public string     Filepath;
            public string     Name;
        }

        public class LightObject : AssObject
        {
            public LightType LightType;
            public Vector3   Color;
            public float     Intensity;
            public float     HotspotSize;
            public float     HotspotFalloff;
            public bool      UseNearAttenuation;
            public float     NearAttenuationStart;
            public float     NearAttenuationEnd;
            public bool      UseFarAttenuation;
            public float     FarAttenuationStart;
            public float     FarAttenuationEnd;
            public string    LightShape;
            public float     AspectRatio;
        }

        public class MeshObject : AssObject
        {
            public List<Vertex>   Verts;
            public List<Triangle> Tris;
        }

        public class SphereObject : AssObject
        {
            public int MatIdx;
            public float Radius;
        }

        public enum ObjectType
        {
            GENERIC_LIGHT,
            SPHERE,
            BOX,
            PILL,
            MESH
        }

        public enum LightType
        {
            SPOT_LGT,
            DIRECT_LGT,
            OMNI_LGT,
            AMBIENT_LGT
        }

        public class Vertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector3 Color;

            public List<VertexWeight> Weights;
            public List<Vector3>      Uvws;
        }

        public class VertexWeight
        {
            public int   Index;
            public float Weight;
        }

        public class Triangle
        {
            public int MatIndex;
            public int Vert1Idx;
            public int Vert2Idx;
            public int Vert3Idx;
        }

        public class Instance
        {
            public int                    ObjectIdx;
            public string                 Name;
            public int                    UniqueId;
            public int                    ParentId;
            public int                    InheritanceFlags;
            public Quaternion             Rotation;
            public Vector3                Position;
            public float                  Scale;
            public Quaternion             PivotRotation;
            public Vector3                PivotPosition;
            public float                  PivotScale;
        }
    }
}