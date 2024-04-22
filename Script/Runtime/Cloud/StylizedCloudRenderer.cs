using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Misaki.StylizedSky
{
    public class StylizedCloudRenderer : CloudRenderer
    {
        static readonly int _PixelCoordToViewDirWS = Shader.PropertyToID("_PixelCoordToViewDirWS");
        static readonly int _CloudTexture = Shader.PropertyToID("_CloudTexture");

        Material m_CloudMaterial;

        MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

        private static int m_RenderCubemapID = 0; // FragBaking
        private static int m_RenderFullscreenCloudID = 1; // FragRender

        public override void Build()
        {
            m_CloudMaterial = CoreUtils.CreateEngineMaterial(GetNewCloudShader());
        }

        Shader GetNewCloudShader()
        {
            return Shader.Find("Hidden/HDRP/Sky/StylizedCloud");
        }

        public override void Cleanup()
        {

        }

        public override void RenderClouds(BuiltinSkyParameters builtinParams, bool renderForCubemap)
        {
            using (new ProfilingSample(builtinParams.commandBuffer, "Draw clouds"))
            {
                StylizedCloud stylizedCloud = builtinParams.cloudSettings as StylizedCloud;

                int passID = renderForCubemap ? m_RenderCubemapID : m_RenderFullscreenCloudID;

                m_PropertyBlock.SetMatrix(_PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);
                m_PropertyBlock.SetTexture(_CloudTexture, stylizedCloud.clouds.value);

                CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_CloudMaterial, m_PropertyBlock, passID);
            }
        }
    }
}
