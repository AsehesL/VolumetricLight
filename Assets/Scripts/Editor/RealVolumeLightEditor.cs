using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(RealVolumeLight))]
public class RealVolumeLightEditor : Editor
{
    private RealVolumeLight m_Target;

    private SerializedProperty m_CullingMask;
    private SerializedProperty m_Quality;

    void OnEnable()
    {
        m_Target = (RealVolumeLight)target;
        m_CullingMask = serializedObject.FindProperty("m_CullingMask");
        m_Quality = serializedObject.FindProperty("m_Quality");
    }

    public override void OnInspectorGUI()
    {
        //        base.OnInspectorGUI();
        serializedObject.Update();
        m_Target.directional = EditorGUILayout.Toggle("平行光", m_Target.directional);
        m_Target.range = Mathf.Max(0.01f, EditorGUILayout.FloatField("Range", m_Target.range));
        if (m_Target.directional)
        {
            m_Target.size = Mathf.Max(0.01f, EditorGUILayout.FloatField("Size", m_Target.size));
        }
        else
        {
            m_Target.angle = Mathf.Clamp(EditorGUILayout.FloatField("Angle", m_Target.angle), 0.001f,
                  179f);
        }
        m_Target.aspect = Mathf.Max(0.01f, EditorGUILayout.FloatField("Aspect", m_Target.aspect));
        m_Target.color = EditorGUILayout.ColorField("Color", m_Target.color);
        m_Target.intensity = Mathf.Max(0f, EditorGUILayout.FloatField("Intensity", m_Target.intensity));
        m_Target.shadowBias = Mathf.Max(0f, EditorGUILayout.FloatField("ShadowBias", m_Target.shadowBias));
        m_Target.cookie = EditorGUILayout.ObjectField("Cookie", m_Target.cookie, typeof(Texture2D), false) as Texture2D;
        EditorGUILayout.PropertyField(m_CullingMask);
        EditorGUILayout.PropertyField(m_Quality);

        serializedObject.ApplyModifiedProperties();
    }
}
