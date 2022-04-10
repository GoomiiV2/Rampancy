using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plugins.Rampancy.Runtime;
using RampantC20;
using RealtimeCSG.Components;
using UnityEditor;
using UnityEngine;

namespace Rampancy
{
    // Store per level data
    [ExecuteInEditMode]
    public class RampancySentinel : MonoBehaviour
    {
        public const string NAME = "Rampancy Sentinel";

        public string       LevelName;
        public string       DataDir;
        public bool         DisplayDebugGeo = true;
        public GameVersions GameVersion     = GameVersions.Halo1Mcc;

        private DebugGeoData      DebugGeo        = null;
        private FileSystemWatcher DebugWrlWatcher = null;
        private bool              ShouldReloadWrl = true;

        [SerializeField] public string[] MatIdToPathLookup_Guids;
        [SerializeField] public string[] MatIdToPathLookup_Paths;

        public string GetWrlPath()
        {
            return Path.Combine(Rampancy.Cfg.ActiveGameConfig.DataPath, DataDir ?? "", "models", $"{LevelName}_errors.wrl");
        }

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

    #region Debug Geo Handling

        public void WatchForWrlFile()
        {
            var dirPath = Path.GetDirectoryName(GetWrlPath());
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

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

            //BuildMatIdToPathList();
        }

        public void ReloadWrl()
        {
            var debugGeoRoot = GameObject.Find("Frame/DebugGeo");
            DestroyImmediate(debugGeoRoot);

            if (File.Exists(GetWrlPath())) {
                DebugGeo = new DebugGeoData();
                DebugGeo.LoadFromWrl(GetWrlPath());

                foreach (var item in DebugGeo.Items) {
                    var debugGeoGO = DebugGeoObj.Create(item);
                }
            }
            else {
                DebugGeo = null;
            }
        }

        private void OnDrawGizmos() //OnRenderObject()
        {
            if (DisplayDebugGeo) {
                //DrawDebugGeo();
            }
        }

        private static Material lineMaterial;

        private static void CreateLineMaterial()
        {
            if (!lineMaterial) {
                // Unity has a built-in shader that is useful for drawing
                // simple colored things.
                var shader = Shader.Find("Hidden/Internal-Colored");
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

            if (DebugGeo != null)
                foreach (var debugItem in DebugGeo.Items) {
                    if (DebugGeoMarker.ItemFlags.Line.HasFlag(debugItem.Flags)) {
                        var startPoint = rot * Vector3.Scale(scale, debugItem.Verts[debugItem.Indices[0]]);
                        for (var i = 1; i < debugItem.Indices.Count; i++) {
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

                        for (var i = 0; i < debugItem.Indices.Count; i++) {
                            var vert = rot * Vector3.Scale(scale, debugItem.Verts[debugItem.Indices[i]]);
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

    #endregion

    #region Material tracking

        public void BuildMatIdToPathList()
        {
            var matIdLookup = new Dictionary<string, string>();
            var allBrushes  = FindObjectsOfType<CSGBrush>();
            foreach (var brush in allBrushes)
            foreach (var texGen in brush.Shape.TexGens)
                if (texGen.RenderMaterial != null) {
                    var path = AssetDatabase.GetAssetPath(texGen.RenderMaterial);
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(texGen.RenderMaterial, out var guid, out long localId))
                        if (!matIdLookup.ContainsKey(guid))
                            matIdLookup.Add(guid, path);
                }

            MatIdToPathLookup_Guids = matIdLookup.Keys.ToArray();
            MatIdToPathLookup_Paths = matIdLookup.Values.ToArray();
        }

        public Dictionary<string, string> GetMatIdToPathLookup()
        {
            var matIdToPathLookup = new Dictionary<string, string>();

            for (var i = 0; i < MatIdToPathLookup_Guids.Length; i++) {
                var guid = MatIdToPathLookup_Guids[i];
                var path = MatIdToPathLookup_Paths[i];
                matIdToPathLookup.Add(guid, path);
            }

            return matIdToPathLookup;
        }

    # endregion
    }
}