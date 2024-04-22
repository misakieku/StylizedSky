using UnityEditor;

namespace Misaki.StylizedSky
{
    [CustomEditor(typeof(StylizedDirectionalLight))]
    public class StylizedDirectionalLightEditor : Editor
    {
        StylizedDirectionalLight stylizedDirectionalLight;

        SerializedProperty interactsWithSky;

        SerializedProperty angularDiameterOverrideMode;
        SerializedProperty diameterMultiplier;
        SerializedProperty diameterOverride;

        SerializedProperty distance;

        SerializedProperty bodySource;
        SerializedProperty bodyLightSource;
        SerializedProperty earthShine;
        SerializedProperty sunColor;
        SerializedProperty sunIntensity;
        SerializedProperty phase;
        SerializedProperty phaseRotation;

        SerializedProperty surfaceTexture;
        SerializedProperty surfaceTint;

        SerializedProperty flareSize;
        SerializedProperty flareFalloff;
        SerializedProperty flareTint;
        SerializedProperty flareIntensity;

        SerializedProperty changeLightColor;
        SerializedProperty lightColorGradient;

        SerializedProperty changeLightIntensity;
        SerializedProperty lightIntensityCurve;
        SerializedProperty lightIntensityMultiplier;

        SerializedProperty priority;

        private void OnEnable()
        {
            stylizedDirectionalLight = target as StylizedDirectionalLight;

            serializedObject.Update();

            interactsWithSky = serializedObject.FindProperty(nameof(stylizedDirectionalLight.interactsWithSky));

            angularDiameterOverrideMode = serializedObject.FindProperty(nameof(stylizedDirectionalLight.diameterOverrideMode));
            diameterMultiplier = serializedObject.FindProperty(nameof(stylizedDirectionalLight.diameterMultiplier));
            diameterOverride = serializedObject.FindProperty(nameof(stylizedDirectionalLight.diameterOverride));

            distance = serializedObject.FindProperty(nameof(stylizedDirectionalLight.distance));

            bodySource = serializedObject.FindProperty(nameof(stylizedDirectionalLight.bodySource));
            bodyLightSource = serializedObject.FindProperty(nameof(stylizedDirectionalLight.bodyLightSource));
            earthShine = serializedObject.FindProperty(nameof(stylizedDirectionalLight.earthShine));
            sunColor = serializedObject.FindProperty(nameof(stylizedDirectionalLight.sunColor));
            sunIntensity = serializedObject.FindProperty(nameof(stylizedDirectionalLight.sunIntensity));
            phase = serializedObject.FindProperty(nameof(stylizedDirectionalLight.phase));
            phaseRotation = serializedObject.FindProperty(nameof(stylizedDirectionalLight.phaseRotation));

            surfaceTexture = serializedObject.FindProperty(nameof(stylizedDirectionalLight.surfaceTexture));
            surfaceTint = serializedObject.FindProperty(nameof(stylizedDirectionalLight.surfaceTint));

            flareSize = serializedObject.FindProperty(nameof(stylizedDirectionalLight.flareSize));
            flareFalloff = serializedObject.FindProperty(nameof(stylizedDirectionalLight.flareFalloff));
            flareTint = serializedObject.FindProperty(nameof(stylizedDirectionalLight.flareTint));
            flareIntensity = serializedObject.FindProperty(nameof(stylizedDirectionalLight.flareIntensity));

            changeLightColor = serializedObject.FindProperty(nameof(stylizedDirectionalLight.changeLightColor));
            lightColorGradient = serializedObject.FindProperty(nameof(stylizedDirectionalLight.lightColorGradient));

            changeLightIntensity = serializedObject.FindProperty(nameof(stylizedDirectionalLight.changeLightIntensity));
            lightIntensityCurve = serializedObject.FindProperty(nameof(stylizedDirectionalLight.lightIntensityCurve));
            lightIntensityMultiplier = serializedObject.FindProperty(nameof(stylizedDirectionalLight.lightIntensityMultiplier));

            priority = serializedObject.FindProperty(nameof(stylizedDirectionalLight.priority));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(interactsWithSky);

            EditorGUI.BeginDisabledGroup(!stylizedDirectionalLight.interactsWithSky);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(angularDiameterOverrideMode);
            switch (stylizedDirectionalLight.diameterOverrideMode)
            {
                case StylizedDirectionalLight.DiameterOverrideMode.None:
                    break;
                case StylizedDirectionalLight.DiameterOverrideMode.Multiply:
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(diameterMultiplier);
                    EditorGUI.indentLevel--;
                    break;
                case StylizedDirectionalLight.DiameterOverrideMode.Override:
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(diameterOverride);
                    EditorGUI.indentLevel--;
                    break;
                default:
                    break;
            }

            EditorGUILayout.PropertyField(distance);

            EditorGUILayout.PropertyField(bodySource);
            if (stylizedDirectionalLight.bodySource == StylizedDirectionalLight.CelestialBodyShadingSource.ReflectSunLight)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(bodyLightSource);
                EditorGUILayout.PropertyField(earthShine);
                EditorGUI.indentLevel--;
            }
            else if (stylizedDirectionalLight.bodySource == StylizedDirectionalLight.CelestialBodyShadingSource.Manual)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(sunColor);
                EditorGUILayout.PropertyField(sunIntensity);
                EditorGUILayout.PropertyField(phase);
                EditorGUILayout.PropertyField(phaseRotation);
                EditorGUILayout.PropertyField(earthShine);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(surfaceTexture);
            EditorGUILayout.PropertyField(surfaceTint);

            EditorGUILayout.PropertyField(flareSize);
            EditorGUILayout.PropertyField(flareFalloff);
            EditorGUILayout.PropertyField(flareTint);
            EditorGUILayout.PropertyField(flareIntensity);

            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(changeLightColor);
            EditorGUI.BeginDisabledGroup(!stylizedDirectionalLight.changeLightColor);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(lightColorGradient);
            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(changeLightIntensity);
            EditorGUI.BeginDisabledGroup(!stylizedDirectionalLight.changeLightIntensity);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(lightIntensityCurve);
            EditorGUILayout.PropertyField(lightIntensityMultiplier);
            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(priority);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
