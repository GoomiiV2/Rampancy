using System.Collections.Generic;
using System.IO;

namespace Plugins.Rampancy.RampantC20
{
    // Scan and find tags / data in Halo
    public class AssetDb
    {
        public string BasePath;
        public string BaseTagDir  => Path.Combine(BasePath, "tags");
        public string BaseDataDir => Path.Combine(BasePath, "data");

        public Dictionary<string, List<TagInfo>> TagLookup = new();

        public void ScanTags()
        {
            TagLookup.Clear();
            var tagDir  = Runtime.Rampancy.Config.ActiveGameConfig.TagsPath;
            var dirInfo = new DirectoryInfo(tagDir);
            var files   = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            foreach (var fileInfo in files) {
                var type = fileInfo.Extension.TrimStart('.');
                var tagInfo = new TagInfo
                {
                    Name = Path.GetFileNameWithoutExtension(fileInfo.Name),
                    Path = fileInfo.FullName
                };

                if (!TagLookup.ContainsKey(type)) TagLookup[type] = new List<TagInfo>();
                TagLookup[type].Add(tagInfo);
            }
        }

        // Get a tag by name
        public TagInfo? FindTag(string name, string tagType)
        {
            if (!TagLookup.TryGetValue(tagType, out var ofType)) return null;
            foreach (var tagInfo in ofType) {
                if (tagInfo.Name == name) {
                    return tagInfo;
                }
            }

            return null;
        }

        // Get all the tags of a type, eg. bitmap, shader
        public IEnumerable<TagInfo> TagsOfType(string tagType)
        {
            if (!TagLookup.TryGetValue(tagType, out var ofType)) return null;
            return ofType;
        }
        
        public IEnumerable<TagInfo> TagsOfType(params  string[] tagTypes)
        {
            var tagInfos = new List<TagInfo>();

            foreach (var tagType in tagTypes) {
                if (TagLookup.TryGetValue(tagType, out var ofType)) {
                    tagInfos.AddRange(ofType);
                }
            }
            
            return tagInfos;
        }
        
        public string GetBaseTagPath(TagInfo tagInfo) => tagInfo.Path.Replace(Path.GetFullPath(Runtime.Rampancy.Config.ActiveGameConfig.TagsPath), "").TrimEnd('/', '\\');

        public struct TagInfo
        {
            public  string  Name;
            public  string  Path;

            // Give me spans!!
            public byte[]       GetFileBytes() => File.ReadAllBytes(Path);
            public BinaryReader GetReader()    => new BinaryReader(File.OpenRead(Path));
        }
    }
}