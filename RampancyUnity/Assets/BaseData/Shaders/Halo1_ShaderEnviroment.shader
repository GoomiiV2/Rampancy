// Upgrade NOTE: replaced 'defined IS_ALPHA_TESTED' with 'defined (IS_ALPHA_TESTED)'

Shader "Rampancy/Halo1_ShaderEnviroment"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap ("BumpMap (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0
        _Metallic ("Metallic", Range(0,1)) = 1
        _NormalStrength ("Normal Strength", Range(0,1)) = 0.015
        _UseBumpAlpha ("Use Bump Alpha", Range(-1, 0)) = 0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _NormalStrength;
        float _UseBumpAlpha;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float3 HeightToNormal(float height, float3 normal, float3 pos)
        {
            float3 worldDirivativeX = ddx(pos);
            float3 worldDirivativeY = ddy(pos);
            float3 crossX = cross(normal, worldDirivativeX);
            float3 crossY = cross(normal, worldDirivativeY);
            float3 d = abs(dot(crossY, worldDirivativeX));
            float3 inToNormal = ((((height + ddx(height)) - height) * crossY) + (((height + ddy(height)) - height) * crossX)) * sign(d);
            inToNormal.y *= -1.0;
            return normalize((d * normal) - inToNormal);
        }
 
        float3 WorldToTangentNormalVector(Input IN, float3 normal) {
            float3 t2w0 = WorldNormalVector(IN, float3(1,0,0));
            float3 t2w1 = WorldNormalVector(IN, float3(0,1,0));
            float3 t2w2 = WorldNormalVector(IN, float3(0,0,1));
            float3x3 t2w = float3x3(t2w0, t2w1, t2w2);
            return normalize(mul(t2w, normal));
        }
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 bump = tex2D(_BumpMap, IN.uv_MainTex);
            clip(_UseBumpAlpha * (1 - bump.a));
            
            half h = tex2D(_BumpMap, IN.uv_MainTex).r * - _NormalStrength;
            IN.worldNormal = WorldNormalVector(IN, float3(0,0,1));
            float3 worldNormal = HeightToNormal(h, IN.worldNormal, IN.worldPos);
 
            o.Normal = WorldToTangentNormalVector(IN, worldNormal);

            //o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            fixed4 n = tex2D (_BumpMap, IN.uv_MainTex);
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = bump.a;
        }
        
        ENDCG
    }
    FallBack "Diffuse"
}
