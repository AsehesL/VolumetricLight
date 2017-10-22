using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexVolumeLight : MonoBehaviour
{
    public enum Quality
    {
        High,
        Middle,
        Low,
    }

    public bool directional
    {
        get { return this.m_Directional; }
        set { ResetDirectional(value); }
    }

    public float shadowBias
    {
        get { return this.m_ShadowBias; }
        set { ResetShadowBias(value); }
    }

    public float range
    {
        get { return this.m_Range; }
        set { ResetRange(value); }
    }

    public float angle
    {
        get { return this.m_Angle; }
        set { ResetAngle(value); }
    }

    public float size
    {
        get { return this.m_Size; }
        set { ResetSize(value); }
    }

    public float aspect
    {
        get { return this.m_Aspect; }
        set { ResetAspect(value); }
    }

    public Color color
    {
        get { return this.m_Color; }
        set { ResetColor(value, m_Intensity); }
    }

    public float intensity
    {
        get { return m_Intensity; }
        set { ResetColor(m_Color, value); }
    }

    public LayerMask cullingMask
    {
        get { return m_CullingMask; }
        set { ResetCullingMask(value); }
    }

    public Quality quality
    {
        get { return this.m_Quality; }
    }

    //private const string shadowMapShader = "Shaders/RealVolumeLight/ShadowMapRenderer";
    private const string shadowMapShaderPath = "Shaders/ReplaceDepth";
    private const string volumetricShaderPath = "Shaders/RealVolumeLight/VertexVolumetricLight";
    private int m_InternalWorldLightVPID;
    private int m_InternalWorldLightMVID;
    private int m_InternalProjectionParams;
    private int m_InternalShadowMapID;
    private int m_InternalBiasID;
    private int m_InternalCookieID;
    private int m_InternalLightPosID;
    private int m_InternalLightColorID;

    [SerializeField] private bool m_Directional;
    [SerializeField] private float m_ShadowBias;
    [SerializeField] private float m_Range;
    [SerializeField] private float m_Angle;
    [SerializeField] private float m_Size;
    [SerializeField] private float m_Aspect;
    [SerializeField] private Color m_Color = new Color32(255, 247, 216, 255);
    [SerializeField] private float m_Intensity;
    [SerializeField] private LayerMask m_CullingMask;
    [SerializeField] private Quality m_Quality;
    [SerializeField] private float m_Subdivision = 0.7f;

    private Camera m_DepthRenderCamera;
    private Mesh m_LightMesh;
    private Material m_LightMaterial;
    private RenderTexture m_ShadowMap;

    private bool m_IsInitialized;

    private Shader m_ShadowRenderShader;
    private Shader m_VolumetricLightShader;

    private List<Vector3> m_VertexList;
    private List<Color> m_ColorList;
    private int[] m_Indexes;

    private Matrix4x4 m_Projection;
    private Matrix4x4 m_WorldToCam;
    private Vector4 m_LightPos;

    void Start()
    {
        if (!CheckSupport())
            return;

        m_InternalWorldLightVPID = Shader.PropertyToID("internalWorldLightVP");
        m_InternalWorldLightMVID = Shader.PropertyToID("internalWorldLightMV");
        m_InternalProjectionParams = Shader.PropertyToID("internalProjectionParams");
        m_InternalShadowMapID = Shader.PropertyToID("internalShadowMap");
        m_InternalBiasID = Shader.PropertyToID("internalBias");
        m_InternalCookieID = Shader.PropertyToID("internalCookie");
        m_InternalLightPosID = Shader.PropertyToID("internalWorldLightPos");
        m_InternalLightColorID = Shader.PropertyToID("internalWorldLightColor");

        InitCamera();
        InitShadowMap();
        InitLightMesh();

        m_Projection = m_DepthRenderCamera.projectionMatrix;
        Shader.SetGlobalMatrix(m_InternalWorldLightVPID, m_Projection);
        Shader.SetGlobalMatrix("internalProjectionInv", m_Projection.inverse);
        m_WorldToCam = m_DepthRenderCamera.worldToCameraMatrix;
        Shader.SetGlobalMatrix(m_InternalWorldLightMVID, m_WorldToCam);
        SetLightProjectionParams();
        Shader.SetGlobalFloat(m_InternalBiasID, m_ShadowBias);
        Shader.SetGlobalColor(m_InternalLightColorID, new Color(m_Color.r* m_Intensity,m_Color.g* m_Intensity,m_Color.b* m_Intensity,m_Color.a));

        ResetQuality(m_Quality == Quality.Low, m_Quality == Quality.Middle, m_Quality == Quality.High);

         RefreshMesh();

        m_IsInitialized = true;

    }

    void OnDestroy()
    {
        SafeDestroy(ref m_LightMesh);
        SafeDestroy(ref m_ShadowMap);
        SafeDestroy(ref m_LightMaterial);
        if (m_ShadowRenderShader)
            Resources.UnloadAsset(m_ShadowRenderShader);
        m_ShadowRenderShader = null;
        if (m_VolumetricLightShader)
            Resources.UnloadAsset(m_VolumetricLightShader);
        m_VolumetricLightShader = null;

        if (m_VertexList != null)
            m_VertexList.Clear();
        m_VertexList = null;
        if (m_ColorList != null)
            m_ColorList.Clear();
        m_ColorList = null;
    }

    void OnPreRender()
    {
        if (!m_IsInitialized)
            return;
        if (m_Projection != m_DepthRenderCamera.projectionMatrix)
        {
            m_Projection = m_DepthRenderCamera.projectionMatrix;
            Shader.SetGlobalMatrix(m_InternalWorldLightVPID, m_Projection);
            Shader.SetGlobalMatrix("internalProjectionInv", m_Projection.inverse);
            RefreshMesh();
        }
        if (m_WorldToCam != m_DepthRenderCamera.worldToCameraMatrix)
        {
            m_WorldToCam = m_DepthRenderCamera.worldToCameraMatrix;
            Shader.SetGlobalMatrix(m_InternalWorldLightMVID, m_WorldToCam);
        }
        
        if (LightPosChange())
        {
            if (!m_Directional)
            {
                Shader.SetGlobalVector(m_InternalLightPosID, new Vector4(transform.position.x, transform.position.y, transform.position.z, 1));
            }
            else
            {
                Shader.SetGlobalVector(m_InternalLightPosID, new Vector4(transform.forward.x, transform.forward.y, transform.forward.z, 0));
            }
        }
    }

    private bool CheckSupport()
    {
        m_ShadowRenderShader = Resources.Load<Shader>(shadowMapShaderPath);
        if (m_ShadowRenderShader == null || !m_ShadowRenderShader.isSupported)
            return false;
        m_VolumetricLightShader = Resources.Load<Shader>(volumetricShaderPath);
        if (m_VolumetricLightShader == null || !m_VolumetricLightShader.isSupported)
            return false;
        return true;
    }

    private void InitCamera()
    {
        if (m_DepthRenderCamera == null)
        {
            m_DepthRenderCamera = GetComponent<Camera>();
            if (m_DepthRenderCamera == null)
                m_DepthRenderCamera = gameObject.AddComponent<Camera>();
            //m_DepthRenderCamera.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            m_DepthRenderCamera.aspect = m_Aspect;
            m_DepthRenderCamera.backgroundColor = new Color(0, 0, 0, 0);
            m_DepthRenderCamera.clearFlags = CameraClearFlags.SolidColor;
            m_DepthRenderCamera.depth = 0;
            m_DepthRenderCamera.farClipPlane = m_Range;
            m_DepthRenderCamera.nearClipPlane = 0.01f;
            m_DepthRenderCamera.fieldOfView = m_Angle;
            m_DepthRenderCamera.orthographic = m_Directional;
            m_DepthRenderCamera.orthographicSize = m_Size;
            m_DepthRenderCamera.cullingMask = m_CullingMask;
            m_DepthRenderCamera.SetReplacementShader(m_ShadowRenderShader, "RenderType");
        }

    }

    private void InitShadowMap()
    {
        if (m_ShadowMap == null)
        {
            int size = 0;
            switch (m_Quality)
            {
                case Quality.High:
                case Quality.Middle:
                    size = 1024;
                    break;
                case Quality.Low:
                    size = 512;
                    break;
            }
            m_ShadowMap = new RenderTexture(size, size, 16);
            m_DepthRenderCamera.targetTexture = m_ShadowMap;
            Shader.SetGlobalTexture(m_InternalShadowMapID, m_ShadowMap);
        }
    }

    private void InitLightMesh()
    {
        MeshRenderer meshRender = gameObject.GetComponent<MeshRenderer>();
        if (meshRender == null)
            meshRender = gameObject.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        m_LightMaterial = new Material(m_VolumetricLightShader);
        meshRender.sharedMaterial = m_LightMaterial;
        m_LightMesh = new Mesh();
        m_LightMesh.MarkDynamic();
        meshFilter.sharedMesh = m_LightMesh;
    }

    private void ResetDirectional(bool directional)
    {
        if (m_Directional == directional) return;
        m_Directional = directional;
        if (!m_IsInitialized) return;
        m_DepthRenderCamera.orthographic = m_Directional;
    }

    private void ResetShadowBias(float shadowBias)
    {
        if (m_ShadowBias == shadowBias) return;
        m_ShadowBias = shadowBias;
        if (!m_IsInitialized) return;
        Shader.SetGlobalFloat(m_InternalBiasID, m_ShadowBias);
    }

    private void ResetRange(float range)
    {
        if (m_Range == range) return;
        m_Range = range;
        if (!m_IsInitialized) return;
        m_DepthRenderCamera.farClipPlane = m_Range;
        SetLightProjectionParams();
    }

    private void ResetAngle(float angle)
    {
        if (m_Angle == angle) return;
        m_Angle = angle;
        if (!m_IsInitialized) return;
        m_DepthRenderCamera.fieldOfView = m_Angle;
    }

    private void ResetSize(float size)
    {
        if (m_Size == size) return;
        m_Size = size;
        if (!m_IsInitialized) return;
        m_DepthRenderCamera.orthographicSize = m_Size;
    }

    private void ResetAspect(float aspect)
    {
        if (m_Aspect == aspect) return;
        m_Aspect = aspect;
        if (!m_IsInitialized) return;
        m_DepthRenderCamera.aspect = m_Aspect;
    }

    private void ResetColor(Color color, float intensity)
    {
        if (m_Color == color && m_Intensity == intensity) return;
        m_Color = color;
        m_Intensity = intensity;
        if (!m_IsInitialized) return;
        Shader.SetGlobalColor(m_InternalLightColorID, new Color(m_Color.r * m_Intensity, m_Color.g * m_Intensity, m_Color.b * m_Intensity, m_Color.a));
        for (int i = 0; i < m_ColorList.Count; i++)
        {
            m_ColorList[i] = m_Color;
        }
        m_LightMesh.Clear();
        m_LightMesh.SetVertices(m_VertexList);
        m_LightMesh.SetColors(m_ColorList);
        m_LightMesh.SetTriangles(m_Indexes, 0);
    }

    private void ResetCullingMask(LayerMask cullingMask)
    {
        if (m_CullingMask == cullingMask) return;
        m_CullingMask = cullingMask;
        if (!m_IsInitialized) return;
        m_DepthRenderCamera.cullingMask = m_CullingMask;
    }

    private void ResetQuality(bool low, bool middle, bool high)
    {
        if (low)
            Shader.EnableKeyword("VOLUMETRIC_LIGHT_QUALITY_LOW");
        else
            Shader.DisableKeyword("VOLUMETRIC_LIGHT_QUALITY_LOW");
        if (middle)
            Shader.EnableKeyword("VOLUMETRIC_LIGHT_QUALITY_MIDDLE");
        else
            Shader.DisableKeyword("VOLUMETRIC_LIGHT_QUALITY_MIDDLE");
        if (high)
            Shader.EnableKeyword("VOLUMETRIC_LIGHT_QUALITY_HIGH");
        else
            Shader.DisableKeyword("VOLUMETRIC_LIGHT_QUALITY_HIGH");
    }

    private void RefreshMesh()
    {
        Matrix4x4 mt = transform.worldToLocalMatrix * m_DepthRenderCamera.cameraToWorldMatrix * m_DepthRenderCamera.projectionMatrix.inverse;
        m_LightMesh.Clear();

        int zstep = (int)(m_Range / m_Subdivision);
        int hstep = 20;

        if (m_VertexList == null)
            m_VertexList = new List<Vector3>();
        if (m_ColorList == null)
            m_ColorList = new List<Color>();
        if (m_Indexes == null)
            m_Indexes = new int[4*(zstep*hstep*6)];

        //int currentIndex = 0;
        int index = 0;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                float bx = i == 0 ? -1 : 1;
                float by = i == j ? -1 : 1;
                float tx = i == j ? -1 : 1;
                float ty = i == 1 ? -1 : 1;

                Vector3 beg1 = mt.MultiplyPoint(new Vector3(bx, by, -1));
                Vector3 to1 = mt.MultiplyPoint(new Vector3(bx, by, 1));
                Vector3 beg2 = mt.MultiplyPoint(new Vector3(tx, ty, -1));
                Vector3 to2 = mt.MultiplyPoint(new Vector3(tx, ty, 1));

                int faceIndex = i * 2 + j;

                for (int k = 0; k <= zstep; k++)
                {
                    float zl = ((float)k) / zstep;
                    Vector3 v1 = Vector3.Lerp(beg1, to1, zl);
                    Vector3 v2 = Vector3.Lerp(beg2, to2, zl);
                    for (int p = 0; p <= hstep; p++)
                    {
                        float hl = ((float) p)/hstep;
                        float x = Mathf.Lerp(v1.x, v2.x, hl);
                        float y = Mathf.Lerp(v1.y, v2.y, hl);
                        int currentIndex = faceIndex*(zstep+1)*(hstep+1) + k*(hstep + 1) + p;
                        AddVertex(new Vector3(x, y, v1.z), currentIndex);
                        if (k < zstep && p < hstep)
                        {
                            m_Indexes[index] = faceIndex * (zstep + 1) * (hstep + 1) + k*(hstep + 1) + p;
                            m_Indexes[index + 1] = faceIndex * (zstep + 1) * (hstep + 1) + (k+1)*(hstep + 1) + p;
                            m_Indexes[index + 2] = faceIndex * (zstep + 1) * (hstep + 1) + (k+1)*(hstep + 1) + p + 1;
                            m_Indexes[index + 3] = faceIndex * (zstep + 1) * (hstep + 1) + k*(hstep + 1) + p;
                            m_Indexes[index + 4] = faceIndex * (zstep + 1) * (hstep + 1) + (k+1)*(hstep + 1) + p + 1;
                            m_Indexes[index + 5] = faceIndex * (zstep + 1) * (hstep + 1) + k*(hstep + 1) + p + 1;
                            index += 6;
                        }
                       
                        //currentIndex ++;
                    }
                }

                //AddVertex(mt.MultiplyPoint(new Vector3(x, y, -1)), i * 4 + j * 2);
                //AddVertex(mt.MultiplyPoint(new Vector3(x, y, 1)), i * 4 + j * 2 + 1);
//                if (i > 0 && j > 0)
//                {
//
//                    m_Indexes[i * 12 + j * 6] = i * 4 + j * 2;
//                    m_Indexes[i * 12 + j * 6 + 1] = i * 4 + j * 2 + 1;
//                    m_Indexes[i * 12 + j * 6 + 2] = 1;
//                    m_Indexes[i * 12 + j * 6 + 3] = i * 4 + j * 2;
//                    m_Indexes[i * 12 + j * 6 + 4] = 1;
//                    m_Indexes[i * 12 + j * 6 + 5] = 0;
//                }
//                else
//                {
//                    m_Indexes[i * 12 + j * 6] = i * 4 + j * 2;
//                    m_Indexes[i * 12 + j * 6 + 1] = i * 4 + j * 2+1;
//                    m_Indexes[i * 12 + j * 6 + 2] = i * 4 + j * 2+3;
//                    m_Indexes[i * 12 + j * 6 + 3] = i * 4 + j * 2;
//                    m_Indexes[i * 12 + j * 6 + 4] = i * 4 + j * 2+3;
//                    m_Indexes[i * 12 + j * 6 + 5] = i * 4 + j * 2+2;
//                }
            }
        }

        m_LightMesh.SetVertices(m_VertexList);
        m_LightMesh.SetColors(m_ColorList);
        m_LightMesh.SetTriangles(m_Indexes, 0);
    }

    private void AddVertex(Vector3 position, int index)
    {
        if (index >= m_VertexList.Count)
        {
            m_VertexList.Add(position);
            m_ColorList.Add(m_Color);
        }
        else
        {
            m_VertexList[index] = position;
            m_ColorList[index] = m_Color;
        }
    }

    private void SetLightProjectionParams()
    {
        //float x = 1 - m_Range/0.01f;
        //float y = m_Range/0.01f;
        //Shader.SetGlobalVector(m_InternalProjectionParams, new Vector4(x, y, x / m_Range, y / m_Range));
        float x = -1 + m_Range/0.01f;
        //float y = 1;
        //Shader.SetGlobalVector(m_InternalProjectionParams, new Vector4(x, y, x / m_Range, y / m_Range));
        Shader.SetGlobalVector(m_InternalProjectionParams, new Vector4(x, (m_Range-0.01f)/(2*m_Range*0.01f), (m_Range+0.01f)/(2*m_Range*0.01f), 1/m_Range));
    }

    private bool SafeDestroy<T>(ref T obj) where T : UnityEngine.Object
    {
        if (!obj)
            return false;
        Destroy(obj);
        obj = null;
        return true;
    }

    private bool LightPosChange()
    {
        if(m_LightPos.w == 1 && m_Directional)
            return true;
        if (m_LightPos.w == 0 && !m_Directional)
            return true;
        if (m_Directional)
        {
            if (m_LightPos.x != transform.forward.x)
                return true;
            if (m_LightPos.y != transform.forward.y)
                return true;
            if (m_LightPos.z != transform.forward.z)
                return true;
        }
        else
        {
            if (m_LightPos.x != transform.position.x)
                return true;
            if (m_LightPos.y != transform.position.y)
                return true;
            if (m_LightPos.z != transform.position.z)
                return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        if (m_Directional)
        {
            GizmosEx.DrawOrtho(transform, m_Aspect, m_Size, 0.01f, m_Range,
                new Color(0.5f, 0.5f, 0.5f, 0.7f));
        }
        else
        {
            GizmosEx.DrawPerspective(transform, m_Aspect, m_Angle, 0.01f, m_Range,
                new Color(0.5f, 0.5f, 0.5f, 0.7f));
        }
    }
}