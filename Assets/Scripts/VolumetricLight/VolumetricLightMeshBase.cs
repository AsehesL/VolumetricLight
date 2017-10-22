using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 体积光载体mesh基类
/// </summary>
public abstract class VolumetricLightMeshBase
{
    private Mesh lightMesh;
    private Material m_LightMaterial;
    private Shader m_LightShader;

    private List<Vector3> m_VertexList;
    private List<Color> m_ColorList;
    private int[] m_Indexes;

    private bool m_IsSupport = false;

    public bool CheckSupport()
    {
        m_LightShader = LoadShader();
        if (m_LightShader == null || !m_LightShader.isSupported)
            return false;
        m_IsSupport = true;
        return m_IsSupport;
    }

    public void InitMesh(VolumetricLight light)
    {
        if (!m_IsSupport)
            return;
        MeshRenderer renderer = light.gameObject.GetComponent<MeshRenderer>();
        if (!renderer)
            renderer = light.gameObject.AddComponent<MeshRenderer>();
        MeshFilter meshfilter = light.gameObject.GetComponent<MeshFilter>();
        if (!meshfilter)
            meshfilter = light.gameObject.AddComponent<MeshFilter>();
        m_LightMaterial = new Material(m_LightShader);
        renderer.sharedMaterial = m_LightMaterial;
        lightMesh = new Mesh();
        lightMesh.MarkDynamic();
        meshfilter.sharedMesh = lightMesh;

        OnInit(light);
    }

    public void RefreshMesh(Color color, float intensity, float range, float subdivision, Matrix4x4 matrix)
    {
        if (!m_IsSupport)
            return;
        lightMesh.Clear();
        if (m_VertexList == null)
            m_VertexList = new List<Vector3>();
        if (m_ColorList == null)
            m_ColorList = new List<Color>();
        Color col = new Color(color.r * intensity * 0.5f, color.g * intensity * 0.5f, color.b * intensity * 0.5f, color.a);
        OnRefreshMesh(col, range, subdivision, matrix);
    }
    

    public void Destroy()
    {
        if (lightMesh)
            Object.Destroy(lightMesh);
        lightMesh = null;
        if (m_LightMaterial)
            Object.Destroy(m_LightMaterial);
        m_LightMaterial = null;
        if (m_LightShader)
            UnLoadShader(m_LightShader);
        m_LightShader = null;

        if (m_VertexList != null)
            m_VertexList.Clear();
        m_VertexList = null;
        if (m_ColorList != null)
            m_ColorList.Clear();
        m_ColorList = null;

        OnDestroy();
    }

    public void RefreshColor(Color color, float intensity)
    {
        if (m_ColorList == null) return;
        if (m_VertexList == null) return;
        if (m_Indexes == null) return;
        Color col = new Color(color.r*intensity*0.5f, color.g*intensity*0.5f, color.b*intensity*0.5f, color.a);
        for (int i = 0; i < m_ColorList.Count; i++)
        {
            m_ColorList[i] = col;
        }
        lightMesh.Clear();
        lightMesh.SetVertices(m_VertexList);
        lightMesh.SetColors(m_ColorList);
        lightMesh.SetTriangles(m_Indexes, 0);
    }

    protected abstract Shader LoadShader();

    protected abstract void UnLoadShader(Shader shader);

    protected virtual void OnInit(VolumetricLight light) { }

    protected virtual void OnDestroy() { }

    protected virtual void OnRefreshMesh(Color color, float range, float subdivision, Matrix4x4 matrix) { }

    protected void ResetIndexLength(int length)
    {
        if (m_Indexes == null || m_Indexes.Length != length)
            m_Indexes = new int[length];
    }

    protected void BuildMesh()
    {
        lightMesh.SetVertices(m_VertexList);
        lightMesh.SetColors(m_ColorList);
        lightMesh.SetTriangles(m_Indexes, 0);
    }

    protected void SetVertex(Vector3 position, Color color, int index)
    {
        if (m_VertexList == null) return;
        if (index >= m_VertexList.Count)
        {
            m_VertexList.Add(position);
            m_ColorList.Add(color);
        }
        else
        {
            m_VertexList[index] = position;
            m_ColorList[index] = color;
        }
    }

    protected void SetIndex(int index, int value)
    {
        if (m_Indexes == null) return;
        m_Indexes[index] = value;
    }
}
