﻿using System;
using System.IO;
using Plugins.Rampancy.RampantC20;
using Rampancy.RampantC20;
using UnityEditor;
using UnityEngine;

namespace Plugins.Rampancy.Runtime
{
    // Store per level data
    [ExecuteInEditMode]
    public class RampancySentinel : MonoBehaviour
    {
        public const string NAME = "Rampancy Sentinel";

        public string LevelName;
        public string DataDir;
        public bool   DisplayDebugGeo = true;

        private DebugGeoData      DebugGeo        = null;
        private FileSystemWatcher DebugWrlWatcher = null;
        private bool              ShouldReloadWrl = true;
        
        public string GetWrlPath() => Path.Combine(Rampancy.Config.ActiveGameConfig.DataPath, "models", $"{LevelName}_errors.wrl");

        public static RampancySentinel GetOrCreateInScene()
        {
            var existingGO = GameObject.Find(NAME);
            if (existingGO != null) return existingGO.GetComponent<RampancySentinel>();

            var go = new GameObject(NAME);
            var rs = go.AddComponent<RampancySentinel>();

            return rs;
        }

        public void Awake()
        {
            Debug.Log("RampancySentinel Awake :D");
            WatchForWrlFile();
        }

        public void WatchForWrlFile()
        {
            var dirPath = Path.GetDirectoryName(GetWrlPath());
            if (!Directory.Exists(dirPath)) {
                Directory.CreateDirectory(dirPath);
            }
            
            DebugWrlWatcher = new FileSystemWatcher(dirPath);

            DebugWrlWatcher.NotifyFilter = NotifyFilters.CreationTime
                                         | NotifyFilters.LastAccess
                                         | NotifyFilters.LastWrite;

            DebugWrlWatcher.Changed += (sender, args) => MarkWrlForReload();
            DebugWrlWatcher.Created += (sender, args) => MarkWrlForReload();
            DebugWrlWatcher.Deleted += (sender, args) => MarkWrlForReload();

            DebugWrlWatcher.Filter                = Path.GetFileName(GetWrlPath());
            DebugWrlWatcher.IncludeSubdirectories = true;
            DebugWrlWatcher.EnableRaisingEvents   = true;
            
            Debug.Log($"Setup debug wrl watcher for: {GetWrlPath()}");
        }

        public void MarkWrlForReload()
        {
            ShouldReloadWrl = true;
            Debug.Log($"Loading debugdata from: {GetWrlPath()}");
        }

        public void Update()
        {
            if (DebugWrlWatcher == null) {
                WatchForWrlFile();
                ShouldReloadWrl = true;
            }
            
            if (ShouldReloadWrl) {
                ReloadWrl();
                ShouldReloadWrl = false;
            }
        }

        public void ReloadWrl()
        {
            if (File.Exists(GetWrlPath())) {
                DebugGeo = new();
                DebugGeo.LoadFromWrl(GetWrlPath());
            }
            else {
                DebugGeo = null;
            }
        }

        private void OnDrawGizmos() //OnRenderObject()
        {
            if (DisplayDebugGeo) {
                DrawDebugGeo();
            }
        }

        static Material lineMaterial;

        static void CreateLineMaterial()
        {
            if (!lineMaterial) {
                // Unity has a built-in shader that is useful for drawing
                // simple colored things.
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                lineMaterial           = new Material(shader);
                lineMaterial.hideFlags = HideFlags.HideAndDontSave;
                // Turn on alpha blending
                lineMaterial.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
                lineMaterial.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                // Turn backface culling off
                lineMaterial.SetInt("_Cull", (int) UnityEngine.Rendering.CullMode.Off);
                // Turn off depth writes
                lineMaterial.SetInt("_ZWrite", 0);
            }
        }

        public void DrawDebugGeo()
        {
            CreateLineMaterial();

            var scale = new Vector3(Statics.ExportScale, -Statics.ExportScale, Statics.ExportScale);
            var rot   = Quaternion.Euler(new Vector3(-90, 0, 0));

            if (DebugGeo != null) {
                foreach (var debugItem in DebugGeo.Items) {
                    if (DebugGeoData.Item.ItemFlags.Line.HasFlag(debugItem.Flags)) {
                        var startPoint = rot * Vector3.Scale(scale, debugItem.Verts[debugItem.Indices[0]]);
                        for (int i = 1; i < debugItem.Indices.Count; i++) {
                            var point = rot * Vector3.Scale(scale, debugItem.Verts[debugItem.Indices[i]]);
                            Debug.DrawLine(startPoint, point, debugItem.Color);
                        }
                    }
                    else {
                        GL.PushMatrix();
                        GL.MultMatrix(transform.localToWorldMatrix);
                        lineMaterial.SetPass(0);
                        GL.wireframe = false;
                        GL.Begin(GL.TRIANGLES);
                        GL.Color(debugItem.Color);

                        /*var vert1 = rot * Vector3.Scale(scale, face.Verts[0]);
                        var vert2 = rot * Vector3.Scale(scale, face.Verts[1]);
                        var vert3 = rot * Vector3.Scale(scale, face.Verts[2]);
                        */
                        //Handles.Label(vert1, $"\n{vert1}\n{vert2}\n{vert3}");

                        for (int i = 0; i < debugItem.Indices.Count; i++) {
                            var vert = rot * Vector3.Scale(scale, debugItem.Verts[debugItem.Indices[i]]);
                            //Gizmos.DrawSphere(vert, 0.1f);
                            //Handles.Label(vert, $"{new string('\n', faceidx + i)} faceidx: {faceidx}: idx: {i}, vert: {vert.ToString()}, indice: {face.Indices[i]}, org vert pos: {face.Verts[face.Indices[i]]}");
                            GL.Vertex(vert);
                        }

                        GL.End();
                        GL.PopMatrix();
                    }
                    
                    // Draw a small sphere on the verts to help make it easyier to see multiple problems near each other :>
                    foreach (var vert in debugItem.Verts) {
                        var pos = rot * Vector3.Scale(scale, vert);
                        Gizmos.color = debugItem.Color;
                        Gizmos.DrawSphere(pos, 0.02f);
                    }
                }
            }
        }
    }
}