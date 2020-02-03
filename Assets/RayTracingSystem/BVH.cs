using System;
using System.Collections;
using System.Collections.Generic;
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

public class BVH : MonoBehaviour
{
    public int maxDepth = 3;
    public int maxTriangle = 100;
    [HideInInspector] public BVHNode nodeRoot;
    Mesh meshStatic;
    [Button]
    public void Init()
    {
        meshStatic = new Mesh();
        List<CombineInstance> combines = new List<CombineInstance>();
        var RayTracingEntities = GameObject.FindObjectsOfType<RayTracingEntity>();
        foreach (RayTracingEntity entity in RayTracingEntities)
        {
            if (entity.entityType == RayTracingEntity.EntityType.Mesh
                && entity.mesh != null
                && entity.rayTracingMaterial != null
                && entity.gameObject.activeInHierarchy == true)
            {
                CombineInstance c = new CombineInstance();
                c.mesh = entity.mesh;
                c.transform = entity.transform.localToWorldMatrix;
                combines.Add(c);
            }
        }
        meshStatic.CombineMeshes(combines.ToArray());

        nodeRoot = new BVHNode();
        nodeRoot.depth = 0;
        nodeRoot.bounds = GeometryUtility.CalculateBounds(meshStatic.vertices, Matrix4x4.identity);

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

        CutNode(nodeRoot);
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
        if (node.depth < maxDepth || node.depth < maxDepth)
        {
            float sah = 0.5f;
            float random = UnityEngine.Random.value;
            CutPlane planeDir;
            if (random < 0.33333f)
            {
                planeDir = CutPlane.x;
            }
            else if (random < 0.66666f)
            {
                planeDir = CutPlane.y;
            }
            else
            {
                planeDir = CutPlane.z;
            }

            node.subNode0 = new BVHNode();
            node.subNode0.depth = node.depth + 1;

            node.subNode1 = new BVHNode();
            node.subNode1.depth = node.depth + 1;

            CutBounds(node.bounds, ref node.subNode0.bounds, ref node.subNode1.bounds, sah, planeDir);

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
            Gizmos.color = Color.gray;
            Gizmos.DrawWireMesh(meshStatic, Vector3.zero, Quaternion.identity);
        }
        Gizmos.color = Color.cyan;
        DrawBound(nodeRoot);
    }

    void DrawBound(BVHNode node)
    {
        if (node != null)
        {
            var bounds = node.bounds;
            if (bounds != null && node.triangles.Count > 0)
            {
                //Gizmos.color = (Color.white / (float) depth) * (float) node.depth;
                Handles.Label(bounds.center, $"{node.triangles.Count}");
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

            DrawBound(node.subNode0);
            DrawBound(node.subNode1);
        }
    }
}