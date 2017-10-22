using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VolumetricLight))]
public class VolumetricLightEditor : Editor {

    private VolumetricLight m_Target;

    private SerializedProperty m_CullingMask;
    private SerializedProperty m_Quality;
    private SerializedProperty m_VertexBased;
    private SerializedProperty m_Subdivision;

    void OnEnable()
    {
        m_Target = (VolumetricLight)target;
        m_CullingMask = serializedObject.FindProperty("m_CullingMask");
        m_Quality = serializedObject.FindProperty("m_Quality");
        m_VertexBased = serializedObject.FindProperty("m_VertexBased");
        m_Subdivision = serializedObject.FindProperty("m_Subdivision");
    }

    public override void OnInspectorGUI()
    {
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
        Undo.RecordObject(m_Target, "Set VolumetricLightProperty");
        if (m_Target.vertexBased)
            EditorGUILayout.PropertyField(m_Subdivision);
        EditorGUILayout.PropertyField(m_CullingMask);
        EditorGUILayout.PropertyField(m_Quality);
        EditorGUILayout.PropertyField(m_VertexBased);

        serializedObject.ApplyModifiedProperties();
    }
}
