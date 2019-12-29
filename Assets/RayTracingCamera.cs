using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class RayTracingCamera : MonoBehaviour
{

    public ComputeShader RayTracingShader;
    [Range(1, 10)] public int depth = 3;
    public Texture2D _SkyboxTexture;
    RenderTexture renderTexture;
    RenderTexture renderTextureCover;
    Camera _camera;

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
    public bool cover;
    public uint currentSample = 0;
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
    private static List<MeshObject> _meshObjects = new List<MeshObject>();
    private static List<Vector3> _vertices = new List<Vector3>();
    private static List<int> _indices = new List<int>();
    private ComputeBuffer _meshObjectBuffer;
    private ComputeBuffer _vertexBuffer;
    private ComputeBuffer _indexBuffer;

    private void OnDisable()
    {
        if (sphereBuffer != null)
            sphereBuffer.Release();

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

        if (_meshObjectBuffer != null)
            _meshObjectBuffer.Release();
        if (_vertexBuffer != null)
            _vertexBuffer.Release();
        if (_indexBuffer != null)
            _indexBuffer.Release();
    }

    Vector3 ColorToVector3(Color c)
    {
        return new Vector3(c.r, c.g, c.b);
    }
    void SetUpScene()
    {
        SetSpheres();
    }

    void SetSpheres()
    {
        spheres.Clear();
        var entitys = GameObject.FindObjectsOfType(typeof(RayTracingEntity)) as RayTracingEntity[];
        foreach (var item in entitys)
        {
            if (item.entityType == RayTracingEntity.EntityType.Sphere && item.gameObject.activeSelf == true)
            {
                var sphere = new SphereInfo();
                sphere.position = item.transform.position;
                sphere.rotation = item.transform.rotation.eulerAngles;
                sphere.radius = item.radius / 2.0f;
                sphere.albedo = ColorToVector3(item.albedo);
                sphere.specular = ColorToVector3(item.specular);
                sphere.smoothness = item.smoothness;
                sphere.emission = ColorToVector3(item.emission);
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

    private void RebuildMeshObjectBuffers()
    {
        // Clear all lists
        _meshObjects.Clear();
        _vertices.Clear();
        _indices.Clear();

        // Loop over all objects and gather their data
        var entitys = GameObject.FindObjectsOfType(typeof(RayTracingEntity)) as RayTracingEntity[];
        foreach (RayTracingEntity obj in entitys)
        {
            if (obj.entityType != RayTracingEntity.EntityType.Mesh) continue;
            MeshFilter meshFilter = obj.gameObject.GetComponent<MeshFilter>();
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
                localToWorldMatrix = obj.transform.localToWorldMatrix,
                    indices_offset = firstIndex,
                    indices_count = indices.Length,

                    albedo = ColorToVector3(obj.albedo),
                    specular = ColorToVector3(obj.specular),
                    smoothness = obj.smoothness,
                    emission = ColorToVector3(obj.emission),
            });
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
            RayTracingShader.SetBuffer(0, name, buffer);
        }
    }

    void SetShaderParameters()
    {
        if (_camera == null) _camera = GetComponent<Camera>();

        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);

        RayTracingShader.SetTexture(0, "_SkyboxTexture", _SkyboxTexture);

        RayTracingShader.SetVector("_PixelOffset", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));

        RayTracingShader.SetVector("_ScreenSize", new Vector3(Screen.width, Screen.height, 0));

        RayTracingShader.SetBuffer(0, "_Spheres", sphereBuffer);
        RayTracingShader.SetInt("_numSpheres", sphereBuffer.count);
        RayTracingShader.SetInt("_depth", depth);
        RayTracingShader.SetFloat("_Seed", UnityEngine.Random.value);

        //mesh
        SetComputeBuffer("_MeshObjects", _meshObjectBuffer);
        SetComputeBuffer("_Vertices", _vertexBuffer);
        SetComputeBuffer("_Indices", _indexBuffer);
        RayTracingShader.SetInt("_numMeshObjects", _meshObjectBuffer.count);
        RayTracingShader.SetFloat("_CameraFar", _camera.farClipPlane);
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetUpScene();

        RebuildMeshObjectBuffers();
        SetShaderParameters();
        Render(destination);
    }
    void Render(RenderTexture destination)
    {
        InitRenderTexture();
        RayTracingShader.SetTexture(0, "Result", renderTexture);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        if (cover)
        {
            if (addMaterial == null)
                addMaterial = new Material(Shader.Find("Hidden/AddShader"));
            addMaterial.SetFloat("_Sample", currentSample);
            Graphics.Blit(renderTexture, destination, addMaterial);
            //currentSample += 1;
        }
        else
        {
            //currentSample = 0;
            Graphics.Blit(renderTexture, destination);
        }

    }

    void InitRenderTexture()
    {
        Vector2Int texSize = new Vector2Int((int) Screen.width, (int) Screen.height);
        if (renderTexture == null
            || renderTexture.width != Screen.width
            || renderTexture.height != Screen.height)
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
            || renderTextureCover.width != Screen.width
            || renderTextureCover.height != Screen.height)
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
}