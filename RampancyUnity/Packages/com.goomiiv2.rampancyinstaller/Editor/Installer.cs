using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

// Thanks to Unity not supporting git dependencies in a package, but supporting them on a project I had to make this package that install the packages needed as well as Rampancy
// sigh
namespace RampancyInstaller
{
    [InitializeOnLoad]
    public class Installer
    {
        private static List<RequiredPackageInfo> PckgsToInstall = new ()
        {
            new("com.ionic.zlib", "https://github.com/PixelWizards/com.ionic.zlib.git"),
            new("com.prenominal.realtimecsg", "https://github.com/LogicalError/realtime-CSG-for-unity.git"),
            new("com.goomiiv2.rampancy", "https://github.com/GoomiiV2/Rampancy.git?path=RampancyUnity/Packages/com.goomiiv2.rampancy#Halo3")
        };
        
        private static int         PackageIdx          = 0;
        private static List<int>   PackagesMissingIdxs = new ();
        private static AddRequest  Request;
        private static ListRequest ListRequest;
        private static DateTime    LastPackageInstallTime;

        static Installer()
        {
            ListRequest              =  Client.List(true, false);
            EditorApplication.update += CheckIfListIsReady;
        }

        private static void CheckIfListIsReady()
        {
            if (ListRequest.IsCompleted) {
                for (int i = 0; i < PckgsToInstall.Count; i++) {
                    var pckg = PckgsToInstall[i];
                    if (!ListRequest.Result.Any(x => x.name == pckg.Name)) {
                        Debug.Log($"Package {pckg.Name} wasn't found, installing.");
                        PackagesMissingIdxs.Add(i);
                    }
                }
                
                EditorApplication.update += InstallNextPackage;
                EditorApplication.update -= CheckIfListIsReady;
            }
        }
        
        private static void InstallNextPackage()
        {
            if ((LastPackageInstallTime + TimeSpan.FromSeconds(2)) > DateTime.UtcNow) {
                return;
            }
            
            if ((Request == null || Request.IsCompleted) && PackagesMissingIdxs.Count > 0) {
                var package = PckgsToInstall[PackagesMissingIdxs[PackageIdx]];
                Request = Client.Add(package.GitUrl);
                Debug.Log($"Added package {package.Name}");
                PackageIdx++;

                LastPackageInstallTime = DateTime.UtcNow;
            }

            if (PackageIdx >= PackagesMissingIdxs.Count || PackagesMissingIdxs.Count == 0) {
                EditorApplication.update -= InstallNextPackage;
            }
        }

        public class RequiredPackageInfo
        {
            public string Name;
            public string GitUrl;

            public RequiredPackageInfo(string name, string gitUrl)
            {
                Name   = name;
                GitUrl = gitUrl;
            }
        }
    }
}