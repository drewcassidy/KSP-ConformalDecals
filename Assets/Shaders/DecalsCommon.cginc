#ifndef DECALS_COMMON_INCLUDED
// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members screenUV)
#pragma exclude_renderers d3d11
#define DECALS_COMMON_INCLUDED

#include "AutoLight.cginc"
#include "Lighting.cginc"
#include "LightingKSPDeferred.cginc"

#define CLIP_MARGIN 0.05
#define EDGE_MARGIN 0.01

// UNIFORM VARIABLES
// Projection matrix, normal, and tangent vectors
float4x4 _ProjectionMatrix;
float3 _DecalNormal;
float3 _DecalTangent;

// Common Shading Paramaters
float _Cutoff;
float _DecalOpacity;
float4 _Background;

sampler2D _Decal;
float4 _Decal_ST;

// Variant Shading Parameters
#ifdef DECAL_BASE_NORMAL
    sampler2D _BumpMap;
    float4 _BumpMap_ST;
    float _EdgeWearStrength;
    float _EdgeWearOffset;
#endif //DECAL_BASE_NORMAL

#ifdef DECAL_BUMPMAP 
    sampler2D _BumpMap;
    float4 _BumpMap_ST;
#endif //DECAL_BUMPMAP

#ifdef DECAL_SPECMAP
    sampler2D _SpecMap;
    float4 _SpecMap_ST;
    // specular color is declared in a unity CGINC for some reason??
#endif //DECAL_SPECMAP

fixed _Shininess;


#ifdef DECAL_EMISSIVE
    sampler2D _Emissive;
    float4 _Emissive_ST;
    fixed4 _Emissive_Color;
#endif //DECAL_EMISSIVE

// KSP EFFECTS
// opacity and color
float _Opacity;
float _RimFalloff;
float4 _RimColor;

// SURFACE INPUT STRUCT
struct DecalSurfaceInput
{
    float3 uv;
    float2 uv_decal;
    
    #ifdef DECAL_BUMPMAP
        float2 uv_bumpmap;
    #endif //DECAL_BUMPMAP
    
    #ifdef DECAL_SPECMAP
        float2 uv_specmap;
    #endif //DECAL_SPECMAP
    
    #ifdef DECAL_EMISSIVE
        float2 uv_emissive;
    #endif //DECAL_EMISSIVE
    
    #ifdef DECAL_BASE_NORMAL
        float3 normal;
    #endif //DECAL_BASE_NORMAL

    float3 vertex_normal;
    float3 viewDir;
    float3 worldPosition;
};

struct appdata_decal
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    #if defined(DECAL_BASE_NORMAL) || defined(DECAL_PREVIEW)
        float4 texcoord : TEXCOORD0;
        float4 tangent : TANGENT;
    #endif
};

struct v2f
{
    UNITY_POSITION(pos);
    float3 normal : NORMAL;
    float4 uv_decal : TEXCOORD0;
    
    #ifdef DECAL_BASE_NORMAL
        float2 uv_base : TEXCOORD1;
    #endif //DECAL_BASE_NORMAL
    
    float4 tSpace0 : TEXCOORD2;
    float4 tSpace1 : TEXCOORD3;
    float4 tSpace2 : TEXCOORD4;
    
    #ifdef UNITY_PASS_FORWARDBASE
        fixed3 vlight : TEXCOORD5;
        UNITY_SHADOW_COORDS(6)
    #endif //UNITY_PASS_FORWARDBASE
    
    #ifdef UNITY_PASS_FORWARDADD
        UNITY_LIGHTING_COORDS(5,6)
    #endif //UNITY_PASS_FORWARDADD

    #ifdef UNITY_PASS_DEFERRED
        float3 screenUV : TEXCOORD5;
    #endif
};


inline void decalClipAlpha(float alpha) {
    #ifndef DECAL_PREVIEW 
        clip(alpha - 0.001);
    #endif
}

inline float CalcMipLevel(float2 texture_coord) {
    float2 dx = ddx(texture_coord);
    float2 dy = ddy(texture_coord);
    float delta_max_sqr = max(dot(dx, dx), dot(dy, dy));
    
    return 0.5 * log2(delta_max_sqr);
}

inline float BoundsDist(float3 p, float3 normal, float3 projNormal) {
    float3 q = abs(p - 0.5) - 0.5; // 1x1 square/cube centered at (0.5,0.5)
    //float dist = length(max(q,0)) + min(max(q.x,max(q.y,q.z)),0.0); // true SDF
    #ifdef DECAL_PREVIEW
        return 10 * max(q.x, q.y); // 2D pseudo SDF
    #else
        float dist = max(max(q.x, q.y), q.z); // pseudo SDF
        float ndist = EDGE_MARGIN - dot(normal, projNormal); // SDF to normal
        return 10 * max(dist, ndist); // return intersection
    #endif //DECAL_PREVIEW
}

// declare surf function,
// this must be defined in any shader using this cginc
void surf (DecalSurfaceInput IN, inout SurfaceOutput o);

v2f vert(appdata_decal v)
{
    v2f o;
    UNITY_INITIALIZE_OUTPUT(v2f,o);

    o.pos = UnityObjectToClipPos(v.vertex);
    o.normal = v.normal;

    #ifdef DECAL_PREVIEW
        o.uv_decal = v.texcoord;
    #else
        o.uv_decal = mul (_ProjectionMatrix, v.vertex);
    #endif //DECAL_PREVIEW

    #ifdef DECAL_BASE_NORMAL
        o.uv_base = TRANSFORM_TEX(v.texcoord, _BumpMap);
    #endif //DECAL_BASE_NORMAL

    float3 worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
    float3 worldNormal = UnityObjectToWorldNormal(v.normal);

    #if defined(DECAL_BASE_NORMAL) || defined(DECAL_PREVIEW)
        // use tangent of base geometry
        fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
        fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
        fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
    #else
        // use tangent of projector
        fixed3 decalTangent = UnityObjectToWorldDir(_DecalTangent);
        fixed3 worldBinormal = cross(decalTangent, worldNormal);
        fixed3 worldTangent = cross(worldNormal, worldBinormal);
    #endif //defined(DECAL_BASE_NORMAL) || defined(DECAL_PREVIEW)

    o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPosition.x);
    o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPosition.y);
    o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPosition.z);

    // forward base pass specific lighting code
    #ifdef UNITY_PASS_FORWARDBASE
        // SH/ambient light
        #if UNITY_SHOULD_SAMPLE_SH
            float3 shlight = ShadeSH9 (float4(worldNormal,1.0));
            o.vlight = shlight;
        #else
            o.vlight = 0.0;
        #endif // UNITY_SHOULD_SAMPLE_SH

        // vertex light
        #ifdef VERTEXLIGHT_ON
            o.vlight += Shade4PointLights (
            unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
            unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
            unity_4LightAtten0, worldPosition, worldNormal );
        #endif // VERTEXLIGHT_ON
    #endif // UNITY_PASS_FORWARDBASE

    // pass shadow and, possibly, light cookie coordinates to pixel shader
    UNITY_TRANSFER_LIGHTING(o, 0.0);

    #ifdef UNITY_PASS_DEFERRED
        o.screenUV = o.pos.xyw;

        // Correct flip when rendering with a flipped projection matrix.
        // (I've observed this differing between the Unity scene & game views)
        o.screenUV.y *= _ProjectionParams.x;
    #endif
    return o;
}


SurfaceOutput frag_common(v2f IN, out float3 viewDir, out UnityGI gi) {
    SurfaceOutput o;

    // setup world-space TBN vectors
    UNITY_EXTRACT_TBN(IN);

    float3 worldPos = float3(IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w);
    float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
    viewDir = _unity_tbn_0 * worldViewDir.x + _unity_tbn_1 * worldViewDir.y  + _unity_tbn_2 * worldViewDir.z;

    #ifdef DECAL_PREVIEW
        fixed4 uv_projected = IN.uv_decal;
    #else
        // perform decal projection
        fixed4 uv_projected = UNITY_PROJ_COORD(IN.uv_decal);

        clip(uv_projected.xyz + CLIP_MARGIN);
        clip(CLIP_MARGIN + (1-uv_projected.xyz));
    #endif //DECAL_PREVIEW

    // declare data
    DecalSurfaceInput i;

    // initialize surface input
    UNITY_INITIALIZE_OUTPUT(DecalSurfaceInput, i)
    i.uv_decal = TRANSFORM_TEX(uv_projected, _Decal);
    i.uv = uv_projected;

    #ifdef DECAL_BUMPMAP
        i.uv_bumpmap = TRANSFORM_TEX(uv_projected, _BumpMap);
    #endif //DECAL_BUMPMAP

    #ifdef DECAL_SPECMAP
        i.uv_specmap = TRANSFORM_TEX(uv_projected, _SpecMap);
    #endif //DECAL_SPECMAP

    #ifdef DECAL_EMISSIVE
        i.uv_emissive = TRANSFORM_TEX(uv_projected, _Emissive);
    #endif //DECAL_EMISSIVE

    #ifdef DECAL_BASE_NORMAL
        #ifdef DECAL_PREVIEW
           i.normal = fixed3(0,0,1);
        #else
           i.normal = UnpackNormalDXT5nm(tex2D(_BumpMap, IN.uv_base));
        #endif //DECAL_PREVIEW
    #endif //DECAL_BASE_NORMAL

    i.vertex_normal = IN.normal;
    i.viewDir = viewDir;
    i.worldPosition = worldPos;

    // initialize surface output
    o.Albedo = 0.0;
    o.Emission = 0.0;
    o.Specular = 0.0;
    o.Alpha = 0.0;
    o.Gloss = 0.0;
    o.Normal = fixed3(0,0,1);

    // call surface function
    surf(i, o);

    // compute world normal
    float3 worldN;
    worldN.x = dot(_unity_tbn_0, o.Normal);
    worldN.y = dot(_unity_tbn_1, o.Normal);
    worldN.z = dot(_unity_tbn_2, o.Normal);
    worldN = normalize(worldN);
    o.Normal = worldN;

    // compute lighting & shadowing factor
    #if UNITY_PASS_DEFERRED
    fixed atten = 0;
    #else
    UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
    #endif

    // setup GI
    UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
    gi.indirect.diffuse = 0;
    gi.indirect.specular = 0;
    gi.light.color = 0;
    gi.light.dir = half3(0,1,0);

    // setup light information in forward modes
    #ifndef UNITY_PASS_DEFERRED
    #ifndef USING_DIRECTIONAL_LIGHT
    fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
    #else
    fixed3 lightDir = _WorldSpaceLightPos0.xyz;
    #endif
    gi.light.color = _LightColor0.rgb;
    gi.light.dir = lightDir;
    #endif

    // Call GI (lightmaps/SH/reflections) lighting function
    UnityGIInput giInput;
    UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
    giInput.light = gi.light;
    giInput.worldPos = worldPos;
    giInput.worldViewDir = worldViewDir;
    giInput.atten = atten;
    giInput.ambient = 0.0;

    LightingBlinnPhongSmooth_GI(o, giInput, gi);

    #ifdef DECAL_PREVIEW
        if (any(IN.uv_decal > 1) || any(IN.uv_decal < 0)) o.Alpha = 0;

        o.Albedo = lerp(_Background.rgb, o.Albedo, o.Alpha) * _Color.rgb;
        o.Normal = lerp(float3(0,0,1), o.Normal, o.Alpha);
        o.Specular = lerp(_Background.a, o.Specular, o.Alpha);
        o.Emission = lerp(0, o.Emission, o.Alpha);
        o.Alpha = _Opacity;
    #endif //DECAL_PREVIEW

    return o;
}

fixed4 frag_forward(v2f IN) : SV_Target
{
    fixed3 viewDir = 0;
    UnityGI gi;

    SurfaceOutput o = frag_common(IN, viewDir, gi);

    //call modified KSP lighting function
    return LightingBlinnPhongSmooth(o, viewDir, gi);
}

void frag_deferred (v2f IN,
    out half4 outGBuffer0 : SV_Target0,
    out half4 outGBuffer1 : SV_Target1,
    #if defined(DECAL_BUMPMAP) || defined(DECAL_PREVIEW)
        out half4 outGBuffer2 : SV_Target2,
    #endif
    out half4 outEmission : SV_Target3)
{
    #if !(defined(DECAL_BUMPMAP) || defined(DECAL_PREVIEW))
        half4 outGBuffer2 = 0; // define dummy normal buffer when we're not writing to it
    #endif

    float3 viewDir;
    UnityGI gi;
    SurfaceOutput o = frag_common(IN, viewDir, gi);

    #ifdef DECAL_PREVIEW
        o.Alpha = 1;
    #endif

    outEmission = LightingBlinnPhongSmooth_Deferred(o, viewDir, gi, outGBuffer0, outGBuffer1, outGBuffer2);

    #ifndef UNITY_HDR_ON
    outEmission.rgb = exp2(-outEmission.rgb);
    #endif

    outGBuffer0.a = o.Alpha;
    outGBuffer1 *= o.Alpha;
    outGBuffer2.a = o.Alpha;
    outEmission.a = o.Alpha;
}

void frag_deferred_prepass(v2f IN, out half4 outGBuffer1: SV_Target1) {
    float3 viewDir;
    UnityGI gi;

    SurfaceOutput o = frag_common(IN, viewDir, gi);

    outGBuffer1 = o.Alpha;
}

#endif