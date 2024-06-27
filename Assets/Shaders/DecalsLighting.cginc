#ifndef DECALS_LIGHTING_INCLUDED
#define DECALS_LIGHTING_INCLUDED

#include "UnityPBSLighting.cginc"
#include "LegacyToStandard.cginc"

// modifed version of the KSP BlinnPhong because it does some weird things
inline fixed4 LightingBlinnPhongDecal(SurfaceOutputStandardSpecular s, fixed3 lightDir, half3 viewDir, fixed atten)
{
    s.Normal = normalize(s.Normal);
    half3 h = normalize(lightDir + viewDir);

    fixed diff = max(0, dot(s.Normal, lightDir));

    float nh = max(0, dot(s.Normal, h));
    float spec = pow(nh, s.Smoothness*128.0) * s.Specular; // specular and smoothness inverted from usual KSP lighting function

    fixed4 c = 0;
    c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * _SpecColor.rgb * spec) * (atten);
    return c;
}

// KSP underwater fog function
float4 UnderwaterFog(float3 worldPos, float3 color)
{
    float3 toPixel = worldPos - _LocalCameraPos.xyz;
    float toPixelLength = length(toPixel);

    float underwaterDetection = _UnderwaterFogFactor * _LocalCameraDir.w;
    float albedoLerpValue = underwaterDetection * (_UnderwaterMaxAlbedoFog * saturate(toPixelLength * _UnderwaterAlbedoDistanceScalar));
    float alphaFactor = 1 - underwaterDetection * (_UnderwaterMaxAlphaFog * saturate((toPixelLength - _UnderwaterMinAlphaFogDistance) * _UnderwaterAlphaDistanceScalar));

    return float4(lerp(color, _UnderwaterFogColor.rgb, albedoLerpValue), alphaFactor);
}


inline half4 KSPLightingStandardSpecular_Deferred (SurfaceOutputStandardSpecular s, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2, out half4 outEmission)
{
    GetStandardSpecularPropertiesFromLegacy(s.Smoothness, s.Specular, _SpecColor, s.Smoothness, s.Specular);

    // energy conservation
    half oneMinusReflectivity;
    s.Albedo = EnergyConservationBetweenDiffuseAndSpecular (s.Albedo, s.Specular, oneMinusReflectivity);

    UnityStandardData data;
    data.diffuseColor   = s.Albedo;
    data.occlusion      = s.Occlusion;
    data.specularColor  = s.Specular;
    data.smoothness     = s.Smoothness;
    data.normalWorld    = s.Normal;

    // RT0: diffuse color (rgb), occlusion (a) - sRGB rendertarget
    outGBuffer0 = half4(data.diffuseColor, data.occlusion);

    // RT1: spec color (rgb), smoothness (a) - sRGB rendertarget
    outGBuffer1 = half4(data.specularColor, data.smoothness);

    // RT2: normal (rgb), --unused, very low precision-- (a)
    outGBuffer2 = half4(data.normalWorld * 0.5f + 0.5f, 1.0f);

    outEmission = half4(s.Emission, s.Alpha);

    #ifndef UNITY_HDR_ON
    outEmission.rgb = saturate(exp2(-outEmission.rgb));
    #endif

    return outEmission;
}


#endif
