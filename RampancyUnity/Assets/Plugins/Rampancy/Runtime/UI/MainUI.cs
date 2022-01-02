using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Plugins.Rampancy.RampantC20;
using Plugins.Rampancy.Runtime.Tests;
using Rampancy.RampantC20;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Plugins.Rampancy.Runtime.UI
{
    public class LevelUI : EditorWindow
    {
        [MenuItem("Rampancy/Create New Level", false, 0)]
        public static void CreateNewLevel()
        {
            var window = CreateInstance<CreateLevelPopup>();
            window.position = new Rect(Screen.width, Screen.height, 300, 100);
            window.ShowPopup();
        }

    #region Launch

        [MenuItem("Rampancy/Launch/Sapien", false, 1)]
        public static void LaunchSapien() => RunExeIfExists(Runtime.Rampancy.Config.ActiveGameConfig.SapienPath);

        [MenuItem("Rampancy/Launch/Guerilla", false, 2)]
        public static void LaunchGuerilla() => RunExeIfExists(Runtime.Rampancy.Config.ActiveGameConfig.GuerillaPath);

        [MenuItem("Rampancy/Launch/TagTest", false, 3)]
        public static void LaunchTagTest() => RunExeIfExists(Runtime.Rampancy.Config.ActiveGameConfig.TagTestPath);

        [MenuItem("Rampancy/Launch/Tool CMD", false, 4)]
        public static void LaunchToolCmd() => Runtime.Rampancy.LaunchCMD("");

        [MenuItem("Rampancy/Launch/Open level in Tag Test _F5", false, 5)]
        public static void OpenInTagTest()
        {
            var rs   = RampancySentinel.GetOrCreateInScene();
            var path = $@"{rs.DataDir}\{rs.LevelName}".Replace("/", @"\");
            Actions.H1_LaunchTagTest(path);
        }

        public static void RunExeIfExists(string exePath)
        {
            if (File.Exists(exePath)) {
                Rampancy.LaunchProgram(exePath, "");
            }
        }

    #endregion

    #region Compile

        // Export the jms and compile to a bsp
        [MenuItem("Rampancy/Compile/Structure", false, 2)]
        public static void CompileStructure()
        {
            Actions.H1_CompileStructure();
        }

        [MenuItem("Rampancy/Compile/Preview lightmaps", false, 2)]
        public static void CompilePreviewLightmaps() => Actions.H1_CompileToolLightmaps(false, 0.1f);

        [MenuItem("Rampancy/Compile/Structure and Preview lightmaps _F6", false, 2)]
        public static void CompileStructureAndPreviewLightmaps()
        {
            Actions.H1_CompileStructure();
            CompilePreviewLightmaps();
        }

    #endregion

    #region Import / Export

        [MenuItem("Rampancy/Import-Export/Import Jms", false, 3)]
        public static void ImportJms() => Actions.ImportJmsDialog();

        [MenuItem("Rampancy/Import-Export/Export Jms", false, 3)]
        public static void ExportJms() => Actions.ExportLevelJmsDialog();

        [MenuItem("Rampancy/Import-Export/Export Jms Collision", false, 3)]
        public static void ExportJmsCollision() => Actions.ExportLevelCollisionJmsDialog();

    #endregion

    #region Help
        [MenuItem("Rampancy/Help/Rampancy Docs", false, 4)]
        public static void HelpRampancyDocs() => Application.OpenURL("https://github.com/GoomiiV2/Rampancy/wiki");
        
        [MenuItem("Rampancy/Help/Realtime CSG Docs", false, 4)]
        public static void HelpRealtimeCsgDocs() => Application.OpenURL("https://realtimecsg.com");
    #endregion

    #region Debug

        [MenuItem("Rampancy/Debug/Debug UI", false, 4)]
        public static void ShowWindow() => GetWindow(typeof(LevelUI));

        /*[MenuItem("Rampancy/Debug/Dmesh Test", false, 4)]
        public static void DebugDMeshTest()
        {
            var go = new GameObject("DMesh Preview");
            go.transform.position = new Vector3(200, 0, 0);
            
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();

            var meshdata = LevelExporter.GetRcsgMesh();
            Debug.Log($"Mesh before: verts: {meshdata.mehs.vertices.Length}, tris: {meshdata.mehs.triangles.Length}");
            
            var dmesh    = DMeshTools.MeshToDMesh(meshdata.mehs);
            var ops = new gs.RemesherPro(dmesh);
            ops.EnableCollapses = true;
            ops.SmoothType      = Remesher.SmoothTypes.Cotan;
            ops.EnableSmoothing = false;
            ops.MaxEdgeLength   = 1000000000f;
            //ops.FastestRemesh(2, false);

            var repair = new gs.MeshAutoRepair(ops.Mesh);
            repair.RemoveMode = MeshAutoRepair.RemoveModes.None;
            //repair.Apply();
            
            MergeCoincidentEdges merge = new MergeCoincidentEdges(dmesh);
            merge.OnlyUniquePairs = false;
            merge.MergeDistance   = 0.0000001f;
            merge.Apply();
            
            float quality        = 0.2f;
            var   meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
            meshSimplifier.SimplificationOptions = new SimplificationOptions()
            {
                //EnableSmartLink = true
                PreserveBorderEdges = true
            };
            
            meshSimplifier.Initialize(meshdata.mehs);
            meshSimplifier.SimplifyMesh(quality);
            var destMesh = meshSimplifier.ToMesh();


             var mesh     = DMeshTools.DMeshToMesh(merge.Mesh);

             var rcsgData = LevelExporter.GetRcsgMesh();
            var  jms      = JMSConverter.MeshToJms(destMesh, rcsgData.matNames); //new[] { "none" });
            
            var rs            = RampancySentinel.GetOrCreateInScene();
            var exportJmsPath = $"{Runtime.Rampancy.Config.ToolBasePath}/data/{rs.DataDir}/physics/{rs.LevelName}.jms";
            jms.Save(exportJmsPath);
            
            Debug.Log($"Mesh before: verts: {mesh.vertices.Length}, tris: {mesh.triangles.Length}");

            mf.mesh = destMesh;

            LevelExporter.AddMatsToRender(mr, meshdata.matNames);
        }*/

    #endregion

        void OnGUI()
        {
            GUILayout.Label("Rampancy Level Editor :D", EditorStyles.boldLabel);


            GUILayout.Label("Test commands", EditorStyles.boldLabel);
            if (GUILayout.Button("Import JMS")) {
                var path = EditorUtility.OpenFilePanel("JMS file", "", "jms");
                if (!string.IsNullOrEmpty(path)) {
                    var jmsModel   = JMS.Load(path);
                    var name       = Path.GetFileNameWithoutExtension(path);
                    var testGo     = new GameObject(name);
                    var meshFiler  = testGo.AddComponent<MeshFilter>();
                    var meshRender = testGo.AddComponent<MeshRenderer>();

                    meshFiler.mesh = JmsConverter.JmsToMesh(jmsModel);
                    JmsConverter.AddMatsToRender(meshRender, jmsModel);
                }
            }

            if (GUILayout.Button("Scan Tags")) {
                var path    = EditorUtility.OpenFolderPanel("Base dir", "", "");
                var assetDb = new AssetDb();
                assetDb.BasePath = path;
                assetDb.ScanTags();

                var bitmapTagInfo = assetDb.TagLookup["bitmap"].First(x => x.Name == "example_tutorial_panels");
                var colorPlate    = RampantC20.Utils.GetColorPlateFromBitMap(bitmapTagInfo);
            }

            if (GUILayout.Button("Import Bitmaps")) {
                Rampancy.Init();
                BitmapConverter.ImportBitmaps();
            }

            if (GUILayout.Button("Export Level")) {
                var sceneName = SceneManager.GetActiveScene().name;
                var path      = EditorUtility.SaveFilePanel("Save jms export", Rampancy.AssetDB.BaseDataDir, sceneName, "jms");
                var frame     = GameObject.Find("Frame");
                //frame.transform.localScale = new Vector3(1, 1, -1);
                var baseMeshs = GameObject.Find("Frame/LevelGeo/[generated-meshes]");

                var mesh     = new Mesh();
                var combines = new List<CombineInstance>(baseMeshs.transform.childCount - 1);
                var matNames = new List<string>();
                for (int i = 0; i < baseMeshs.transform.childCount; i++) {
                    var childMesh = baseMeshs.transform.GetChild(i);
                    if (childMesh.name != "[generated-collider-mesh]") {
                        var mf          = childMesh.GetComponent<MeshFilter>();
                        var mr          = childMesh.GetComponent<MeshRenderer>();
                        var trimmedName = mr.material.name.Replace(" (Instance)", "").Replace("_mat", "");
                        if (trimmedName != "Skip") {
                            var combine = new CombineInstance
                            {
                                mesh      = mf.mesh,
                                transform = childMesh.transform.localToWorldMatrix
                            };

                            matNames.Add(trimmedName);
                            combines.Add(combine);
                        }
                    }
                }

                mesh.CombineMeshes(combines.ToArray(), false);
                mesh.Optimize();

                var jms   = new JMS();
                var rot   = Quaternion.Euler(new Vector3(90, 0, 0));
                var scale = new Vector3(Statics.ImportScale, Statics.ImportScale, -Statics.ImportScale);
                jms.Nodes.Add(new JMS.Node
                {
                    Name       = "frame",
                    ChildIdx   = -1,
                    SiblingIdx = -1,
                    Rotation   = Quaternion.identity,
                    Position   = new Vector3(0, 0, 0)
                });

                jms.Regions.Add(new JMS.Region
                {
                    Name = "unnamed"
                });

                foreach (var matName in matNames) {
                    jms.Materials.Add(new JMS.Material
                    {
                        Name = matName,
                        Path = "<none>"
                    });
                }

                for (int i = 0; i < mesh.vertexCount; i++) {
                    var vert = new JMS.Vert
                    {
                        Position   = rot * Vector3.Scale(scale, mesh.vertices[i]),
                        UV         = mesh.uv[i],
                        Normal     = (rot * mesh.normals[i]),
                        NodeIdx    = -1,
                        NodeWeight = 0f
                    };

                    jms.Verts.Add(vert);
                }

                for (int i = 0; i < mesh.subMeshCount; i++) {
                    var subMesh = mesh.GetSubMesh(i);
                    for (int idx = subMesh.indexStart; idx < subMesh.indexStart + subMesh.indexCount; idx += 3) {
                        var tri = new JMS.Tri
                        {
                            RegionIdx   = 0,
                            MaterialIdx = i,
                            VertIdx0    = mesh.triangles[idx + 2],
                            VertIdx1    = mesh.triangles[idx + 1],
                            VertIdx2    = mesh.triangles[idx]
                        };

                        jms.Tris.Add(tri);
                    }
                }

                jms.Save(path);

                //frame.transform.localScale = new Vector3(1, 1, 1);
            }

            if (GUILayout.Button("Load test debug data")) {
                var debugData = new DebugGeoData();
                debugData.LoadFromWrl(@"D:\Games\Steam\steamapps\common\Chelan_1\data\levels\test\rampentTest\models/rampenttest_errors.wrl");

                Debug.Log("");
            }


            if (GUILayout.Button("Test Half Mesh")) {
                var levelMesh    = LevelExporter.GetRcsgMesh();
                var halfEdgeTest = FindObjectOfType<HalfEdgeMeshTester>();
                halfEdgeTest.Mesh         = levelMesh.mehs;
                halfEdgeTest.HalfEdgeMesh = new HalfMesh();

                var makeHalfEdgeMeshTime = Stopwatch.StartNew();
                halfEdgeTest.HalfEdgeMesh.FromUnityMesh(halfEdgeTest.Mesh);
                makeHalfEdgeMeshTime.Stop();
                Debug.Log($"FromUnityMesh took: {makeHalfEdgeMeshTime.Elapsed}");

                var tJunctions = halfEdgeTest.HalfEdgeMesh.FindTJunctions();
                foreach (var tJunction in tJunctions) {
                    halfEdgeTest.HalfEdgeMesh.SplitEdge(tJunction.Item1, halfEdgeTest.HalfEdgeMesh.Verts[tJunction.Item2].Pos);
                }

                var testGo = new GameObject();
                testGo.transform.position = new Vector3(50, 0, 0);
                var meshRender = testGo.AddComponent<MeshRenderer>();
                var meshFilter = testGo.AddComponent<MeshFilter>();

                var halfEdgeMeshToUnityMeshTime = Stopwatch.StartNew();
                meshFilter.mesh = halfEdgeTest.HalfEdgeMesh.ToMesh();
                halfEdgeMeshToUnityMeshTime.Stop();
                Debug.Log($"ToMesh took: {halfEdgeMeshToUnityMeshTime.Elapsed}");

                LevelExporter.AddMatsToRender(meshRender, levelMesh.matNames);
            }

            if (GUILayout.Button("Show T-Junctions")) {
                var levelMesh    = LevelExporter.GetRcsgMesh();
                var halfEdgeTest = FindObjectOfType<HalfEdgeMeshTester>();
                halfEdgeTest.Mesh         = levelMesh.mehs;
                halfEdgeTest.HalfEdgeMesh = new HalfMesh();
                halfEdgeTest.HalfEdgeMesh.FromUnityMesh(halfEdgeTest.Mesh);

                halfEdgeTest.RefreshTJunctionFinder = true;
                halfEdgeTest.DrawTJunctions         = true;
            }
        }
    }
}