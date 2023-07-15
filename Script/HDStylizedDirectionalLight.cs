using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Misaki.StylizedSky
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Light))]
    public class HDStylizedDirectionalLight : MonoBehaviour
    {
        public HDAdditionalLightData lightComponent;

        [Space]
        [Header("Color")]
        public bool changeLightColor;
        public Gradient lightColorGradient;

        [Space]
        [Header("Intensity")]
        public bool changeLightIntensity;
        public AnimationCurve lightIntensityCurve;
        public float lightIntensityMultiplier;

        [Space]
        [Header("Misc")]
        [Range(-100, 100)]
        public int priority = 0;

        private void OnEnable()
        {
            lightComponent = GetComponent<HDAdditionalLightData>();

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

            var sunDirection = -transform.forward;
            var ForwardY = Mathf.Max(sunDirection.y * 0.5f + 0.5f, 0f);

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