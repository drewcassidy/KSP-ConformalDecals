Shader "ConformalDecals/UI/Color Swatch"
{
    Properties
    {
        _Color("Color", Color) = (0,0,0,0)
        _Radius("Radius", Float) = 4

        _OutlineGradient("Outline Gradient Step", Range (0, 1)) = 0.6
        _OutlineOpacity("Outline Opacity", Range (0, 0.5)) = 0.1
        _OutlineWidth("Outline Width", Float) = 3

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma require integers
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "HSL.cginc"
            #include "../SDF.cginc"
            
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local HUE RED GREEN BLUE
            
            float4 _ClipRect;
            float _Radius;
            float4 _Color;

            float _OutlineGradient;
            float _OutlineOpacity;
            float _OutlineWidth;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPosition = v.vertex;
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 color = _Color;
                
                color.a = saturate(0.5 - sdRoundedUVBox(i.uv, _Radius));
                
                float rrect = sdRoundedUVBox(i.uv, _Radius);
                float gradient = smoothstep(_OutlineGradient, 1 - _OutlineGradient, i.uv.y);
                float outlineOpacity = _OutlineOpacity * smoothstep(-1*_OutlineWidth, 0, rrect);
                
                color.rgb = lerp(color.rgb, gradient, outlineOpacity);
                
                #ifdef UNITY_UI_CLIP_RECT
                    color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip (color.a - 0.001);
                #endif
                
                return color;
            }
            ENDCG
        }
    }
}