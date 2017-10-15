using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VolumeLight2))]
public class VolumeLightEditor : Editor
{

    private VolumeLight2 m_Target;

    private SerializedProperty m_CullingMask;

    void OnEnable()
    {
        m_Target = (VolumeLight2) target;
        m_CullingMask = serializedObject.FindProperty("m_CullingMask");
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();
        serializedObject.Update();
        m_Target.directional = EditorGUILayout.Toggle("平行光", m_Target.directional);
        if (m_Target.directional)
        {
            m_Target.near = Mathf.Max(0.01f, EditorGUILayout.FloatField("Near", m_Target.near));
        }
        else
        {
            m_Target.near = EditorGUILayout.FloatField("Near", m_Target.near);
        }
        m_Target.far = Mathf.Max(m_Target.near+0.01f, EditorGUILayout.FloatField("Far",m_Target.far));
        if (m_Target.directional)
        {
            m_Target.size = Mathf.Max(0.01f, EditorGUILayout.FloatField("Size", m_Target.size));
        }
        else
        {
            m_Target.fieldOfView = Mathf.Clamp(EditorGUILayout.FloatField("Field Of View", m_Target.fieldOfView), 0.001f,
                  179f);
        }
        m_Target.aspect = Mathf.Max(0.01f, EditorGUILayout.FloatField("Aspect", m_Target.aspect));
        m_Target.color = EditorGUILayout.ColorField("Color", m_Target.color);
        m_Target.intensity = Mathf.Max(0f, EditorGUILayout.FloatField("Intensity", m_Target.intensity));
        m_Target.atten = Mathf.Max(0f, EditorGUILayout.FloatField("Atten", m_Target.atten));
        m_Target.bias = Mathf.Max(0f, EditorGUILayout.FloatField("Bias", m_Target.bias));
        m_Target.cookie = EditorGUILayout.ObjectField("Cookie", m_Target.cookie, typeof (Texture2D), false) as Texture2D;
        EditorGUILayout.PropertyField(m_CullingMask);

        serializedObject.ApplyModifiedProperties();
    }
}
