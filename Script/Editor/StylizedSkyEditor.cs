using UnityEditor;
using Misaki.StylizedSky;
using UnityEditor.Rendering;
using UnityEditor.Rendering.HighDefinition;

[CustomEditor(typeof(StylizedSky))]
public class StylizedSkyEditor : SkySettingsEditor
{
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

    SerializedDataParameter m_SkyExposureCurve;
    public override void OnEnable()
    {
        var o = new PropertyFetcher<StylizedSky>(serializedObject);

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

        m_SkyExposureCurve = Unpack(o.Find(x => x.skyEVCurve));
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sky Color", EditorStyles.boldLabel);
        PropertyField(m_SkyGradient);
        PropertyField(m_GroundGradient);
        PropertyField(m_HorizonLineGradient);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Space", EditorStyles.boldLabel);
        PropertyField(m_RenderSpace);
        PropertyField(m_SkyTexture);
        PropertyField(m_SpaceRotation);
        PropertyField(m_SpaceEV);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sky Control", EditorStyles.boldLabel);
        PropertyField(m_HorizonLineContribution);
        PropertyField(m_HorizonLineExponent);
        PropertyField(m_SunHaloContribution);
        PropertyField(m_SunHaloExponent);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Misc", EditorStyles.boldLabel);
        PropertyField(m_SkyExposureCurve);

        serializedObject.ApplyModifiedProperties();
    }
}