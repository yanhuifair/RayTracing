using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum MinMaxAttributeType
{
    Float,
    Int
}

public class MinMaxAttribute : PropertyAttribute
{
    public MinMaxAttributeType type;
    public float? min = null;
    public float? max = null;
    public MinMaxAttribute(MinMaxAttributeType type)
    {
        this.type = type;
    }
    public MinMaxAttribute(MinMaxAttributeType type, float min, float max)
    {
        this.type = type;
        this.min = min;
        this.max = max;
    }
}

[CustomPropertyDrawer(typeof(MinMaxAttribute))]
public class MinMaxDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        if (property.propertyType == SerializedPropertyType.Vector4)
        {
            MinMaxAttribute minmax = attribute as MinMaxAttribute;
            var vector4Value = property.vector4Value;

            //Field
            if (minmax.min != null && minmax.max != null)
            {
                vector4Value = EditorGUI.Vector4Field(position, label, new Vector4((float) minmax.min, vector4Value.y, vector4Value.z, (float) minmax.max));
                vector4Value.y = Mathf.Clamp(vector4Value.y, (float) minmax.min, property.vector4Value.z);
                vector4Value.z = Mathf.Clamp(vector4Value.z, property.vector4Value.y, (float) minmax.max);

                position.yMin += EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode?1 : 2);

                EditorGUI.MinMaxSlider(position,
                    ref vector4Value.y, ref vector4Value.z,
                    (float) minmax.min, (float) minmax.max
                );
            }
            else
            {
                vector4Value = EditorGUI.Vector4Field(position, label, vector4Value);
                vector4Value.y = Mathf.Clamp(vector4Value.y, property.vector4Value.x, property.vector4Value.z);
                vector4Value.z = Mathf.Clamp(vector4Value.z, property.vector4Value.y, property.vector4Value.w);

                position.yMin += EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode?1 : 2);

                EditorGUI.MinMaxSlider(position,
                    ref vector4Value.y, ref vector4Value.z,
                    vector4Value.x, vector4Value.w);
                vector4Value.y = Mathf.Clamp(vector4Value.y, vector4Value.x, vector4Value.w);
            }

            //Type
            if (minmax.type == MinMaxAttributeType.Float)
            {
                property.vector4Value = vector4Value;
            }
            else if (minmax.type == MinMaxAttributeType.Int)
            {
                property.vector4Value = new Vector4(
                    Mathf.RoundToInt(vector4Value.x),
                    Mathf.RoundToInt(vector4Value.y),
                    Mathf.RoundToInt(vector4Value.z),
                    Mathf.RoundToInt(vector4Value.w));
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int totalLine = 2 + (EditorGUIUtility.wideMode?0 : 1);
        return EditorGUIUtility.singleLineHeight * totalLine;
    }
}