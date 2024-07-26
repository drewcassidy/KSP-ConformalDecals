#ifndef HSL_INCLUDED
#define HSL_INCLUDED

inline float3 HSL2RGB(float3 hsl) {
    int3 n = int3(0, 8, 4);
    float3 k = (n + hsl.x * 12) % 12;
    float a = hsl.y * min(hsl.z, 1 - hsl.z);
    return hsl.z - a * max(-1, min(k - 3, min(9 - k, 1)));
}

inline float3 HSV2RGB(float3 hsv) {
    int3 n = int3(5, 3, 1);
    float3 k = (n + hsv.x * 6) % 6;
    return hsv.z - hsv.z * hsv.y * max(0, min(1, min(k, 4 - k)));
}

#endif