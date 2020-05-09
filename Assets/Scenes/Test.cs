using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
public class Test : MonoBehaviour
{
    public RayTracingMaterial material;
    [Button]
    void FindMaterial()
    {
        var mats = (RayTracingMaterial[]) Resources.FindObjectsOfTypeAll(typeof(RayTracingMaterial));
        foreach (var item in mats)
        {
            if (material.GetInstanceID() == item.GetInstanceID())
            {
                Debug.Log($"{item.name} {item.GetInstanceID()}");
            }
        }
    }
}