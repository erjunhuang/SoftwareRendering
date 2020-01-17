using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class RenderingObject : MonoBehaviour
{
    public Texture2D albedoTexture, aoTexture, roughTexture, metalTexture, normalTexture;
    // Start is called before the first frame update
    void OnEnable()
    {
        RenderingMaster.RegisterObject(this);
    }

    // Update is called once per frame
    void OnDisable()
    {
        RenderingMaster.UnregisterObject(this);
    }
}