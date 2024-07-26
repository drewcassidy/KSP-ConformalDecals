Shader "ConformalDecals/SelectionGlow"
{
    Properties
    {
        [Header(Effects)]
        _RimEdgeGlow("Rim Edge Glow", Range(0, 1)) = 0.45
        _RimEdgePow("Rim Edge Falloff", Range(0,5)) = 5
        _RimEdgeOpacity("Rim Edge Opacity", Range(0,1)) = 0.2
        _RimFalloff("Rim Falloff", Range(0.01,5) ) = 0.1
        _RimColor("Rim Color", Color) = (0,0,0,0)
        _RimOpacity("Rim Opacity", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "true" }
        Cull Back
        ZWrite Off 
        
        Pass
        {
            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            half _RimEdgeGlow;
            half _RimEdgePow;
            half _RimEdgeOpacity;
            float _RimFalloff;
            float4 _RimColor;
            half _RimOpacity;
            
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPosition : TEXCOORD1;
                half3 worldNormal : TEXCOORD2;
            };

            v2f vert(float4 vertex : POSITION, float3 normal : NORMAL, float2 uv : TEXCOORD0) {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.uv = uv;
                o.worldPosition = mul(unity_ObjectToWorld, vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 c = 0;

                float3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPosition));
                half rim = 1.0 - saturate(dot (normalize(worldViewDir), i.worldNormal));
                c.rgb = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a * _RimOpacity;
                half edgeGlow = 0;
                edgeGlow = max(edgeGlow, pow(1-saturate(i.uv.x / _RimEdgeGlow), _RimEdgePow));
                edgeGlow = max(edgeGlow, pow(1-saturate(i.uv.y / _RimEdgeGlow), _RimEdgePow));
                edgeGlow = max(edgeGlow, pow(1-saturate((1-i.uv.x) / _RimEdgeGlow), _RimEdgePow));
                edgeGlow = max(edgeGlow, pow(1-saturate((1-i.uv.y) / _RimEdgeGlow), _RimEdgePow));

                c.rgb = max(c.rgb, _RimColor.rgb * _RimColor.a * edgeGlow * _RimEdgeOpacity);

                return c;
            }

            ENDCG
        } 
    }
}    