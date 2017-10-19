using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RealVolumeLight : MonoBehaviour
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
        set { }
    }

    public float size
    {
        get { return this.m_Size; }
        set { }
    }

    public float aspect
    {
        get { return this.m_Aspect; }
        set { }
    }

    public Color color
    {
        get { return this.m_Color; }
        set { }
    }

    public Quality quality
    {
        get { return this.m_Quality; }
    }

    private const string shadowMapShader = "Shaders/RealVolumeLight/ShadowMapRenderer";
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
    [SerializeField] private Texture2D m_Cookie;
    [SerializeField] private LayerMask m_CullingMask;
    [SerializeField] private Quality m_Quality;

    private Camera m_DepthRenderCamera;
    private Mesh m_LightMesh;
    private RenderTexture m_ShadowMap;

    private bool m_IsInitialized;

    private Shader m_ShadowRenderShader;

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
        m_WorldToCam = m_DepthRenderCamera.worldToCameraMatrix;
        Shader.SetGlobalMatrix(m_InternalWorldLightMVID, m_WorldToCam);
        SetLightProjectionParams();
        Shader.SetGlobalFloat(m_InternalBiasID, m_ShadowBias);
        Shader.SetGlobalColor(m_InternalLightColorID, new Color(m_Color.r* m_Intensity,m_Color.g* m_Intensity,m_Color.b* m_Intensity,m_Color.a));
        if (m_Cookie)
        {
            Shader.EnableKeyword("USE_COOKIE");
            Shader.SetGlobalTexture(m_InternalCookieID, m_Cookie);
        }
        else
            Shader.DisableKeyword("USE_COOKIE");

        m_IsInitialized = true;
    }

    void OnDestroy()
    {
        SafeDestroy(ref m_LightMesh);
        SafeDestroy(ref m_ShadowMap);
    }

    void OnPreRender()
    {
        if (!m_IsInitialized)
            return;
        if (m_Projection != m_DepthRenderCamera.projectionMatrix)
        {
            m_Projection = m_DepthRenderCamera.projectionMatrix;
            Shader.SetGlobalMatrix(m_InternalWorldLightVPID, m_Projection);
        }
        if (m_WorldToCam != m_DepthRenderCamera.worldToCameraMatrix)
        {
            m_WorldToCam = m_DepthRenderCamera.worldToCameraMatrix;
            Shader.SetGlobalMatrix(m_InternalWorldLightMVID, m_WorldToCam);
        }
        
        if (LightPosChange())
        {
            m_LightPos = new Vector4(transform.position.x, transform.position.y, transform.position.z,
                m_Directional ? 0 : 1);
            Shader.SetGlobalVector(m_InternalLightPosID, m_LightPos);
        }
    }

    private bool CheckSupport()
    {
        m_ShadowRenderShader = Resources.Load<Shader>(shadowMapShader);
        if (m_ShadowRenderShader == null || !m_ShadowRenderShader.isSupported)
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

    private void SetLightProjectionParams()
    {
        float x = -1 + m_Range/0.01f;
        float y = 1;
        Shader.SetGlobalVector(m_InternalProjectionParams, new Vector4(x, y, x/m_Range, 1/m_Range));
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
        if (m_LightPos.x != transform.position.x)
            return true;
        if (m_LightPos.y != transform.position.y)
            return true;
        if (m_LightPos.z != transform.position.z)
            return true;
        if (m_LightPos.w == 1 && m_Directional)
            return true;
        if (m_LightPos.w == 0 && !m_Directional)
            return true;
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