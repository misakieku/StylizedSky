using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Misaki.StylizedSky
{
    public class StylizedSkyRenderer : SkyRenderer
    {
        public static StylizedSkyRenderer instance;

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

        static readonly int _RenderSunDisk = Shader.PropertyToID("_renderSunDisk");
        static readonly int _PixelCoordToViewDirWS = Shader.PropertyToID("_PixelCoordToViewDirWS");

        StylizedSky stylizedSky;
        public BuiltinSkyParameters builtinSkyParams;
        Light mainLight = null;

        // Renders a cubemap into a render texture (can be cube or 2D)
        Material m_StylizedSkyMaterial;
        MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

        public override void Build()
        {
            instance = this;
            m_StylizedSkyMaterial = CoreUtils.CreateEngineMaterial(GetSkyShader());
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_StylizedSkyMaterial);
        }

        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            stylizedSky = builtinParams.skySettings as StylizedSky;
            builtinSkyParams = builtinParams;

            Vector3 sunDirection;
            float ForwardY;
            UpdateMainLight(builtinParams, out sunDirection, out ForwardY);

            m_PropertyBlock.SetColor(_skyColor, stylizedSky.skyGradient.value.Evaluate(ForwardY));
            m_PropertyBlock.SetColor(_groundColor, stylizedSky.groundGradient.value.Evaluate(ForwardY));
            m_PropertyBlock.SetFloat(_skyIntensity, stylizedSky.skyEVCurve.value.Evaluate(ForwardY));

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

            m_PropertyBlock.SetColor(_horizonLineColor, stylizedSky.horizonLineGradient.value.Evaluate(ForwardY));
            m_PropertyBlock.SetFloat(_horizonLineContribution, stylizedSky.horizonLineContribution.value);
            m_PropertyBlock.SetFloat(_horizonLineExponent, stylizedSky.horizonLineExponent.value);
            m_PropertyBlock.SetColor(_sunColor, mainLight.color);
            m_PropertyBlock.SetFloat(_sunHaloContribution, stylizedSky.sunHaloContribution.value);
            m_PropertyBlock.SetFloat(_sunHaloExponent, stylizedSky.sunHaloExponent.value);

            m_PropertyBlock.SetVector(_sunDirection, sunDirection);

            m_PropertyBlock.SetMatrix(_PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);
            m_PropertyBlock.SetInt(_RenderSunDisk, renderSunDisk ? 1 : 0);
            CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_StylizedSkyMaterial, m_PropertyBlock, renderForCubemap ? 0 : 1);
        }

        void UpdateMainLight(BuiltinSkyParameters builtinParams, out Vector3 sunDirection, out float ForwardY)
        {
            // Default values when no sun is provided
            sunDirection = Vector3.zero;
            ForwardY = 0.5f;

            if (mainLight == null)
            {
                mainLight = builtinParams.sunLight;
            }

            if (mainLight != null)
            {
                sunDirection = -mainLight.transform.forward;
                ForwardY = Mathf.Max(sunDirection.y * 0.5f + 0.5f, 0f);
            }
        }

        void GetDirectionalLight()
        {
            var lightList = Object.FindObjectsByType<HDStylizedDirectionalLight>(FindObjectsSortMode.None);

            if (lightList.Count() <= 0)
                return;

            mainLight = lightList.First().lightComponent.GetComponent<Light>();
            float top = -100;
            if (lightList.Count() > 1)
            {
                foreach (var item in lightList)
                {
                    if (item.priority > top)
                    {
                        mainLight = item.lightComponent.GetComponent<Light>();
                        top = item.priority;
                    }
                }
            }
        }

        public void UpdateSky() 
        {
            GetDirectionalLight();

            var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdrp == null)
                return;

            hdrp.RequestSkyEnvironmentUpdate();
        }

        private Shader GetSkyShader()
        {
            return Shader.Find("Hidden/HDRP/Sky/StylizedSky");
        }
    }
}
