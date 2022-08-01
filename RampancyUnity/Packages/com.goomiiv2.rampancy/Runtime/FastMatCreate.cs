using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Rampancy.Halo3;
using RampantC20;

namespace Rampancy
{
    // A hack to create a mat asset fast, just wite the text to disk
    public static class FastMatCreate
    {
        public static string Halo3_BasicMatTemplate;
        public static string Halo3_BasicMatTemplateTrans;
        public static string Halo3_AdvMatTemplate;
        public static string Halo3_AdvMatTemplateTrans;

        static FastMatCreate()
        {
            var halo3TemplatesBase = Path.GetFullPath($"Packages/{Statics.PackageName}/Assets/Halo3/Shader Material Templates");
            Halo3_BasicMatTemplate      = File.ReadAllText(Path.Combine(halo3TemplatesBase, "BasicMat.txt"));
            Halo3_BasicMatTemplateTrans = File.ReadAllText(Path.Combine(halo3TemplatesBase, "BasicMatTrans.txt"));
            Halo3_AdvMatTemplate        = File.ReadAllText(Path.Combine(halo3TemplatesBase, "AdvnacedMat.txt"));
            Halo3_AdvMatTemplateTrans   = File.ReadAllText(Path.Combine(halo3TemplatesBase, "AdvancedMatTrans.txt"));
        }

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

        public static string CreateBasicMat(GameVersions gameVersion, string matPath, BasicShaderData shaderData, string[] tags = null)
        {
            return CreateHalo3BasicMat(matPath, shaderData, tags, gameVersion);
        }


        public static string CreateHalo3BasicMat(string matPath, BasicShaderData shaderData, string[] tags = null, GameVersions gameVersion = GameVersions.Halo3)
        {
            var cfg               = (H3GameConfig) Rampancy.Cfg.GetGameConfig(gameVersion);
            var isOdst            = gameVersion == GameVersions.Halo3ODST;
            var matName           = Path.GetFileName(matPath).Replace(".asset", "");
            var shaderGuid        = TagPathHash.GetHash(shaderData.TagPath.Replace("/", "\\"), gameVersion);
            var baseTexGuid       = TagPathHash.GetHash(shaderData.DiffuseTex, gameVersion);
            var bumpTexGuid       = TagPathHash.GetHash(shaderData.BumpTex, gameVersion);
            var bumpDetailTexGuid = TagPathHash.GetHash(shaderData.BumpDetailTex, gameVersion);
            var detailTexGuid     = TagPathHash.GetHash(shaderData.DetailTex, gameVersion);
            var selfIllumTexGuid  = TagPathHash.GetHash(shaderData.SelfIllum, gameVersion);

            var texType = isOdst ? 2 : 1;
            var vars = new Dictionary<string, string>
            {
                {"MatName", matName},
                {"ShaderGuid", shaderGuid},
                {"BaseTex", TexRefLine(baseTexGuid, texType)},
                {"BaseTexScale", $"{shaderData.BaseMapScale}"},
                {"TexType", $"{1}"},
                {"BumpTex", TexRefLine(bumpTexGuid, 3)},
                {"BumpDetailTex", TexRefLine(bumpDetailTexGuid, 3)},
                {"DetailTex", TexRefLine(detailTexGuid, texType)},
                {"SelfIllumTex", TexRefLine(selfIllumTexGuid, texType)}
            };

            var templateStr = shaderData.IsAlphaTested ? Halo3_BasicMatTemplateTrans : Halo3_BasicMatTemplate;
            if (cfg.CreateAdvancedShaders) {
                templateStr = shaderData.IsAlphaTested ? Halo3_AdvMatTemplateTrans : Halo3_AdvMatTemplate;
            }

            var matStr = ReplaceTemplateVars(templateStr, vars);

            File.WriteAllText(matPath, matStr);

            CreateMatMeta(matPath, shaderGuid, tags);

            return shaderGuid;
        }

        private static void CreateMatMeta(string path, string shaderGuid, string[] tags = null)
        {
            tags = tags ?? new string[] { };

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

            File.WriteAllText($"{path}.meta", matMetaStr);
        }

        private static string ReplaceTemplateVars(string template, Dictionary<string, string> vars)
        {
            var result = Regex.Replace(template, @"\{\{(.+?)\}\}", m => vars[m.Groups[1].Value]);
            return result;
        }

        private static string TexRefLine(string guid, int texType = 1)
        {
            return guid == null ? "m_Texture: {fileID: 0}" : $"m_Texture: {{fileID: 2800000, guid: {guid}, type: {texType}}}";
        }
    }
}