using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class ToneMapping : PostEffect
{
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (material == null)
        {
            material = new Material(shader);
        }
        Graphics.Blit(src, dest, material);
    }
}