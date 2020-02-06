using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
[AddComponentMenu("Ray Tracing/Ray Tracing Entity")]
public class RayTracingEntity : MonoBehaviour
{
    public enum EntityType
    {
        Mesh = 0,
        Sphere = 1,
        Box = 2,
        Fog = 3,
    }

    public EntityType entityType = EntityType.Sphere;
    public float radius = 1;
    public Vector3 boxSize = Vector3.one;
    public Mesh mesh;

    public RayTracingMaterial rayTracingMaterial;
    public bool attributeChanged = false;

    public void AttributeChanged()
    {
        attributeChanged = true;
    }

    public bool isAnyChanged()
    {
        return transform.hasChanged || attributeChanged;
    }

    public void ResetChanged()
    {
        transform.hasChanged = false;
        attributeChanged = false;
    }
    private void OnDrawGizmos()
    {
        if (entityType == EntityType.Mesh && mesh != null)
        {
            Gizmos.color = Color.white;
            var matrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireMesh(mesh, Vector3.zero, Quaternion.identity);
            Gizmos.matrix = matrix;
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (entityType == EntityType.Mesh && mesh != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }

    public Bounds bounds
    {
        get
        {
            return GeometryUtility.CalculateBounds(mesh.vertices, transform.localToWorldMatrix);
        }
    }
}