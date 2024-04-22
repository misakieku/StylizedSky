using Misaki.StylizedSky;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.HighDefinition;

[CustomEditor(typeof(StylizedSky))]
public class StylizedSkyEditor : SkySettingsEditor
{
    StylizedSky stylizedSky;

    SerializedDataParameter m_SkyGradient;
    SerializedDataParameter m_GroundGradient;
    SerializedDataParameter m_HorizonLineGradient;

    SerializedDataParameter m_RenderSpace;
    SerializedDataParameter m_SkyTexture;
    SerializedDataParameter m_SpaceRotation;
    SerializedDataParameter m_SpaceEV;

    SerializedDataParameter m_HorizonLineContribution;
    SerializedDataParameter m_SunHaloContribution;
    SerializedDataParameter m_HorizonLineExponent;
    SerializedDataParameter m_SunHaloExponent;

    SerializedDataParameter m_ExposureType;
    SerializedDataParameter m_SkyExposureCurve;
    SerializedDataParameter m_SkyExposure;
    SerializedDataParameter m_SkyEVAdjustment;
    public override void OnEnable()
    {
        stylizedSky = target as StylizedSky;

        PropertyFetcher<StylizedSky> o = new PropertyFetcher<StylizedSky>(serializedObject);

        m_SkyGradient = Unpack(o.Find(x => x.skyGradient));
        m_GroundGradient = Unpack(o.Find(x => x.groundGradient));
        m_HorizonLineGradient = Unpack(o.Find(x => x.horizonLineGradient));

        m_RenderSpace = Unpack(o.Find(x => x.renderSpace));
        m_SkyTexture = Unpack(o.Find(x => x.spaceTexture));
        m_SpaceRotation = Unpack(o.Find(x => x.spaceRotation));
        m_SpaceEV = Unpack(o.Find(x => x.spaceEV));

        m_HorizonLineContribution = Unpack(o.Find(x => x.horizonLineContribution));
        m_HorizonLineExponent = Unpack(o.Find(x => x.horizonLineExponent));
        m_SunHaloContribution = Unpack(o.Find(x => x.sunHaloContribution));
        m_SunHaloExponent = Unpack(o.Find(x => x.sunHaloExponent));

        m_ExposureType = Unpack(o.Find(x => x.exposureMode));
        m_SkyExposureCurve = Unpack(o.Find(x => x.skyEVCurve));
        m_SkyExposure = Unpack(o.Find(x => x.skyFixedExposure));
        m_SkyEVAdjustment = Unpack(o.Find(x => x.skyEVAdjustment));
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Sky Color", EditorStyles.miniLabel);
        PropertyField(m_SkyGradient);
        PropertyField(m_GroundGradient);
        PropertyField(m_HorizonLineGradient);

        EditorGUILayout.LabelField("Space", EditorStyles.miniLabel);
        PropertyField(m_RenderSpace);
        PropertyField(m_SkyTexture);
        PropertyField(m_SpaceRotation);
        PropertyField(m_SpaceEV);

        EditorGUILayout.LabelField("Sky Control", EditorStyles.miniLabel);
        PropertyField(m_HorizonLineContribution);
        PropertyField(m_HorizonLineExponent);
        PropertyField(m_SunHaloContribution);
        PropertyField(m_SunHaloExponent);

        EditorGUILayout.LabelField("Exposure", EditorStyles.miniLabel);
        PropertyField(m_ExposureType);
        switch (stylizedSky.exposureMode.value)
        {
            case StylizedSky.ExposureType.Curve:
                PropertyField(m_SkyExposureCurve);
                break;

            case StylizedSky.ExposureType.Fixed:
                PropertyField(m_SkyExposure);
                break;

            case StylizedSky.ExposureType.Automatic:
                PropertyField(m_SkyEVAdjustment);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}