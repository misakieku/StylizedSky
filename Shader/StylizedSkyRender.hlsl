#include "../Script/Runtime/Light/StylizedLightDefinition.cs.hlsl"

StructuredBuffer<StylizedDirectionalData> _StylizedDirectionalDatas;
uint _StylizedDirectionalCount;

TEXTURE2D(_SurfaceAtlas);
float4 _SurfaceAtlasData; //x: COOKIE_ATLAS_SIZE; y: COOKIE_ATLAS_RCP_PADDING; z: COOKIE_ATLAS_LAST_VALID_MIP

// Adjust UVs using the LOD padding size
float2 UVWithPadding(float2 coord, float2 size, float rcpPaddingWidth, float lod)
{
    // This is likely wrong because for the proper calculation, size must be the size without padding.
    // TextureAtlas allocator returns scale offset with padding inclusive. So the size here is the size with padding.
    // The error is subtle but can lead to some unexpected results.
    float2 scale = rcp(size + rcpPaddingWidth) * size;
    float2 offset = 0.5 * (1.0 - scale);

    // Avoid edge bleeding for texture when sampling with lod by clamping uvs:
    float2 mipClamp = pow(2, ceil(lod)) / (size * _SurfaceAtlasData.x * 2);
    return clamp(coord * scale + offset, mipClamp, 1 - mipClamp);
}

float3 SampleSurfaceTexture2D(float2 coord, float4 scaleOffset, float lod = 0) // TODO: mip maps for cookies
{
    float2 scale = scaleOffset.xy;
    float2 offset = scaleOffset.zw;

    // Remap the uv to take in account the padding
    coord = UVWithPadding(coord, scale, _SurfaceAtlasData.y, max(0, lod - _SurfaceAtlasData.z));

    // Apply atlas scale and offset
    float2 atlasCoords = coord * scale + offset;

    float3 color = SAMPLE_TEXTURE2D_LOD(_SurfaceAtlas, s_trilinear_clamp_sampler, atlasCoords, lod).rgb;

    // Mip visualization (0 -> red, 10 -> blue)
    // color = saturate(1 - abs(3 * lod / 10 - float4(0, 1, 2, 3))).rgb;

    return color;
}

float ComputePhase(StylizedDirectionalData moon, float3 V)
{
    float3 M = moon.forward.xyz * moon.distanceFromCamera;

    float radialDistance = moon.distanceFromCamera, rcpRadialDistance = rcp(radialDistance);
    float2 t = IntersectSphere(moon.radius, dot(moon.forward.xyz, -V), radialDistance, rcpRadialDistance);

    float3 N = normalize(M - t.x * V);

    return saturate(-dot(N, moon.sunDirection));
}

float ComputeShine(StylizedDirectionalData moon)
{
    // Approximate earthshine: sun light reflected from earth
    // cf. A Physically-Based Night Sky Model

    // Compute the percentage of earth surface that is illuminated by the sun as seen from the moon
    //float earthPhase = PI - acos(dot(sun.forward.xyz, -light.forward.xyz));
    //float earthshine = 1.0f - sin(0.5f * earthPhase) * tan(0.5f * earthPhase) * log(rcp(tan(0.25f * earthPhase)));

    // Cheaper approximation of the above (https://www.desmos.com/calculator/11ny6d5j1b)
    float sinPhase = sqrt(max(1 - dot(moon.sunDirection, moon.forward), 0.0f)) * INV_SQRT2;
    float earthshine = 1.0f - sinPhase * sqrt(sinPhase);

    return earthshine * moon.earthshine;
}

float EV2Lux(float In)
{
    float lux = 2.5f * pow(2.0f, In);
    return lux;
}

// Calculates the sun shape
float4 SunDisc(float3 V, int isRendersunDisc, float skyIntensity)
{
    bool rendersunDisc = isRendersunDisc != 0;
    float3 sunDisc = float3(0, 0, 0);
    float alpha = 0;

    if (rendersunDisc)
    {
        uint i = 0;
        for (i = 0; i < _StylizedDirectionalCount; i++)
        {
            StylizedDirectionalData light = _StylizedDirectionalDatas[i];

            if (asint(light.angularRadius) != 0 && light.distanceFromCamera >= 0)
            {
                    // We may be able to see the celestial body.
                float3 L = light.forward.xyz;
                float LdotV = dot(L, V);
                float rad = acos(LdotV);
                float radInner = light.angularRadius;

                if (LdotV >= light.flareCosOuter)
                {
                    float3 color = light.surfaceColor;

                    if (LdotV >= light.flareCosInner) // Sun disk.
                    {
                        float2 uv = 0;
                        if (light.type != 0)
                            color *= ComputePhase(light, V) * INV_PI + ComputeShine(light); // Lambertian BRDF
                        
                        if (light.surfaceTextureScaleOffset.x > 0)
                        {
                            // The cookie code de-normalizes the axes.
                            float2 proj = float2(dot(V, light.right), dot(V, light.up));
                            float2 angles = float2(FastASin(proj.x), FastASin(-proj.y));
                            uv = angles * rcp(radInner) * 0.5 + 0.5;
                            color *= SampleSurfaceTexture2D(uv, light.surfaceTextureScaleOffset);
                        }

                        sunDisc = color;
                        alpha = 1;
                    }
                    else // Flare region.
                    {
                        float r = max(0, rad - radInner);
                        float w = saturate(1 - r * rcp(light.flareSize));

                        float3 color = light.flareColor;
                        color *= SafePositivePow(w, light.flareFalloff);
                        sunDisc += color;
                    }
                    sunDisc = (sunDisc * TWO_PI) / EV2Lux(skyIntensity) * saturate(-light.forward.y);
                }
            }
        }
    }
    return float4(sunDisc.r, sunDisc.g, sunDisc.b, saturate(alpha));
}