#ifndef LIGHTING_KSP_INCLUDED
#define LIGHTING_KSP_INCLUDED


#define blinnPhongShininessPower 0.215

// An exact conversion from blinn-phong to PBR is impossible, but the look can be approximated perceptually
// and by observing how blinn-phong looks and feels at various settings, although it can never be perfect
// 1) The specularColor can be used as is in the PBR specular flow, just needs to be divided by PI so it sums up to 1 over the hemisphere
// 2) Blinn-phong shininess doesn't stop feeling shiny unless at very low values, like below 0.04
// while the PBR smoothness feels more linear -> map shininess to smoothness accordingly using a function
// that increases very quickly at first then slows down, I went with something like x^(1/4) or x^(1/6) then made the power configurable
// I tried various mappings from the literature but nothing really worked as well as this
// 3) Finally I noticed that some parts still looked very shiny like the AV-R8 winglet while in stock they looked rough thanks a low
// specularColor but high shininess and specularMap, so I multiplied the smoothness by the sqrt of the specularColor and that caps
// the smoothness when specularColor is low
float4 GetStandardSpecularPropertiesFromLegacy(float legacyShininess, float specularMap)
{
    float3 legacySpecularColor = saturate(_SpecColor);

    float smoothness = pow(legacyShininess, blinnPhongShininessPower) * specularMap;
    smoothness *= sqrt(length(legacySpecularColor));

    float3 specular = legacySpecularColor * UNITY_INV_PI;
    return float4(specular, smoothness);
}

float4 _Color;

fixed4 LightingBlinnPhongSmooth(SurfaceOutput s, half3 viewDir, UnityGI gi)
{
    fixed4 c;
    c = UnityBlinnPhongLight(s, viewDir, gi.light);

    #ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
    c.rgb += s.Albedo * gi.indirect.diffuse;
    #endif

    return c;
}

half4 LightingBlinnPhongSmooth_Deferred(SurfaceOutput s, half3 viewDir, UnityGI gi,
                                               out half4 outDiffuseOcclusion, out half4 outSpecSmoothness,
                                               out half4 outNormal)
{
    outDiffuseOcclusion = half4(s.Albedo, 1.0);
    outSpecSmoothness = GetStandardSpecularPropertiesFromLegacy(s.Specular, s.Gloss);
    outNormal = half4(s.Normal, 1.0);

    half4 emission = half4(s.Emission, 1);

    #ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
    emission.rgb += s.Albedo * gi.indirect.diffuse;
    #endif

    return emission;
}


inline void LightingBlinnPhongSmooth_GI(inout SurfaceOutput s, UnityGIInput gi_input, inout UnityGI gi)
{
    gi = UnityGlobalIllumination(gi_input, 1.0, s.Normal);
}

float4 _LocalCameraPos;
float4 _LocalCameraDir;
float4 _UnderwaterFogColor;
float _UnderwaterMinAlphaFogDistance;
float _UnderwaterMaxAlbedoFog;
float _UnderwaterMaxAlphaFog;
float _UnderwaterAlbedoDistanceScalar;
float _UnderwaterAlphaDistanceScalar;
float _UnderwaterFogFactor;

float4 UnderwaterFog(float3 worldPos, float3 color)
{
    // skip fog in deferred mode
    #ifdef UNITY_PASS_DEFERRED
    return float4(color, 1);
    #endif

    float3 toPixel = worldPos - _LocalCameraPos.xyz;
    float toPixelLength = length(toPixel); ///< Comment out the math--looks better without it.

    float underwaterDetection = _UnderwaterFogFactor * _LocalCameraDir.w; ///< sign(1 - sign(_LocalCameraPos.w));
    float albedoLerpValue = underwaterDetection * (_UnderwaterMaxAlbedoFog * saturate(
        toPixelLength * _UnderwaterAlbedoDistanceScalar));
    float alphaFactor = 1 - underwaterDetection * (_UnderwaterMaxAlphaFog * saturate(
        (toPixelLength - _UnderwaterMinAlphaFogDistance) * _UnderwaterAlphaDistanceScalar));

    return float4(lerp(color, _UnderwaterFogColor.rgb, albedoLerpValue), alphaFactor);
}

#endif
