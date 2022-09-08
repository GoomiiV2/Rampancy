using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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
        public override bool         CanOpenTagTest()      => false;
        public override bool         CanCompileLightmaps() => false;
        public override bool         CanExportScene()      => true;

        public override void CreateNewScene(string name, string location, GameVersions gameVersion, Action customAction = null)
        {
            
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
            
            GameObject.Destroy(tempGo);
        }
    }
}