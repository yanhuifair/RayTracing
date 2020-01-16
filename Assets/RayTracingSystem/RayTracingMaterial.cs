using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Ray Tracing/Material")]
public class RayTracingMaterial : ScriptableObject
{
    [SerializeField] public Color albedo = Color.white;
    [SerializeField] public Color specular = Color.black;
    [SerializeField] public float smoothness = 0;
    [SerializeField] public Color emission = Color.black;
}