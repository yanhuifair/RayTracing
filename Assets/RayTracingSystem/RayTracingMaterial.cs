using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Ray Tracing/Material")]
public class RayTracingMaterial : ScriptableObject
{
    public Texture2D texture2D;
    public Color albedo = Color.white;
    public Color specular = Color.black;
    [Range(0, 1)] public float smoothness = 0;
    [ColorUsageAttribute(false, true)] public Color emission = Color.black;
}