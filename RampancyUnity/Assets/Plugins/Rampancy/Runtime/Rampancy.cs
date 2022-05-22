using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Rampancy.Halo1;
using Rampancy.Halo3;
using Rampancy.Halo3_ODST;
using Rampancy.RampantC20;
using Rampancy.UI;
using RampantC20;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Rampancy
{
    [InitializeOnLoad]
    public class Rampancy
    {
        public static Config  Cfg     = new();
        public static AssetDb AssetDB = null;

        public static Dictionary<GameVersions, GameImplementationBase> GameImplementation = new()
        {
            {GameVersions.Halo1Mcc, new Halo1Implementation()},
            {GameVersions.Halo3, new Halo3Implementation()},
            {GameVersions.Halo3ODST, new Halo3ODSTImplementation()}
        };

        public static ToolTasker ToolTaskRunner = new ();

        public static GameImplementationBase CurrentGameImplementation => GameImplementation[Cfg.GameVersion];

        public static Halo1Implementation Halo1Implementation     => GameImplementation[GameVersions.Halo1Mcc] as Halo1Implementation;
        public static Halo3Implementation Halo3Implementation     => GameImplementation[GameVersions.Halo3] as Halo3Implementation;
        public static Halo3Implementation Halo3ODSTImplementation => GameImplementation[GameVersions.Halo3ODST] as Halo3ODSTImplementation;

        public static GameVersions ActiveGameVersion => Cfg.GameVersion;

        public static string BaseUnityDir => Path.Combine("Assets", $"{Cfg.GameVersion}");
        public static string SceneDir     => Path.Combine(BaseUnityDir, Statics.SrcLevelsName);
        public static string TagsDbPath   => Path.Combine(Cfg.ActiveGameConfig.UnityBaseDir, "TagsDb.json");

        static Rampancy()
        {
            Init();
        }

        public static void Init()
        {
            Cfg = Config.Load();

            if (Cfg == null) {
                Cfg = new Config();

                EditorApplication.CallbackFunction showSettings = null;
                showSettings = () =>
                {
                    Settings.ShowWindow();
                    EditorApplication.update -= showSettings;
                };
                EditorApplication.update += showSettings;
            }
            else {
                SetupTagDb();
            }

            EditorSceneManager.sceneSaving += (scene, path) =>
            {
                var rampancySentinelGO = GameObject.Find(RampancySentinel.NAME);
                if (rampancySentinelGO != null) {
                    var rampancySentinel = rampancySentinelGO.GetComponent<RampancySentinel>();
                    rampancySentinel.BuildMatIdToPathList();
                }

                var meshes                                  = Object.FindObjectsOfType<GameObject>().Where(x => x.name == "[generated-meshes]");
                foreach (var mesh in meshes) mesh.hideFlags = HideFlags.DontSaveInEditor;
            };

            EditorSceneManager.sceneOpened += (scene, mode) => { Actions.UpdateSceneMatRefs(); };

            Selection.selectionChanged += SelectionChanged;

            EditorApplication.update += () =>
            {
                ToolTaskRunner.MainThreadTick();
            };
        }

        public static void SetupTagDb()
        {
            AssetDB = new(Cfg.ActiveGameConfig.ToolBasePath);
            AssetDB.OnTagChanged += (changeType, path) =>
            {
                var ext = Path.GetExtension(path)[1..];
                //var tagPath = path[(Cfg.ActiveGameConfig.TagsPath.Length+1)..];
                CurrentGameImplementation.OnTagChanged(path, ext, changeType);
                Debug.Log($"Tag {changeType}: {path}");
            };

            AssetDB.LoadDb(TagsDbPath);
            //AssetDB.CheckForChanges(TagsDbPath);
        }

        public static void AssetDBSave() => AssetDB.Save(TagsDbPath);

        public static void AssetDBCheckForChanges() => AssetDB.CheckForChanges(TagsDbPath);

        // Import tag changes
        public static void TagSync()
        {
            //AssetDatabase.StartAssetEditing();
            Debug.Log("Checking for asset changes");
            AssetDBCheckForChanges();
            Debug.Log("Checking for asset changes, done");
            //AssetDatabase.StopAssetEditing();
            //AssetDatabase.Refresh();
        }

        private static Transform LastSelectedTransform = null;

        private static void SelectionChanged()
        {
            // If its a prefab, and the parent is an instance, select the instance object
            // Don't select the instance if the instance was the last thing selected, the user really wanted the prefab then
            if (Selection.activeGameObject       != null
             && Selection.activeTransform.parent != null
             && PrefabUtility.IsPartOfAnyPrefab(Selection.activeGameObject)
             && Selection.activeTransform.parent.GetComponent<Instance>() != null
             && Selection.activeTransform.parent                          != LastSelectedTransform)
                EditorApplication.delayCall += () => Selection.activeTransform = Selection.activeTransform.parent;

            LastSelectedTransform = Selection.activeTransform;
        }

        public static void RunToolCommand(string cmdStr)
        {
            RunProgram(Cfg.ActiveGameConfig.ToolPath, cmdStr);
        }

        // Run a program like tool hidden and log the output
        public static void RunProgram(string program, string cmd, bool dontLog = false, bool hideWidnow = false)
        {
            try {
                ToolOutput.LogInfo($"Running command: {Path.GetFileName(program)} {cmd}");

                var ps = new ProcessStartInfo();
                ps.WorkingDirectory       = Cfg.ActiveGameConfig.ToolBasePath;
                ps.FileName               = program;
                ps.Arguments              = cmd;
                ps.RedirectStandardOutput = !dontLog;
                ps.RedirectStandardError  = !dontLog;
                ps.UseShellExecute        = false;
                ps.CreateNoWindow         = hideWidnow;
                ps.WindowStyle            = ProcessWindowStyle.Hidden;
                var process = Process.Start(ps);

                if (!dontLog) {
                    process.OutputDataReceived += (_, args) => ToolOutput.LogInfo(args.Data);  //Debug.Log(args.Data);
                    process.ErrorDataReceived  += (_, args) => ToolOutput.LogError(args.Data); //Debug.LogError(args.Data);

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }

                process.WaitForExit();
            }
            catch (Exception e) {
                Debug.LogError(e.ToString());
            }
        }

        // Launch a program like tag test to be visible
        public static Process LaunchProgram(string program, string cmd)
        {
            try {
                ToolOutput.LogInfo($"Launching program: {Path.GetFileName(program)} {cmd}");

                var ps = new ProcessStartInfo();
                ps.WorkingDirectory       = Cfg.ActiveGameConfig.ToolBasePath;
                ps.FileName               = program;
                ps.Arguments              = cmd;
                ps.RedirectStandardOutput = true;
                ps.RedirectStandardError  = true;
                ps.UseShellExecute        = false;
                ps.WindowStyle            = ProcessWindowStyle.Normal;
                var process = Process.Start(ps);

                process.OutputDataReceived += (_, args) => ToolOutput.LogInfo(args.Data);
                process.ErrorDataReceived  += (_, args) => ToolOutput.LogError(args.Data);

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                return process;
            }
            catch (Exception e) {
                Debug.LogError(e.ToString());
                return null;
            }
        }

        public static Process LaunchCMD(string cmd)
        {
            try {
                var ps = new ProcessStartInfo();
                ps.WorkingDirectory = Cfg.ActiveGameConfig.ToolBasePath;
                ps.FileName         = "cmd.exe";
                ps.Arguments        = cmd;
                ps.UseShellExecute  = true;
                ps.WindowStyle      = ProcessWindowStyle.Normal;
                var process = Process.Start(ps);

                return process;
            }
            catch (Exception e) {
                Debug.LogError(e.ToString());
                return null;
            }
        }

        // Run a tool and capture the output to std out as a string
        public static string GetToolOutput(string toolPath, string args)
        {
            var process = new Process();
            process.StartInfo.FileName               = toolPath;
            process.StartInfo.Arguments              = args;
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError  = true;
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var err    = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return output;
        }
    }
}