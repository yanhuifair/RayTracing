using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransfromExtension
{
    public static void SetPositionX(this Transform transform, float value)
    {
        transform.position = new Vector3(value, transform.position.y, transform.position.z);
    }
    public static void SetPositionY(this Transform transform, float value)
    {
        transform.position = new Vector3(transform.position.x, value, transform.position.z);
    }
    public static void SetPositionZ(this Transform transform, float value)
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, value);
    }

    public static Quaternion RotationEulerAxis(this Quaternion quaternion, Vector3 axis, float angle)
    {
        Quaternion rotationAngel = Quaternion.Euler(axis * angle);
        Vector3 euler = quaternion.eulerAngles;
        euler = rotationAngel * euler;

        Quaternion result = Quaternion.Euler(euler);
        return result;
    }

    public static Quaternion ToQuaternion(this Vector3 vector3)
    {
        Quaternion quaternion = Quaternion.Euler(vector3);
        return quaternion;
    }

    public static bool IsApproximate(this Vector3 v1, Vector3 v2, float precision = Vector3.kEpsilon)
    {
        bool equal = true;

        if (Mathf.Abs(v1.x - v2.x) > precision) equal = false;
        if (Mathf.Abs(v1.y - v2.y) > precision) equal = false;
        if (Mathf.Abs(v1.z - v2.z) > precision) equal = false;

        return equal;
    }

    public static bool IsApproximate(this Quaternion q1, Quaternion q2, float precision = Quaternion.kEpsilon)
    {
        return Mathf.Abs(Quaternion.Dot(q1, q2)) >= 1 - precision;
    }
}