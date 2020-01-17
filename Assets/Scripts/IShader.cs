using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IShader  {
    public Matrix4x4 M, V, P, MV,MVP,N;
    RendererTexture albedoTexture, aoTexture, roughTexture, metalTexture, normalTexture;
    float ambientStrength = 0.05f;
     
    Vector3 cameraPos;
    public  IShader(Transform model_transform, MeshObject  meshObject) {

        M = GetModelMatrix(model_transform);
        V = GetViewMatrix();
        P = GetProjectionMatrix();
         
        MV = V * M;
        MVP = P * MV ;
        N = M.inverse.transpose;

        if (meshObject.albedoTexture) {
            albedoTexture = new RendererTexture(meshObject.albedoTexture);
        }
        if (meshObject.aoTexture)
        {
            aoTexture = new RendererTexture(meshObject.aoTexture);
        }
        if (meshObject.roughTexture)
        {
            roughTexture = new RendererTexture(meshObject.roughTexture);
        }
        if (meshObject.metalTexture)
        {
            metalTexture = new RendererTexture(meshObject.metalTexture);
        }
        if (meshObject.normalTexture)
        {
            normalTexture = new RendererTexture(meshObject.normalTexture);
        }

        cameraPos = Camera.main.transform.position;
    }

    // 顶点着色器输入输出
    Vector3[] normals = new Vector3[3];
    Vector3[] viewDir = new Vector3[3];
    Vector2[] texCoords = new Vector2[3];
    Vector3[] lightDir = new Vector3[3];

    Vector3 worldPos;
    Vector3 interpLightDir;

    Vector3 normal_WS, tangent_WS, biTangent_WS;
    Matrix4x4 TBN;

    public Vector4 vertex(Vector4 vertex , Vector3 normal , Vector2 textureVals ,Vector4 tangent,int index,Vector3 light) {

        worldPos = M * vertex;
        normals[index] =  (N * normal).normalized;
        viewDir[index] = (cameraPos - worldPos).normalized;
        texCoords[index] = textureVals;
        interpLightDir = light.normalized;

        return MVP * vertex;
    }

     
    Vector3 interpCol, interpNormal;
    Color textureColor;
    Vector3 interpViewDir;
    Vector2 interpCoords;

    float ambientInt = 0.03f;
    float interpAO, interpRough, interpMetal;
    public Vector4 fragment(float u, float v) {
        interpNormal = normals[0] + (normals[1] - normals[0]) * u + (normals[2] - normals[0]) * v;
        interpCoords = texCoords[0] + (texCoords[1] - texCoords[0]) * u + (texCoords[2] - texCoords[0]) * v;
        interpViewDir = viewDir[0] + (viewDir[1] - viewDir[0]) * u + (viewDir[2] - viewDir[0]) * v;

        if (albedoTexture != null) {
            textureColor = albedoTexture[interpCoords.x, interpCoords.y];
            interpCol = new Vector3(textureColor.r, textureColor.g, textureColor.b);
        }

        if (aoTexture != null)
        {
            textureColor = aoTexture[interpCoords.x, interpCoords.y];
            interpAO = (float)textureColor.r;
        }

        if (roughTexture != null)
        {
            textureColor = roughTexture[interpCoords.x, interpCoords.y];
            interpRough = (float)textureColor.r;
        }

        if (metalTexture != null)
        {
            textureColor = metalTexture[interpCoords.x, interpCoords.y];
            interpMetal = (float)textureColor.r;
        }

        //if (normalTexture != null)
        //{
        //    textureColor = normalTexture[interpCoords.x, interpCoords.y];
        //    interpNormal = new Vector4(textureColor.r, textureColor.g, textureColor.b);
        //}
        interpNormal = interpNormal.normalized;
        interpViewDir = interpViewDir.normalized;

        Vector3 radianceLights = Vector3.zero;

        Vector3 halfwayDir = (interpLightDir + interpViewDir).normalized;
        float nDotL = Mathf.Max(0.0f, Vector3.Dot(interpNormal, interpLightDir));
        Vector3 diffuse = nDotL * Vector3.one;

        float spec = Mathf.Pow(Mathf.Max(0.0f, Vector3.Dot(interpNormal, halfwayDir)), 128);
        Vector3 specular = spec * Vector3.one;

        radianceLights = interpCol.Vector3MultiplyVector3(diffuse) + specular;

        //Ambient 
        Vector3 ambient = interpCol* interpAO* ambientInt;

        Vector3 col = ambient + radianceLights;

        return new Vector4(col.x, col.y, col.z, 1f);
    }

    //BRDF functions
    Vector3 fresnelSchlick(float cosTheta, Vector3 fresnel0 )
    {
        float invcCosTheta = 1.0f - cosTheta;
        return fresnel0 + (Vector3.one - fresnel0) * (invcCosTheta * invcCosTheta * invcCosTheta * invcCosTheta * invcCosTheta);
    }

    float distributionGGX(Vector3 normal, Vector3 halfway, float roughness)
    {
        float a = roughness * roughness;
        float a2 = a * a;
        float NdotH = Mathf.Max(Vector3.Dot(normal,halfway), 0.0f);
        float NdotH2 = NdotH * NdotH;

        float denom = (NdotH2 * (a2 - 1.0f) + 1.0f);
        denom =  Mathf.PI / (denom * denom);

        return a2 * denom;
    }

    float GeometrySchlickGGX(float Ndot, float roughness)
    {
        float r = (roughness + 1.0f);
        float k = (r * r) / 8.0f; //Only useful for direct lighting must be changed in ibr
        float denom = 1.0f / (Ndot * (1.0f - k) + k);

        return Ndot * denom;
    }

    float GeometrySmith(float roughness, float nDL, float nDV)
    {
        return GeometrySchlickGGX(nDL, roughness) * GeometrySchlickGGX(nDV, roughness);
    }

    public Vector3 reflect( Vector3 I,  Vector3 N){
        return I - 2.0f * (N * Vector3.Dot(N, I));
    }

    public  Matrix4x4 GetModelMatrix(Transform model_transform)
    {
        Matrix4x4 translationMatrix = new Matrix4x4(
          new Vector4(1, 0, 0, 0),
          new Vector4(0, 1, 0, 0),
          new Vector4(0, 0, 1, 0),
          new Vector4(model_transform.localPosition.x, model_transform.localPosition.y, model_transform.localPosition.z, 1));

        Matrix4x4 rotX = new Matrix4x4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, Mathf.Cos(model_transform.eulerAngles.x * Mathf.PI / 180), Mathf.Sin(model_transform.eulerAngles.x * Mathf.PI / 180), 0),
            new Vector4(0, -Mathf.Sin(model_transform.eulerAngles.x * Mathf.PI / 180), Mathf.Cos(model_transform.eulerAngles.x * Mathf.PI / 180), 0),
            new Vector4(0, 0, 0, 1));

        Matrix4x4 rotY = new Matrix4x4(
               new Vector4(Mathf.Cos(model_transform.eulerAngles.y * Mathf.PI / 180), 0, -Mathf.Sin(model_transform.eulerAngles.y * Mathf.PI / 180), 0),
               new Vector4(0, 1, 0, 0),
               new Vector4(Mathf.Sin(model_transform.eulerAngles.y * Mathf.PI / 180), 0, Mathf.Cos(model_transform.eulerAngles.y * Mathf.PI / 180), 0),
               new Vector4(0, 0, 0, 1));

        Matrix4x4 rotZ = new Matrix4x4(
           new Vector4(Mathf.Cos(model_transform.eulerAngles.z * Mathf.PI / 180), Mathf.Sin(model_transform.eulerAngles.z * Mathf.PI / 180), 0, 0),
           new Vector4(-Mathf.Sin(model_transform.eulerAngles.z * Mathf.PI / 180), Mathf.Cos(model_transform.eulerAngles.z * Mathf.PI / 180), 0, 0),
           new Vector4(0, 0, 1, 0),
           new Vector4(0, 0, 0, 1));

        Matrix4x4 scaleMatrix = new Matrix4x4 (
          new Vector4(model_transform.localScale.x, 0, 0, 0),
          new Vector4(0, model_transform.localScale.y, 0, 0),
          new Vector4(0, 0, model_transform.localScale.z, 0),
          new Vector4(0, 0, 0, 1)
         );

        Matrix4x4 localToWorldMatrix = translationMatrix * rotY * rotX * rotZ * scaleMatrix;
        return localToWorldMatrix;
    }

    public  Matrix4x4 GetViewMatrix()
    {
        Transform model_transform = Camera.main.transform;

        Matrix4x4 translationMatrix = new Matrix4x4(
       new Vector4(1, 0, 0, 0),
       new Vector4(0, 1, 0, 0),
       new Vector4(0, 0, 1, 0),
       new Vector4(-model_transform.localPosition.x, -model_transform.localPosition.y, -model_transform.localPosition.z, 1));

        Matrix4x4 rotX = new Matrix4x4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, Mathf.Cos(-model_transform.eulerAngles.x * Mathf.PI / 180), Mathf.Sin(-model_transform.eulerAngles.x * Mathf.PI / 180), 0),
            new Vector4(0, -Mathf.Sin(-model_transform.eulerAngles.x * Mathf.PI / 180), Mathf.Cos(-model_transform.eulerAngles.x * Mathf.PI / 180), 0),
            new Vector4(0, 0, 0, 1));

        Matrix4x4 rotY = new Matrix4x4(
               new Vector4(Mathf.Cos(-model_transform.eulerAngles.y * Mathf.PI / 180), 0, -Mathf.Sin(-model_transform.eulerAngles.y * Mathf.PI / 180), 0),
               new Vector4(0, 1, 0, 0),
               new Vector4(Mathf.Sin(-model_transform.eulerAngles.y * Mathf.PI / 180), 0, Mathf.Cos(-model_transform.eulerAngles.y * Mathf.PI / 180), 0),
               new Vector4(0, 0, 0, 1));

        Matrix4x4 rotZ = new Matrix4x4(
           new Vector4(Mathf.Cos(-model_transform.eulerAngles.z * Mathf.PI / 180), Mathf.Sin(-model_transform.eulerAngles.z * Mathf.PI / 180), 0, 0),
           new Vector4(-Mathf.Sin(-model_transform.eulerAngles.z * Mathf.PI / 180), Mathf.Cos(-model_transform.eulerAngles.z * Mathf.PI / 180), 0, 0),
           new Vector4(0, 0, 1, 0),
           new Vector4(0, 0, 0, 1));

        Matrix4x4 scaleMatrix = new Matrix4x4(
          new Vector4(model_transform.localScale.x, 0, 0, 0),
          new Vector4(0, model_transform.localScale.y, 0, 0),
          new Vector4(0, 0, model_transform.localScale.z, 0),
          new Vector4(0, 0, 0, 1)
         );

        Matrix4x4 worldToLocalMatrix = scaleMatrix * rotZ * rotX * rotY * translationMatrix;
        Matrix4x4 z_inverse = new Matrix4x4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, -1, 0),
            new Vector4(0, 0, 0, 1)
        );
        Matrix4x4 viewMatrix = z_inverse * worldToLocalMatrix;
        return viewMatrix;
    }

    public  Matrix4x4 GetProjectionMatrix()
    {
        //Matrix4x4 projectionMatrix = Camera.main.projectionMatrix;
        float fov,  aspect,  near,  far;
        fov = Camera.main.fieldOfView;
        aspect = (float)Screen.width/Screen.height;
        near = Camera.main.nearClipPlane;
        far = Camera.main.farClipPlane;

        Matrix4x4 projectionMat = Matrix4x4.zero;
        float fax = 1 / Mathf.Tan((fov / 2) * (Mathf.PI / 180));

        projectionMat[0, 0] = (float)(fax / aspect);
        projectionMat[1, 1] = (float)(fax);
        projectionMat[2, 2] = -(far + near) / (far - near);
        projectionMat[2, 3] = -(2 * far * near) / (far - near);
        projectionMat[3, 2] = -1;

        return projectionMat;
    }

    public Vector4 PerspectiveDivide(Vector4 clippedVertices) {
        clippedVertices.x *= 1/clippedVertices.w;
        clippedVertices.y *= 1/clippedVertices.w;
        clippedVertices.z *= 1/clippedVertices.w;
        clippedVertices.w *= 1/clippedVertices.w;
        return clippedVertices;
    }

    public Vector4 ViewportTransform(Vector4 vertices) {
        //原本公式
        //vertices.x = (vertices.x * Screen.width) / (2 * vertices.w) + Screen.width / 2;
        //vertices.y = (vertices.y * Screen.height) / (2 * vertices.w) + Screen.height / 2;

        //优化后的公式
        vertices.x = ((vertices.x + 1) * Screen.width * 0.5f);
        vertices.y = ((vertices.y + 1) * Screen.height * 0.5f);
        return vertices;
    }
}
