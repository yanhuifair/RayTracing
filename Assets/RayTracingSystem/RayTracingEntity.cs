using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
[ExecuteInEditMode]
public class RayTracingEntity : MonoBehaviour
{
    public enum EntityType
    {
        Mesh,
        Fog,
        Sphere,
        Box
    }
    // private void OnEnable()
    // {
    //     var list = GameObject.FindObjectOfType<RayTracingCamera>().RayTracingEntities;
    //     if (!list.Contains(this))
    //         list.Add(this);
    // }

    [EnumToggleButtons] public EntityType entityType;
    [Header("Sphere")]
    [ShowIf("entityType", EntityType.Sphere)] public float radius = 1;
    [ShowIf("entityType", EntityType.Box)] public Vector3 boxSize = Vector3.one;

    [Header("Render")]
    [ColorUsageAttribute(false, false)] public Color albedo = Color.gray;
    [ColorUsageAttribute(false, false)] public Color specular = Color.gray;
    [Range(0, 1)] public float smoothness;
    [ColorUsageAttribute(false, true)] public Color emission;
}