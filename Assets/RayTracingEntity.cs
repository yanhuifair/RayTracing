using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public class RayTracingEntity : MonoBehaviour
{
    public enum EntityType
    {
        Mesh,
        Sphere,
    }

    [EnumToggleButtons] public EntityType entityType;
    [Header("Sphere")]
    public float radius = 1;

    [Header("Render")]
    [ColorUsageAttribute(false, false)] public Color albedo = Color.gray;
    [ColorUsageAttribute(false, false)] public Color specular = Color.gray;
    [Range(0, 1)] public float smoothness;
    [ColorUsageAttribute(false, true)] public Color emission;
}