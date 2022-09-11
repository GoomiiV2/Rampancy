using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Rampancy.RampantC20.HaloReach;
using RampantC20;
using RealtimeCSG.Components;
using UnityEditor;
using UnityEngine;
using UnityFBXExporter;
using Vector3 = UnityEngine.Vector3;

namespace Rampancy.Halo_Reach
{
    public class HaloReachImplementation : GameImplementationBase
    {
        public virtual  GameVersions GameVersion           => GameVersions.HaloReach;
        public override string       GetUnityBasePath()    => Path.Combine("Assets", $"{GameVersions.HaloReach}");
        public override bool         CanOpenTagTest()      => true;
        public override bool         CanCompileLightmaps() => false;
        public override bool         CanExportScene()      => true;

        public override void OpenInTagTest(string mapPath = null)
        {
            const string INIT_FILE_NAME = "bonobo_init.txt";

            if (mapPath == null) {
                var rs = RampancySentinel.GetOrCreateInScene();
                mapPath = $@"{rs.DataDir}\{rs.LevelName}".Replace("/", @"\");;
            }

            try {
                var sb = new StringBuilder();

                var tagTestDir  = Rampancy.Cfg.HaloReachGameConfig.ToolBasePath;
                var initTxtPath = Path.Combine(tagTestDir, "init.txt");
                if (File.Exists(initTxtPath)) {
                    var initTxt = File.ReadAllText(initTxtPath);
                    sb.AppendLine(initTxt);
                }

                sb.AppendLine("framerate_throttle 1");
                sb.AppendLine($"game_start {mapPath}");

                var rampancyInitPath = Path.Combine(tagTestDir, INIT_FILE_NAME);
                File.WriteAllText(rampancyInitPath, sb.ToString());

                Rampancy.LaunchProgram(Rampancy.Cfg.HaloReachGameConfig.TagTestPath, $"-windowed");

                Debug.Log("Launched Halo Reach Tag Test");
            }
            catch (Exception e) {
                Debug.LogError($"Error launching tag test for Reach: {e}");
            }
        }

        public override void CreateNewScene(string name, string location, GameVersions gameVersion, Action customAction = null)
        {
            base.CreateNewScene(name, location, gameVersion, () =>
            {
                var rs = RampancySentinel.GetOrCreateInScene();
                rs.LevelName   = name;
                rs.DataDir     = $"levels/{location}/{name}";
                rs.GameVersion = GameVersion;
            });
            
            // Create dir and sidecar
            var reachLevelBase = $"{Rampancy.Cfg.GetGameConfig(gameVersion).DataPath}\\levels\\{location}\\{name}";
            var topLevelDirs   = new [] { "structure", "scripts", "bitmaps" };
            Directory.CreateDirectory(reachLevelBase);
            foreach (var dirName in topLevelDirs) {
                Directory.CreateDirectory(Path.Join(reachLevelBase, dirName));
            }
            Directory.CreateDirectory(Path.Join(reachLevelBase, "structure", "000"));

            var sidecarPath = Path.Combine(reachLevelBase, $"{name}.sidecar.xml");
            var sidecar     = Sidecar.CreateStructureSidecar(name, $"levels\\{location}");
            sidecar.Save(sidecarPath);
        }
        
        public override void CompileStructure()
        {
            var rs             = RampancySentinel.GetOrCreateInScene();
            var reachLevelBase = $"{Rampancy.Cfg.GetGameConfig(GameVersions.HaloReach).DataPath}\\{rs.DataDir}";
            
            var exportMeshPath = Path.Combine(reachLevelBase, "structure", "000", $"{rs.LevelName}_000.fbx");
            ExportScene(exportMeshPath);
            
            var convertFbxCmd = $"fbx-to-gr2 {exportMeshPath}";
            Rampancy.RunToolCommand(convertFbxCmd);
            
            var sidecarPath    = Path.Combine(rs.DataDir, $"{rs.LevelName}.sidecar.xml");
            var importLevelCmd = $"import {sidecarPath}";
            Rampancy.RunToolCommand(importLevelCmd);

            Debug.Log("Compiled Halo Reach structure");
        }
        
        public override void ExportScene(string path = null)
        {
            path ??= Utils.OpenFileDialog("Export FBX file", "fbx");

            var meshData  = LevelExporter.GetRcsgMesh();
            var fixedMesh = LevelExporter.FixTJunctionsV2(meshData.mesh);
            var tempGo    = new GameObject("Temp Export Mesh");
            var model     = tempGo.AddComponent<MeshRenderer>();
            var mf        = tempGo.AddComponent<MeshFilter>();

            model.materials = new Material[meshData.matNames.Length];
            for (int i = 0; i < meshData.matNames.Length; i++) {
                model.materials[i] = null;
            }

            mf.sharedMesh = fixedMesh;

            tempGo.transform.localScale = Vector3.one * Statics.ImportScale;
            FBXExporter.ExportGameObjToFBX(tempGo, path, false, false, false);
            
            GameObject.DestroyImmediate(tempGo);
        }
    }
}