using System.Collections.Generic;
using RampantC20;
using UnityEngine;

namespace Plugins.Rampancy.Runtime
{
    public class DebugGeoObj : MonoBehaviour
    {
        public static GameObject Create(DebugGeoMarker item)
        {
            var root = GameObject.Find("Frame/DebugGeo");
            if (root == null) {
                root = new GameObject("DebugGeo");
                var frame    = GameObject.Find("Frame");
                root.transform.parent = frame.transform;
            }

            root.hideFlags = HideFlags.DontSaveInEditor;
            
            var obj = new GameObject(item.Name);
            obj.transform.parent   = root.transform;
            
            var center = Vector3.zero;
            var idx    = 0;
            foreach (var vert in item.Verts) {
                var scale = new Vector3(Statics.ExportScale, -Statics.ExportScale, Statics.ExportScale);
                var rot   = Quaternion.Euler(new Vector3(-90, 0, 0));
                
                center += rot * Vector3.Scale(scale, vert);
                var point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                point.name = $"Vert: {idx++}";
                
                point.transform.localScale = Vector3.one * 0.1f;
                point.transform.position   = rot         * Vector3.Scale(scale, vert);
                point.transform.parent     = obj.transform;

                var mat = point.GetComponent<MeshRenderer>().material;
                mat.color                                   = item.Color;
                point.GetComponent<MeshRenderer>().material = mat;
            }

            var mesh       = Utils.TrisToMesh(new List<DebugGeoMarker> { item });
            var meshFilter = obj.AddComponent<MeshFilter>();
            var meshRender = obj.AddComponent<MeshRenderer>();
            meshFilter.mesh = mesh;
            //meshRender.sharedMaterial = 

            return obj;
        }
    }
}