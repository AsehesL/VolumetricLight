using UnityEngine;
using System.Collections;

public static class GizmosEx
{

    public static void DrawOrtho(Transform transform, float aspect, float size, float near, float far, Color color)
    {
        Matrix4x4 proj = Matrix4x4.Ortho(-aspect*size, aspect*size, -size, size, -near, -far);

        proj = (proj*transform.worldToLocalMatrix).inverse;
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

        Gizmos.color = color;

        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);

        Gizmos.DrawLine(p5, p6);
        Gizmos.DrawLine(p6, p7);
        Gizmos.DrawLine(p7, p8);
        Gizmos.DrawLine(p8, p5);

        Gizmos.DrawLine(p1, p5);
        Gizmos.DrawLine(p2, p6);
        Gizmos.DrawLine(p3, p7);
        Gizmos.DrawLine(p4, p8);
    }

    public static void DrawPerspective(Transform transform, float aspect, float fieldOfView, float near, float far,
        Color color)
    {
        Matrix4x4 proj = Matrix4x4.Perspective(fieldOfView, aspect, -near, -far);

        proj = (proj*transform.worldToLocalMatrix).inverse;
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

        Gizmos.color = color;

        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);

        Gizmos.DrawLine(p5, p6);
        Gizmos.DrawLine(p6, p7);
        Gizmos.DrawLine(p7, p8);
        Gizmos.DrawLine(p8, p5);

        Gizmos.DrawLine(transform.position, p5);
        Gizmos.DrawLine(transform.position, p6);
        Gizmos.DrawLine(transform.position, p7);
        Gizmos.DrawLine(transform.position, p8);
    }
}
