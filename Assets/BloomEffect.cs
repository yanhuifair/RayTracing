using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class BloomEffect : PostEffect
{
    [Range(0, 10)] public float threshold = 2;
    [Range(0, 1)] public float softThreshold = 0.5f;
    [Range(1, 10)] public int iterations = 10;
    [Range(0, 1)] public float intensity = 1;
    float iterationsRatios = 1;
    public bool debug;
    const int BoxDownPrefilterPass = 0;
    const int BoxDownPass = 1;
    const int BoxUpPass = 2;
    const int ApplyBloomPass = 3;
    const int DebugBloomPass = 4;
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (material == null)
        {
            material = new Material(shader);
            material.hideFlags = HideFlags.HideAndDontSave;
        }
        material.SetFloat("_Threshold", threshold);
        material.SetFloat("_SoftThreshold", softThreshold);

        iterationsRatios = Screen.height <= Screen.width?Screen.height / 1080.0f : 1920.0f / Screen.width;
        material.SetFloat("_IterationsRatios", iterationsRatios);

        float knee = threshold * softThreshold;
        Vector4 filter;
        filter.x = threshold;
        filter.y = filter.x - knee;
        filter.z = 2f * knee;
        filter.w = 0.25f / (knee + 0.000001f);
        material.SetVector("_Filter", filter);

        material.SetFloat("_Intensity", Mathf.GammaToLinearSpace(intensity));

        int width = source.width;
        int height = source.height;
        RenderTextureFormat format = source.format;

        RenderTexture[] textures = new RenderTexture[iterations];

        RenderTexture currentDestination = textures[0] = RenderTexture.GetTemporary(width, height, 0, format);
        Graphics.Blit(source, currentDestination, material, BoxDownPrefilterPass);

        RenderTexture currentSource = currentDestination;
        int i = 1;
        for (; i < iterations; i++)
        {
            width /= 2;
            height /= 2;
            if (height < 2) break;
            currentDestination = textures[i] = RenderTexture.GetTemporary(width, height, 0, format);
            Graphics.Blit(currentSource, currentDestination, material, BoxDownPass);
            currentSource = currentDestination;
        }
        for (i -= 2; i >= 0; i--)
        {
            currentDestination = textures[i];
            textures[i] = null;
            Graphics.Blit(currentSource, currentDestination, material, BoxUpPass);
            RenderTexture.ReleaseTemporary(currentSource);
            currentSource = currentDestination;
        }
        if (debug)
        {
            Graphics.Blit(currentSource, destination, material, DebugBloomPass);
        }
        else
        {
            material.SetTexture("_SourceTex", source);
            Graphics.Blit(currentSource, destination, material, ApplyBloomPass);
        }
        RenderTexture.ReleaseTemporary(currentSource);
    }
}