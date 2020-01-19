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
        Mesh,
        Fog,
        Sphere,
        Box
    }

    [OnValueChanged("AttributeChanged")][EnumToggleButtons] public EntityType entityType = EntityType.Sphere;
    [OnValueChanged("AttributeChanged")][ShowIf("entityType", EntityType.Sphere)] public float radius = 1;
    [OnValueChanged("AttributeChanged")][ShowIf("entityType", EntityType.Box)] public Vector3 boxSize = Vector3.one;

    [OnValueChanged("AttributeChanged")] public RayTracingMaterial rayTracingMaterial;

    //
    [ReadOnly][SerializeField] bool attributeChanged = false;

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
        if (entityType == EntityType.Mesh && GetBounds() != null)
        {
            Gizmos.DrawWireCube(((Bounds) GetBounds()).center, ((Bounds) GetBounds()).size);
        }
    }

    public Bounds? GetBounds()
    {
        var bounds = transform.GetComponent<MeshRenderer>()?.bounds;
        return bounds;
    }
}