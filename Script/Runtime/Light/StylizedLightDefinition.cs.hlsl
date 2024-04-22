//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef STYLIZEDLIGHTDEFINITION_CS_HLSL
#define STYLIZEDLIGHTDEFINITION_CS_HLSL
// Generated from Misaki.StylizedSky.StylizedDirectionalData
// PackingRules = Exact
struct StylizedDirectionalData
{
    float3 color;
    float radius;
    float3 forward;
    float distanceFromCamera; // -1 if interact with sky is false
    float3 right;
    float angularRadius;
    float3 up;
    int type;
    float3 surfaceColor;
    float earthshine;
    float4 surfaceTextureScaleOffset;
    float3 sunDirection;
    float flareCosInner;
    float2 phaseAngleSinCos;
    float flareCosOuter;
    float flareSize;
    float3 flareColor;
    float flareFalloff;
};


#endif
