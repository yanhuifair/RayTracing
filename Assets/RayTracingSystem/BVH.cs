using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

[Serializable]
public class Triangle
{
    public int[] triangles;
    public Mesh mesh;
    public Matrix4x4 matrix4X4;
    public Bounds GetBounds()
    {
        Vector3[] vertices = new Vector3[]
        {
            mesh.vertices[triangles[0]],
            mesh.vertices[triangles[1]],
            mesh.vertices[triangles[2]]
        };
        return GeometryUtility.CalculateBounds(vertices, matrix4X4);
    }
    public float Volume()
    {
        var bound = GetBounds();
        return (bound.max.x - bound.min.x) * (bound.max.y - bound.min.y)
            + (bound.max.x - bound.min.x) * (bound.max.z - bound.min.z)
            + (bound.max.y - bound.min.y) * (bound.max.z - bound.min.z);

    }
}

[Serializable]
public class BVHNode
{
    public int index;
    public int depth;
    public Bounds bounds;
    public BVHNode subNode0;
    public BVHNode subNode1;
    public List<Vector3> vertices = new List<Vector3>();
    public List<Triangle> triangles = new List<Triangle>();

}

[ExecuteInEditMode]
public class BVH : MonoBehaviour
{
    [Range(0, 10)] public int maxDepth = 3;
    public bool showMesh;
    public bool showBounds;
    [HideInInspector] public BVHNode nodeRoot;
    Mesh meshStatic;
    Mesh meshDynamic;

    [Button]
    public void Combine()
    {
        meshStatic = new Mesh();
        List<CombineInstance> combines = new List<CombineInstance>();
        var RayTracingEntities = GameObject.FindObjectsOfType<RayTracingEntity>();
        foreach (RayTracingEntity entity in RayTracingEntities)
        {
            if (entity.entityType == RayTracingEntity.EntityType.Mesh
                && entity.mesh != null
                && entity.rayTracingMaterial != null
                && entity.gameObject.activeInHierarchy == true
                && entity.gameObject.isStatic == true)
            {
                CombineInstance c = new CombineInstance();
                c.mesh = entity.mesh;
                c.transform = entity.transform.localToWorldMatrix;
                combines.Add(c);
            }
        }
        if (combines.Count > 0) meshStatic.CombineMeshes(combines.ToArray());

        Debug.Log($"Combine Mesh {meshStatic.triangles.Length/3}");
    }

    [Button]
    public void Cut()
    {
        nodeRoot = new BVHNode();
        nodeRoot.depth = 0;
        nodeRoot.bounds = GeometryUtility.CalculateBounds(meshStatic.vertices, Matrix4x4.identity);
        nodeRoot.index = 1;
        for (int i = 0; i < meshStatic.triangles.Length; i += 3)
        {
            var t = new Triangle();
            t.mesh = meshStatic;
            t.matrix4X4 = Matrix4x4.identity;
            t.triangles = new int[]
            {
                meshStatic.triangles[i],
                meshStatic.triangles[i + 1],
                meshStatic.triangles[i + 2]
            };
            nodeRoot.triangles.Add(t);
        }

        Debug.Log($"Cut Node {nodeRoot.triangles.Count}");
        if (nodeRoot.triangles.Count > 0)
        {
            CutNode(nodeRoot);
        }
        else
        {
            Debug.LogWarning("Triangles None");
        }
    }

    enum CutPlane
    {
        x,
        y,
        z
    }
    void CutNode(BVHNode node)
    {
        if (node.triangles.Count == 0) return;
        if (node.depth < maxDepth)
        {
            //CaculatePlane
            Bounds b0 = new Bounds(), b1 = new Bounds();
            CutBounds(node.bounds, ref b0, ref b1, 0.5f, CutPlane.x);
            float fx = CaculateBoundsFangcha(node, b0, b1);

            CutBounds(node.bounds, ref b0, ref b1, 0.5f, CutPlane.y);
            float fy = CaculateBoundsFangcha(node, b0, b1);

            CutBounds(node.bounds, ref b0, ref b1, 0.5f, CutPlane.z);
            float fz = CaculateBoundsFangcha(node, b0, b1);

            Dictionary<CutPlane, float> dic = new Dictionary<CutPlane, float>();
            dic.Add(CutPlane.x, fx);
            dic.Add(CutPlane.y, fy);
            dic.Add(CutPlane.z, fz);
            var result = dic.OrderByDescending(d => d.Value);
            var cutPlane = result.First().Key;

            //Create subnode
            node.subNode0 = new BVHNode();
            node.subNode0.depth = node.depth + 1;
            node.subNode0.index = 2 * node.index;

            node.subNode1 = new BVHNode();
            node.subNode1.depth = node.depth + 1;
            node.subNode0.index = 2 * node.index + 1;

            float sah = 0.5f;
            CutBounds(node.bounds, ref node.subNode0.bounds, ref node.subNode1.bounds, sah, cutPlane);

            foreach (var t in node.triangles)
            {
                if (node.subNode0.bounds.Intersects(t.GetBounds()))
                {
                    node.subNode0.triangles.Add(t);
                }
                if (node.subNode1.bounds.Intersects(t.GetBounds()))
                {
                    node.subNode1.triangles.Add(t);
                }
            }
            node.triangles.Clear();
            CutNode(node.subNode0);
            CutNode(node.subNode1);
        }
    }

    float CaculateBoundsFangcha(BVHNode parant, Bounds b0, Bounds b1)
    {
        float fangcha = 0;
        float v0 = 0, v1 = 0;
        foreach (var item in parant.triangles)
        {
            if (b0.Intersects(item.GetBounds())) v0 += 1;
            if (b1.Intersects(item.GetBounds())) v1 += 1;
        }
        fangcha = Mathf.Abs(v0 * v0 - v1 * v1);
        return fangcha;
    }

    void CutBounds(Bounds parant, ref Bounds sub0, ref Bounds sub1, float sah, CutPlane planeDir)
    {
        sub0 = new Bounds();
        sub1 = new Bounds();
        sub0.min = parant.min;
        sub1.max = parant.max;

        switch (planeDir)
        {
            case CutPlane.x:
                {
                    float x = parant.max.x - parant.min.x;
                    sub0.max = parant.max - Vector3.right * x * sah;
                    sub1.min = parant.min + Vector3.right * x * (1 - sah);
                }
                break;
            case CutPlane.y:
                {
                    float y = parant.max.y - parant.min.y;
                    sub0.max = parant.max - Vector3.up * y * sah;
                    sub1.min = parant.min + Vector3.up * y * (1 - sah);
                }
                break;
            case CutPlane.z:
                {
                    float z = parant.max.z - parant.min.z;
                    sub0.max = parant.max - Vector3.forward * z * sah;
                    sub1.min = parant.min + Vector3.forward * z * (1 - sah);
                }
                break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (meshStatic != null)
        {
            if (showMesh)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawWireMesh(meshStatic, Vector3.zero, Quaternion.identity);
            }
        }

        if (showBounds)
        {
            DrawBound(nodeRoot);
        }
    }

    void DrawBound(BVHNode node)
    {
        if (node != null)
        {
            var bounds = node.bounds;
            if (bounds != null)
            {
                if (node.triangles.Count > 0) Handles.Label(bounds.center, $"{node.triangles.Count}");
                Gizmos.color = node.triangles.Count > 0 ? Color.cyan : Color.grey;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

            DrawBound(node.subNode0);
            DrawBound(node.subNode1);
        }
    }
}