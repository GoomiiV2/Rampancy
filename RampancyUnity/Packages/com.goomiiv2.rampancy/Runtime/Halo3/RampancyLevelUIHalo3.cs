using System;
using System.Collections.Generic;
using Rampancy.Common;
using Rampancy.Halo3;
using RampantC20.Halo3;
using UnityEditor;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

namespace Rampancy.UI
{
    public partial class RampancyLevelUI
    {
        private static Dictionary<Ass.BmFlags, string> FlagDescriptions = new()
        {
            {Ass.BmFlags.TwoSided, "Two-sided path findable geometry. This flag or shader symbol when applied to a material that is applied to a face or surface renders both sides of the surface instead of just the side that the normal is facing."},
            {Ass.BmFlags.TransparentOneSided, "One-sided but non-manifold collidable geometry."},
            {Ass.BmFlags.TransparentTwoSided, "Two-sided collidable geometry that is not connected to or touching one-sided geometry."},
            {Ass.BmFlags.RenderOnly, "Non-collidable, Non-solid geometry."},
            {Ass.BmFlags.CollisionOnly, "Non-rendered geometry."},
            {Ass.BmFlags.SphereCollisionOnly, "Non-rendered geometry that ray tests pass through but spheres (bipeds and vehicles) will not."},
            {
                Ass.BmFlags.FogPlane,
                "Non-collidable fog plane. This shader symbol when applied to a material that is applied to a face or surface makes the surface not be rendered. The faces acts as a fog plane that can be used to define a volumetric fog region."
            },
            {Ass.BmFlags.Ladder, "Climbable geometry. This flag or shader symbol when applied to a material that is applied to a face or surface sets the surface up to act as a ladder for the player."},
            {Ass.BmFlags.Breakable, "Two-sided breakable geometry."},
            {Ass.BmFlags.AiDeafening, "A portal that does not propagate sound. This property does not apply to multiplayer levels."},
            {Ass.BmFlags.NoShadow, "Does not cast real time shadows."},
            {Ass.BmFlags.ShadowOnly, "Casts real time shadows but is not visible."},
            {Ass.BmFlags.LightmapOnly, "Emits light in the light mapper but is otherwise non-existent. (non-collidable and non-rendered)"},
            {Ass.BmFlags.Precise, "Points and triangles are precise and will not be fiddled with in the BSP pass."},
            {Ass.BmFlags.Conveyor, "Geometry which will have a surface coordinate system and velocity. This has been deprecated and no longer functions."},
            {Ass.BmFlags.PortalOneWay, "Portal can only be seen through in a single direction."},
            {Ass.BmFlags.PortalDoor, "Portal visibility is attached to a device machine state."},
            {Ass.BmFlags.PortalVisBlocker, "Portal visibility is completely blocked by this portal."},
            {Ass.BmFlags.DislikesPhotons, "Photons from sky/sun quads will ignore these materials."},
            {Ass.BmFlags.IgnoredByLightmaps, "Lightmapper will not add this geometry to it's raytracing scene representation."},
            {Ass.BmFlags.BlocksSound, "Portal that does not propagate any sound."},
            {Ass.BmFlags.DecalOffset, "Offsets the faces that this material is applied to as it would normally for a decal."},
            {Ass.BmFlags.WaterSurface, "Sets the surface to be a water surface."},
            {Ass.BmFlags.SlipSurface, "Units (bipeds and vehicles) will slip off this surface."},
            {Ass.BmFlags.GroupTransparentsbyPlane, "Group transparent geometry by fitted planes."}
        };

        private static Queue<SceneMatInfoHalo3> H3MatInfosPendingSave = new();
        private static PostponableAction        H3SaveChangedMatInfos = new(TimeSpan.FromSeconds(1), H3SaveChangedMats);

        private static bool DrawMaterialInfoHalo3Basic(SceneMatInfo matInfoIn)
        {
            var needsSave   = false;
            var toggleWidth = 155f;
            var matInfo     = (SceneMatInfoHalo3) matInfoIn;

            if (matInfo.MatMeta == null || matInfo.MatMeta.IsLevelMat == false)
                return false;

            EditorGUI.BeginChangeCheck();
            // @formatter:off
            GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                    needsSave |= ToggleHalo3Flag("Two Sided", matInfo.MatMeta, Ass.BmFlags.TwoSided, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Sphere Collision Only", matInfo.MatMeta, Ass.BmFlags.SphereCollisionOnly, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Transparent One Sided", matInfo.MatMeta, Ass.BmFlags.TransparentOneSided, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Transparent Two Sided", matInfo.MatMeta, Ass.BmFlags.TransparentTwoSided, toggleWidth);
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                    needsSave |= ToggleHalo3Flag("Collision Only", matInfo.MatMeta, Ass.BmFlags.CollisionOnly, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Breakable", matInfo.MatMeta, Ass.BmFlags.Breakable, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Ladder", matInfo.MatMeta, Ass.BmFlags.Ladder, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Render Only", matInfo.MatMeta, Ass.BmFlags.RenderOnly, toggleWidth);
                GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            // @formatter:on 
            
            var hasChanged = EditorGUI.EndChangeCheck();
            if (hasChanged) {
                H3MatInfosPendingSave.Enqueue(matInfo);
                H3SaveChangedMatInfos.Invoke();
            }

            return true;
        }

        private static void DrawMaterialInfoHalo3Adv(SceneMatInfo matInfoIn)
        {
            var needsSave   = false;
            var toggleWidth = 190f;
            var matInfo     = (SceneMatInfoHalo3) matInfoIn;

            if (matInfo.MatMeta == null)
                return;
            
            EditorGUI.BeginChangeCheck();

            // @formatter:off
            GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                    needsSave |= ToggleHalo3Flag("Precise", matInfo.MatMeta, Ass.BmFlags.Precise, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Ai Defeating", matInfo.MatMeta, Ass.BmFlags.AiDeafening, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Shadow Only", matInfo.MatMeta, Ass.BmFlags.ShadowOnly, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Blocks Sound", matInfo.MatMeta, Ass.BmFlags.BlocksSound, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Slip Surface", matInfo.MatMeta, Ass.BmFlags.SlipSurface, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Portal Door", matInfo.MatMeta, Ass.BmFlags.PortalDoor, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Portal Vis Blocker", matInfo.MatMeta, Ass.BmFlags.PortalVisBlocker, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Group Transparents By Plane", matInfo.MatMeta, Ass.BmFlags.GroupTransparentsbyPlane, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Ignored by Lightmapper", matInfo.MatMeta, Ass.BmFlags.IgnoredByLightmaps, toggleWidth);
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                    needsSave |= ToggleHalo3Flag("Fog Plane", matInfo.MatMeta, Ass.BmFlags.FogPlane, toggleWidth);
                    needsSave |= ToggleHalo3Flag("No Shadow", matInfo.MatMeta, Ass.BmFlags.NoShadow, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Lightmap Only", matInfo.MatMeta, Ass.BmFlags.LightmapOnly, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Conveyor", matInfo.MatMeta, Ass.BmFlags.Conveyor, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Portal One Way", matInfo.MatMeta, Ass.BmFlags.PortalOneWay, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Ignored By Lightmaps", matInfo.MatMeta, Ass.BmFlags.IgnoredByLightmaps, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Decal Offset", matInfo.MatMeta, Ass.BmFlags.DecalOffset, toggleWidth);
                    needsSave |= ToggleHalo3Flag("Water Surface", matInfo.MatMeta, Ass.BmFlags.WaterSurface, toggleWidth);
                GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            // @formatter:on 

            matInfo.MatMeta.LmRes ??= new()
            {
                Res                   = 1,
                PhotonFidelity        = 1,
                TransparentTint       = Vector3.Zero,
                LightmapTransparency  = false,
                AdditiveTint          = Vector3.Zero,
                UseShaderGel          = false,
                IgnoreDefaultResScale = false
            };
            matInfo.MatMeta.LmBasic ??= new();
            matInfo.MatMeta.LmAttenuation ??= new()
            {
                Enabled = false,
                Falloff = 1000,
                Cutoff  = 2000
            };
            matInfo.MatMeta.LmFrustum ??= new()
            {
                Blend   = 0,
                Falloff = 25,
                Cutoff  = 25
            };

            GUILayout.Space(10);
            DrawTitle("  Lightmapping Res Settings", 12);

            if (matInfo.MatMeta.LmRes != null) {
                matInfo.MatMeta.LmRes.Res                   = EditorGUILayout.FloatField(new GUIContent("    Lightmap Res", ""), matInfo.MatMeta.LmRes.Res);
                matInfo.MatMeta.LmRes.PhotonFidelity        = EditorGUILayout.IntField(new GUIContent("    Photon Fidelity", ""), matInfo.MatMeta.LmRes.PhotonFidelity);
                matInfo.MatMeta.LmRes.TransparentTint       = ColorField("    Transparent Tint", "", matInfo.MatMeta.LmRes.TransparentTint);
                matInfo.MatMeta.LmRes.LightmapTransparency  = EditorGUILayout.Toggle(new GUIContent("    Lightmap Transparency", ""), matInfo.MatMeta.LmRes.LightmapTransparency);
                matInfo.MatMeta.LmRes.AdditiveTint          = ColorField("    Additive Tint", "", matInfo.MatMeta.LmRes.AdditiveTint);
                matInfo.MatMeta.LmRes.UseShaderGel          = EditorGUILayout.Toggle(new GUIContent("    Use Shader Gel", ""), matInfo.MatMeta.LmRes.UseShaderGel);
                matInfo.MatMeta.LmRes.IgnoreDefaultResScale = EditorGUILayout.Toggle(new GUIContent("    Ignore Default Res Scale   ", ""), matInfo.MatMeta.LmRes.IgnoreDefaultResScale);
            }

            GUILayout.Space(10);
            DrawTitle("  Lightmapping Emit Basic Settings", 12);

            if (matInfo.MatMeta.LmBasic != null) {
                matInfo.MatMeta.LmBasic.Power         = EditorGUILayout.FloatField(new GUIContent("    Power", ""), matInfo.MatMeta.LmBasic.Power);
                matInfo.MatMeta.LmBasic.Color         = ColorField("    Color", "", matInfo.MatMeta.LmBasic.Color);
                matInfo.MatMeta.LmBasic.Quality       = EditorGUILayout.FloatField(new GUIContent("    Quality", ""), matInfo.MatMeta.LmBasic.Quality);
                matInfo.MatMeta.LmBasic.PowerPerArea  = EditorGUILayout.IntField(new GUIContent("    Power Per Area", ""), matInfo.MatMeta.LmBasic.PowerPerArea);
                matInfo.MatMeta.LmBasic.EmissiveFocus = EditorGUILayout.FloatField(new GUIContent("    Emissive Focus", ""), matInfo.MatMeta.LmBasic.EmissiveFocus);
            }

            GUILayout.Space(10);
            DrawTitle("  Lightmapping Emit Frustum Settings", 12);

            if (matInfo.MatMeta.LmAttenuation != null) {
                matInfo.MatMeta.LmAttenuation.Enabled = EditorGUILayout.Toggle(new GUIContent("    Enabled", ""), matInfo.MatMeta.LmAttenuation.Enabled);
                matInfo.MatMeta.LmAttenuation.Falloff = EditorGUILayout.FloatField(new GUIContent("    Falloff", ""), matInfo.MatMeta.LmAttenuation.Falloff);
                matInfo.MatMeta.LmAttenuation.Cutoff  = EditorGUILayout.FloatField(new GUIContent("    Cutoff", ""), matInfo.MatMeta.LmAttenuation.Cutoff);
            }

            GUILayout.Space(10);
            DrawTitle("  Lightmapping Emit Attenuation Settings", 12);

            if (matInfo.MatMeta.LmFrustum != null) {
                matInfo.MatMeta.LmFrustum.Blend   = EditorGUILayout.FloatField(new GUIContent("    Blend", ""), matInfo.MatMeta.LmFrustum.Blend);
                matInfo.MatMeta.LmFrustum.Falloff = EditorGUILayout.FloatField(new GUIContent("    Falloff", ""), matInfo.MatMeta.LmFrustum.Falloff);
                matInfo.MatMeta.LmFrustum.Cutoff  = EditorGUILayout.FloatField(new GUIContent("    Cutoff", ""), matInfo.MatMeta.LmFrustum.Cutoff);
            }

            EditorGUILayout.Separator();

            var hasChanged = EditorGUI.EndChangeCheck();
            if (hasChanged) {
                Debug.Log("Mat data changed");
                H3MatInfosPendingSave.Enqueue(matInfo);
                H3SaveChangedMatInfos.Invoke();
            }
        }

        private static bool ToggleHalo3Flag(string name, MatInfo matInfo, Ass.BmFlags flag, float width = 150f)
        {
            bool isSet = matInfo.Flags.HasFlag(flag);
            var  desc  = FlagDescriptions[flag];
            //char flagSymbol     = Ass.MaterialSymbols.FlagToSymbol(flag);
            bool checkBoxResult = EditorGUILayout.ToggleLeft(new GUIContent(name, desc), isSet, GUILayout.Width(width));
            if (checkBoxResult) {
                matInfo.Flags |= flag;
            }
            else {
                matInfo.Flags &= ~flag;
            }

            bool hasChanged = isSet != checkBoxResult;
            return hasChanged;
        }

        private static void H3SaveChangedMats()
        {
            foreach (var matInfo in H3MatInfosPendingSave) {
                matInfo.MatMeta.Save(matInfo.MatPath);
            }
        }
    }
}