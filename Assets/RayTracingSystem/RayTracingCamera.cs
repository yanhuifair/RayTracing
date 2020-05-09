using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class RayTracingCamera : MonoBehaviour
{
    public ComputeShader RayTracingComputeShader;
    public Shader addSahder;
    int? kernalIndex = null;
    int KERNALINDEX
    {
        get
        {
            if (kernalIndex == null)
            {
                kernalIndex = RayTracingComputeShader.FindKernel("CSMain");
            }
            return (int) kernalIndex;
        }
    }

    [Range(1, 10)] public int SamplePerPixel = 1;
    [Range(1, 10)] public int depth = 3;
    public Texture2D skyBoxTexture;
    public RenderTexture renderTexture;
    public RenderTexture renderTextureCover;
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
                _camera = GameObject.Find("SceneCamera").GetComponent<Camera>();
            }
            //_camera = transform.GetComponent<Camera>();
            return _camera;
        }
    }

    //Box
    public struct BoxInfo
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 size;
        public Vector3 albedo;
        public Vector3 specular;
        public float smoothness;
        public Vector3 emission;
    };
    ComputeBuffer boxBuffer;
    public List<BoxInfo> boxs = new List<BoxInfo>();

    //Sphere
    public struct SphereInfo
    {
        public Vector3 position;
        public Vector3 rotation;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
        public float smoothness;
        public Vector3 emission;
    };
    ComputeBuffer sphereBuffer;
    public List<SphereInfo> spheres = new List<SphereInfo>();
    [OnValueChanged("ReSample")] public bool cover;
    bool reSample;
    public int currentSample = 0;
    private Material addMaterial;
    //mesh 
    struct MeshObject
    {
        public Matrix4x4 localToWorldMatrix;
        public int indices_offset;
        public int indices_count;

        public Vector3 albedo;
        public Vector3 specular;
        public float smoothness;
        public Vector3 emission;
    };

    public List<RayTracingEntity> RayTracingEntities = new List<RayTracingEntity>();
    private static List<MeshObject> _meshObjects = new List<MeshObject>();
    private static List<Vector3> _vertices = new List<Vector3>();
    private static List<int> _indices = new List<int>();
    private ComputeBuffer _meshObjectBuffer;
    private ComputeBuffer _vertexBuffer;
    private ComputeBuffer _indexBuffer;

    [ContextMenu("Tools/Select Scene Camera")]
    void SelectSceneCamera()
    {
        var sceneCamera = GameObject.Find("SceneCamera");
        Selection.activeObject = sceneCamera;
    }

    private void OnEnable()
    {
        // EditorApplication.update += EditorUpdate;
    }

    public Vector3 lastPosition;
    public Quaternion lastQuaternion;
    private void Update()
    {
        // if (!CameraCurrent.transform.position.IsApproximate(lastPosition, 0.01f)
        //     || !CameraCurrent.transform.rotation.IsApproximate(lastQuaternion, 0.01f)
        // )
        // {
        //     lastPosition = CameraCurrent.transform.position;
        //     lastQuaternion = CameraCurrent.transform.rotation;
        //     ReSample();
        // }
    }

    private void OnDisable()
    {
        if (renderTexture)
        {
            renderTexture.Release();
            renderTexture = null;
        }

        if (renderTextureCover)
        {
            renderTextureCover.Release();
            renderTextureCover = null;
        }

        if (sphereBuffer != null)
            sphereBuffer.Release();

        if (boxBuffer != null)
            boxBuffer.Release();

        if (_meshObjectBuffer != null)
            _meshObjectBuffer.Release();
        if (_vertexBuffer != null)
            _vertexBuffer.Release();
        if (_indexBuffer != null)
            _indexBuffer.Release();

        // EditorApplication.update -= EditorUpdate;
    }

    Vector3 ColorToVector3(Color c)
    {
        return new Vector3(c.r, c.g, c.b);
    }
    void SetupScene()
    {
        SetupSpheres();
        SetupBoxs();
        RebuildMeshObjectBuffers();
    }

    void SetupSpheres()
    {
        spheres.Clear();
        foreach (var item in RayTracingEntities)
        {
            if (item.entityType == RayTracingEntity.EntityType.Sphere && item.gameObject.activeSelf == true)
            {
                var sphere = new SphereInfo();
                sphere.position = item.transform.position;
                sphere.rotation = item.transform.rotation.eulerAngles;
                sphere.radius = item.radius / 2.0f;
                sphere.albedo = ColorToVector3(item.rayTracingMaterial.albedo);
                sphere.specular = ColorToVector3(item.rayTracingMaterial.specular);
                sphere.smoothness = item.rayTracingMaterial.smoothness;
                sphere.emission = ColorToVector3(item.rayTracingMaterial.emission);
                spheres.Add(sphere);
            }
        }
        if (sphereBuffer != null)
            sphereBuffer.Release();
        // Assign to compute buffer
        int stride;
        unsafe
        {
            stride = sizeof(SphereInfo);
        }
        sphereBuffer = new ComputeBuffer(spheres.Count == 0 ? CreateEmtpySpheres() : spheres.Count, stride);
        sphereBuffer.SetData(spheres);
    }
    int CreateEmtpySpheres()
    {
        spheres.Add(new SphereInfo());
        return spheres.Count;
    }

    void SetupBoxs()
    {
        boxs.Clear();
        foreach (var item in RayTracingEntities)
        {
            if (item.entityType == RayTracingEntity.EntityType.Box && item.gameObject.activeSelf == true)
            {
                var box = new BoxInfo();
                box.position = item.transform.position;
                box.rotation = item.transform.rotation.eulerAngles;
                box.size = item.boxSize;
                box.albedo = ColorToVector3(item.rayTracingMaterial.albedo);
                box.specular = ColorToVector3(item.rayTracingMaterial.specular);
                box.smoothness = item.rayTracingMaterial.smoothness;
                box.emission = ColorToVector3(item.rayTracingMaterial.emission);
                boxs.Add(box);
            }
        }
        if (boxBuffer != null)
            boxBuffer.Release();
        // Assign to compute buffer
        int stride;
        unsafe
        {
            stride = sizeof(BoxInfo);
        }
        boxBuffer = new ComputeBuffer(boxs.Count == 0 ? CreateEmtpyBoxs() : boxs.Count, stride);
        boxBuffer.SetData(boxs);
    }
    int CreateEmtpyBoxs()
    {
        boxs.Add(new BoxInfo());
        return boxs.Count;
    }

    private void RebuildMeshObjectBuffers()
    {
        // Clear all lists
        _meshObjects.Clear();
        _vertices.Clear();
        _indices.Clear();

        foreach (RayTracingEntity item in RayTracingEntities)
        {
            if (item.entityType == RayTracingEntity.EntityType.Mesh && item.gameObject.activeSelf == true)
            {
                MeshFilter meshFilter = item.gameObject.GetComponent<MeshFilter>();
                if (meshFilter == null) continue;

                Mesh mesh = meshFilter.sharedMesh;
                if (mesh == null) continue;

                // Add vertex data
                int firstVertex = _vertices.Count;
                _vertices.AddRange(mesh.vertices);

                // Add index data - if the vertex buffer wasn't empty before, the
                // indices need to be offset
                int firstIndex = _indices.Count;
                var indices = mesh.GetIndices(0);
                _indices.AddRange(indices.Select(index => index + firstVertex));

                // Add the object itself
                _meshObjects.Add(new MeshObject()
                {
                    localToWorldMatrix = item.transform.localToWorldMatrix,
                        indices_offset = firstIndex,
                        indices_count = indices.Length,

                        albedo = ColorToVector3(item.rayTracingMaterial.albedo),
                        specular = ColorToVector3(item.rayTracingMaterial.specular),
                        smoothness = item.rayTracingMaterial.smoothness,
                        emission = ColorToVector3(item.rayTracingMaterial.emission),
                });
            }
        }
        unsafe
        {
            CreateComputeBuffer(ref _meshObjectBuffer, _meshObjects, sizeof(MeshObject));
            CreateComputeBuffer(ref _vertexBuffer, _vertices, sizeof(Vector3));
            CreateComputeBuffer(ref _indexBuffer, _indices, sizeof(int));
        }
    }
    private static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
    where T : struct
    {
        // Do we already have a compute buffer?
        if (buffer != null)
        {
            // If no data or buffer doesn't match the given criteria, release it
            if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
            {
                buffer.Release();
                buffer = null;
            }
        }

        if (data.Count != 0)
        {
            // If the buffer has been released or wasn't there to
            // begin with, create it
            if (buffer == null)
            {
                buffer = new ComputeBuffer(data.Count, stride);
            }

            // Set data on the buffer
            buffer.SetData(data);
        }
    }

    private void SetComputeBuffer(string name, ComputeBuffer buffer)
    {
        if (buffer != null)
        {
            RayTracingComputeShader.SetBuffer(KERNALINDEX, name, buffer);
        }
    }

    void SetShaderParameters()
    {
        RayTracingComputeShader.SetMatrix("_CameraToWorld", CameraCurrent.cameraToWorldMatrix);
        RayTracingComputeShader.SetMatrix("_CameraInverseProjection", CameraCurrent.projectionMatrix.inverse);

        RayTracingComputeShader.SetTexture(KERNALINDEX, "_SkyboxTexture", skyBoxTexture);

        RayTracingComputeShader.SetVector("_PixelOffset", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));

        RayTracingComputeShader.SetVector("_ScreenSize", new Vector3(Screen.width, Screen.height, 0));

        RayTracingComputeShader.SetInt("_depth", depth);
        RayTracingComputeShader.SetInt("_SamplePerPixel", SamplePerPixel);
        RayTracingComputeShader.SetFloat("_Seed", UnityEngine.Random.value);

        //Sphere
        RayTracingComputeShader.SetBuffer(KERNALINDEX, "_Spheres", sphereBuffer);
        RayTracingComputeShader.SetInt("_numSpheres", sphereBuffer.count);

        //Boxs
        RayTracingComputeShader.SetBuffer(KERNALINDEX, "_Boxs", boxBuffer);
        RayTracingComputeShader.SetInt("_numBoxs", boxBuffer.count);

        //mesh
        SetComputeBuffer("_MeshObjects", _meshObjectBuffer);
        SetComputeBuffer("_Vertices", _vertexBuffer);
        SetComputeBuffer("_Indices", _indexBuffer);

        RayTracingComputeShader.SetInt("_numMeshObjects", _meshObjectBuffer.count);
        RayTracingComputeShader.SetFloat("_CameraFar", CameraCurrent.farClipPlane);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetupScene();
        SetShaderParameters();
        Render(destination);
    }
    void Render(RenderTexture destination)
    {
        InitRenderTexture();
        RayTracingComputeShader.SetTexture(KERNALINDEX, "Result", renderTexture);
        int threadGroupsX = Mathf.CeilToInt(renderTexture.width / 8);
        int threadGroupsY = Mathf.CeilToInt(renderTexture.height / 8);
        RayTracingComputeShader.Dispatch(KERNALINDEX, threadGroupsX, threadGroupsY, 1);

        if (cover)
        {
            if (addMaterial == null)
                addMaterial = new Material(addSahder);
            addMaterial.SetFloat("_Sample", currentSample);
            Graphics.Blit(renderTexture, destination, addMaterial);
            // Graphics.Blit(renderTextureCover, destination, addMaterial);
            currentSample += 1;
        }
        else
        {
            currentSample = 0;
            Graphics.Blit(renderTexture, destination);
        }

    }

    void InitRenderTexture()
    {
        Vector2Int texSize = GetTextureSize();
        if (renderTexture == null
            || renderTexture.width != texSize.x
            || renderTexture.height != texSize.y)
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
            }
            renderTexture = new RenderTexture(texSize.x, texSize.y, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }

        if (renderTextureCover == null
            || renderTextureCover.width != texSize.x
            || renderTextureCover.height != texSize.y)
        {
            if (renderTextureCover != null)
            {
                renderTextureCover.Release();
            }

            renderTextureCover = new RenderTexture(texSize.x, texSize.y, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            renderTextureCover.enableRandomWrite = true;
            renderTextureCover.Create();
        }
    }

    Vector2Int GetTextureSize()
    {
        Vector2Int texSize = Vector2Int.zero;
        if (EditorApplication.isPlaying)
        {
            texSize = new Vector2Int((int) Screen.width, (int) Screen.height);
        }
        else
        {
            GameObject sceneCamObj = GameObject.Find("SceneCamera");
            if (sceneCamObj != null)
            {
                var rect = sceneCamObj.GetComponent<Camera>().pixelRect;
                texSize = new Vector2Int((int) rect.width, (int) rect.height);
            }
        }
        return texSize;
    }

    void RenderTextureCoverReset()
    {
        var texSize = GetTextureSize();
        if (renderTextureCover != null)
        {
            renderTextureCover.Release();
        }

        renderTextureCover = new RenderTexture(texSize.x, texSize.y, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
        renderTextureCover.enableRandomWrite = true;
        renderTextureCover.Create();
    }

    void ReSample()
    {
        Debug.Log("ReSample");
        RenderTextureCoverReset();
        currentSample = 0;
    }
}