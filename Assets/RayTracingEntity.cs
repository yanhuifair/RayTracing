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
    }
    private void OnEnable()
    {
        var list = GameObject.FindObjectOfType<RayTracingCamera>().RayTracingEntities;
        if (!list.Contains(this))
            list.Add(this);
    }

    private void OnDisable()
    {
        var list = GameObject.FindObjectOfType<RayTracingCamera>().RayTracingEntities;
        if (list.Contains(this))
            list.Remove(this);
    }

    [EnumToggleButtons] public EntityType entityType;
    [Header("Sphere")]
    [ShowIf("entityType", EntityType.Sphere)] public float radius = 1;

    [Header("Render")]
    [ColorUsageAttribute(false, false)] public Color albedo = Color.gray;
    [ColorUsageAttribute(false, false)] public Color specular = Color.gray;
    [Range(0, 1)] public float smoothness;
    [ColorUsageAttribute(false, true)] public Color emission;
}