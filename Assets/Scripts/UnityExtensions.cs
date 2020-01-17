using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UnityExtensions
{
    public static Vector4 Vector3ToVector4(this UnityEngine.Vector3 vector3) {

        return new Vector4(vector3.x, vector3.y, vector3.z, 1);
    }

    public static Vector3 Vector3MultiplyVector3(this UnityEngine.Vector3 vector3,Vector3 otherVector3)
    {

        return new Vector3(vector3.x * otherVector3.x, vector3.y * otherVector3.y, vector3.z * otherVector3.z);
    }

    public static bool IsDestroyed(this UnityEngine.Object obj)
    {
        return obj == null && !ReferenceEquals(obj, null);
    }
}