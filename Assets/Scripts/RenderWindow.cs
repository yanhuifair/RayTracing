using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
public class RenderWindow : EditorWindow
{
    static ComputeShader RayTracingComputeShader;
    static int SamplePerPixel;
    static int depth = 3;
    static Vector2Int resolution = new Vector2Int(1280, 720);
    static Texture2D skyBoxTexture;
    static RenderTexture renderTexture;
    static Camera _camera;
    static Camera CameraCurrent
    {
        get
        {
            if (EditorApplication.isPlaying)
            {
                _camera = Camera.main;
            }
            else
            {
                _camera = SceneView.lastActiveSceneView.camera;
            }
            return _camera;
        }
    }

    static RayTracingSystem rayTracingSystem = new RayTracingSystem();

    static RenderWindow window;
    static RenderWindow WINDOW
    {
        get
        {
            if (window == null) window = (RenderWindow) EditorWindow.GetWindow(typeof(RenderWindow));
            return window;
        }
    }

    [MenuItem("Ray Tracing/Render Window")]
    static void Init()
    {
        WINDOW.Show();
    }

    private void OnEnable()
    {
        Debug.Log("OnEnable");
        timeAdd = 0;
        EditorApplication.update += Upadte;
    }

    private void OnDisable()
    {
        Debug.Log("OnDisable");
        EditorApplication.update -= Upadte;
        rayTracingSystem.Release();
    }

    void OnGUI()
    {

        if (renderTexture)
        {
            var ratio = (float) renderTexture.width / renderTexture.height;
            var TextureRect = new Rect(0, position.height - position.width / ratio, position.width, position.width / ratio);
            EditorGUI.DrawPreviewTexture(TextureRect, renderTexture);
        }

        RayTracingComputeShader = EditorGUILayout.ObjectField("ComputeShader", RayTracingComputeShader, typeof(ComputeShader), false) as ComputeShader;
        skyBoxTexture = EditorGUILayout.ObjectField("SkyBox Texture", skyBoxTexture, typeof(Object), false) as Texture2D;
        SamplePerPixel = EditorGUILayout.IntSlider("SamplePerPixel", SamplePerPixel, 1, 10);
        depth = EditorGUILayout.IntSlider("Depth", depth, 1, 10);

        GUILayout.BeginHorizontal();
        resolution = EditorGUILayout.Vector2IntField("Resolution", resolution);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Sample Count", rayTracingSystem.layerCount.ToString());
        if (GUILayout.Button("Reset"))
        {
            rayTracingSystem.needReset += 1;
        }
        if (GUILayout.RepeatButton("Interation"))
        {
            Interation();
        }
        GUILayout.EndHorizontal();

    }

    static UnityEngine.Vector3 positionLast;
    static UnityEngine.Quaternion quaternionLast;
    static Vector3 lastSize;

    static float timeMax = 0.01f;
    static float timeAdd;
    static void Upadte()
    {
        resolution = new Vector2Int(
            (int) SceneView.lastActiveSceneView.position.width,
            (int) SceneView.lastActiveSceneView.position.height);

        if (!lastSize.IsApproximate(new Vector3(resolution.x, resolution.y, 0), 1))
        {
            lastSize = new Vector3(resolution.x, resolution.y, 0);
            rayTracingSystem.needReset += 1;
        }

        timeAdd += 1 / 100.0f;
        if (timeAdd >= timeMax)
        {
            timeAdd = 0;
            Interation();
        }
    }

    void AngChanged()
    {

    }

    static void Interation()
    {

        if (CameraCurrent != null && RayTracingComputeShader != null)
        {
            if (!CameraCurrent.transform.position.IsApproximate(positionLast, 0.01f)
                || !CameraCurrent.transform.rotation.IsApproximate(quaternionLast)

            )
            {
                positionLast = CameraCurrent.transform.position;
                quaternionLast = CameraCurrent.transform.rotation;
                rayTracingSystem.needReset += 1;
                // AngChanged();
            }

            rayTracingSystem.RayTracingComputeShader = RayTracingComputeShader;
            rayTracingSystem.camera = CameraCurrent;
            rayTracingSystem.resolution = resolution;
            rayTracingSystem.skyBoxTexture = skyBoxTexture;
            rayTracingSystem.samplePerPixel = SamplePerPixel;
            rayTracingSystem.depth = depth;
            renderTexture = rayTracingSystem.Render();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }

}