using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RenderWindow : EditorWindow
{
    static int samplePerPixel = 1;
    static Vector2 resolution = new Vector2(1280, 720);
    static float resolutionScale = 1;
    static Texture2D skyBoxTexture;
    static RenderTexture renderTexture;
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

    static string path = "/Users/fairimac/Desktop";

    [MenuItem("Ray Tracing/Render Window")]
    static void Init()
    {
        WINDOW.Show();
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
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float singleLineHeight = EditorGUIUtility.singleLineHeight;

        GUILayout.BeginHorizontal();
        rayTracingSystem.RayTracingComputeShader = EditorGUILayout.ObjectField("ComputeShader", rayTracingSystem.RayTracingComputeShader, typeof(ComputeShader), false) as ComputeShader;
        rayTracingSystem.skyBoxTexture = EditorGUILayout.ObjectField("SkyBox Texture", rayTracingSystem.skyBoxTexture, typeof(Object), false) as Texture2D;
        GUILayout.EndHorizontal();

        //Camera
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
                if (rayTracingSystem.camera == null) rayTracingSystem.camera = camera;
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
            cameraIndex = EditorGUILayout.Popup(cameraIndex, cameraNames, GUILayout.Width(position.width / 2 - spacing * 3));
            if (EditorGUI.EndChangeCheck())
            {
                rayTracingSystem.camera = cameras[cameraIndex];
                rayTracingSystem.needReset = true;
            }
        }
        GUILayout.EndHorizontal();

        //Resolution
        GUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(true);
        var x = EditorGUILayout.FloatField("Width", resolution.x);
        var y = EditorGUILayout.FloatField("Height", resolution.y);
        EditorGUI.EndDisabledGroup();
        resolutionScale = EditorGUILayout.Slider("Scale", resolutionScale, 0.1f, 2f);
        GUILayout.EndHorizontal();

        //Dof
        GUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        rayTracingSystem.focusObject = EditorGUILayout.ObjectField("Focus Object", rayTracingSystem.focusObject, typeof(GameObject), true) as GameObject;
        if (rayTracingSystem.focusObject != null && rayTracingSystem.camera != null)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Focus",
                Vector3.Distance(rayTracingSystem.focusObject.transform.position, rayTracingSystem.camera.transform.position));
            EditorGUI.EndDisabledGroup();
        }
        else
        {
            rayTracingSystem.focus = EditorGUILayout.FloatField("Focus", rayTracingSystem.focus);
        }
        rayTracingSystem.circleOfConfusion = EditorGUILayout.Slider("Circle", rayTracingSystem.circleOfConfusion, 0, 1);
        if (EditorGUI.EndChangeCheck())
        {
            rayTracingSystem.needReset = true;
        }
        GUILayout.EndHorizontal();

        //Sample
        GUILayout.BeginHorizontal();
        samplePerPixel = EditorGUILayout.IntSlider("Sample Per Pixel", samplePerPixel, 1, 1000);
        rayTracingSystem.bounces = EditorGUILayout.IntSlider("Bounces", rayTracingSystem.bounces, 1, 10);
        GUILayout.EndHorizontal();

        //Button
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Sample Count: " + rayTracingSystem.sampleCount.ToString(), GUILayout.Width(position.width / 4 - spacing * 2));
        if (GUILayout.Button("Reset", GUILayout.Width(position.width / 4 - spacing * 2)))
        {
            rayTracingSystem.needReset = true;
        }
        if (GUILayout.Button($"Interation ({samplePerPixel})", GUILayout.Width(position.width / 4 - spacing * 2)))
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

        //Texture
        #region Texture
        if (renderTexture)
        {
            var ratio = (float) renderTexture.width / renderTexture.height;
            var TextureRect = new Rect(
                spacing, singleLineHeight * 6 + spacing * 9,
                (position.width - spacing * 2), (position.width - spacing * 2) / ratio);

            EditorGUI.DrawPreviewTexture(TextureRect, renderTexture);
        }
        #endregion

        //Save
        #region Save
        EditorGUI.TextField(new Rect(spacing, position.height - singleLineHeight - spacing, (position.width - spacing * 4) / 3 - spacing, singleLineHeight), "", path);
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
    static Vector3 lastResolution;

    static void Upadte()
    {
        if (rayTracingSystem.camera)
        {
            resolution = new Vector2(rayTracingSystem.camera.pixelWidth, rayTracingSystem.camera.pixelHeight) * resolutionScale;
        }

        if (!lastResolution.IsApproximate(new Vector3(resolution.x, resolution.y, 0), 1))
        {
            lastResolution = new Vector3(resolution.x, resolution.y, 0);
            rayTracingSystem.resolution = resolution;
            renderTexture = rayTracingSystem.ResetRenderTexture();
            rayTracingSystem.needReset = true;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        if (rayTracingSystem.camera)
        {
            if (!rayTracingSystem.camera.transform.position.IsApproximate(positionLast, 0.001f)
                || !rayTracingSystem.camera.transform.rotation.IsApproximate(quaternionLast)
            )
            {
                positionLast = rayTracingSystem.camera.transform.position;
                quaternionLast = rayTracingSystem.camera.transform.rotation;
                rayTracingSystem.needReset = true;
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }
    }

    static void Interation()
    {
        if (rayTracingSystem.camera != null && rayTracingSystem.RayTracingComputeShader != null)
        {
            renderTexture = rayTracingSystem.Render();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }
}