using UnityEngine;
using System.Collections;

/// <summary>
/// 基于顶点着色器的体积光Mesh
/// </summary>
public class VLVertexRenderMesh : VolumetricLightMeshBase
{

    protected override Shader LoadShader()
    {
        return Resources.Load<Shader>("Shaders/VolumetricLight/VertexVolumetricLight");
    }

    protected override void UnLoadShader(Shader shader)
    {
        Resources.UnloadAsset(shader);
    }

    protected override void OnRefreshMesh(Color color, float range, float subdivision, Matrix4x4 matrix)
    {
        base.OnRefreshMesh(color, range, subdivision, matrix);

        int zstep = (int)(range / subdivision);
        int hstep = 20;
        ResetIndexLength(4*(zstep*hstep*6));

        int index = 0;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                float bx = i == 0 ? -1 : 1;
                float by = i == j ? -1 : 1;
                float tx = i == j ? -1 : 1;
                float ty = i == 1 ? -1 : 1;

                Vector3 beg1 = matrix.MultiplyPoint(new Vector3(bx, by, -1));
                Vector3 to1 = matrix.MultiplyPoint(new Vector3(bx, by, 1));
                Vector3 beg2 = matrix.MultiplyPoint(new Vector3(tx, ty, -1));
                Vector3 to2 = matrix.MultiplyPoint(new Vector3(tx, ty, 1));

                int faceIndex = i * 2 + j;

                for (int k = 0; k <= zstep; k++)
                {
                    float zl = ((float)k) / zstep;
                    Vector3 v1 = Vector3.Lerp(beg1, to1, zl);
                    Vector3 v2 = Vector3.Lerp(beg2, to2, zl);
                    for (int p = 0; p <= hstep; p++)
                    {
                        float hl = ((float)p) / hstep;
                        float x = Mathf.Lerp(v1.x, v2.x, hl);
                        float y = Mathf.Lerp(v1.y, v2.y, hl);
                        int currentIndex = faceIndex * (zstep + 1) * (hstep + 1) + k * (hstep + 1) + p;
                        SetVertex(new Vector3(x, y, v1.z), color, currentIndex);
                        if (k < zstep && p < hstep)
                        {
                            SetIndex(index, faceIndex*(zstep + 1)*(hstep + 1) + k*(hstep + 1) + p);
                            SetIndex(index + 1, faceIndex * (zstep + 1) * (hstep + 1) + (k + 1) * (hstep + 1) + p);
                            SetIndex(index + 2, faceIndex * (zstep + 1) * (hstep + 1) + (k + 1) * (hstep + 1) + p + 1);
                            SetIndex(index + 3, faceIndex * (zstep + 1) * (hstep + 1) + k * (hstep + 1) + p);
                            SetIndex(index + 4, faceIndex * (zstep + 1) * (hstep + 1) + (k + 1) * (hstep + 1) + p + 1);
                            SetIndex(index + 5, faceIndex * (zstep + 1) * (hstep + 1) + k * (hstep + 1) + p + 1);
                            index += 6;
                        }
                    }
                }
            }
        }

        BuildMesh();
    }
}
