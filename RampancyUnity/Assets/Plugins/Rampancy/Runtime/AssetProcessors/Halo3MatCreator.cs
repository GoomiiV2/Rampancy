using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Rampancy.AssetProcessors
{
    public class Halo3MatCreator : AssetPostprocessor
    {
        void OnPostprocessTexture(Texture2D texture)
        {
            
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            /*foreach (var imported in importedAssets)
            {
                if (imported.Contains("Halo3/TagData") && !imported.EndsWith("_mat"))
                {
                    var matName = Path.GetFileNameWithoutExtension(imported);
                    if (matName.EndsWith("_00"))
                    {
                        matName = matName.Replace("_00", "");
                    }

                    if (matName.EndsWith("_diff"))
                    {
                        matName = matName.Replace("_diff", "");
                    }

                    if (matName.EndsWith("_diffuse"))
                    {
                        matName = matName.Replace("_diffuse", "");
                    }

                    var path = Path.Combine(Path.GetDirectoryName(imported), matName);

                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(imported);
                    if (texture != null)
                    {
                        Actions.CreateBasicMat(texture, path);
                    }
                }
            }*/
        }
    }
}
