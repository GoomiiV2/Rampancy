using System;
using System.Diagnostics;
using System.IO;
using Plugins.Rampancy.RampantC20;
using Rampancy.RampantC20;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Plugins.Rampancy.Runtime
{
    [InitializeOnLoad]
    public class Rampancy
    {
        public static Config  Config  = new();
        public static AssetDb AssetDB = new();

        public static string BaseUnityDir => Path.Combine("Assets", $"{Config.GameVersion}");
        public static string SceneDir     => Path.Combine(BaseUnityDir, Statics.SrcLevelsName);

        static Rampancy()
        {
            Init();
        }

        public static void Init()
        {
            Config = Config.Load();
            
            if (string.IsNullOrEmpty(Config.ActiveGameConfig.ToolBasePath)) {
                Config.ActiveGameConfig.ToolBasePath = EditorUtility.OpenFolderPanel($"{Config.GameVersion} base tool dir, where the data and tags folder are", "", "");
                Config.Save();
            }

            //AssetDB.BasePath = Config.ToolBasePath;
            AssetDB.ScanTags();
        }

        public static void RunToolCommand(string cmdStr)
        {
            RunProgram(Config.ActiveGameConfig.ToolPath, cmdStr);
        }

        // Run a program like tool hidden and log the output
        public static void RunProgram(string program, string cmd)
        {
            try {
                ToolOutput.LogInfo($"Running command: {Path.GetFileName(program)} {cmd}");

                var ps = new ProcessStartInfo();
                ps.WorkingDirectory       = Config.ActiveGameConfig.ToolBasePath;
                ps.FileName               = program;
                ps.Arguments              = cmd;
                ps.RedirectStandardOutput = true;
                ps.RedirectStandardError  = true;
                ps.UseShellExecute        = false;
                ps.WindowStyle            = ProcessWindowStyle.Hidden;
                var process = Process.Start(ps);

                process.OutputDataReceived += (sender, args) => ToolOutput.LogInfo(args.Data);  //Debug.Log(args.Data);
                process.ErrorDataReceived  += (sender, args) => ToolOutput.LogError(args.Data); //Debug.LogError(args.Data);

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

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
                ps.WorkingDirectory       = Config.ActiveGameConfig.ToolBasePath;
                ps.FileName               = program;
                ps.Arguments              = cmd;
                ps.RedirectStandardOutput = true;
                ps.RedirectStandardError  = true;
                ps.UseShellExecute        = false;
                ps.WindowStyle            = ProcessWindowStyle.Normal;
                var process = Process.Start(ps);

                process.OutputDataReceived += (sender, args) => ToolOutput.LogInfo(args.Data);
                process.ErrorDataReceived  += (sender, args) => ToolOutput.LogError(args.Data);

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
                ps.WorkingDirectory = Config.ActiveGameConfig.ToolBasePath;
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
    }
}