using UnityEngine;
using System.Collections;

/// <summary>
/// 深度渲染相机
/// </summary>
public class VolumetricLightDepthCamera
{
    public Camera depthRenderCamera { get { return m_DepthRenderCamera; } }

    private Camera m_DepthRenderCamera;

    private RenderTexture m_ShadowMap;

    private Shader m_ShadowRenderShader;

    private const string shadowMapShaderPath = "Shaders/VolumetricLight/ShadowMapRenderer";

    private int m_InternalShadowMapID;

    private bool m_IsSupport = false;

    public bool CheckSupport()
    {
        m_ShadowRenderShader = Resources.Load<Shader>(shadowMapShaderPath);
        if (m_ShadowRenderShader == null || !m_ShadowRenderShader.isSupported)
            return false;
        m_IsSupport = true;
        return m_IsSupport;
    }

    public void InitCamera(VolumetricLight light)
    {
        if (!m_IsSupport)
            return;
        m_InternalShadowMapID = Shader.PropertyToID("internalShadowMap");

        if (m_DepthRenderCamera == null)
        {
            m_DepthRenderCamera = light.gameObject.GetComponent<Camera>();
            if (m_DepthRenderCamera == null)
                m_DepthRenderCamera = light.gameObject.AddComponent<Camera>();
            m_DepthRenderCamera.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            m_DepthRenderCamera.aspect = light.aspect;
            m_DepthRenderCamera.backgroundColor = new Color(0,0,0,0);
            m_DepthRenderCamera.clearFlags = CameraClearFlags.SolidColor;
            m_DepthRenderCamera.depth = 0;
            m_DepthRenderCamera.farClipPlane = light.range;
            m_DepthRenderCamera.nearClipPlane = 0.01f;
            m_DepthRenderCamera.fieldOfView = light.angle;
            m_DepthRenderCamera.orthographic = light.directional;
            m_DepthRenderCamera.orthographicSize = light.size;
            m_DepthRenderCamera.cullingMask = light.cullingMask;
            m_DepthRenderCamera.SetReplacementShader(m_ShadowRenderShader, "RenderType");
        }

        if (m_ShadowMap == null)
        {
            int size = 0;
            switch (light.quality)
            {
                case VolumetricLight.Quality.High:
                case VolumetricLight.Quality.Middle:
                    size = 1024;
                    break;
                case VolumetricLight.Quality.Low:
                    size = 512;
                    break;
            }
            m_ShadowMap = new RenderTexture(size, size, 16);
            m_DepthRenderCamera.targetTexture = m_ShadowMap;
            Shader.SetGlobalTexture(m_InternalShadowMapID, m_ShadowMap);
        }
    }


    public void Destroy()
    {
        if (m_ShadowMap)
            Object.Destroy(m_ShadowMap);
        m_ShadowMap = null;
        if (m_ShadowRenderShader)
            Resources.UnloadAsset(m_ShadowRenderShader);
        m_ShadowRenderShader = null;
    }
}
