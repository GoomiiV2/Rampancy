using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Rampancy.UI;
using RampantC20;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Rampancy
{
    [InitializeOnLoad]
    public class Rampancy
    {
        public static Config  Cfg  = new();
        public static AssetDb AssetDB = new();

        public static string BaseUnityDir => Path.Combine("Assets", $"{Cfg.GameVersion}");
        public static string SceneDir     => Path.Combine(BaseUnityDir, Statics.SrcLevelsName);
        
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
                AssetDB.ScanTags(Cfg.ActiveGameConfig.TagsPath);
            }

            EditorSceneManager.sceneSaving += (scene, path) =>
            {
                var rampancySentinelGO = GameObject.Find(RampancySentinel.NAME);
                if (rampancySentinelGO != null) {
                    var rampancySentinel = rampancySentinelGO.GetComponent<RampancySentinel>();
                    rampancySentinel.BuildMatIdToPathList();
                }
                
                var meshes = GameObject.FindObjectsOfType<GameObject>().Where(x => x.name == "[generated-meshes]");
                foreach (var mesh in meshes) {
                    mesh.hideFlags = HideFlags.DontSaveInEditor;
                }
            };

            EditorSceneManager.sceneOpened += (scene, mode) =>
            {
                Actions.UpdateSceneMatRefs();
            };
        }

        public static void RunToolCommand(string cmdStr) => RunProgram(Cfg.ActiveGameConfig.ToolPath, cmdStr);

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
                ps.CreateNoWindow = hideWidnow;
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
            Process process = new Process();
            process.StartInfo.FileName               = toolPath;
            process.StartInfo.Arguments              = args;
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError  = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string err    = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return output;
        }
    }
}