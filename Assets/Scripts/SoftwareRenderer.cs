using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoftwareRenderer
{
    public static List<Vector4> _pixels = new List<Vector4>();
    public static List<float> _zBuffers = new List<float>();
    public void Init() {
        ClearBuffers();
    }

    void ClearBuffers() {
        _pixels.Clear();
        _zBuffers.Clear();
        for (int y = 0; y < Screen.height; y++)
        {
            for (int x = 0; x < Screen.width; x++)
            {
                Vector4 color;
                color.x = 0;
                color.y = 0;
                color.z = 0;
                color.w = 1.0f;

                _pixels.Add(color);
                _zBuffers.Add(1);
            }
        }
    }

    public bool BackFaceCulling(Vector3 normal, Vector3 vert, Matrix4x4 worldToObject)
    {
        Vector4 CameraPos = new Vector4(Camera.main.transform.localPosition.x, Camera.main.transform.localPosition.y,
            Camera.main.transform.localPosition.z, 1);
        Vector3 viewDir = (Vector3)(worldToObject * CameraPos) - vert;

        viewDir = viewDir.normalized;
        float intensity = Vector3.Dot(normal, viewDir);
        return intensity <= 0;
    }

    bool ClipTriangles(List<Vector4> clipSpaceVertices)
    {
        int count = 0;
        for (int i = 0; i < 3; ++i)
        {
            Vector4 vertex = clipSpaceVertices[i];
            bool inside = (-vertex.w <= vertex.x && vertex.x <= vertex.w)
                && (-vertex.w <= vertex.y && vertex.y <= vertex.w)
                && (0 <= vertex.z && vertex.z <= vertex.w);
            if (!inside) ++count;
        }
        //If count equals three it means every vertex was out so we skip it
        return count == 3;
    }

    List<Vector4> vector4s = new List<Vector4>();
    List<Vector3> normals = new List<Vector3>();
    List<Vector3> textureVals = new List<Vector3>();
    List<Vector4> tangents = new List<Vector4>();

    public void DrawTriangularMesh(MeshObject meshObject)
    {
        int offset = meshObject.indices_offset;
        int count = offset + meshObject.indices_count;

        IShader _IShader = new IShader(meshObject.model_transform, meshObject);

        for (int i = offset; i < count; i += 3)
        {
            Vector4 v0 = RenderingMaster._instance._vertices[RenderingMaster._instance._indices[i]].Vector3ToVector4();
            Vector4 v1 = RenderingMaster._instance._vertices[RenderingMaster._instance._indices[i + 1]].Vector3ToVector4();
            Vector4 v2 = RenderingMaster._instance._vertices[RenderingMaster._instance._indices[i + 2]].Vector3ToVector4();

            vector4s.Clear();
            vector4s.Add(v0);
            vector4s.Add(v1);
            vector4s.Add(v2);

            Vector3 _N1 = RenderingMaster._instance._normals[RenderingMaster._instance._indices[i]];
            Vector3 _N2 = RenderingMaster._instance._normals[RenderingMaster._instance._indices[i + 1]];
            Vector3 _N3 = RenderingMaster._instance._normals[RenderingMaster._instance._indices[i + 2]];

            normals.Clear();
            normals.Add(_N1);
            normals.Add(_N2);
            normals.Add(_N3);

            Vector2 _UV1 = RenderingMaster._instance._textureVals[RenderingMaster._instance._indices[i]];
            Vector2 _UV2 = RenderingMaster._instance._textureVals[RenderingMaster._instance._indices[i + 1]];
            Vector2 _UV3 = RenderingMaster._instance._textureVals[RenderingMaster._instance._indices[i + 2]];

            textureVals.Clear();
            textureVals.Add(_UV1);
            textureVals.Add(_UV2);
            textureVals.Add(_UV3);

            Vector4 _T1 = RenderingMaster._instance._tangents[RenderingMaster._instance._indices[i]];
            Vector4 _T2 = RenderingMaster._instance._tangents[RenderingMaster._instance._indices[i + 1]];
            Vector4 _T3 = RenderingMaster._instance._tangents[RenderingMaster._instance._indices[i + 2]];

            tangents.Clear();
            tangents.Add(_T1);
            tangents.Add(_T2);
            tangents.Add(_T3);

            Matrix4x4 inverse_modelMatrix = _IShader.M.inverse;
            Vector3 N1 = vector4s[1] - vector4s[0];
            Vector3 N2 = vector4s[2] - vector4s[0];

            Vector3 normal = Vector3.Cross(N1, N2).normalized;

            if (RenderingMaster._instance.GetBackFaceCulling()) {
                //背面剔除
                if (BackFaceCulling(normal, vector4s[0], inverse_modelMatrix))
                {
                    //Debug.Log("在背部__________________________");
                    continue;
                }
            }

            for (int j = 0; j < 3; ++j)
            {
                vector4s[j] = _IShader.vertex(vector4s[j], normals[j], textureVals[j], tangents[j], j, RenderingMaster._instance.DirectionalLight.transform.localPosition);
            }

            Vector3 hW = new Vector3(1 / vector4s[0].w, 1 / vector4s[1].w, 1 / vector4s[2].w);

             //视锥体裁剪
            if (ClipTriangles(vector4s))
            {
                //Debug.Log("在视锥体外面 剔除掉");
                continue;
            }

            //NDC(透视除法) 
            for (int j = 0; j < 3; ++j)
            {
                vector4s[j] = _IShader.PerspectiveDivide(vector4s[j]);
            }

            //屏幕映射
            for (int j = 0; j < vector4s.Count; ++j)
            {
                vector4s[j] = _IShader.ViewportTransform(vector4s[j]);
            }

            if (RenderingMaster._instance.GetDrawTrianglesType() == 0)
            {
                //裁剪线段
                RenderingMaster._instance.rasterizer.DrawWireFrame(vector4s,true);
            }
            else if (RenderingMaster._instance.GetDrawTrianglesType() == 1)
            {
                RenderingMaster._instance.rasterizer.DrawWireFrame(vector4s);
            }
            else if (RenderingMaster._instance.GetDrawTrianglesType() == 2) { 
                RenderingMaster._instance.rasterizer.DrawTriangles(vector4s, _IShader, hW);
            }
        }
    }
}
