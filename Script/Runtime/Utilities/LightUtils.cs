using UnityEngine;

namespace Misaki.StylizedSky
{
    class LightUtils
    {
        internal static Color EvaluateLightColor(Light light, bool useIntensity = true)
        {
            Color finalColor;
            if (useIntensity)
                finalColor = light.color.linear * light.intensity;
            else
                finalColor = light.color.linear;

            if (light.useColorTemperature)
                finalColor *= Mathf.CorrelatedColorTemperatureToRGB(light.colorTemperature);
            return finalColor;
        }
    }
}