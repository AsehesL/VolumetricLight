using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeLight_Test : MonoBehaviour {

    public float nearClipPlane;
    public float farClipPlane;
    public float fieldOfView;
    public float aspect;
    public bool orthographic;
    public float orthographicSize;

    public LayerMask cullingMask;

    private Camera m_DepthRenderCamera;
    private Material m_Material;

    private RenderTexture m_DepthTexture;

	void Start () {
        CreateCamera();
        CreateMesh();
	}

    void OnDestroy()
    {
        if (m_DepthTexture)
            Destroy(m_DepthTexture);
        m_DepthTexture = null;
    }

    void OnPreRender()
    {
        m_Material.SetMatrix("internalProjection", m_DepthRenderCamera.projectionMatrix);
        m_Material.SetMatrix("internalProjectionInv", m_DepthRenderCamera.projectionMatrix.inverse);
        m_Material.SetVector("lightZParams", new Vector4(m_DepthRenderCamera.farClipPlane, m_DepthRenderCamera.orthographicSize*m_DepthRenderCamera.aspect, 0, 0));

    }

    private void CreateCamera()
    {
        m_DepthRenderCamera = gameObject.AddComponent<Camera>();
        m_DepthRenderCamera.orthographic = orthographic;
        m_DepthRenderCamera.aspect = aspect;
        m_DepthRenderCamera.clearFlags = CameraClearFlags.SolidColor;
        m_DepthRenderCamera.cullingMask = cullingMask;
        m_DepthRenderCamera.depth = 0;
        m_DepthRenderCamera.nearClipPlane = nearClipPlane;
        m_DepthRenderCamera.farClipPlane = farClipPlane;
        m_DepthRenderCamera.fieldOfView = fieldOfView;
        m_DepthRenderCamera.orthographicSize = orthographicSize;
        m_DepthRenderCamera.SetReplacementShader(Shader.Find("Hidden/ShadowMapRenderer"), "RenderType");

        m_DepthTexture = new RenderTexture(1024, 1024, 24);
        m_DepthRenderCamera.targetTexture = m_DepthTexture;
    }

    private void CreateMesh()
    {
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        m_Material = new Material(Shader.Find("Unlit/VolumeLight_test3"));
        meshRenderer.sharedMaterial = m_Material;
        m_Material.SetTexture("_DepthTex", m_DepthTexture);
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        Matrix4x4 mt = transform.worldToLocalMatrix*m_DepthRenderCamera.cameraToWorldMatrix*m_DepthRenderCamera.projectionMatrix.inverse;
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
    }

    void OnDrawGizmosSelected()
    {
        if (orthographic)
        {
            GizmosEx.DrawOrtho(transform, aspect, orthographicSize, nearClipPlane, farClipPlane,
                new Color(0.5f, 0.5f, 0.5f, 0.7f));
        }
        else
        {
            GizmosEx.DrawPerspective(transform, aspect, fieldOfView, nearClipPlane, farClipPlane,
                new Color(0.5f, 0.5f, 0.5f, 0.7f));
        }
        
    }
}
