float4 _DecalColor;

float4 _OutlineColor;
float _OutlineWidth;

void surf(DecalSurfaceInput IN, inout SurfaceOutputStandardSpecular o) {
    float4 color = _DecalColor;
    float dist = _Cutoff - tex2D(_Decal, IN.uv_decal).r; // text distance
    o.Specular = 0.4;
    o.Occlusion = 1;
    
    #ifdef DECAL_OUTLINE
        // Outline
        float outlineOffset = _OutlineWidth * 0.25;
        float outlineRadius = _OutlineWidth * 0.5;
        
        #ifdef DECAL_FILL
            // Outline and Fill
            float outlineDist = -dist - outlineOffset;
            float outlineFactor = SDFAA(outlineDist);
            dist -= outlineOffset;
            color = lerp(_DecalColor, _OutlineColor, outlineFactor);
        #else
            // Outline Only
            float outlineDist = abs(dist) - outlineOffset;
            dist = outlineDist;
            color = _OutlineColor;
        #endif
    #endif

    dist = max(dist, BoundsDist(IN.uv, IN.vertex_normal, _DecalNormal));
    float ddist = SDFdDist(dist); // distance gradient magnitude
    o.Alpha = _DecalOpacity * SDFAA(dist, ddist);
    o.Albedo = UnderwaterFog(IN.worldPosition, color).rgb;

    #ifdef DECAL_BASE_NORMAL
        float3 normal = IN.normal;
        float wearFactor = 1 - normal.z;
        float wearFactorAlpha = saturate(_EdgeWearStrength * wearFactor);
        o.Alpha *= saturate(1 + _EdgeWearOffset - saturate(_EdgeWearStrength * wearFactor));
    #endif

    #ifdef DECAL_SPECMAP
        float4 specular = tex2D(_SpecMap, IN.uv_specmap);
        o.Specular = specular;
    #endif

    o.Smoothness = _Shininess;

    half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
    o.Emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
}