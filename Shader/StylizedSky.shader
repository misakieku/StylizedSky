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
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSkyRendering.hlsl"
    #include "StylizedSkyRender.hlsl"

    int _rendersunDisc;

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

    void ComputeSkyMasks(Varyings input, float3 V, out float4 sunDisc, out float sunHalo, out float horizon, out float gradient, out float skyGradient)
    {
        // Reverse it to point into the scene
        float3 dir = -V;
        float VdotL = saturate(dot(dir, _sunDirection));
        float y = dir.y;

        float bellCurve = pow(saturate(VdotL), _sunHaloExponent * saturate(abs(y)));
        float horizonSoften = 1 - pow(1 - saturate(y), 50);
        if(_rendersunDisc != 0)
        {
            sunHalo = saturate(bellCurve * horizonSoften);
        }
        else
        {
			sunHalo = 0;
		}

        float tFrag    = FLT_INF;
        sunDisc = SunDisc(V, _rendersunDisc, _skyIntensity) * horizonSoften;

        horizon = saturate(1.0 - abs(y));
        horizon = saturate(pow(horizon, _horizonLineExponent));

        gradient = saturate(y);
        gradient = saturate(pow(gradient, 0.5));

        skyGradient = y * 0.25 + 0.75;
    }

    float4 RenderSky(Varyings input)
    {
        float3 V = GetSkyViewDirWS(input.positionCS.xy);
        float maskSunHalo, maskHorizon, maskGradient, maskSkyGradient = 0;
        float4 sunDisc = 0;
        float3 skyColor, spaceColor, finalColor = 0;
        ComputeSkyMasks(input, V, sunDisc, maskSunHalo, maskHorizon, maskGradient, maskSkyGradient);

        skyColor = lerp(_groundColor, _skyColor * maskSkyGradient, maskGradient);

        // Space
        if(_renderSpace == 1)
        {
            spaceColor = SAMPLE_TEXTURECUBE(_spaceTexture, s_trilinear_clamp_sampler, mul(-V, (float3x3)_spaceRotation)).rgb * maskGradient;
            spaceColor = spaceColor * EV2Lux(_spaceEV);
            finalColor += spaceColor * (1 - sunDisc.a);
        }

        // Add sun disc
        skyColor += sunDisc.rgb;

        // Add horizon line.
        skyColor += _horizonLineColor * _horizonLineContribution * maskHorizon * maskSkyGradient;

        // Add sun halo.
        skyColor += _sunColor * _sunHaloContribution * maskSunHalo;

        skyColor = skyColor * EV2Lux(_skyIntensity);

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