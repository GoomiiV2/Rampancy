using System.IO;
using Plugins.Rampancy.Runtime;
using RampantC20.Halo3;
using UnityEditor;
using UnityEngine;

namespace Rampancy
{
    public static partial class Actions
    {
        public static void H3_ImportAssDialog()
        {
            var filePath = EditorUtility.OpenFilePanel("Import Ass file", "", "ass");
            if (!string.IsNullOrEmpty(filePath)) {
                H3_ImportAss(filePath);
            }
        }

        public static void H3_ImportAss(string path)
        {
            Debug.Log("Importing Ass :D");
            
            var ass  = Ass.Load(path);
            var name = Path.GetFileNameWithoutExtension(path);
            AssConverter.ImportToScene(ass, name);
        }
    }
}