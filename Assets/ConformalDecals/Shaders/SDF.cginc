#ifndef SDF_INCLUDED
#define SDF_INCLUDED

// based on functions by Inigo Quilez
// https://iquilezles.org/www/articles/distfunctions2d/distfunctions2d.htm

// SDF of a box
float sdBox( in float2 p, in float2 b ) {
    float2 d = abs(p)-b;
    return length(max(d,0.0)) + min(max(d.x,d.y),0.0);
}

// SDF of a box with corner radius r
float sdRoundedBox( in float2 p, in float2 b, in float r ) {
    float2 d = abs(p)-b+r;
    return min(max(d.x,d.y),0.0) + length(max(d,0.0)) - r;
}

// SDF of a box with corner radius r, based on the current UV position
// UV must be ∈ (0,1), with (0,0) on one corner
float sdRoundedUVBox( float2 uv, float r ) {
    float dx = ddx(uv.x);
    float dy = ddy(uv.y);
    
    float2 dim = abs(float2(1/dx, 1/dy));
    float2 halfDim = dim / 2;
    float2 pos = (dim * uv) - halfDim;
    
    return sdRoundedBox(pos, halfDim, r);
}

inline float SDFdDist(float dist) {
    return length(float2(ddx(dist), ddy(dist)));
}

inline float SDFAA(float dist, float ddist) {
    float pixelDist = dist / ddist;
    return saturate(0.5-pixelDist);
}

inline float SDFAA(float dist) {
    return SDFAA(dist, SDFdDist(dist));
}

#endif