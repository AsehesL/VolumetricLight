using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 体积光渲染脚本
/// </summary>
public class VolumetricLight : MonoBehaviour {

    public enum Quality
    {
        High,
        Middle,
        Low,
    }

    /// <summary>
    /// 是否平行光
    /// </summary>
    public bool directional
    {
        get { return this.m_Directional; }
        set { ResetDirectional(value); }
    }

    /// <summary>
    /// 阴影Bias
    /// </summary>
    public float shadowBias
    {
        get { return this.m_ShadowBias; }
        set { ResetShadowBias(value); }
    }
    /// <summary>
    /// 渲染范围
    /// </summary>
    public float range
    {
        get { return this.m_Range; }
        set { ResetRange(value); }
    }
    /// <summary>
    /// 灯光夹角（非平行光）
    /// </summary>
    public float angle
    {
        get { return this.m_Angle; }
        set { ResetAngle(value); }
    }
    /// <summary>
    /// 灯光区域大小（平行光）
    /// </summary>
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
    /// <summary>
    /// 灯光颜色
    /// </summary>
    public Color color
    {
        get { return this.m_Color; }
        set { ResetColor(value, m_Intensity); }
    }
    /// <summary>
    /// 灯光强度
    /// </summary>
    public float intensity
    {
        get { return m_Intensity; }
        set { ResetColor(m_Color, value); }
    }

    public Texture2D cookie
    {
        get { return m_Cookie; }
        set { ResetCookie(value); }
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

    public bool vertexBased
    {
        get { return m_VertexBased; }
    }

    [SerializeField]
    private bool m_Directional;
    [SerializeField]
    private float m_ShadowBias;
    [SerializeField]
    private float m_Range;
    [SerializeField]
    private float m_Angle;
    [SerializeField]
    private float m_Size;
    [SerializeField]
    private float m_Aspect;
    [SerializeField]
    private Color m_Color = new Color32(255, 247, 216, 255);
    [SerializeField]
    private float m_Intensity;
    [SerializeField]
    private Texture2D m_Cookie;
    [SerializeField]
    private LayerMask m_CullingMask;
    [SerializeField]
    private Quality m_Quality;
    [SerializeField]
    private bool m_VertexBased;
    [SerializeField]
    private float m_Subdivision = 0.7f;

    private VolumetricLightDepthCamera m_DepthCamera;
    private VolumetricLightMeshBase m_Mesh;

    private int m_InternalWorldLightVPID;
    private int m_InternalWorldLightMVID;
    private int m_InternalProjectionParams;
    private int m_InternalBiasID;
    private int m_InternalCookieID;
    private int m_InternalLightPosID;
    private int m_InternalLightColorID;

    private Matrix4x4 m_Projection;
    private Matrix4x4 m_WorldToCam;
    private Vector4 m_LightPos;

    private bool m_IsInitialized;

    void Start()
    {
        m_DepthCamera = new VolumetricLightDepthCamera();
        
        m_Subdivision = Mathf.Clamp(m_Subdivision, 0.1f, m_Range*0.9f);
        if (m_VertexBased)
            m_Mesh = new VLVertexRenderMesh();
        else
            m_Mesh = new VLPixelRenderMesh();

        if (!CheckSupport())
            return;

        m_InternalWorldLightVPID = Shader.PropertyToID("internalWorldLightVP");
        m_InternalWorldLightMVID = Shader.PropertyToID("internalWorldLightMV");
        m_InternalProjectionParams = Shader.PropertyToID("internalProjectionParams");
        m_InternalBiasID = Shader.PropertyToID("internalBias");
        m_InternalCookieID = Shader.PropertyToID("internalCookie");
        m_InternalLightPosID = Shader.PropertyToID("internalWorldLightPos");
        m_InternalLightColorID = Shader.PropertyToID("internalWorldLightColor");

        m_DepthCamera.InitCamera(this);
        m_Mesh.InitMesh(this);

        m_Projection = m_DepthCamera.depthRenderCamera.projectionMatrix;
        Shader.SetGlobalMatrix(m_InternalWorldLightVPID, m_Projection);
        Shader.SetGlobalMatrix("internalProjectionInv", m_Projection.inverse);
        m_WorldToCam = m_DepthCamera.depthRenderCamera.worldToCameraMatrix;
        Shader.SetGlobalMatrix(m_InternalWorldLightMVID, m_WorldToCam);
        SetLightProjectionParams();
        Shader.SetGlobalFloat(m_InternalBiasID, m_ShadowBias);
        Shader.SetGlobalColor(m_InternalLightColorID, new Color(m_Color.r * m_Intensity, m_Color.g * m_Intensity, m_Color.b * m_Intensity, m_Color.a));
        if (m_Cookie && !m_VertexBased)
        {
            Shader.EnableKeyword("USE_COOKIE");
            Shader.SetGlobalTexture(m_InternalCookieID, m_Cookie);
        }
        else
            Shader.DisableKeyword("USE_COOKIE");

        ResetQuality(m_Quality == Quality.Low, m_Quality == Quality.Middle, m_Quality == Quality.High);

        m_Mesh.RefreshMesh(m_Color, m_Intensity, m_Range, m_Subdivision, transform.worldToLocalMatrix * m_DepthCamera.depthRenderCamera.cameraToWorldMatrix * m_DepthCamera.depthRenderCamera.projectionMatrix.inverse);

        m_IsInitialized = true;
    }

    void OnDestroy()
    {
        if (m_DepthCamera != null)
            m_DepthCamera.Destroy();
        m_DepthCamera = null;
        if (m_Mesh != null)
            m_Mesh.Destroy();
        m_Mesh = null;
    }

    void OnPreRender()
    {
        if (!m_IsInitialized)
            return;
        if (m_Projection != m_DepthCamera.depthRenderCamera.projectionMatrix)
        {
            m_Projection = m_DepthCamera.depthRenderCamera.projectionMatrix;
            Shader.SetGlobalMatrix(m_InternalWorldLightVPID, m_Projection);
            m_Mesh.RefreshMesh(m_Color, m_Intensity, m_Range, m_Subdivision, transform.worldToLocalMatrix * m_DepthCamera.depthRenderCamera.cameraToWorldMatrix * m_DepthCamera.depthRenderCamera.projectionMatrix.inverse);
        }
        if (m_WorldToCam != m_DepthCamera.depthRenderCamera.worldToCameraMatrix)
        {
            m_WorldToCam = m_DepthCamera.depthRenderCamera.worldToCameraMatrix;
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
        if (m_DepthCamera == null)
            return false;
        if (!m_DepthCamera.CheckSupport())
            return false;
        if (m_Mesh == null)
            return false;
        if (!m_Mesh.CheckSupport())
            return false;
        return true;
    }

    private void ResetDirectional(bool directional)
    {
        if (m_Directional == directional) return;
        m_Directional = directional;
        if (!m_IsInitialized) return;
        if (m_DepthCamera != null) m_DepthCamera.depthRenderCamera.orthographic = m_Directional;
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
        if (m_DepthCamera != null) m_DepthCamera.depthRenderCamera.farClipPlane = m_Range;
        SetLightProjectionParams();
    }

    private void ResetAngle(float angle)
    {
        if (m_Angle == angle) return;
        m_Angle = angle;
        if (!m_IsInitialized) return;
        if (m_DepthCamera != null) m_DepthCamera.depthRenderCamera.fieldOfView = m_Angle;
    }

    private void ResetSize(float size)
    {
        if (m_Size == size) return;
        m_Size = size;
        if (!m_IsInitialized) return;
        if (m_DepthCamera != null) m_DepthCamera.depthRenderCamera.orthographicSize = m_Size;
    }

    private void ResetAspect(float aspect)
    {
        if (m_Aspect == aspect) return;
        m_Aspect = aspect;
        if (!m_IsInitialized) return;
        if (m_DepthCamera != null) m_DepthCamera.depthRenderCamera.aspect = m_Aspect;
    }

    private void ResetColor(Color color, float intensity)
    {
        if (m_Color == color && m_Intensity == intensity) return;
        m_Color = color;
        m_Intensity = intensity;
        if (!m_IsInitialized) return;
        Shader.SetGlobalColor(m_InternalLightColorID, new Color(m_Color.r * m_Intensity, m_Color.g * m_Intensity, m_Color.b * m_Intensity, m_Color.a));
        if (m_Mesh == null) return;
        m_Mesh.RefreshColor(m_Color, m_Intensity);
    }

    private void ResetCookie(Texture2D cookie)
    {
        if (m_Cookie == cookie) return;
        if (m_VertexBased) return;
        m_Cookie = cookie;
        if (!m_IsInitialized) return;
        if (m_Cookie && !m_VertexBased)
        {
            Shader.EnableKeyword("USE_COOKIE");
            Shader.SetGlobalTexture(m_InternalCookieID, m_Cookie);
        }
        else
            Shader.DisableKeyword("USE_COOKIE");
    }

    private void ResetCullingMask(LayerMask cullingMask)
    {
        if (m_CullingMask == cullingMask) return;
        m_CullingMask = cullingMask;
        if (!m_IsInitialized) return;
        if(m_DepthCamera!=null) m_DepthCamera.depthRenderCamera.cullingMask = m_CullingMask;
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

    private void SetLightProjectionParams()
    {
        float x = -1 + m_Range / 0.01f;
        Shader.SetGlobalVector(m_InternalProjectionParams, new Vector4(x, (m_Range - 0.01f) / (2 * m_Range * 0.01f), (m_Range + 0.01f) / (2 * m_Range * 0.01f), 1 / m_Range));
    }

    private bool LightPosChange()
    {
        if (m_LightPos.w == 1 && m_Directional)
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
