using System;
using RampantC20.Halo3;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rampancy.Halo3
{
    [RequireComponent(typeof(UnityEngine.Light))]
    public class Light : MonoBehaviour
    {
        public Ass.LightType Type;
        public Color         Color          = Color.white;
        public float         Intensity      = 1.0f;
        public float         HotspotSize    = 10f;
        public float         HotspotFalloff = 1f;

        public bool  UseNearAttenuation   = false;
        public float NearAttenuationStart = 0f;
        public float NearAttenuationEnd   = 400.0f;

        public bool  UseFarAttenuation   = false;
        public float FarAttenuationStart = 400.9f;
        public float FarAttenuationEnd   = 1000.0f;

        public Ass.LightShape LightShape;
        public float          AspectRatio = 1.0f;

        public void OnValidate()
        {
            var unityLight = GetComponent<UnityEngine.Light>();
            if (Type == Ass.LightType.AMBIENT_LGT) {
                RenderSettings.ambientLight     = Color;
                RenderSettings.ambientIntensity = Intensity;
                RenderSettings.ambientMode      = AmbientMode.Flat;
                unityLight.enabled              = false;
                return;
            }
            
            RenderSettings.ambientLight     = Color.white;
            RenderSettings.ambientIntensity = 0;
            RenderSettings.ambientMode      = AmbientMode.Flat;
            unityLight.enabled              = true;

                var lightType = Type switch
            {
                Ass.LightType.OMNI_LGT   => LightType.Point,
                Ass.LightType.SPOT_LGT   => LightType.Spot,
                Ass.LightType.DIRECT_LGT => LightType.Directional,
                _                        => LightType.Point
            };

            unityLight.type      = lightType;
            unityLight.color     = Color;
            unityLight.intensity = Intensity;
            unityLight.spotAngle = HotspotSize;
            unityLight.range     = Intensity + 2; // Just a rough approximate
        }
    }
}