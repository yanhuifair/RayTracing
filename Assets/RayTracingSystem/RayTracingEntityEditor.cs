using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RayTracingEntity))]
public class RayTracingEntityEditor : Editor
{
    SerializedProperty entityType;
    SerializedProperty radius;
    SerializedProperty boxSize;
    SerializedProperty rayTracingMaterial;
    SerializedProperty attributeChanged;

    void OnEnable()
    {
        entityType = serializedObject.FindProperty("entityType");
        radius = serializedObject.FindProperty("radius");
        boxSize = serializedObject.FindProperty("boxSize");
        rayTracingMaterial = serializedObject.FindProperty("rayTracingMaterial");
        attributeChanged = serializedObject.FindProperty("attributeChanged");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(entityType);
        EditorGUILayout.PropertyField(radius);
        EditorGUILayout.PropertyField(boxSize);
        EditorGUILayout.PropertyField(rayTracingMaterial);
        // if (rayTracingMaterial.)

        EditorGUILayout.PropertyField(attributeChanged);

        serializedObject.ApplyModifiedProperties();
    }
}