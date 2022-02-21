using System.IO;
using Newtonsoft.Json;
using RampantC20;

namespace Rampancy
{
    public class Config
    {
        public static readonly string FileName = "RampancyConfig.json";

        public GameVersions    GameVersion              = GameVersions.Halo1Mcc;
        public bool            ToolOutputClearOnCompile = true;
        public H1MccGameConfig Halo1MccGameConfig       = new();
        public H3GameConfig    Halo3MccGameConfig       = new();

        //public string ToolBasePath = "";

        public GameConfig GetGameConfig(GameVersions version)
        {
            return version switch
            {
                GameVersions.Halo1Mcc => Halo1MccGameConfig,
                GameVersions.Halo3    => Halo3MccGameConfig,
                _                     => Halo1MccGameConfig
            };
        }

        [JsonIgnore] public GameConfig ActiveGameConfig => GetGameConfig(GameVersion);

        public static Config Load()
        {
            if (!File.Exists(FileName)) File.WriteAllText(FileName, "");

            var json   = File.ReadAllText(FileName);
            var config = JsonConvert.DeserializeObject<Config>(json);
            return config;
        }

        public void Save()
        {
            var textSettings = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(FileName, textSettings);
        }
    }

    public class GameConfig
    {
        public string ToolBasePath = "";

        [JsonIgnore] public         string ToolPath     => Path.Combine(ToolBasePath, "tool.exe");
        [JsonIgnore] public         string SapienPath   => Path.Combine(ToolBasePath, "sapien.exe");
        [JsonIgnore] public         string GuerillaPath => Path.Combine(ToolBasePath, "guerilla.exe");
        [JsonIgnore] public         string DataPath     => Path.Combine(ToolBasePath, "data");
        [JsonIgnore] public         string TagsPath     => Path.Combine(ToolBasePath, "tags");
        [JsonIgnore] public virtual string TagTestPath  => null;
    }

    public class H1MccGameConfig : GameConfig
    {
        public override string TagTestPath => Path.Combine(ToolBasePath, "halo_tag_test.exe");
    }

    public class H3GameConfig : GameConfig
    {
        public override string TagTestPath => Path.Combine(ToolBasePath, "halo3_tag_test.exe");

        [JsonIgnore] public string ToolFastPath => Path.Combine(ToolBasePath, "tool_fast.exe");
    }
}