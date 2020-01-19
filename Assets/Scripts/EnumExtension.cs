using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public static class EnumExtension
{
    public static int ToInt(this System.Enum e)
    {
        return e.GetHashCode();
    }

    public static T ToEnum<T>(this int value)
    {
        Type type = typeof(T);
        return (T) Enum.ToObject(type, value);
    }

    public static string[] ToStringArray<T>()
    {
        return Enum.GetNames(typeof(T));
    }
}