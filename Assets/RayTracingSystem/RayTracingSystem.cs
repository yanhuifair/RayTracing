﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class RayTracingSystem
{
    ComputeShader computeShader;
    public ComputeShader RayTracingComputeShader
    {
        get
        {
            if (computeShader == null)
            {
                ComputeShader[] computeShaders = (ComputeShader[])Resources.FindObjectsOfTypeAll(typeof(ComputeShader));

                for (int i = 0; i < computeShaders.Length; i++)
                {
                    if (computeShaders[i].name == "RayTracingComputeShader")
                    {
                        computeShader = computeShaders[i];
                        break;
                    }
                }
            }
            return computeShader;
        }

        set
        {
            computeShader = value;
        }
    }
    int? kernalIndex = null;
    int KERNALINDEX
    {
        get
        {
            if (kernalIndex == null)
            {
                kernalIndex = RayTracingComputeShader?.FindKernel("CSMain");
            }
            return (int)kernalIndex;
        }
    }
    public int samplePerPixel = 1;
    public int bounces = 3;
    public Vector2 resolution = new Vector2(1280, 720);
    public Texture2D skyBoxTexture;
    public Texture2D SkyBoxTexture
    {
        get
        {
            if (skyBoxTexture == null)
            {
                skyBoxTexture = new Texture2D(2, 2);
                skyBoxTexture.Apply();
            }
            return skyBoxTexture;
        }
        set
        {
            skyBoxTexture = value;
        }
    }

    RenderTexture renderTextureNormal;
    RenderTexture GetRenderTextureNormal
    {
        get
        {
            if (renderTextureNormal == null)
            {
                renderTextureNormal = RenderTexture.GetTemporary((int)resolution.x, (int)resolution.y, 0, RenderTextureFormat.ARGBFloat);
                renderTextureNormal.filterMode = FilterMode.Point;
                renderTextureNormal.enableRandomWrite = true;
                renderTextureNormal.autoGenerateMips = false;
                renderTextureNormal.Create();
            }
            return renderTextureNormal;
        }
    }

    RenderTexture renderTextureAdd;
    RenderTexture GetRenderTextureAdd
    {
        get
        {
            if (renderTextureAdd == null)
            {
                renderTextureAdd = RenderTexture.GetTemporary((int)resolution.x, (int)resolution.y, 0, RenderTextureFormat.ARGBFloat);
                renderTextureAdd.filterMode = FilterMode.Point;
                renderTextureAdd.enableRandomWrite = true;
                renderTextureAdd.autoGenerateMips = false;
                renderTextureAdd.Create();
            }
            return renderTextureAdd;
        }
    }

    RenderTexture renderTextureOut;
    RenderTexture GetRenderTextureOut
    {
        get
        {
            if (renderTextureOut == null)
            {
                renderTextureOut = RenderTexture.GetTemporary((int)resolution.x, (int)resolution.y, 0, RenderTextureFormat.ARGBFloat);
                renderTextureOut.filterMode = FilterMode.Point;
                renderTextureOut.enableRandomWrite = true;
                renderTextureOut.autoGenerateMips = false;
                renderTextureOut.Create();
            }
            return renderTextureOut;
        }
    }

    public int needReset = 0;
    public RenderTexture ResetRenderTexture()
    {
        if (renderTextureAdd != null) renderTextureAdd.Release();
        renderTextureAdd = null;

        if (renderTextureOut != null) renderTextureOut.Release();
        renderTextureOut = null;

        return GetRenderTextureAdd;
    }
    public Camera camera;
    //DOF
    public float focus = 5;
    public GameObject focusObject = null;
    public float circleOfConfusion = 0;

    public int sampleCount = 0;
    public int _hammersleyCurrent = 0;
    //Scene
    //List<RayTracingEntity> RayTracingEntities = new List<RayTracingEntity>();
    public RayTracingEntity[] RayTracingEntities;

    //Entity
    [MenuItem("GameObject/Ray Tracing/Entity", false)]
    static void CreateNewEntity(MenuCommand menuCommand)
    {
        var obj = new GameObject();
        obj.name = "New Ray Tracing Entity";
        obj.AddComponent<RayTracingEntity>();
        GameObjectUtility.SetParentAndAlign(obj, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(obj, "Create" + obj.name);
        Selection.activeObject = obj;
    }

    public void Release()
    {
        if (renderTextureAdd)
        {
            renderTextureAdd.Release();
            renderTextureAdd = null;
        }

        if (renderTextureOut)
        {
            renderTextureOut.Release();
            renderTextureOut = null;
        }

        if (renderTextureNormal)
        {
            renderTextureNormal.Release();
            renderTextureNormal = null;
        }

        if (sphereBuffer != null)
            sphereBuffer.Release();

        if (boxBuffer != null)
            boxBuffer.Release();

        if (meshObjectBuffer != null)
            meshObjectBuffer.Release();
        if (vertexBuffer != null)
            vertexBuffer.Release();
        if (indexBuffer != null)
            indexBuffer.Release();

        if (randomBuffer != null)
            randomBuffer.Release();
    }

    void SetupScene()
    {
        SetupSpheres();
        SetupBoxs();
        RebuildMeshObjectBuffers();
    }
    Vector3 ColorToVector3(Color c)
    {
        return new Vector3(c.r, c.g, c.b);
    }
    //Sphere
    public struct SphereInfo
    {
        public Vector3 position;
        public Vector3 rotation;
        public float radius;
        public int materialIndex;
    };
    ComputeBuffer sphereBuffer;
    public List<SphereInfo> spheres = new List<SphereInfo>();
    void SetupSpheres()
    {
        spheres.Clear();
        foreach (var item in RayTracingEntities)
        {
            if (item.entityType == RayTracingEntity.EntityType.Sphere
                && item.rayTracingMaterial != null
                && item.gameObject.activeInHierarchy == true)
            {
                spheres.Add(new SphereInfo()
                {
                    position = item.transform.position,
                    rotation = item.transform.rotation.eulerAngles,
                    radius = item.radius / 2.0f,
                    materialIndex = raytracingMaterials.IndexOf(item.rayTracingMaterial),
                });
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
        sphereBuffer = new ComputeBuffer(spheres.Count == 0 ? CreateEmptySpheres() : spheres.Count, stride);
        sphereBuffer.SetData(spheres);
    }

    int CreateEmptySpheres()
    {
        spheres.Add(new SphereInfo());
        return spheres.Count;
    }

    //Box
    public struct BoxInfo
    {
        public Matrix4x4 localToWorldMatrix;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 size;
        public int materialIndex;
    };
    ComputeBuffer boxBuffer;
    public List<BoxInfo> boxs = new List<BoxInfo>();
    void SetupBoxs()
    {
        boxs.Clear();
        foreach (var item in RayTracingEntities)
        {
            if (item.entityType == RayTracingEntity.EntityType.Box
                && item.rayTracingMaterial != null
                && item.gameObject.activeInHierarchy == true)
            {
                Matrix4x4 matrix4X4 = Matrix4x4.TRS(item.transform.position, item.transform.rotation, item.transform.lossyScale);
                boxs.Add(new BoxInfo()
                {
                    localToWorldMatrix = matrix4X4,
                    position = item.transform.position,
                    rotation = item.transform.rotation.eulerAngles,
                    size = item.boxSize,
                    materialIndex = raytracingMaterials.IndexOf(item.rayTracingMaterial),
                });
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
        boxBuffer = new ComputeBuffer(boxs.Count == 0 ? CreateEmptyBoxs() : boxs.Count, stride);
        boxBuffer.SetData(boxs);
    }
    int CreateEmptyBoxs()
    {
        boxs.Add(new BoxInfo());
        return boxs.Count;
    }

    //mesh 
    public struct MeshObject
    {
        public Vector3 position;
        public Vector3 boundsSize;
        public Matrix4x4 localToWorldMatrix;
        public int indices_offset;
        public int indices_count;

        //Material
        public int materialIndex;
    };

    private List<MeshObject> _meshObjects = new List<MeshObject>();
    private List<Vector3> _vertices = new List<Vector3>();
    private List<int> _indices = new List<int>();
    private ComputeBuffer meshObjectBuffer;
    private ComputeBuffer vertexBuffer;
    private ComputeBuffer indexBuffer;
    private void RebuildMeshObjectBuffers()
    {
        // Clear all lists
        _meshObjects.Clear();
        _vertices.Clear();
        _indices.Clear();

        foreach (RayTracingEntity item in RayTracingEntities)
        {
            if (item.entityType == RayTracingEntity.EntityType.Mesh
                && item.mesh != null
                && item.rayTracingMaterial != null
                && item.gameObject.activeInHierarchy == true)
            {
                // Add vertex data
                int firstVertex = _vertices.Count;
                _vertices.AddRange(item.mesh.vertices);

                // Add index data - if the vertex buffer wasn't empty before, the
                // indices need to be offset
                int firstIndex = _indices.Count;
                var indices = item.mesh.GetIndices(0);
                _indices.AddRange(indices.Select(index => index + firstVertex));

                // Add the object itself
                _meshObjects.Add(new MeshObject()
                {
                    position = item.transform.position,
                    boundsSize = item.bounds.size,
                    localToWorldMatrix = item.transform.localToWorldMatrix,
                    indices_offset = firstIndex,
                    indices_count = indices.Length,
                    materialIndex = raytracingMaterials.IndexOf(item.rayTracingMaterial),
                });
            }
        }

        unsafe
        {
            CreateComputeBuffer(ref meshObjectBuffer, _meshObjects, sizeof(MeshObject));
            CreateComputeBuffer(ref vertexBuffer, _vertices, sizeof(Vector3));
            CreateComputeBuffer(ref indexBuffer, _indices, sizeof(int));
        }
    }
    void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride) where T : struct
    {
        // Do we already have a compute buffer?
        if (buffer != null)
        {
            // If no data or buffer doesn't match the given criteria, release it
            if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
            {
                buffer.Dispose();
                buffer = null;
            }
        }

        if (data.Count != 0)
        {
            // If the buffer has been released or wasn't there to
            // begin with, create it
            if (buffer != null)
            {
                buffer.Dispose();
            }
            buffer = new ComputeBuffer(data.Count, stride);
            // Set data on the buffer
            buffer.SetData(data);
        }
    }

    private ComputeBuffer randomBuffer;
    List<float> randList = new List<float>();
    void SetupRandom(int samplePerPixel)
    {
        randList.Clear();
        UnityEngine.Random.InitState(System.DateTime.Now.Millisecond);
        for (int i = 0; i < samplePerPixel; i++)
        {
            randList.Add(UnityEngine.Random.value);
        }
        CreateComputeBuffer(ref randomBuffer, randList, sizeof(float));
    }

    public struct MaterialInfo
    {
        //public Texture2D texture2D;
        public int textureIndex;
        public Vector3 albedo;
        public Vector3 specular;
        public float smoothness;
        public Vector3 emission;
    };

    ComputeBuffer materialInfoBuffer;
    List<MaterialInfo> materialInfos = new List<MaterialInfo>();
    List<RayTracingMaterial> raytracingMaterials = new List<RayTracingMaterial>();

    Texture2DArray materialTexture2DArray;

    void SetupMaterials()
    {
        raytracingMaterials.Clear();
        materialInfos.Clear();

        // if (materialTexture2DArray != null)
        // {
        //     for (int i = 0; i < materialTexture2DArray.depth; i++)
        //     {
        //         if (materialTexture2DArray[i] != null)materialTexture2DArray.get
        //         {

        //         }
        //         }
        //         foreach (var item in materialTexture2DArray.depth)
        //         {

        //         }
        //         }
        //         materialTexture2DArray = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32)
        //         {
        //         dimension = TextureDimension.Tex2DArray,
        //         enableRandomWrite = true,
        //         volumeDepth = 3,
        // };
        // materialTexture2DArray.Create();

        foreach (var item in RayTracingEntities)
        {
            if (item.rayTracingMaterial == null) continue;
            if (raytracingMaterials.Contains(item.rayTracingMaterial)) continue;
            raytracingMaterials.Add(item.rayTracingMaterial);
        }

        foreach (var item in raytracingMaterials)
        {
            MaterialInfo materialInfo = new MaterialInfo();
            //materialInfo.texture2D = item.texture2D;
            materialInfo.albedo = ColorToVector3(item.albedo);
            materialInfo.specular = ColorToVector3(item.specular);
            materialInfo.smoothness = item.smoothness;
            materialInfo.emission = ColorToVector3(item.emission);
            materialInfos.Add(materialInfo);
        }

        unsafe
        {
            int size = sizeof(MaterialInfo);
            // int size = sizeof(Vector3) + sizeof(Vector3) + sizeof(float) + +sizeof(Vector3);
            CreateComputeBuffer(ref materialInfoBuffer, materialInfos, size);
        }
    }

    void ResetParameters()
    {
        _hammersleyCurrent = 0;
        sampleCount = 0;
    }

    void SetShaderParameters()
    {
        //Camera
        RayTracingComputeShader.SetFloat("_farClipPlane", camera.farClipPlane);
        RayTracingComputeShader.SetFloat("_focus",
            focusObject ? Vector3.Distance(focusObject.transform.position, camera.transform.position) : focus);
        RayTracingComputeShader.SetFloat("_circleOfConfusion", circleOfConfusion);
        RayTracingComputeShader.SetMatrix("_CameraToWorld", camera.cameraToWorldMatrix);
        RayTracingComputeShader.SetMatrix("_CameraInverseProjection", camera.projectionMatrix.inverse);

        RayTracingComputeShader.SetTexture(KERNALINDEX, "_SkyboxTexture", SkyBoxTexture);

        RayTracingComputeShader.SetVector("_PixelOffset", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));

        RayTracingComputeShader.SetVector("_ScreenSize", new Vector2(GetRenderTextureAdd.width, GetRenderTextureAdd.height));

        RayTracingComputeShader.SetInt("_bounces", bounces);
        RayTracingComputeShader.SetInt("_samplePerPixel", samplePerPixel);

        //Random
        // SetupRandom(samplePerPixel);
        SetComputeBuffer("_randomBuffer", randomBuffer);
        RayTracingComputeShader.SetFloat("_Seed", UnityEngine.Random.value);
        RayTracingComputeShader.SetInt("_dimension", 0);
        RayTracingComputeShader.SetInt("_randomIndex", 0);
        RayTracingComputeShader.SetInt("_hammersleyCurrent", _hammersleyCurrent);
        RayTracingComputeShader.SetInt("_hammersley", 1024);
        _hammersleyCurrent++;

        sampleCount += samplePerPixel;
        RayTracingComputeShader.SetInt("_sampleCount", sampleCount);

        //Sphere
        RayTracingComputeShader.SetBuffer(KERNALINDEX, "_Spheres", sphereBuffer);
        RayTracingComputeShader.SetInt("_numSpheres", sphereBuffer.count);

        //Boxs
        RayTracingComputeShader.SetBuffer(KERNALINDEX, "_Boxs", boxBuffer);
        RayTracingComputeShader.SetInt("_numBoxs", boxBuffer.count);

        //mesh
        SetComputeBuffer("_MeshObjects", meshObjectBuffer);
        SetComputeBuffer("_Vertices", vertexBuffer);
        SetComputeBuffer("_Indices", indexBuffer);
        RayTracingComputeShader.SetInt("_numMeshObjects", meshObjectBuffer.count);

        //Material
        SetComputeBuffer("_materialBuffer", materialInfoBuffer);
        // RayTracingComputeShader.SetTexture(KERNALINDEX, "_materialTextures", materialTexture2DArray);
    }

    private void SetComputeBuffer(string name, ComputeBuffer buffer)
    {
        if (buffer != null)
        {
            RayTracingComputeShader.SetBuffer(KERNALINDEX, name, buffer);
        }
    }

    public RenderTexture Render()
    {
        RayTracingEntities = GameObject.FindObjectsOfType<RayTracingEntity>();
        foreach (var item in RayTracingEntities)
        {
            if (item.isAnyChanged())
            {
                needReset += 1;
                item.ResetChanged();
            }
        }

        if (needReset > 0)
        {
            ResetRenderTexture();
            SetupMaterials();
            SetupScene();
            ResetParameters();
            needReset = 0;
        }

        SetShaderParameters();

        Dispatch();
        return renderTextureOut;
    }

    void Dispatch()
    {
        RayTracingComputeShader.SetTexture(KERNALINDEX, "Result", GetRenderTextureAdd);
        RayTracingComputeShader.SetTexture(KERNALINDEX, "ResultOut", GetRenderTextureOut);
        int threadGroupsX = Mathf.CeilToInt(GetRenderTextureAdd.width / 8);
        int threadGroupsY = Mathf.CeilToInt(GetRenderTextureAdd.height / 8);
        RayTracingComputeShader.Dispatch(KERNALINDEX, threadGroupsX, threadGroupsY, 1);
    }
}