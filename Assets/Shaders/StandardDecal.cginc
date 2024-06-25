void surf(DecalSurfaceInput IN, inout SurfaceOutputStandardSpecular o) {
    float4 color = tex2D(_Decal, IN.uv_decal);
    o.Albedo = UnderwaterFog(IN.worldPosition, color).rgb;
    o.Alpha =  _DecalOpacity;
    o.Occlusion = 1;

    #ifdef DECAL_BASE_NORMAL
        float3 normal = IN.normal;
        float wearFactor = 1 - normal.z;
        float wearFactorAlpha = saturate(_EdgeWearStrength * wearFactor);
        o.Alpha *= saturate(1 + _EdgeWearOffset - saturate(_EdgeWearStrength * wearFactor));
    #endif

    #ifdef DECAL_BUMPMAP
        o.Normal = tex2D(_BumpMap, IN.uv_bumpmap);
    #endif

    #ifdef DECAL_SPECMAP
        float4 specular = tex2D(_SpecMap, IN.uv_specmap);
        o.Smoothness = _Shininess;
        o.Specular = specular
    #endif

    half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
    o.Emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;

    #ifdef DECAL_EMISSIVE
        o.Emission += tex2D(_Emissive, IN.uv_emissive).rgb * _Emissive_Color.rgb * _Emissive_Color.a;
    #endif

    float dist = BoundsDist(IN.uv, IN.vertex_normal, _DecalNormal);
    #ifdef DECAL_SDF_ALPHA
        float decalDist = _Cutoff - color.a;
        o.Alpha *= SDFAA(max(decalDist, dist));
    #else
        o.Alpha *= SDFAA(dist);
        o.Alpha *= color.a;
    #endif 
}