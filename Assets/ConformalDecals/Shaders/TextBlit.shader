Shader "ConformalDecals/Text Blit"
{
    Properties
    {
        _MainTex("_MainTex (RGB spec(A))", 2D) = "white" {}
        
        _WeightNormal("Weight Normal", float) = 0
        _WeightBold("Weight Bold", float) = 0.5
        
        _ScaleRatioA("Scale RatioA", float) = 1
        _ScaleRatioB("Scale RatioB", float) = 1
        _ScaleRatioC("Scale RatioC", float) = 1
    }
    
    SubShader
    {
        Tags { "Queue" = "Transparent" }
        Cull Off
        ZWrite Off 
        
        Pass
        {
            BlendOp Max

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            
            // font weights to fake bold
            float _WeightNormal;
            float _WeightBold;
            
            // no idea what these do
            float _ScaleRatioA;
            float _ScaleRatioB;
            float _ScaleRatioC;

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct v2f {
                float4 pos : SV_POSITION;
                float4 uv : TEXCOORD0; // u, v, bias, 0
            };

            v2f vert(float4 vertex : POSITION, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) {
                float bold = step(uv1.y, 0);
                float weight = lerp(_WeightNormal, _WeightBold, bold) * _ScaleRatioA / 8.0;
                float bias = 1 - weight;
                
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.uv = float4(uv0.x, uv0.y, bias, weight);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = i.uv.xy;
                float bias = i.uv.z;
                float weight = i.uv.w;
                
                fixed4 c = 0;
                c.r = saturate(tex2D(_MainTex,(uv)).a + weight);
                return c;
            }

            ENDCG
        } 
    }
}    