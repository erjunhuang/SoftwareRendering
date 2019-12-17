using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class RenderingObject : MonoBehaviour
{
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
