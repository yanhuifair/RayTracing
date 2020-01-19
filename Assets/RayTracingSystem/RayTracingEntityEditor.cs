using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RayTracingEntity))]
public class RayTracingEntityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var rayTracingEntity = target as RayTracingEntity;
        EditorGUI.BeginChangeCheck();
        rayTracingEntity.entityType = GUILayout.Toolbar(rayTracingEntity.entityType.ToInt(), EnumExtension.ToStringArray<RayTracingEntity.EntityType>()).ToEnum<RayTracingEntity.EntityType>();

        switch (rayTracingEntity.entityType)
        {
            case RayTracingEntity.EntityType.Mesh:
                {
                    rayTracingEntity.mesh = EditorGUILayout.ObjectField("Mesh", rayTracingEntity.mesh, typeof(Mesh), false) as Mesh;
                }
                break;
            case RayTracingEntity.EntityType.Sphere:
                {
                    rayTracingEntity.radius = EditorGUILayout.FloatField("Radius", rayTracingEntity.radius);
                }
                break;
            case RayTracingEntity.EntityType.Box:
                {
                    rayTracingEntity.boxSize = EditorGUILayout.Vector3Field("Box Size", rayTracingEntity.boxSize);
                }
                break;
            case RayTracingEntity.EntityType.Fog:
                {

                }
                break;
        }

        EditorGUILayout.Space();
        rayTracingEntity.rayTracingMaterial = EditorGUILayout.ObjectField("Material", rayTracingEntity.rayTracingMaterial, typeof(RayTracingMaterial), true) as RayTracingMaterial;
        if (rayTracingEntity.rayTracingMaterial != null)
        {
            EditorGUILayout.BeginVertical("helpbox");
            rayTracingEntity.rayTracingMaterial.albedo = EditorGUILayout.ColorField("Albedo", rayTracingEntity.rayTracingMaterial.albedo);
            rayTracingEntity.rayTracingMaterial.specular = EditorGUILayout.ColorField("Specular", rayTracingEntity.rayTracingMaterial.specular);
            rayTracingEntity.rayTracingMaterial.emission = EditorGUILayout.ColorField("Emission", rayTracingEntity.rayTracingMaterial.emission);
            rayTracingEntity.rayTracingMaterial.smoothness = EditorGUILayout.Slider("Smoothness", rayTracingEntity.rayTracingMaterial.smoothness, 0, 1);
            EditorGUILayout.EndVertical();
        }
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Entity Changed");
            rayTracingEntity.attributeChanged = true;
        }

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.Toggle("Changed", rayTracingEntity.attributeChanged);
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }
}