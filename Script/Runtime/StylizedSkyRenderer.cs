using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Misaki.StylizedSky
{
    public class StylizedSkyRenderer : SkyRenderer
    {
        public static StylizedSkyRenderer instance;

        static readonly int _surfaceAtlasSize = Shader.PropertyToID("_SurfaceAtlasSize");
        static readonly int _surfaceAtlas = Shader.PropertyToID("_SurfaceAtlas");
        static readonly int _surfaceAtlasData = Shader.PropertyToID("_SurfaceAtlasData");
        static readonly int _StylizedDirectionalCount = Shader.PropertyToID("_StylizedDirectionalCount");
        static readonly int _StylizedDirectionalDatas = Shader.PropertyToID("_StylizedDirectionalDatas");


        static readonly int _skyColor = Shader.PropertyToID("_skyColor");
        static readonly int _groundColor = Shader.PropertyToID("_groundColor");
        static readonly int _horizonLineContribution = Shader.PropertyToID("_horizonLineContribution");

        static readonly int _skyIntensity = Shader.PropertyToID("_skyIntensity");

        static readonly int _horizonLineColor = Shader.PropertyToID("_horizonLineColor");
        static readonly int _horizonLineExponent = Shader.PropertyToID("_horizonLineExponent");
        static readonly int _sunColor = Shader.PropertyToID("_sunColor");
        static readonly int _sunHaloContribution = Shader.PropertyToID("_sunHaloContribution");
        static readonly int _sunHaloExponent = Shader.PropertyToID("_sunHaloExponent");

        static readonly int _sunDirection = Shader.PropertyToID("_sunDirection");

        static readonly int _renderSpace = Shader.PropertyToID("_renderSpace");
        static readonly int _spaceTexture = Shader.PropertyToID("_spaceTexture");
        static readonly int _spaceRotation = Shader.PropertyToID("_spaceRotation");
        static readonly int _spaceEV = Shader.PropertyToID("_spaceEV");

        static readonly int _RendersunDisc = Shader.PropertyToID("_rendersunDisc");
        static readonly int _PixelCoordToViewDirWS = Shader.PropertyToID("_PixelCoordToViewDirWS");

        StylizedSky stylizedSky;
        public BuiltinSkyParameters builtinSkyParams;
        static GraphicsBuffer s_StylizedDirectionalBuffer;
        static StylizedDirectionalData[] s_StylizedDirectionalDatas;
        static int s_DataFrameUpdate = -1;
        static uint s_StylizedDirectionalLightCount;
        static float s_StylizedDirectionalLightExposure;
        Light mainLight = null;

        // Renders a cubemap into a render texture (can be cube or 2D)
        Material m_StylizedSkyMaterial;
        MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

        internal class LightLoopTextureCaches
        {
            // Structure for cookies used by directional and spotlights
            public SurfaceTextureManager surfaceTextureManager { get; private set; }

            public void Initialize()
            {
                //var lightLoopSettings = hdrpAsset.currentPlatformRenderPipelineSettings.lightLoopSettings;

                surfaceTextureManager = new SurfaceTextureManager();
            }

            public void Cleanup()
            {
                surfaceTextureManager.Release();
            }

            public void NewFrame()
            {
                surfaceTextureManager.NewFrame();
            }

            public void NewRender()
            {

            }
        }

        internal LightLoopTextureCaches m_TextureCaches = new();

        public override void Build()
        {
            instance = this;
            m_StylizedSkyMaterial = CoreUtils.CreateEngineMaterial(GetSkyShader());

            HDRenderPipelineAsset asset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;

            m_TextureCaches.Initialize();

            GlobalLightLoopSettings lightLoopSettings = asset.currentPlatformRenderPipelineSettings.lightLoopSettings;
            CreateDirectionalBuffer(lightLoopSettings.maxDirectionalLightsOnScreen);

            UpdateSky();
        }

        private void CreateDirectionalBuffer(int maxLightCount)
        {
            int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(StylizedDirectionalData));
            s_StylizedDirectionalBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxLightCount, stride);
            s_StylizedDirectionalDatas = new StylizedDirectionalData[maxLightCount];
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_StylizedSkyMaterial);
            m_TextureCaches.Cleanup();
        }

        public override void PreRenderSky(BuiltinSkyParameters builtinParams)
        {
            base.PreRenderSky(builtinParams);
        }

        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool rendersunDisc)
        {
            // Only update the LightLoopTextureCaches if need to render the sun disc
            if (rendersunDisc)
            {
                // Set the render target to the atlas texture
                builtinParams.commandBuffer.SetRenderTarget(m_TextureCaches.surfaceTextureManager.atlasTexture);
                m_TextureCaches.NewFrame();
                UpdateStylizedDirectionalBuffer(builtinParams.commandBuffer, builtinParams);

                // Set the render target back to the color buffer
                builtinParams.commandBuffer.SetRenderTarget(builtinParams.colorBuffer, builtinParams.depthBuffer);
            }

            stylizedSky = builtinParams.skySettings as StylizedSky;
            builtinSkyParams = builtinParams;

            StylizedLightRenderDatabase lightEntities = StylizedLightRenderDatabase.instance;

            (Vector3, float) direction = GetMainLightDirection();
            Vector3 sunDirection = direction.Item1;
            float forwardY = direction.Item2;

            Vector4 surfaceAtlasSize = m_TextureCaches.surfaceTextureManager.GetCookieAtlasSize();
            Vector4 surfaceAtlasData = m_TextureCaches.surfaceTextureManager.GetCookieAtlasDatas();

            m_PropertyBlock.SetVector(_surfaceAtlasSize, new Vector4(surfaceAtlasSize.x, surfaceAtlasSize.y, 1.0f / surfaceAtlasSize.x, 1.0f / surfaceAtlasSize.y));
            m_PropertyBlock.SetTexture(_surfaceAtlas, m_TextureCaches.surfaceTextureManager.atlasTexture);
            m_PropertyBlock.SetVector(_surfaceAtlasData, surfaceAtlasData);
            m_PropertyBlock.SetInt(_StylizedDirectionalCount, lightEntities.lightCount);
            m_PropertyBlock.SetBuffer(_StylizedDirectionalDatas, s_StylizedDirectionalBuffer);

            m_PropertyBlock.SetColor(_skyColor, stylizedSky.skyGradient.value.Evaluate(forwardY));
            m_PropertyBlock.SetColor(_groundColor, stylizedSky.groundGradient.value.Evaluate(forwardY));
            switch (stylizedSky.exposureMode.value)
            {
                case StylizedSky.ExposureType.Curve:
                    m_PropertyBlock.SetFloat(_skyIntensity, stylizedSky.skyEVCurve.value.Evaluate(forwardY));
                    break;
                case StylizedSky.ExposureType.Fixed:
                    m_PropertyBlock.SetFloat(_skyIntensity, stylizedSky.skyFixedExposure.value);
                    break;
                case StylizedSky.ExposureType.Automatic:
                    m_PropertyBlock.SetFloat(_skyIntensity, Mathf.Log(s_StylizedDirectionalLightExposure / 2.5f, 2) + stylizedSky.skyEVAdjustment.value);
                    break;
                default:
                    break;
            }

            if (stylizedSky.spaceTexture.value != null)
            {
                m_PropertyBlock.SetInt(_renderSpace, stylizedSky.renderSpace.value ? 1 : 0);
                m_PropertyBlock.SetTexture(_spaceTexture, stylizedSky.spaceTexture.value);
                Quaternion spaceRotation = Quaternion.Euler(stylizedSky.spaceRotation.value.x, stylizedSky.spaceRotation.value.y, stylizedSky.spaceRotation.value.z);
                m_PropertyBlock.SetMatrix(_spaceRotation, Matrix4x4.Rotate(spaceRotation));
                m_PropertyBlock.SetFloat(_spaceEV, stylizedSky.spaceEV.value);
            }
            else
            {
                m_PropertyBlock.SetInt(_renderSpace, 0);
            }

            m_PropertyBlock.SetColor(_horizonLineColor, stylizedSky.horizonLineGradient.value.Evaluate(forwardY));
            m_PropertyBlock.SetFloat(_horizonLineContribution, stylizedSky.horizonLineContribution.value);
            m_PropertyBlock.SetFloat(_horizonLineExponent, stylizedSky.horizonLineExponent.value);
            Color mainColor = mainLight == null ? Color.white : (Vector4)LightUtils.EvaluateLightColor(mainLight, false);
            m_PropertyBlock.SetColor(_sunColor, mainColor);
            m_PropertyBlock.SetFloat(_sunHaloContribution, stylizedSky.sunHaloContribution.value);
            m_PropertyBlock.SetFloat(_sunHaloExponent, stylizedSky.sunHaloExponent.value);

            m_PropertyBlock.SetVector(_sunDirection, sunDirection);

            m_PropertyBlock.SetMatrix(_PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);
            m_PropertyBlock.SetInt(_RendersunDisc, rendersunDisc ? 1 : 0);

            CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_StylizedSkyMaterial, m_PropertyBlock, renderForCubemap ? 0 : 1);
        }

        private void UpdateStylizedDirectionalBuffer(CommandBuffer cmd, BuiltinSkyParameters builtinParams)
        {
            if (s_StylizedDirectionalBuffer == null)
            {
                CreateDirectionalBuffer(16);
            }

            if (builtinParams.frameIndex != s_DataFrameUpdate)
            {
                s_DataFrameUpdate = builtinParams.frameIndex;
                List<StylizedDirectionalLight> directionalLights = StylizedLightRenderDatabase.instance.directionalLights;

                float exposure = 1.0f;

                uint lightCount = 0;
                foreach (StylizedDirectionalLight light in directionalLights)
                {
                    if (light.legacyLight.enabled && light.interactsWithSky && light.lightComponent.intensity != 0.0f)
                    {
                        FillStylizedDirectionalData(cmd, light, ref s_StylizedDirectionalDatas[lightCount++]);
                        exposure = Mathf.Max(light.lightComponent.intensity * -light.transform.forward.y, exposure);
                    }
                    else
                    {
                        s_StylizedDirectionalDatas[lightCount++].distanceFromCamera = -1;
                    }
                }

                s_StylizedDirectionalLightCount = lightCount;
                s_StylizedDirectionalLightExposure = exposure;

                s_StylizedDirectionalBuffer.SetData(s_StylizedDirectionalDatas);
            }
        }

        internal void FillStylizedDirectionalData(CommandBuffer cmd, StylizedDirectionalLight stylizedLight, ref StylizedDirectionalData stylizedDirectionalData)
        {
            Light light = stylizedLight.legacyLight;
            Transform transform = light.transform;
            HDAdditionalLightData hdLightData = stylizedLight.lightComponent;

            stylizedDirectionalData.color = (Vector4)LightUtils.EvaluateLightColor(light);

            // General
            stylizedDirectionalData.forward = transform.forward;
            stylizedDirectionalData.right = transform.right.normalized;
            stylizedDirectionalData.up = transform.up.normalized;

            float angularDiameter = hdLightData.angularDiameter;
            switch (stylizedLight.diameterOverrideMode)
            {
                case StylizedDirectionalLight.DiameterOverrideMode.None:
                    break;
                case StylizedDirectionalLight.DiameterOverrideMode.Multiply:
                    angularDiameter *= stylizedLight.diameterMultiplier;
                    break;
                case StylizedDirectionalLight.DiameterOverrideMode.Override:
                    angularDiameter = stylizedLight.diameterOverride;
                    break;
                default:
                    break;
            }

            stylizedDirectionalData.angularRadius = angularDiameter * 0.5f * Mathf.Deg2Rad;
            stylizedDirectionalData.distanceFromCamera = stylizedLight.distance;
            stylizedDirectionalData.radius = Mathf.Tan(stylizedDirectionalData.angularRadius) * stylizedDirectionalData.distanceFromCamera;

            stylizedDirectionalData.surfaceColor = (Vector4)stylizedLight.surfaceTint.linear;
            stylizedDirectionalData.earthshine = stylizedLight.earthShine * 0.01f; // earth reflects about 0.01% of sun light

            // Surface texture
            if (stylizedLight.surfaceTexture != null)
            {
                m_TextureCaches.surfaceTextureManager.ReserveSpace(stylizedLight.surfaceTexture);
                m_TextureCaches.surfaceTextureManager.LayoutIfNeeded();

                stylizedDirectionalData.surfaceTextureScaleOffset = m_TextureCaches.surfaceTextureManager.Fetch2DCookie(cmd, stylizedLight.surfaceTexture);
            }
            else
            {
                stylizedDirectionalData.surfaceTextureScaleOffset = Vector4.zero;
            }

            // Flare
            stylizedDirectionalData.flareSize = Mathf.Max(stylizedLight.flareSize * Mathf.Deg2Rad, 5.960464478e-8f);
            stylizedDirectionalData.flareFalloff = stylizedLight.flareFalloff;

            stylizedDirectionalData.flareCosInner = Mathf.Cos(stylizedDirectionalData.angularRadius);
            stylizedDirectionalData.flareCosOuter = Mathf.Cos(stylizedDirectionalData.angularRadius + stylizedDirectionalData.flareSize);

            stylizedDirectionalData.flareColor = stylizedLight.flareIntensity * (Vector4)stylizedLight.flareTint.linear;

            // Shading
            StylizedDirectionalLight.CelestialBodyShadingSource source = stylizedLight.bodySource;
            if (source == StylizedDirectionalLight.CelestialBodyShadingSource.Emission)
            {
                stylizedDirectionalData.type = 0;
                float rcpSolidAngle = 1.0f / (Mathf.PI * 2.0f * (1 - stylizedDirectionalData.flareCosInner));
                stylizedDirectionalData.surfaceColor *= rcpSolidAngle;
                stylizedDirectionalData.flareColor *= rcpSolidAngle;

                stylizedDirectionalData.surfaceColor = Vector4.Scale(stylizedDirectionalData.color, stylizedDirectionalData.surfaceColor);
                stylizedDirectionalData.flareColor = Vector4.Scale(stylizedDirectionalData.color, stylizedDirectionalData.flareColor);
            }
            else
            {
                Color sunColor;
                if (source == StylizedDirectionalLight.CelestialBodyShadingSource.Manual)
                {
                    Quaternion rotation = Quaternion.AngleAxis(stylizedLight.phaseRotation, stylizedDirectionalData.forward);
                    Quaternion remap = Quaternion.FromToRotation(Vector3.right, stylizedDirectionalData.forward);
                    float phase = stylizedLight.phase * 2.0f * Mathf.PI;

                    sunColor = stylizedLight.sunColor * stylizedLight.sunIntensity;
                    stylizedDirectionalData.sunDirection = rotation * remap * new Vector3(Mathf.Cos(phase), 0, Mathf.Sin(phase));
                }
                else
                {
                    Light lightSource = stylizedLight.bodyLightSource;
                    if (lightSource == null || lightSource == stylizedLight.legacyLight || lightSource.type != LightType.Directional)
                        lightSource = mainLight;
                    sunColor = lightSource != null ? (Vector4)LightUtils.EvaluateLightColor(lightSource) : Vector4.zero;
                    stylizedDirectionalData.sunDirection = lightSource != null ? lightSource.transform.forward : Vector3.forward;
                }

                stylizedDirectionalData.type = 1;
                stylizedDirectionalData.surfaceColor = Vector4.Scale(sunColor, stylizedDirectionalData.surfaceColor);
                stylizedDirectionalData.flareColor = Vector4.Scale(sunColor, stylizedDirectionalData.flareColor);
            }
        }

        private (Vector3, float) GetMainLightDirection()
        {
            // Default values when no sun is provided
            Vector3 sunDirection = Vector3.zero;
            float forwardY = 0.5f;

            // If a main light is provided, use it
            if (mainLight != null)
            {
                // Set the sun direction to the main light's forward vector
                sunDirection = -mainLight.transform.forward;
                // Set the ForwardY value to the maximum of the sun direction's y value and 0.5f
                forwardY = Mathf.Max(sunDirection.y * 0.5f + 0.5f, 0f);
            }

            return (sunDirection, forwardY);
        }

        public void UpdateSky()
        {
            mainLight = StylizedLightRenderDatabase.mainLight == null ? null : StylizedLightRenderDatabase.mainLight.legacyLight;

            if (RenderPipelineManager.currentPipeline is not HDRenderPipeline hdrp)
                return;

            hdrp.RequestSkyEnvironmentUpdate();
        }

        private Shader GetSkyShader()
        {
            return Shader.Find("Hidden/HDRP/Sky/StylizedSky");
        }
    }
}
