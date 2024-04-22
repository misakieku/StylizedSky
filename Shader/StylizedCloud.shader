Shader "Hidden/HDRP/Sky/StylizedCloud"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma editor_sync_compilation
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesGlobal.cs.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SphericalHarmonics.hlsl"

    int _enable;

    StructuredBuffer<float4> _ambientProbeBuffer;
    float _ambientDimmer;

    TEXTURECUBE(_CloudTexture); //r: lighting; g: rim light; b: SDF; a: thickness
    float4x4 _cloudRotation;

    float _rimLightPower;
    float _cloudStep;

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

    float2 SkyUVFromViewDir(float3 viewDir)
	{
		float2 uv;
		uv.x = 0.5 + atan2(viewDir.x, viewDir.z) / (2 * PI);
		uv.y = 0.5 - asin(viewDir.y) / PI;
		return uv;
	}

    float4 RenderClouds(Varyings input)
    {
        float4 Out = 0;

        if(_enable != 0)
        {
            float3 V = GetSkyViewDirWS(input.positionCS.xy);

            // Reverse it to point into the scene
            float3 dir = -V;
            float2 uv = SkyUVFromViewDir(V);

            float4 cloudTexture = SAMPLE_TEXTURECUBE(_CloudTexture, s_trilinear_clamp_sampler, mul(dir, (float3x3)_cloudRotation));
            float3 ambient = SampleSH9(_ambientProbeBuffer, float3(0, -1, 0)) * _ambientDimmer;

            Out.rgb = ambient;

            uint i = 0; // Declare once to avoid the D3D11 compiler warning.
            for (i = 0; i < _DirectionalLightCount; ++i)
            {
                float4 cloudColor;
                DirectionalLightData light = _DirectionalLightDatas[i];

                float3 L = -light.forward;
                float VdotL = saturate(dot(dir, L));

                Out.rgb += light.color.rgb * cloudTexture.r;
                Out.rgb += pow(VdotL, max(1, _rimLightPower)) * light.color.rgb * cloudTexture.g;
            }

            // Todo: Noise distortion
            Out.a = step(_cloudStep, cloudTexture.b) * cloudTexture.a;
        }

        return Out;
    }

    float4 FragBaking(Varyings input) : SV_Target
    {
        return RenderClouds(input);
    }

    float4 FragRender(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        return RenderClouds(input) * GetCurrentExposureMultiplier();
    }

    ENDHLSL

    SubShader
    {
        // For cubemap
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragBaking
            ENDHLSL
        }

        // For fullscreen sky
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragRender
            ENDHLSL
        }
    }
    Fallback Off
}
