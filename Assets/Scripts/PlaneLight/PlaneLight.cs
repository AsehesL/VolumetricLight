using UnityEngine;
using System.Collections;

public class PlaneLight : MonoBehaviour
{
    public int shadowMapSize;

    public Texture2D cookie;
    //public Texture2D noise;
    public bool useCameraMatrix;

    public bool directional;
    public float bias;

    public float near;
    public float far;
    public float fieldOfView;
    public float size;
    public float aspect;
    public Color color;
    public float intensity;
    public float atten;

    private Camera m_DepthRenderCamera;
    private RenderTexture m_DepthTexture;

    private Color m_OldColor;
    private float m_OldIntensity;
    private Matrix4x4 m_OldWorldToLocalMatrix;
    private Matrix4x4 m_OldProjectionMatrix;
    private float m_Bias;
    private float m_Atten;
    private Texture2D m_OldCookie;

    private Texture2D m_DefaultLight;

    private Material m_Material;

    void Start()
    {
        InitRenderTarget();
        CreateMesh();
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
    }

    void Update()
    {
        if (m_OldColor != color || m_OldIntensity != intensity)
        {
            Shader.SetGlobalColor("internalWorldLightColor",
                new Color(color.r*intensity, color.g*intensity, color.b*intensity, color.a));
            m_OldIntensity = intensity;
            m_OldColor = color;
            m_Material.SetColor("_Color", new Color(color.r * intensity, color.g * intensity, color.b * intensity, color.a));
        }
        if (m_OldCookie != cookie)
        {
            if (cookie)
                Shader.SetGlobalTexture("internalCookie", cookie);
            else
                Shader.SetGlobalTexture("internalCookie", m_DefaultLight);
            m_OldCookie = cookie;
        }
        
        m_DepthRenderCamera.aspect = aspect;
        m_DepthRenderCamera.farClipPlane = far;
        m_DepthRenderCamera.nearClipPlane = near;
        m_DepthRenderCamera.fieldOfView = fieldOfView;
        m_DepthRenderCamera.orthographic = directional;
        m_DepthRenderCamera.orthographicSize = size;
    }

    void OnPreRender()
    {
        m_Material.SetMatrix("internalProjection", m_DepthRenderCamera.projectionMatrix);
        m_Material.SetMatrix("internalProjectionInv", m_DepthRenderCamera.projectionMatrix.inverse);
        m_Material.SetVector("_LightParams", new Vector4(m_DepthRenderCamera.farClipPlane, atten, 0, 0));
    }

    void OnPreCull()
    {
        Matrix4x4 wtl = useCameraMatrix ? m_DepthRenderCamera.worldToCameraMatrix : transform.worldToLocalMatrix;
        if (m_OldWorldToLocalMatrix != wtl)
        {
            if (!directional)
                Shader.SetGlobalVector("internalWorldLightPos",
                    new Vector4(transform.position.x, transform.position.y, transform.position.z, 1));
            else
                Shader.SetGlobalVector("internalWorldLightPos",
                    new Vector4(transform.forward.x, transform.forward.y, transform.forward.z, 0));
            Shader.SetGlobalMatrix("internalWorldLightMV", wtl);
            m_OldWorldToLocalMatrix = wtl;
        }
        if (m_OldProjectionMatrix != m_DepthRenderCamera.projectionMatrix)
        {
            Shader.SetGlobalMatrix("internalWorldLightVP", m_DepthRenderCamera.projectionMatrix);
            Shader.SetGlobalVector("internalProjectionParams",
                new Vector4(atten, m_DepthRenderCamera.nearClipPlane, m_DepthRenderCamera.farClipPlane, 1 / m_DepthRenderCamera.farClipPlane));
            m_OldProjectionMatrix = m_DepthRenderCamera.projectionMatrix;
        }
        if (m_Atten != atten)
        {
            Shader.SetGlobalVector("internalProjectionParams",
                new Vector4(atten, m_DepthRenderCamera.nearClipPlane, m_DepthRenderCamera.farClipPlane, 1 / m_DepthRenderCamera.farClipPlane));
     
            m_Atten = atten;
        }
        if (m_Bias != bias)
        {
            m_Bias = bias;
            Shader.SetGlobalFloat("internalBias", bias);
        }
    }

    void InitRenderTarget()
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
            m_DepthRenderCamera.aspect = aspect;
            m_DepthRenderCamera.backgroundColor = new Color(0, 0, 0, 0);
            m_DepthRenderCamera.clearFlags = CameraClearFlags.SolidColor;
            m_DepthRenderCamera.depth = 0;
            m_DepthRenderCamera.farClipPlane = far;
            m_DepthRenderCamera.nearClipPlane = near;
            m_DepthRenderCamera.fieldOfView = fieldOfView;
            m_DepthRenderCamera.orthographic = directional;
            m_DepthRenderCamera.orthographicSize = size;
            m_DepthRenderCamera.SetReplacementShader(Shader.Find("Hidden/ReplaceDepth"), "RenderType");
        }
        if (m_DepthTexture == null)
        {
            m_DepthTexture = new RenderTexture(shadowMapSize, shadowMapSize, 16);
            m_DepthRenderCamera.targetTexture = m_DepthTexture;
            Shader.SetGlobalTexture("internalShadowMap", m_DepthTexture);
            if (cookie)
                Shader.SetGlobalTexture("internalCookie", cookie);
            else
                Shader.SetGlobalTexture("internalCookie", m_DefaultLight);
            m_OldCookie = cookie;
        }
    }

    private void CreateMesh()
    {
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        m_Material = new Material(Shader.Find("Hidden/VolumeLight"));
        meshRenderer.sharedMaterial = m_Material;
        m_Material.SetTexture("_DepthTex", m_DepthTexture);
        m_Material.SetColor("_Color", new Color(color.r * intensity, color.g * intensity, color.b * intensity, color.a));
        //m_Material.SetTexture("_Noise", noise);
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        Matrix4x4 mt = transform.worldToLocalMatrix * m_DepthRenderCamera.cameraToWorldMatrix * m_DepthRenderCamera.projectionMatrix.inverse;
        Vector3 p1 = new Vector3(-1, -1, -1);
        Vector3 p2 = new Vector3(-1, 1, -1);
        Vector3 p3 = new Vector3(1, 1, -1);
        Vector3 p4 = new Vector3(1, -1, -1);

        Vector3 p5 = new Vector3(-1, -1, 1);
        Vector3 p6 = new Vector3(-1, 1, 1);
        Vector3 p7 = new Vector3(1, 1, 1);
        Vector3 p8 = new Vector3(1, -1, 1);
        mesh.vertices = new Vector3[]{
            mt.MultiplyPoint(p1),mt.MultiplyPoint(p2),
            mt.MultiplyPoint(p3),mt.MultiplyPoint(p4),
            mt.MultiplyPoint(p5),mt.MultiplyPoint(p6),
            mt.MultiplyPoint(p7),mt.MultiplyPoint(p8)
        };
        mesh.triangles = new int[]{
            0,4,5,0,5,1,
            1,5,6,1,6,2,
            2,6,7,2,7,3,
            0,3,4,3,7,4,
            4,5,6,4,6,7
        };
        meshFilter.sharedMesh = mesh;
        mesh.RecalculateBounds();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 0.3f, 1f, 0.6f);
        if (directional)
        {
            Vector3 vp1 = transform.position + transform.rotation*new Vector3(-size*aspect, -size, near);
            Vector3 vp2 = transform.position + transform.rotation*new Vector3(-size*aspect, size, near);
            Vector3 vp3 = transform.position + transform.rotation*new Vector3(size*aspect, size, near);
            Vector3 vp4 = transform.position + transform.rotation*new Vector3(size*aspect, -size, near);

            Vector3 vp5 = transform.position + transform.rotation*new Vector3(-size*aspect, -size, far);
            Vector3 vp6 = transform.position + transform.rotation*new Vector3(-size*aspect, size, far);
            Vector3 vp7 = transform.position + transform.rotation*new Vector3(size*aspect, size, far);
            Vector3 vp8 = transform.position + transform.rotation*new Vector3(size*aspect, -size, far);

            Gizmos.DrawLine(vp1, vp2);
            Gizmos.DrawLine(vp2, vp3);
            Gizmos.DrawLine(vp3, vp4);
            Gizmos.DrawLine(vp4, vp1);

            Gizmos.DrawLine(vp5, vp6);
            Gizmos.DrawLine(vp6, vp7);
            Gizmos.DrawLine(vp7, vp8);
            Gizmos.DrawLine(vp8, vp5);

            Gizmos.DrawLine(vp1, vp5);
            Gizmos.DrawLine(vp2, vp6);
            Gizmos.DrawLine(vp3, vp7);
            Gizmos.DrawLine(vp4, vp8);
        }
        else
        {
            float tan = Mathf.Tan(fieldOfView/2*Mathf.Deg2Rad);
            Vector3 vp1 = transform.position + transform.rotation * new Vector3(-tan*near * aspect, -tan * near, near);
            Vector3 vp2 = transform.position + transform.rotation * new Vector3(-tan * near * aspect, tan * near, near);
            Vector3 vp3 = transform.position + transform.rotation * new Vector3(tan * near * aspect, tan * near, near);
            Vector3 vp4 = transform.position + transform.rotation * new Vector3(tan * near * aspect, -tan * near, near);

            Vector3 vp5 = transform.position + transform.rotation * new Vector3(-tan * far * aspect, -tan * far, far);
            Vector3 vp6 = transform.position + transform.rotation * new Vector3(-tan * far * aspect, tan * far, far);
            Vector3 vp7 = transform.position + transform.rotation * new Vector3(tan * far * aspect, tan * far, far);
            Vector3 vp8 = transform.position + transform.rotation * new Vector3(tan * far * aspect, -tan * far, far);

            Gizmos.DrawLine(vp1, vp2);
            Gizmos.DrawLine(vp2, vp3);
            Gizmos.DrawLine(vp3, vp4);
            Gizmos.DrawLine(vp4, vp1);

            Gizmos.DrawLine(vp5, vp6);
            Gizmos.DrawLine(vp6, vp7);
            Gizmos.DrawLine(vp7, vp8);
            Gizmos.DrawLine(vp8, vp5);

            Gizmos.DrawLine(transform.position, vp5);
            Gizmos.DrawLine(transform.position, vp6);
            Gizmos.DrawLine(transform.position, vp7);
            Gizmos.DrawLine(transform.position, vp8);
        }
    }
}
