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
                    if (rayTracingEntity.mesh)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"Triangles:  {rayTracingEntity.mesh.triangles.Length/3}");
                        EditorGUILayout.LabelField($"Vertices:  {rayTracingEntity.mesh.vertices.Length}");
                        EditorGUILayout.EndHorizontal();
                    }
                }
                break;
            case RayTracingEntity.EntityType.Sphere:
                {
                    rayTracingEntity.radius = EditorGUILayout.FloatField("Radius", rayTracingEntity.radius);
                }
                break;
            case RayTracingEntity.EntityType.Box:
                {
                    rayTracingEntity.boxSize = EditorGUILayout.Vector3Field("Size", rayTracingEntity.boxSize);
                }
                break;
            case RayTracingEntity.EntityType.Fog:
                {
                    rayTracingEntity.boxSize = EditorGUILayout.Vector3Field("Size", rayTracingEntity.boxSize);
                }
                break;
        }

        EditorGUILayout.Space();
        rayTracingEntity.rayTracingMaterial = EditorGUILayout.ObjectField("Material", rayTracingEntity.rayTracingMaterial, typeof(RayTracingMaterial), true) as RayTracingMaterial;
        if (rayTracingEntity.rayTracingMaterial != null)
        {
            EditorGUILayout.BeginVertical();
            // EditorGUI.BeginChangeCheck();
            rayTracingEntity.rayTracingMaterial.albedo = EditorGUILayout.ColorField(new GUIContent("Albedo"), rayTracingEntity.rayTracingMaterial.albedo, true, false, false);
            rayTracingEntity.rayTracingMaterial.specular = EditorGUILayout.ColorField(new GUIContent("Specular"), rayTracingEntity.rayTracingMaterial.specular, true, false, false);
            rayTracingEntity.rayTracingMaterial.emission = EditorGUILayout.ColorField(new GUIContent("Emission"), rayTracingEntity.rayTracingMaterial.emission, true, false, true);
            rayTracingEntity.rayTracingMaterial.smoothness = EditorGUILayout.Slider("Smoothness", rayTracingEntity.rayTracingMaterial.smoothness, 0, 1);
            // if (EditorGUI.EndChangeCheck())
            // {
            //     rayTracingEntity.attributeChanged = true;
            //     EditorUtility.SetDirty(rayTracingEntity.rayTracingMaterial);
            // }
            EditorGUILayout.EndVertical();
        }
        if (EditorGUI.EndChangeCheck())
        {
            rayTracingEntity.attributeChanged = true;
            Undo.RecordObject(rayTracingEntity, "Entity Changed");
            EditorUtility.SetDirty(rayTracingEntity);
        }

        serializedObject.ApplyModifiedProperties();
    }

    public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
    {

    }
}