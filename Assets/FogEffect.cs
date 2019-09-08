using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum ClipMode
{
    Distance = 1,
    Depth = 2,
}

[RequireComponent(typeof(Camera))]
[ImageEffectAllowedInSceneView]
[ExecuteInEditMode]
public class FogEffect : MonoBehaviour
{
    Camera cam;
    [HideInInspector] public Shader shader;
    Material mat;
    Material material
    {
        get
        {
            if (mat == null)
            {
                mat = new Material(shader);
            }
            return mat;
        }
        set
        {
            mat = value;
        }
    }
    public Gradient gradient;

    public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
    public ClipMode clipMode = ClipMode.Depth;

    [Header("Distance Fog")]
    public bool distanceFog = true;
    public float fogNear = 0;
    public float fogFar = 50;
    [Range(0, 1)]
    public float densityFar = 1;

    [Header("Height Fog")]
    public bool heightFog = false;
    public float fogLow = 2;
    public float fogHeight = -20;
    [Range(0, 1)]
    public float densityHeight = 1;

    Texture2D gradientTex;
    Texture2D curveTex;

    [Header("Noise")]
    [Range(0, 25)] public float denoise = 5;
    public float time = 1000;
    void OnEnable()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode = DepthTextureMode.Depth;
    }

    // [ImageEffectOpaque]
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if ((!distanceFog && !heightFog))
        {
            Graphics.Blit(source, destination);
            return;
        }
        CheckTex();
        material.SetTexture("_GradientTex", gradientTex);
        material.SetTexture("_CurveTex", curveTex);

        material.SetFloat("_FogNear", fogNear);
        material.SetFloat("_FogFar", fogFar);
        material.SetFloat("_FarMul", densityFar);

        material.SetFloat("_FogLow", fogLow);
        material.SetFloat("_FogHeight", fogHeight);
        material.SetFloat("_HeightMul", densityHeight);

        Transform camtr = cam.transform;
        Vector3[] frustumCorners = new Vector3[4];
        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane, cam.stereoActiveEye, frustumCorners);
        var bottomLeft = camtr.TransformVector(frustumCorners[0]);
        var topLeft = camtr.TransformVector(frustumCorners[1]);
        var topRight = camtr.TransformVector(frustumCorners[2]);
        var bottomRight = camtr.TransformVector(frustumCorners[3]);
        Matrix4x4 frustumCornersArray = Matrix4x4.identity;
        frustumCornersArray.SetRow(0, bottomLeft);
        frustumCornersArray.SetRow(1, bottomRight);
        frustumCornersArray.SetRow(2, topLeft);
        frustumCornersArray.SetRow(3, topRight);

        material.SetMatrix("_FrustumCornersWS", frustumCornersArray);
        material.SetVector("_CameraWS", camtr.position);
        material.SetInt("_clipMode", (int) clipMode);

        material.SetInt("_useDisFog", distanceFog == true ? 1 : 0);
        material.SetInt("_useHeiFog", heightFog == true ? 1 : 0);

        material.SetFloat("_dpi", Screen.dpi);
        material.SetFloat("_time", time);
        material.SetFloat("_denoise", denoise);

        Graphics.Blit(source, destination, material);
    }

    void CheckTex()
    {
        if (gradientTex == null)
        {
            gradientTex = new Texture2D(256, 1);
            gradientTex.wrapMode = TextureWrapMode.Clamp;
            gradientTex.filterMode = FilterMode.Bilinear;
            UpdateGradient();
        }

        if (curveTex == null)
        {
            curve.preWrapMode = WrapMode.Clamp;
            curve.postWrapMode = WrapMode.Clamp;
            curveTex = new Texture2D(256, 1);
            curveTex.wrapMode = TextureWrapMode.Clamp;
            curveTex.filterMode = FilterMode.Bilinear;
            UpdateCurve();
        }
    }

    void UpdateGradient()
    {
        if (gradientTex != null)
        {
            for (int i = 0; i < gradientTex.width; i++)
            {
                if (gradientTex != null && gradient != null)
                {
                    float percent = (float) i / (float) gradientTex.width;
                    Color pixelColor = gradient.Evaluate(percent);
                    gradientTex.SetPixel(i, 0, pixelColor);
                }
            }
            gradientTex.Apply();
        }
    }

    void UpdateCurve()
    {

        if (curveTex != null)
        {
            for (int i = 0; i < curveTex.width; i++)
            {
                Color pixelColor = new Color(1, 1, 1, curve.Evaluate((float) i / curveTex.width));
                curveTex.SetPixel(i, 0, pixelColor);
            }
            curveTex.Apply();
        }
    }
    private void OnValidate()
    {
        UpdateGradient();
        UpdateCurve();
#if UNITY_EDITOR
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif
    }
}