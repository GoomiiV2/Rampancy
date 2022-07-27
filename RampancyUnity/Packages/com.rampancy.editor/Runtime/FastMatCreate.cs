using System.IO;
using System.Linq;

namespace Rampancy
{
    // A hack to create a mat asset fast, just wite the text to disk
    public static class FastMatCreate
    {
        public static void CreateBasicMat(string texId, string matPath, string shaderGuid, string[] tags = null, bool transparent = false, float tiling = 1f, int texType = 2)
        {
            tags = tags ?? new string[] { };
            var matName = Path.GetFileName(matPath).Replace(".asset", "");

            var shaderId = transparent ? "30" : "7";
            var matStr   = GetMatText(matName, texId, shaderId, tiling, texType);

            var matMetaStr = $@"fileFormatVersion: 2
guid: {shaderGuid}
labels:
{string.Join("\n", tags.Select(x => $"- {x}"))}
NativeFormatImporter:texType
  externalObjects: {{}}
  mainObjectFileID: 2100000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
";

            File.WriteAllText(matPath, matStr);
            File.WriteAllText($"{matPath}.meta", matMetaStr);
        }

        public static string GetMatText(string matName, string texId, string shaderId, float tiling = 1f, int texType = 2)
        {
            var matStr = $@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 7
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {matName}
  m_Shader: {{fileID: {shaderId}, guid: 0000000000000000f000000000000000, type: 0}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ShaderKeywords: 
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {{}}
  disabledShaderPasses: []
  m_LockedProperties: 
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _MainTex:
        m_Texture: {{fileID: 2800000, guid: {texId}, type: {texType}}}
        m_Scale: {{x: {tiling}, y: {tiling}}}
        m_Offset: {{x: 0, y: 0}}
    m_Ints: []
    m_Floats: []
    m_Colors:
    - _Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_BuildTextureStacks: []
";

            return matStr;
        }

        public static void CreateMetaForTexture(string filePath, string guid)
        {
            var lines = new string[]
            {
                "fileFormatVersion: 2",
                $"guid: {guid}",
                "NativeFormatImporter:",
                "  externalObjects: {}",
                "  mainObjectFileID: 2800000",
                "  userData: ",
                "  assetBundleName: ",
                "  assetBundleVariant: ",
            };
            
            File.Delete(filePath);
            File.WriteAllLines(filePath, lines);
        }
    }
}