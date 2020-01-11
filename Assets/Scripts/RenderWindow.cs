using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
public class RenderWindow : EditorWindow
{
    ComputeShader RayTracingComputeShader;
    int SamplePerPixel;
    int depth;
    Vector2Int resolution = new Vector2Int(1280, 720);
    Texture2D skyBoxTexture;
    RenderTexture renderTexture;
    Camera _camera;
    Camera CameraCurrent
    {
        get
        {
            if (EditorApplication.isPlaying)
            {
                _camera = Camera.main;
            }
            else
            {
                // _camera = GameObject.Find("SceneCamera").GetComponent<Camera>();
                _camera = SceneView.lastActiveSceneView.camera;
            }
            return _camera;
        }
    }

    RayTracingSystem rayTracingSystem = new RayTracingSystem();

    RenderWindow window;
    [MenuItem("Ray Tracing/Render Window")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        RenderWindow window = (RenderWindow) EditorWindow.GetWindow(typeof(RenderWindow));
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update += Upadte;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Upadte;
        rayTracingSystem.Release();
    }

    void OnGUI()
    {
        RayTracingComputeShader = EditorGUILayout.ObjectField("ComputeShader", RayTracingComputeShader, typeof(ComputeShader), false) as ComputeShader;
        skyBoxTexture = EditorGUILayout.ObjectField("SkyBox Texture", skyBoxTexture, typeof(Object), false) as Texture2D;
        SamplePerPixel = EditorGUILayout.IntSlider("SamplePerPixel", SamplePerPixel, 1, 10);
        depth = EditorGUILayout.IntSlider("Depth", depth, 1, 10);
        resolution = EditorGUILayout.Vector2IntField("Resolution", resolution);

        if (renderTexture)
        {
            var ratio = (float) renderTexture.width / renderTexture.height;
            var TextureRect = new Rect(0, position.height - position.width / ratio, position.width, position.width / ratio);
            EditorGUI.DrawPreviewTexture(TextureRect, renderTexture);
        }
        EditorGUILayout.ObjectField("SkyBox Texture", renderTexture, typeof(Object), true);

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(rayTracingSystem.layerCount.ToString());
        if (GUILayout.Button("Reset"))
        {
            rayTracingSystem.needReset += 1;
        }
        GUILayout.EndHorizontal();

        if (GUILayout.RepeatButton("Interation"))
        {
            Interation();
        }
    }

    UnityEngine.Vector3 positionLast;
    UnityEngine.Quaternion quaternionLast;
    Vector3 lastSize;
    void Upadte()
    {
        resolution = new Vector2Int(
            (int) SceneView.lastActiveSceneView.position.width,
            (int) SceneView.lastActiveSceneView.position.height);
        if (!lastSize.IsApproximate(new Vector3(resolution.x, resolution.y, 0), 1))
        {
            lastSize = new Vector3(resolution.x, resolution.y, 0);
            rayTracingSystem.needReset += 1;
        }
    }

    void AngChanged()
    {

    }

    void Interation()
    {
        if (CameraCurrent != null && RayTracingComputeShader != null)
        {
            if (!CameraCurrent.transform.position.IsApproximate(positionLast, 0.001f)
                || !CameraCurrent.transform.rotation.IsApproximate(quaternionLast, 0.001f)

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