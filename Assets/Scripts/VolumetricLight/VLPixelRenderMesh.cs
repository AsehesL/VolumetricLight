using UnityEngine;
using System.Collections;

/// <summary>
/// 基于片段着色器的体积光Mesh
/// </summary>
public class VLPixelRenderMesh : VolumetricLightMeshBase
{
    protected override Shader LoadShader()
    {
        return Resources.Load<Shader>("Shaders/VolumetricLight/VolumetricLight");
    }

    protected override void UnLoadShader(Shader shader)
    {
        Resources.UnloadAsset(shader);
    }

    protected override void OnRefreshMesh(Color color, float range, float subdivision, Matrix4x4 matrix)
    {
        base.OnRefreshMesh(color, range, subdivision, matrix);

        ResetIndexLength(30);

        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                float x = i == 0 ? -1 : 1;
                float y = i == j ? -1 : 1;
                SetVertex(matrix.MultiplyPoint(new Vector3(x, y, -1)), color, i * 4 + j * 2);
                SetVertex(matrix.MultiplyPoint(new Vector3(x, y, 1)), color, i * 4 + j * 2 + 1);
                if (i > 0 && j > 0)
                {
                    SetIndex(i*12 + j*6, i*4 + j*2);
                    SetIndex(i * 12 + j * 6 + 1, i * 4 + j * 2 + 1);
                    SetIndex(i * 12 + j * 6 + 2, 1);
                    SetIndex(i * 12 + j * 6 + 3, i * 4 + j * 2);
                    SetIndex(i * 12 + j * 6 + 4, 1);
                    SetIndex(i * 12 + j * 6 + 5, 0);
                }
                else
                {
                    SetIndex(i * 12 + j * 6, i * 4 + j * 2);
                    SetIndex(i * 12 + j * 6 + 1, i * 4 + j * 2 + 1);
                    SetIndex(i * 12 + j * 6 + 2, i * 4 + j * 2 + 3);
                    SetIndex(i * 12 + j * 6 + 3, i * 4 + j * 2);
                    SetIndex(i * 12 + j * 6 + 4, i * 4 + j * 2 + 3);
                    SetIndex(i * 12 + j * 6 + 5, i * 4 + j * 2 + 2);
                }
            }
        }

        BuildMesh();
    }
}
