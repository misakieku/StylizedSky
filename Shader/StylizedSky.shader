Shader "Hidden/HDRP/Sky/StylizedSky"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma editor_sync_compilation
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/CookieSampling.hlsl"

    int _renderSunDisk;

    float3 _skyColor;
    float3 _groundColor;
    float _skyIntensity;
    float3 _horizonLineColor;

    float _horizonLineContribution;
    float _sunHaloContribution;
    float _horizonLineExponent;
    float _sunHaloExponent;

    float3 _sunDirection;
    float3 _sunColor;

    int _renderSpace;
    TEXTURECUBE(_spaceTexture);
    float4x4 _spaceRotation;
    float _spaceEV;

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
        return output;
    }

    float EV2Lux(float In)
    {
        float lux = 2.5f * pow(2.0f, In);
        return lux;
    }

    // Calculates the sun shape
    float3 SunDisc(float3 V)
    {
        bool renderSunDisk = _renderSunDisk != 0;
        float3 sunDisk = float3(0, 0, 0);

        if (renderSunDisk)
        {
            for (uint i = 0; i < _DirectionalLightCount; i++)
            {
                DirectionalLightData light = _DirectionalLightDatas[i];

                if (asint(light.angularDiameter) != 0)
                {
                    // We may be able to see the celestial body.
                    float3 L = -light.forward.xyz;
                    float LdotV    = dot(L, V);
                    float rad      = acos(LdotV);
                    float radInner = 0.5 * light.skyAngularDiameter;

                    if (LdotV >= light.flareCosOuter)
                    {
                        float solidAngle = TWO_PI * (1 - light.flareCosInner);
                        float3 color = light.color.rgb * rcp(solidAngle);

                        if (LdotV >= light.flareCosInner) // Sun disk.
                        {
                            float2 uv = 0;
                            if (light.bodyType == 2 || light.surfaceTextureScaleOffset.x > 0)
                            {
                                // The cookie code de-normalizes the axes.
                                float2 proj   = float2(dot(V, normalize(light.right)), dot(V, normalize(light.up)));
                                float2 angles = float2(FastASin(-proj.x), FastASin(proj.y));
                                uv = angles * rcp(radInner);
                            }

                            if (light.surfaceTextureScaleOffset.x > 0)
                            {
                                color *= SampleCookie2D(uv * 0.5 + 0.5, light.surfaceTextureScaleOffset);
                            }

                            color *= light.surfaceTint;
                            sunDisk = color;
                        }
                        else // Flare region.
                        {
                            float r = max(0, rad - radInner);
                            float w = saturate(1 - r * rcp(light.flareSize));

                            color *= light.flareTint;
                            color *= SafePositivePow(w, light.flareFalloff);
                            sunDisk += color;
                        }
                        sunDisk = sunDisk/(EV2Lux(_skyIntensity) * TWO_PI);
                    }
                }
            }
        }
        return sunDisk;
    }

    void ComputeSkyMasks(Varyings input, float3 V, out float3 sunDisc, out float sunHalo, out float horizon, out float gradient, out float skyGradient)
    {
        // Reverse it to point into the scene
        float3 dir = -V;
        float dotViewSun = saturate(dot(dir, _sunDirection));
        float y = dir.y;

        float bellCurve = pow(saturate(dotViewSun), _sunHaloExponent * saturate(abs(y)));
        float horizonSoften = 1 - pow(1 - saturate(y), 50);
        sunHalo = saturate(bellCurve * horizonSoften);

        sunDisc = SunDisc(dir) * horizonSoften;

        horizon = saturate(1.0 - abs(y));
        horizon = saturate(pow(horizon, _horizonLineExponent));

        gradient = saturate(y);
        gradient = saturate(pow(gradient, 0.5));

        skyGradient = dot(dir, _sunDirection) * 0.25 + 0.75;
    }

    float4 RenderSky(Varyings input)
    {
        float3 V = GetSkyViewDirWS(input.positionCS.xy);
        float maskSunHalo, maskHorizon, maskGradient, maskSkyGradient = 0;
        float3 SunDisc = 0;
        float3 skyColor, spaceColor, finalColor = 0;
        ComputeSkyMasks(input, V, SunDisc, maskSunHalo, maskHorizon, maskGradient, maskSkyGradient);

        skyColor = lerp(_groundColor, _skyColor * maskSkyGradient, maskGradient);

        // Add horizon line.
        skyColor += _horizonLineColor * _horizonLineContribution * maskHorizon * maskSkyGradient;

        // Add sun halo.
        skyColor += _sunColor * _sunHaloContribution * maskSunHalo;

        // Add sun disc
        skyColor += SunDisc;

        skyColor = skyColor * EV2Lux(_skyIntensity);

        // Space
        if(_renderSpace == 1)
        {
            spaceColor = SAMPLE_TEXTURECUBE(_spaceTexture, s_trilinear_clamp_sampler, mul(-V, (float3x3)_spaceRotation)).rgb * maskGradient;
            spaceColor = spaceColor * EV2Lux(_spaceEV);
            finalColor += spaceColor;
        }

        finalColor += skyColor;
        return float4(finalColor, 1);
    }

    float4 FragBaking(Varyings input) : SV_Target
    {
        return RenderSky(input);
    }

    float4 FragRender(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        return RenderSky(input) * GetCurrentExposureMultiplier();
    }

    ENDHLSL

    SubShader
    {
        // For cubemap
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragBaking
            ENDHLSL
        }

        // For fullscreen Sky
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragRender
            ENDHLSL
        }
    }
    Fallback Off
}