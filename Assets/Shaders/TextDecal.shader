Shader "ConformalDecals/Decal/Text"
{
    Properties
    {
        [Header(Decal)]
        [Toggle(DECAL_FILL)] _Fill ("Fill", int) = 0
        _Decal("Decal Texture", 2D) = "gray" {}
        _DecalColor("Decal Color", Color) = (1,1,1,1)
        
        _Weight("Text Weight", Range(0,1)) = 0
        
        [Header(Outline)]
        [Toggle(DECAL_OUTLINE)] _Outline ("Outline", int) = 0
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth("Outline Width", Range(0,1)) = 0.1

        [Header(Normal)]
        [Toggle(DECAL_BASE_NORMAL)] _BaseNormal ("Use Base Normal", int) = 0
        _BumpMap("Bump Map", 2D) = "bump" {}
        _EdgeWearStrength("Edge Wear Strength", Range(0,500)) = 100
        _EdgeWearOffset("Edge Wear Offset", Range(0,1)) = 0.1
    
        [Header(Specularity)]
        [Toggle(DECAL_SPECMAP)] _Decal_SpecMap ("Has SpecMap", int) = 0
        _SpecMap ("Specular Map)", 2D) = "black" {}
        _SpecColor ("_SpecColor", Color) = (0.25, 0.25, 0.25, 1)
        _Shininess ("Shininess", Range (0.03, 10)) = 0.3

        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        _DecalOpacity("Opacity", Range(0,1) ) = 1
        _Background("Background Color", Color) = (0.9,0.9,0.9,0.7)
        
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", int) = 2
        [Toggle] _ZWrite ("ZWrite", Float) = 1.0

        [Toggle(DECAL_PREVIEW)] _Preview ("Preview", int) = 0

        [Header(Effects)]
        [PerRendererData]_Opacity("_Opacity", Range(0,1) ) = 1
        _Color("_Color", Color) = (1,1,1,1)
        [PerRendererData]_RimFalloff("_RimFalloff", Range(0.01,5) ) = 0.1
        [PerRendererData]_RimColor("_RimColor", Color) = (0,0,0,0)
        [PerRendererData]_UnderwaterFogFactor ("Underwater Fog Factor", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Geometry+100" "IgnoreProjector" = "true" "DisableBatching" = "true"}
        Cull [_Cull]
        
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }
            ZWrite [_ZWrite] 
            ZTest LEqual  
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_forward

            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap
            #pragma skip_variants SHADOWS_DEPTH SHADOWS_CUBE SHADOWS_SHADOWMASK LIGHTMAP_SHADOW_MIXING POINT_COOKIE
            #pragma multi_compile_local __ DECAL_PREVIEW
            #pragma multi_compile_local __ DECAL_BASE_NORMAL
            #pragma multi_compile_local __ DECAL_SPECMAP
            #pragma multi_compile_local __ DECAL_OUTLINE
            #pragma multi_compile_local __ DECAL_FILL

            #include "UnityCG.cginc"
            #include "DecalsCommon.cginc"
            #include "DecalsSurface.cginc"
            #include "SDF.cginc"
            #include "TextDecal.cginc"
 
            ENDCG
        } 
        
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardAdd" }
            ZWrite Off
            ZTest LEqual  
            Blend One One
            Offset -1, -1

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_forward

            #pragma multi_compile_fwdadd nolightmap nodirlightmap nodynlightmap
            #pragma skip_variants SHADOWS_DEPTH SHADOWS_CUBE SHADOWS_SHADOWMASK LIGHTMAP_SHADOW_MIXING POINT_COOKIE
            #pragma multi_compile_local __ DECAL_PREVIEW
            #pragma multi_compile_local __ DECAL_BASE_NORMAL
            #pragma multi_compile_local __ DECAL_SPECMAP
            #pragma multi_compile_local __ DECAL_OUTLINE
            #pragma multi_compile_local __ DECAL_FILL
  
            #include "UnityCG.cginc"
            #include "DecalsCommon.cginc"
            #include "DecalsSurface.cginc"
            #include "SDF.cginc"
            #include "TextDecal.cginc"

            ENDCG
        } 

        Pass
        {
            Name "DEFERRED_PREPASS"
            Tags { "LightMode" = "Deferred" }
            ZWrite Off
            ZTest LEqual  
            Offset -1, -1
            Blend 1 Zero OneMinusSrcColor, Zero OneMinusSrcAlpha 

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_deferred_prepass
            #pragma target 3.0

            #pragma multi_compile_deferred nolightmap nodirlightmap nodynlightmap
            #pragma skip_variants SHADOWS_DEPTH SHADOWS_CUBE SHADOWS_SHADOWMASK LIGHTMAP_SHADOW_MIXING POINT_COOKIE
            #pragma multi_compile_local __ DECAL_PREVIEW
            // #pragma multi_compile_local __ DECAL_BASE_NORMAL DECAL_BUMPMAP
            #pragma multi_compile_local __ DECAL_SPECMAP
            // #pragma multi_compile_local __ DECAL_EMISSIVE
            #pragma multi_compile_local __ DECAL_SDF_ALPHA
  
            #include "UnityCG.cginc"
            #include "DecalsCommon.cginc"
            #include "DecalsSurface.cginc"
            #include "SDF.cginc"
            #include "TextDecal.cginc"

            ENDCG
        } 

        Pass
        {
            Name "DEFERRED"
            Tags { "LightMode" = "Deferred" }
            ZWrite Off
            ZTest LEqual  
            Offset -1, -1
            Blend 0 SrcAlpha OneMinusSrcAlpha, Zero One
            Blend 1 One One
            Blend 2 SrcAlpha OneMinusSrcAlpha, Zero One
            Blend 3 SrcAlpha OneMinusSrcAlpha, Zero One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_deferred 
            #pragma target 3.0

            #pragma multi_compile_deferred nolightmap nodirlightmap nodynlightmap
            #pragma skip_variants SHADOWS_DEPTH SHADOWS_CUBE SHADOWS_SHADOWMASK LIGHTMAP_SHADOW_MIXING POINT_COOKIE
            #pragma multi_compile_local __ DECAL_PREVIEW
            #pragma multi_compile_local __ DECAL_BASE_NORMAL DECAL_BUMPMAP
            #pragma multi_compile_local __ DECAL_SPECMAP
            #pragma multi_compile_local __ DECAL_EMISSIVE
            #pragma multi_compile_local __ DECAL_SDF_ALPHA
  
            #include "UnityCG.cginc"
            #include "DecalsCommon.cginc"
            #include "DecalsSurface.cginc"
            #include "SDF.cginc"
            #include "TextDecal.cginc"

            ENDCG
        } 
    }
}    