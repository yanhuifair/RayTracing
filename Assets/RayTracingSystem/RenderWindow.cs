using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RenderWindow : EditorWindow
{
    static ComputeShader RayTracingComputeShader;
    static int samplePerPixel = 1;
    static int bounces = 3;
    static Vector2Int resolution = new Vector2Int(1280, 720);
    static Texture2D skyBoxTexture;
    static RenderTexture renderTexture;
    static Camera camera;
    static List<Camera> cameras = new List<Camera>();
    static int cameraIndex = 0;

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

    static string path;

    [MenuItem("Ray Tracing/Render Window")]
    static void Init()
    {
        WINDOW.Show();
    }

    private void OnEnable()
    {
        timeAdd = 0;
        EditorApplication.update += Upadte;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Upadte;
        rayTracingSystem.Release();
    }

    void OnGUI()
    {
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float singleLineHeight = EditorGUIUtility.singleLineHeight;

        GUILayout.BeginHorizontal();
        RayTracingComputeShader = EditorGUILayout.ObjectField("ComputeShader", RayTracingComputeShader, typeof(ComputeShader), false) as ComputeShader;
        skyBoxTexture = EditorGUILayout.ObjectField("SkyBox Texture", skyBoxTexture, typeof(Object), false) as Texture2D;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        samplePerPixel = EditorGUILayout.IntSlider("SamplePerPixel", samplePerPixel, 1, 1000);
        bounces = EditorGUILayout.IntSlider("Bounces", bounces, 1, 10);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        var x = EditorGUILayout.IntField("Width", resolution.x);
        var y = EditorGUILayout.IntField("Height", resolution.y);
        resolution = new Vector2Int(x, y);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Cameras"))
        {
            cameras.Clear();

            var camerasFinded = Resources.FindObjectsOfTypeAll(typeof(Camera)) as Camera[];
            foreach (var camera in camerasFinded)
            {
                if (camera.gameObject.scene == null) continue;
                if (camera.gameObject.name == "Preview Scene Camera") continue;
                if (cameras.Contains(camera)) continue;

                cameras.Add(camera);
                rayTracingSystem.needReset += 1;
            }
        }
        if (cameras.Count > 0)
        {
            string[] cameraNames = new string[cameras.Count];
            for (int i = 0; i < cameras.Count(); i++)
            {
                if (cameras[i] != null) cameraNames[i] = cameras[i].name;
            }
            EditorGUI.BeginChangeCheck();
            cameraIndex = EditorGUILayout.Popup(cameraIndex, cameraNames);
            if (EditorGUI.EndChangeCheck())
            {
                camera = cameras[cameraIndex];
                rayTracingSystem.needReset += 1;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Sample Count", rayTracingSystem.sampleCount.ToString());
        if (GUILayout.Button("Reset"))
        {
            rayTracingSystem.needReset += 1;
        }
        if (GUILayout.Button($"Interation ({samplePerPixel})"))
        {
            rayTracingSystem.samplePerPixel = samplePerPixel;
            Interation();
        }
        if (GUILayout.RepeatButton("Interation Repeat"))
        {
            rayTracingSystem.samplePerPixel = 1;
            Interation();
        }
        GUILayout.EndHorizontal();

        #region Texture
        if (renderTexture)
        {
            var ratio = (float) renderTexture.width / renderTexture.height;
            var TextureRect = new Rect(
                spacing, position.height - position.width / ratio - singleLineHeight - spacing,
                (position.width - spacing * 2), (position.width - spacing * 2) / ratio);

            EditorGUI.DrawPreviewTexture(TextureRect, renderTexture);
        }
        #endregion

        #region Save
        EditorGUI.LabelField(new Rect(spacing, position.height - singleLineHeight - spacing, (position.width - spacing * 2) / 3, singleLineHeight), "", path);
        if (GUI.Button(new Rect((position.width - spacing * 2) / 3, position.height - singleLineHeight - spacing, (position.width - spacing * 2) / 3, singleLineHeight), "Browse"))
        {
            path = EditorUtility.SaveFolderPanel("Path to Save Images", path, Application.dataPath);
        }

        if (GUI.Button(new Rect((position.width - spacing * 2) / 3 * 2, position.height - singleLineHeight - spacing, position.width / 3, singleLineHeight), "Save"))
        {
            string name = System.DateTime.Now.ToString("yyyy.MM.dd HH.mm.ss") + ".png";
            renderTexture.Save(path + "/" + name);
        }
        #endregion
    }

    static UnityEngine.Vector3 positionLast;
    static UnityEngine.Quaternion quaternionLast;
    static Vector3 lastSize;

    static float timeMax = 0.1f;
    static float timeAdd;
    static void Upadte()
    {
        if (camera)
        {
            if (camera.name == "SceneCamera")
            {
                resolution = new Vector2Int(
                    (int) SceneView.lastActiveSceneView.position.width,
                    (int) SceneView.lastActiveSceneView.position.height);
            }
            else
            {
                resolution = new Vector2Int(camera.pixelWidth, camera.pixelHeight);
            }
        }

        if (!lastSize.IsApproximate(new Vector3(resolution.x, resolution.y, 0), 1))
        {
            lastSize = new Vector3(resolution.x, resolution.y, 0);
            rayTracingSystem.resolution = resolution;
            renderTexture = rayTracingSystem.ResetRenderTexture();
            rayTracingSystem.needReset += 1;
        }

        // timeAdd += 1 / 100.0f;
        // if (timeAdd >= timeMax)
        // {
        //     timeAdd = 0;
        //     Interation();
        // }
    }

    void AngChanged()
    {

    }

    static void Interation()
    {

        if (camera != null && RayTracingComputeShader != null)
        {
            if (!camera.transform.position.IsApproximate(positionLast, 0.001f)
                || !camera.transform.rotation.IsApproximate(quaternionLast)

            )
            {
                positionLast = camera.transform.position;
                quaternionLast = camera.transform.rotation;
                rayTracingSystem.needReset += 1;
            }

            rayTracingSystem.RayTracingComputeShader = RayTracingComputeShader;
            rayTracingSystem.camera = camera;
            rayTracingSystem.resolution = resolution;
            rayTracingSystem.skyBoxTexture = skyBoxTexture;
            rayTracingSystem.bounces = bounces;
            renderTexture = rayTracingSystem.Render();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }

}