using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ImageEffectAllowedInSceneView, ExecuteInEditMode]
public class PostEffect : MonoBehaviour
{
    public Shader shader;
    protected Material material;
}