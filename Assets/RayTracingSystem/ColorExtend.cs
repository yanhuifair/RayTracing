using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static public class ColorExtend
{
    static public Vector3 ToVector3(this Color color)
    {
        return new Vector3(color.r, color.g, color.b);
    }
}