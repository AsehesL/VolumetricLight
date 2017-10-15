using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 体积光
/// </summary>
public class VolumeLight2 : MonoBehaviour
{

    public bool directional
    {
        set
        {
            if (m_Directional != value)
            {
                RefreshCamera();
                RefreshLightDir();
            }
            m_Directional = value;
        }
        get { return m_Directional; }
    }
    public float bias
    {
        set
        {
            if (m_Bias != value)
            {
                RefreshBias();
            }
            m_Bias = value;
        }
        get { return m_Bias; }
    }

    public float near
    {
        set
        {
            if (m_Near != value)
            {
                RefreshCamera();
                RefreshAtten();
            }
            m_Near = value;
        }
        get { return m_Near; }
    }
    public float far
    {
        set
        {
            if (m_Far != value)
            {
                RefreshCamera();
                RefreshAtten();
            }
            m_Far = value;
        }get { return m_Far; }
    }
    public float fieldOfView
    {
        set
        {
            if (m_FieldOfView != value)
            {
                RefreshCamera();
            }
            m_FieldOfView = value;
        }
        get { return m_FieldOfView; }
    }
    public float size
    {
        set
        {
            if (m_Size != value)
            {
                RefreshCamera();
            }
            m_Size = value;
        }
        get { return m_Size; }
    }

    public float aspect
    {
        set
        {
            if (m_Aspect != value)
            {
                RefreshCamera();
            }
            m_Aspect = value;
        }
        get { return m_Aspect; }
    }
    public Color color
    {
        set
        {
            if (m_Color != value)
            {
                RefreshColor();
            }
            m_Color = value;
        }
        get { return m_Color; }
    }
    public float intensity
    {
        set
        {
            if (m_Intensity != value)
            {
                RefreshColor();
            }
            m_Intensity = value;
        }
        get { return m_Intensity; }
    }
    public float atten
    {
        set
        {
            if (m_Atten != value)
            {
                RefreshAtten();
            }
            m_Atten = value;
        }
        get { return m_Atten; }
    }

    public Texture2D cookie
    {
        set
        {
            if (m_Cookie != value)
                RefreshCookie();
            m_Cookie = value;
        }
        get { return m_Cookie; }
    }

    [SerializeField]
    private bool m_Directional;
    [SerializeField]
    private float m_Bias;
    [SerializeField]
    private float m_Near;
    [SerializeField]
    private float m_Far;
    [SerializeField]
    private float m_FieldOfView;
    [SerializeField]
    private float m_Size;
    [SerializeField]
    private float m_Aspect;
    [SerializeField]
    private Color m_Color;
    [SerializeField]
    private float m_Intensity;
    [SerializeField]
    private float m_Atten;
    [SerializeField]
    private LayerMask m_CullingMask;
    [SerializeField]
    private Texture2D m_Cookie;

    private Camera m_DepthRenderCamera;
    private RenderTexture m_DepthTexture;

    private Material m_Material;
    private Mesh m_Mesh;
    private Texture2D m_DefaultLight;

    private Matrix4x4 m_WorldToLocal;

    private List<Vector3> m_VertexList = new List<Vector3>();
    private int[] m_IndexesList = {
            0,4,5,0,5,1,
            1,5,6,1,6,2,
            2,6,7,2,7,3,
            0,3,4,3,7,4,
            4,5,6,4,6,7
        };

    void Start ()
    {
        InitDepthCamera();
        InitMesh();
        m_WorldToLocal = transform.worldToLocalMatrix;
        RefreshCookie();
        RefreshBias();
        RefreshLightWorldToLocalMatrix();
        RefreshColor();
        RefreshCamera();
        RefreshCullingMask();
        RefreshAtten();
    }

    void OnPreRender()
    {
        if (m_WorldToLocal != transform.worldToLocalMatrix)
        {
            RefreshLightWorldToLocalMatrix();
        }
    }

    void OnDestroy()
    {
        if (m_DepthTexture)
            Destroy(m_DepthTexture);
        m_DepthTexture = null;
        if (m_DefaultLight)
            Destroy(m_DefaultLight);
        m_DefaultLight = null;
        if (m_Material)
            Destroy(m_Material);
        m_Material = null;
        if (m_Mesh)
            Destroy(m_Mesh);
        m_Mesh = null;
    }

    private void InitDepthCamera()
    {
        if (m_DefaultLight == null)
        {
            m_DefaultLight = new Texture2D(1, 1);
            m_DefaultLight.SetPixel(0, 0, Color.white);
            m_DefaultLight.Apply();
        }
        if (m_DepthRenderCamera == null)
        {
            m_DepthRenderCamera = GetComponent<Camera>();
            if (m_DepthRenderCamera == null)
                m_DepthRenderCamera = gameObject.AddComponent<Camera>();
            m_DepthRenderCamera.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            m_DepthRenderCamera.aspect = m_Aspect;
            m_DepthRenderCamera.backgroundColor = new Color(0, 0, 0, 0);
            m_DepthRenderCamera.clearFlags = CameraClearFlags.SolidColor;
            m_DepthRenderCamera.depth = 0;
            m_DepthRenderCamera.farClipPlane = m_Far;
            m_DepthRenderCamera.nearClipPlane = m_Near;
            m_DepthRenderCamera.fieldOfView = m_FieldOfView;
            m_DepthRenderCamera.orthographic = m_Directional;
            m_DepthRenderCamera.orthographicSize = m_Size;
            m_DepthRenderCamera.cullingMask = m_CullingMask;
            m_DepthRenderCamera.SetReplacementShader(Resources.Load<Shader>("Shaders/ReplaceDepth"), "RenderType");
        }
        if (m_DepthTexture == null)
        {
            m_DepthTexture = new RenderTexture(1024, 1024, 16);
            m_DepthRenderCamera.targetTexture = m_DepthTexture;
            Shader.SetGlobalTexture("internalShadowMap", m_DepthTexture);
        }
    }

    private void InitMesh()
    {
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        m_Material = new Material(Resources.Load<Shader>("Shaders/VolumeLight/VolumeLight"));
        meshRenderer.sharedMaterial = m_Material;
        m_Material.SetTexture("_DepthTex", m_DepthTexture);
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        m_Mesh = new Mesh();
        meshFilter.sharedMesh = m_Mesh;
        
        
    }

    private void RefreshCookie()
    {
        if (m_Cookie)
        {
            Shader.SetGlobalTexture("internalCookie", m_Cookie);
            m_Material.SetTexture("_Cookie", m_Cookie);
        }
        else
        {
            Shader.SetGlobalTexture("internalCookie", m_DefaultLight);
            m_Material.SetTexture("_Cookie", null);
        }
    }

    private void RefreshBias()
    {
        Shader.SetGlobalFloat("internalBias", m_Bias);
    }

    private void RefreshColor()
    {
        Shader.SetGlobalColor("internalWorldLightColor",
               new Color(m_Color.r * m_Intensity, m_Color.g * m_Intensity, m_Color.b * m_Intensity, m_Color.a));
        m_Material.SetColor("_Color", new Color(m_Color.r * m_Intensity, m_Color.g * m_Intensity, m_Color.b * m_Intensity, m_Color.a));
    }

    private void RefreshCamera()
    {
        m_DepthRenderCamera.aspect = m_Aspect;
        m_DepthRenderCamera.farClipPlane = m_Far;
        m_DepthRenderCamera.nearClipPlane = m_Near;
        m_DepthRenderCamera.fieldOfView = m_FieldOfView;
        m_DepthRenderCamera.orthographic = m_Directional;
        m_DepthRenderCamera.orthographicSize = m_Size;

        m_Material.SetMatrix("internalProjection", m_DepthRenderCamera.projectionMatrix);
        m_Material.SetMatrix("internalProjectionInv", m_DepthRenderCamera.projectionMatrix.inverse);

        RefreshMesh();
    }

    private void RefreshCullingMask()
    {
        m_DepthRenderCamera.cullingMask = m_CullingMask;
    }

    private void RefreshAtten()
    {
        m_Material.SetVector("_LightParams", new Vector4(m_DepthRenderCamera.farClipPlane, m_Atten, 0, 0));
        Shader.SetGlobalVector("internalProjectionParams",
               new Vector4(m_Atten, m_DepthRenderCamera.nearClipPlane, m_DepthRenderCamera.farClipPlane, 1 / m_DepthRenderCamera.farClipPlane));
    }

    private void RefreshLightDir()
    {
        if (!m_Directional)
            Shader.SetGlobalVector("internalWorldLightPos",
                new Vector4(transform.position.x, transform.position.y, transform.position.z, 1));
        else
            Shader.SetGlobalVector("internalWorldLightPos",
                new Vector4(transform.forward.x, transform.forward.y, transform.forward.z, 0));
    }

    private void RefreshLightWorldToLocalMatrix()
    {

        RefreshLightDir();
        Shader.SetGlobalMatrix("internalWorldLightMV", m_WorldToLocal);
    }

    private void RefreshMesh()
    {
        m_Mesh.Clear();
        m_VertexList.Clear();

        Matrix4x4 mt = transform.worldToLocalMatrix * m_DepthRenderCamera.cameraToWorldMatrix * m_DepthRenderCamera.projectionMatrix.inverse;
        //Matrix4x4 mt = m_DepthRenderCamera.projectionMatrix.inverse;
        Vector3 p1 = new Vector3(-1, -1, -1);
        Vector3 p2 = new Vector3(-1, 1, -1);
        Vector3 p3 = new Vector3(1, 1, -1);
        Vector3 p4 = new Vector3(1, -1, -1);

        Vector3 p5 = new Vector3(-1, -1, 1);
        Vector3 p6 = new Vector3(-1, 1, 1);
        Vector3 p7 = new Vector3(1, 1, 1);
        Vector3 p8 = new Vector3(1, -1, 1);
        //p1.z = -p1.z; p2.z = -p2.z; p3.z = -p3.z; p4.z = -p4.z;
        //p5.z = -p5.z; p6.z = -p6.z; p7.z = -p7.z; p8.z = -p8.z;
        m_VertexList.Add(mt.MultiplyPoint(p1));
        m_VertexList.Add(mt.MultiplyPoint(p2));
        m_VertexList.Add(mt.MultiplyPoint(p3));
        m_VertexList.Add(mt.MultiplyPoint(p4));
        m_VertexList.Add(mt.MultiplyPoint(p5));
        m_VertexList.Add(mt.MultiplyPoint(p6));
        m_VertexList.Add(mt.MultiplyPoint(p7));
        m_VertexList.Add(mt.MultiplyPoint(p8));

        m_Mesh.SetVertices(m_VertexList);
        m_Mesh.SetTriangles(m_IndexesList, 0);

        m_Mesh.RecalculateBounds();
    }

    void OnDrawGizmosSelected()
    {
        if (m_Directional)
        {
            GizmosEx.DrawOrtho(transform, m_Aspect, m_Size, m_Near, m_Far,
                new Color(0.5f, 0.5f, 0.5f, 0.7f));
        }
        else
        {
            GizmosEx.DrawPerspective(transform, m_Aspect, m_FieldOfView, m_Near, m_Far,
                new Color(0.5f, 0.5f, 0.5f, 0.7f));
        }
    }
}
