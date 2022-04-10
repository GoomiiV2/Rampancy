using RampantC20.Halo3;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Rampancy.Halo3
{
    [CustomEditor (typeof(Light))]
    public class LightEditor : Editor
    {
        void OnEnable()
        {
            target.GetComponent<UnityEngine.Light>().hideFlags = HideFlags.HideInInspector;
        }

        public override void OnInspectorGUI()
        {
            var light = target as Light;

            light.Type      = (Ass.LightType)EditorGUILayout.EnumPopup("Light Type", light.Type);
            light.Color     = EditorGUILayout.ColorField("Color", light.Color);
            light.Intensity = EditorGUILayout.Slider("Intensity", light.Intensity, 0, 1000);
            
            light.HotspotSize    = EditorGUILayout.Slider("Hotspot Size", light.HotspotSize, 0, 360);
            light.HotspotFalloff = EditorGUILayout.Slider("Hotspot Falloff", light.HotspotFalloff, 0, 10000);

            light.UseNearAttenuation = EditorGUILayout.Toggle("Use Near Attenuation", light.UseNearAttenuation);
            if (light.UseNearAttenuation) {
                light.NearAttenuationStart = EditorGUILayout.Slider("Near Attenuation Start", light.NearAttenuationStart, 0, 10000);
                light.NearAttenuationEnd   = EditorGUILayout.Slider("Near Attenuation End", light.NearAttenuationEnd, 0, 10000);
            }
            
            light.UseFarAttenuation = EditorGUILayout.Toggle("Use Far Attenuation", light.UseFarAttenuation);
            if (light.UseFarAttenuation) {
                light.FarAttenuationStart = EditorGUILayout.Slider("Far Attenuation Start", light.FarAttenuationStart, 0, 10000);
                light.FarAttenuationEnd   = EditorGUILayout.Slider("Far Attenuation End", light.FarAttenuationEnd, 0, 10000);
            }
            
            light.LightShape  = (Ass.LightShape)EditorGUILayout.EnumPopup("Light Shape", light.LightShape);
            light.AspectRatio = EditorGUILayout.Slider("Aspect Ratio", light.AspectRatio, 0, 1000);

            light.OnValidate();
        }
    }
}