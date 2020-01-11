using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TransformMarker : MonoBehaviour
{
    [Serializable]
    public class TransformMarkerInfo
    {
        public TransformMarkerInfo(Vector3 positon, Quaternion rotation)
        {
            this.position = positon;
            this.rotation = rotation;
        }
        public Vector3 position;
        public Quaternion rotation;
        public bool foldout;
    }

    public List<TransformMarkerInfo> transformMarkerInfos = new List<TransformMarkerInfo>();

    public TransformMarker.TransformMarkerInfo GetInfo()
    {
        TransformMarker.TransformMarkerInfo info = new TransformMarker.TransformMarkerInfo(
            transform.position,
            transform.rotation);
        return info;
    }

    //     void OnDrawGizmosSelected()
    //     {
    // #if UNITY_EDITOR
    //         Gizmos.color = EditorCommon.hightLightColor;
    //         Matrix4x4 temp = Gizmos.matrix;
    //         for (int i = 0; i < transformMarkerInfos.Count; i++)
    //         {
    //             var info = transformMarkerInfos[i];

    //             if (TryGetComponent(out Camera camera))
    //             {
    //                 Gizmos.matrix = Matrix4x4.TRS(info.position, info.rotation, Vector3.one);
    //                 if (camera.orthographic)
    //                 {
    //                     float spread = camera.farClipPlane - camera.nearClipPlane;
    //                     float center = (camera.farClipPlane + camera.nearClipPlane) * 0.5f;
    //                     Gizmos.DrawWireCube(new Vector3(0, 0, center), new Vector3(camera.orthographicSize * 2 * camera.aspect, camera.orthographicSize * 2, spread));
    //                 }
    //                 else
    //                 {
    //                     Gizmos.DrawFrustum(new Vector3(0, 0, 0), camera.fieldOfView, 1, 0, camera.aspect);
    //                 }
    //                 Gizmos.matrix = temp;
    //             }
    //             else
    //             {
    //                 Handles.PositionHandle(info.position, info.rotation);
    //             }
    //         }
    // #endif
    //     }
}