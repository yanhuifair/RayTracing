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

    [OnValueChanged("AttributeChanged")][EnumToggleButtons] public EntityType entityType;
    [Header("Sphere")]
    [OnValueChanged("AttributeChanged")][ShowIf("entityType", EntityType.Sphere)] public float radius = 1;
    [OnValueChanged("AttributeChanged")][ShowIf("entityType", EntityType.Box)] public Vector3 boxSize = Vector3.one;

    [Header("Render")]
    [OnValueChanged("AttributeChanged")][ColorUsageAttribute(false, false)] public Color albedo = Color.gray;
    [OnValueChanged("AttributeChanged")][ColorUsageAttribute(false, false)] public Color specular = Color.gray;
    [OnValueChanged("AttributeChanged")][Range(0, 1)] public float smoothness;
    [OnValueChanged("AttributeChanged")][ColorUsageAttribute(false, true)] public Color emission;

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
}