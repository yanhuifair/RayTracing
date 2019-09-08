﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[ExecuteInEditMode]

public enum EntityType
{
    Sphere,
    Plane,
    Mesh,
}
public class RayTracingEntity : MonoBehaviour
{
    public EntityType entityType;
    [Header("Sphere")]
    public float radius = 1;

    [Header("Render")]
    [ColorUsageAttribute(false, false)] public Color albedo = Color.gray;
    [ColorUsageAttribute(false, false)] public Color specular = Color.gray;
    [Range(0, 1)] public float smoothness;
    [ColorUsageAttribute(false, true)] public Color emission;
}