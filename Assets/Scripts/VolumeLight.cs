using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeLight : MonoBehaviour {

    public float nearClipPlane;
    public float farClipPlane;
    public float fieldOfView;
    public float aspect;
    public bool orthographic;
    public float orthographicSize;

    public LayerMask cullingMask;

    private Camera m_DepthRenderCamera;

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
        m_DepthRenderCamera.SetReplacementShader(Shader.Find("Hidden/DepthRender"), "RenderType");

        m_DepthTexture = new RenderTexture(1024, 1024, 24);
        m_DepthRenderCamera.targetTexture = m_DepthTexture;
    }

    private void CreateMesh()
    {
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Diffuse"));
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
        };
        meshFilter.sharedMesh = mesh;
    }

    void OnDrawGizmos()
    {
        Matrix4x4 proj = default(Matrix4x4);
        if (orthographic)
        {
            proj = Matrix4x4.Ortho(-aspect * orthographicSize, aspect * orthographicSize, -orthographicSize, orthographicSize, -nearClipPlane, -farClipPlane);
        }
        else
        {
            proj = Matrix4x4.Perspective(fieldOfView, aspect, -nearClipPlane, -farClipPlane);
        }
        proj = (proj * transform.worldToLocalMatrix).inverse;
        Vector3 p1 = new Vector3(-1, -1, -1);
        Vector3 p2 = new Vector3(-1, 1, -1);
        Vector3 p3 = new Vector3(1, 1, -1);
        Vector3 p4 = new Vector3(1, -1, -1);

        Vector3 p5 = new Vector3(-1, -1, 1);
        Vector3 p6 = new Vector3(-1, 1, 1);
        Vector3 p7 = new Vector3(1, 1, 1);
        Vector3 p8 = new Vector3(1, -1, 1);

        p1 = proj.MultiplyPoint(p1);
        p2 = proj.MultiplyPoint(p2);
        p3 = proj.MultiplyPoint(p3);
        p4 = proj.MultiplyPoint(p4);

        p5 = proj.MultiplyPoint(p5);
        p6 = proj.MultiplyPoint(p6);
        p7 = proj.MultiplyPoint(p7);
        p8 = proj.MultiplyPoint(p8);

        Gizmos.color = Color.black;

        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);

        Gizmos.DrawLine(p5, p6);
        Gizmos.DrawLine(p6, p7);
        Gizmos.DrawLine(p7, p8);
        Gizmos.DrawLine(p8, p5);

        if (orthographic)
        {
            Gizmos.DrawLine(p1, p5);
            Gizmos.DrawLine(p2, p6);
            Gizmos.DrawLine(p3, p7);
            Gizmos.DrawLine(p4, p8);
        }
        else
        {
            Gizmos.DrawLine(transform.position, p5);
            Gizmos.DrawLine(transform.position, p6);
            Gizmos.DrawLine(transform.position, p7);
            Gizmos.DrawLine(transform.position, p8);
        }
    }
}
