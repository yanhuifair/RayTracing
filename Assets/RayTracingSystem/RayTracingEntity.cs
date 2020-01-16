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

    [OnValueChanged("AttributeChanged")][EnumToggleButtons] public EntityType entityType = EntityType.Sphere;
    [OnValueChanged("AttributeChanged")][ShowIf("entityType", EntityType.Sphere)] public float radius = 1;
    [OnValueChanged("AttributeChanged")][ShowIf("entityType", EntityType.Box)] public Vector3 boxSize = Vector3.one;

    [Title("Render")]
    [OnValueChanged("AttributeChanged")][ColorUsageAttribute(false, false)] public Color albedo = Color.white;
    [OnValueChanged("AttributeChanged")][ColorUsageAttribute(false, false)] public Color specular = Color.black;
    [OnValueChanged("AttributeChanged")][Range(0, 1)] public float smoothness = 0;
    [OnValueChanged("AttributeChanged")][ColorUsageAttribute(false, true)] public Color emission = Color.black;

    public RayTracingMaterial rayTracingMaterial;

    [ReadOnly][SerializeField] bool attributeChanged = false;

    public void AttributeChanged()
    {
        attributeChanged = true;
    }

    public bool AnyChanged()
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
        //bounds = transform.GetComponent<MeshFilter>()?.mesh?.bounds;
        return bounds;
    }

}