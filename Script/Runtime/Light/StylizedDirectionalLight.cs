using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Misaki.StylizedSky
{
    [ExecuteAlways]
    [RequireComponent(typeof(Light))]
    public class StylizedDirectionalLight : MonoBehaviour
    {
        /// <summary>
        /// Light source used to shade the celestial body.
        /// </summary>
        public enum CelestialBodyShadingSource
        {
            /// <summary>
            /// The celestial body will emit light.
            /// </summary>
            Emission = 1,
            /// <summary>
            /// The celestial body will reflect light from a directional light in the scene.
            /// </summary>
            ReflectSunLight = 0,
            /// <summary>
            /// The celestial body will be illuminated by an artificial light source.
            /// </summary>
            Manual = 2,
        }

        /// <summary>
        /// Override mode for the angular diameter of the light.
        /// </summary>
        public enum DiameterOverrideMode
        {
            /// <summary>
            /// No override will be applied.
            /// </summary>
            None = 0,
            /// <summary>
            /// Multiply the angular diameter of the sun by the value of the override.
            /// </summary>
            Multiply = 1,
            /// <summary>
            /// Override the angular diameter of the sun with the value of the override.
            /// </summary>
            Override = 2,
        }

        public HDAdditionalLightData lightComponent;

        [Header("Celestial Body")]
        public bool interactsWithSky;

        public DiameterOverrideMode diameterOverrideMode = DiameterOverrideMode.None;
        public float diameterMultiplier = 1;
        public float diameterOverride = 0.53f;

        public float distance = 150000000000;

        public CelestialBodyShadingSource bodySource = CelestialBodyShadingSource.Emission;
        public Light bodyLightSource;
        [Min(0)]
        public float earthShine = 1;
        public Color sunColor = Color.white;
        public float sunIntensity = 130000;
        [Range(0, 1)]
        public float phase = 0.2f;
        [Range(0, 360)]
        public float phaseRotation = 0;

        public Texture2D surfaceTexture;
        public Color surfaceTint = Color.white;

        [Range(0, 90)]
        public float flareSize = 2;
        public float flareFalloff = 4;
        public Color flareTint = Color.white;
        [Range(0, 1)]
        public float flareIntensity = 1;

        [Header("Color")]
        public bool changeLightColor;
        public Gradient lightColorGradient;

        [Header("Intensity")]
        public bool changeLightIntensity;
        public AnimationCurve lightIntensityCurve;
        public float lightIntensityMultiplier;

        [Header("Misc")]
        [Range(-100, 100)]
        public int priority = 0;

        [ExcludeCopy]
        internal StylizedLightRenderEntity lightEntity = StylizedLightRenderEntity.Invalid;

        // Runtime datas used to compute light intensity
        [ExcludeCopy]
        Light m_Light;
        internal Light legacyLight
        {
            get
            {
                // Calling TryGetComponent only when needed is faster than letting the null check happen inside TryGetComponent
                if (m_Light == null)
                    TryGetComponent<Light>(out m_Light);

                return m_Light;
            }
        }

        internal void UpdateRenderEntity()
        {
            //todo
        }

        internal void CreateHDLightRenderEntity(bool autoDestroy = false)
        {
            if (!lightEntity.valid)
            {
                StylizedLightRenderDatabase lightEntities = StylizedLightRenderDatabase.instance;
                lightEntity = lightEntities.CreateEntity(autoDestroy);
                lightEntities.AttachGameObjectData(lightEntity, legacyLight.GetInstanceID(), this, legacyLight.gameObject);
            }

            UpdateRenderEntity();
        }
        internal void DestroyHDLightRenderEntity()
        {
            if (!lightEntity.valid)
                return;

            StylizedLightRenderDatabase.instance.DestroyEntity(lightEntity);
            lightEntity = StylizedLightRenderEntity.Invalid;
        }

        private void OnEnable()
        {
            CreateHDLightRenderEntity();
            UpdateSky();
        }

        private void OnDisable()
        {
            DestroyHDLightRenderEntity();
            UpdateSky();
        }

        private void OnDestroy()
        {
            UpdateSky();
        }

        private void OnValidate()
        {
            UpdateSky();
        }

        private static void UpdateSky()
        {
            if (StylizedSkyRenderer.instance == null)
                return;
            StylizedSkyRenderer.instance.UpdateSky();
        }

        private void Update()
        {
            if (lightComponent == null)
                return;

            if (!changeLightColor && !changeLightIntensity)
                return;

            Vector3 sunDirection = -transform.forward;
            float ForwardY = Mathf.Max(sunDirection.y * 0.5f + 0.5f, 0f);

            if (changeLightColor && lightColorGradient != null)
            {
                lightComponent.color = lightColorGradient.Evaluate(ForwardY);
            }

            if (changeLightIntensity && lightIntensityCurve != null)
            {
                lightComponent.intensity = lightIntensityCurve.Evaluate(ForwardY) * lightIntensityMultiplier;
            }
        }
    }
}