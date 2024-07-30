Shader "ConformalDecals/Decal Back"
{
    Properties 
    {
        [Header(Texture Maps)]
        _MainTex("Main Tex (RGB spec(A))", 2D) = "gray" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _Color ("Color", Color) = (1,1,1,1)
        
        _RowOffset("Row Offset", Range (0, 1)) = 0.5
        
        [Header(Specularity)]
        _SpecColor ("_SpecColor", Color) = (0.5, 0.5, 0.5, 1)
        _Shininess ("Shininess", Range (0.03, 10)) = 0.4
        
        [Header(Effects)]
        [PerRendererData]_Opacity("_Opacity", Range(0,1) ) = 1
        [PerRendererData]_RimFalloff("_RimFalloff", Range(0.01,5) ) = 0.1
        [PerRendererData]_RimColor("_RimColor", Color) = (0,0,0,0)
        [PerRendererData]_UnderwaterFogFactor ("Underwater Fog Factor", Range(0,1)) = 0
    }
    
    SubShader 
    {
        Tags { "RenderType"="Opaque" }
        ZWrite On
        ZTest LEqual

        CGPROGRAM

        #include "LightingKSPDeferred.cginc"
        #pragma surface surf BlinnPhongSmooth vertex:vert
        #pragma target 3.0
        
        sampler2D _MainTex;
        sampler2D _BumpMap;

        half _RowOffset;
        
        half _Shininess;

        float _Opacity;
        float _RimFalloff;
        float4 _RimColor;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 viewDir;
            float3 worldPos;
        };
        
        void vert (inout appdata_full v) {

        }
        
        void surf (Input IN, inout SurfaceOutput o)
        {
            float2 uv_BumpMap = IN.uv_BumpMap;
            float2 uv_MainTex = 0;
            
            // 45° rotation
            uv_MainTex.x = IN.uv_MainTex.x - IN.uv_MainTex.y;
            uv_MainTex.y = IN.uv_MainTex.x + IN.uv_MainTex.y;

            // stagger every other row
            uv_MainTex.y *= 2;
            int row = floor(uv_MainTex.y);
            uv_MainTex.x += row * _RowOffset;

            float4 color = _Color * tex2D(_MainTex,(uv_MainTex));
            float3 normal = UnpackNormal(tex2D(_BumpMap, uv_BumpMap));

            half rim = 1.0 - saturate(dot (normalize(IN.viewDir), normal));
            float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;

            float4 fog = UnderwaterFog(IN.worldPos, color);

            o.Albedo = fog.rgb;
            o.Emission = emission;
            o.Gloss = color.a;
            o.Specular = _Shininess;
            o.Normal = normal;
            o.Emission *= _Opacity * fog.a;
            o.Alpha = _Opacity * fog.a;
        }
        ENDCG
    }
    Fallback "Standard"
}