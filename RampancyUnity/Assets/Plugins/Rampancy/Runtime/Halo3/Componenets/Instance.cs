using UnityEngine;

namespace Rampancy.Halo3
{
    //[SelectionBaseAttribute]
    public class Instance : MonoBehaviour
    {
        public bool PerPixelLighting    = true;  // !
        public bool PerVertexLighting   = true;  // ?
        public bool GeneratePathFinding = true;  // -
        public bool WaterGroupObject    = false; // ~

        public string GetName()
        {
            var str = $"%{(PerPixelLighting ? "!" : "")}{(PerVertexLighting ? "?" : "")}{(GeneratePathFinding ? "+" : "-")}{(WaterGroupObject ? "~" : "")}{name}";
            return str;
        }
    }
}