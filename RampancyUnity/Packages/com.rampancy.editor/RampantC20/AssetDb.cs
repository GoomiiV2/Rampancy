using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace RampantC20
{
    // Scan and find tags / data in Halo
    public class AssetDb
    {
        public              string BasePath;
        [JsonIgnore] public string BaseTagDir  => Path.Combine(BasePath, "tags");
        [JsonIgnore] public string BaseDataDir => Path.Combine(BasePath, "data");

        public Dictionary<string, Dictionary<string, TagInfo>> TagLookup = new();

        [JsonIgnore] private FileSystemWatcher TagWatcher = null;

        // Events
        // When the asset db is compared to the last, fire events on tag changes
        public delegate void TagEvent(TagChangedType typ, string tagPath);

        public event TagEvent OnTagChanged;

        public AssetDb(string basePath)
        {
            BasePath = basePath;
            // URGH: File watcher is broken in Unity, for sub directories it won't report the right path, just the base dir and the file name, not the inbetween floders, making it useless
            // seems to be a mono bug that was fixed in, 2012
            //SetupTagWatcher(BaseTagDir);
        }

        public void SetupTagWatcher(string path)
        {
            if (TagWatcher != null) {
                TagWatcher.Dispose();
                TagWatcher = null;
            }

            if (path == null || !Directory.Exists(path)) {
                return;
            }

            var fullDirPath = Path.GetFullPath(path);
            TagWatcher = new(fullDirPath);
            TagWatcher.NotifyFilter = NotifyFilters.Attributes
                                    | NotifyFilters.CreationTime
                                    | NotifyFilters.FileName
                                    | NotifyFilters.LastAccess
                                    | NotifyFilters.LastWrite
                                    | NotifyFilters.Security
                                    | NotifyFilters.Size;

            TagWatcher.Changed += (sender, args) =>
            {
                var isDir   = Path.GetExtension(args.FullPath).Equals("");
                var tagPath = args.FullPath[(BaseTagDir.Length + 1)..];
                var fileExt = Path.GetExtension(tagPath)[1..];
                var tagName = tagPath[..^(fileExt.Length + 1)];

                if (isDir) return;

                var fileExistsNow = File.Exists(args.FullPath);
                var hasTag        = TagLookup.TryGetValue(fileExt, out var tags) && tags.ContainsKey(tagName);
                if (fileExistsNow && hasTag) {
                    OnTagChanged?.Invoke(TagChangedType.Changed, tagPath);
                }
                else if (!fileExistsNow && hasTag) {
                    OnTagChanged?.Invoke(TagChangedType.Deleted, tagPath);
                }
                else if (fileExistsNow && !hasTag) {
                    OnTagChanged?.Invoke(TagChangedType.Added, tagPath);
                }
            };

            TagWatcher.Filter                = "*.*";
            TagWatcher.IncludeSubdirectories = true;
            TagWatcher.EnableRaisingEvents   = true;

            System.Diagnostics.Debug.WriteLine($"Created tag watcher for: {path}");
        }

        public void ScanTags(string tagDir)
        {
            TagLookup.Clear();
            var dirInfo = new DirectoryInfo(tagDir);
            var files   = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            foreach (var fileInfo in files) {
                var type    = fileInfo.Extension.TrimStart('.');
                var relPath = fileInfo.FullName[(tagDir.Length + 1)..];
                var tagInfo = new TagInfo
                {
                    Path         = relPath,
                    LastModified = fileInfo.LastAccessTimeUtc
                };

                if (!TagLookup.ContainsKey(type)) TagLookup[type] = new Dictionary<string, TagInfo>();
                var extension                                     = Path.GetExtension(fileInfo.FullName);
                var start                                         = tagDir.Length            + 1;
                var end                                           = fileInfo.FullName.Length - (start + extension.Length);
                var tagPath                                       = fileInfo.FullName.Substring(start, end);
                TagLookup[type].Add(tagPath, tagInfo);
            }
        }

        // Do a current tag scan and compare against an old db state and raise events for changes in tags
        // not the fastest but fast enough
        public void CheckForChanges(string path)
        {
            var changes = GetTagChanges(path);
            if (changes == null) return;
            
            foreach (var tagChange in changes) {
                OnTagChanged?.Invoke(tagChange.ChangeType, tagChange.Path);
            }
        }

        public List<TagChange> GetTagChanges(string path)
        {
            var lastDb = LoadDb(path) ?? new AssetDb(BasePath);

            ScanTags(lastDb.BaseTagDir);

            var changes = new List<TagChange>();

            // flatten the tags lists
            var lastTags = lastDb.TagLookup.SelectMany(x => x.Value.Select(y => y.Value));
            var currTags = TagLookup.SelectMany(x => x.Value.Select(y => y.Value));

            var lastTagsPaths = lastTags.Select(x => x.Path);
            var currTagsPaths = currTags.Select(x => x.Path);

            var deletedTags = lastTagsPaths.Except(currTagsPaths);
            var addedTags   = currTagsPaths.Except(lastTagsPaths);

            foreach (var tagPath in deletedTags) {
                changes.Add(new TagChange(TagChangedType.Deleted, tagPath));
            }

            foreach (var tagPath in addedTags) {
                changes.Add(new TagChange(TagChangedType.Added, tagPath));
            }

            // check for changes
            var lastTagsLookup = lastTags.ToDictionary(x => x.Path, y => y);
            foreach (var tag in currTags) {
                if (lastTagsLookup.TryGetValue(tag.Path, out var lastTag)) {
                    if (tag.LastModified != lastTag.LastModified) {
                        changes.Add(new TagChange(TagChangedType.Changed, tag.Path));
                    }
                }
            }

            Save(path);

            return changes;
        }

        public void Save(string path)
        {
            var jsonData = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(path, jsonData);
        }

        public AssetDb LoadDb(string path)
        {
            if (!File.Exists(path)) return null;

            var jsonData = File.ReadAllText(path);
            var assetDb  = JsonConvert.DeserializeObject<AssetDb>(jsonData);

            return assetDb;
        }

        // Get a tag by name
        public TagInfo? FindTag(string name, string tagType)
        {
            if (!TagLookup.TryGetValue(tagType, out var ofType)) return null;
            foreach (var tagInfo in ofType.Values) {
                if (tagInfo.Name == name) {
                    tagInfo.AssetDbRef = this;
                    return tagInfo;
                }
            }

            return null;
        }

        public TagInfo? FindTagByRelPath(string relPath, string tagType)
        {
            if (!TagLookup.TryGetValue(tagType, out var ofType)) return null;
            if (ofType.TryGetValue(relPath, out var tagInfo)) {
                tagInfo.AssetDbRef = this;
                return tagInfo;
            }

            return null;
        }

        // Get all the tags of a type, eg. bitmap, shader
        public IEnumerable<TagInfo> TagsOfType(string tagType)
        {
            if (!TagLookup.TryGetValue(tagType, out var ofType)) return null;
            return ofType.Values.Select(x =>
            {
                x.AssetDbRef = this;
                return x;
            });
        }

        public IEnumerable<TagInfo> TagsOfType(params string[] tagTypes)
        {
            var tagInfos = new List<TagInfo>();

            foreach (var tagType in tagTypes) {
                var tags = TagsOfType(tagType);
                tagInfos.AddRange(tags);
            }

            return tagInfos;
        }

        public class TagInfo
        {
            [JsonIgnore] public string   Name => System.IO.Path.GetFileNameWithoutExtension(Path);
            [JsonIgnore] public AssetDb  AssetDbRef;
            [JsonIgnore] public string   FullPath => System.IO.Path.Combine(AssetDbRef.BasePath, "tags", Path);
            public              string   Path;
            public              DateTime LastModified;

            // Give me spans!!
            public byte[]       GetFileBytes() => File.ReadAllBytes(FullPath);
            public BinaryReader GetReader()    => new BinaryReader(File.OpenRead(FullPath));
        }

        public class TagChange
        {
            public TagChangedType ChangeType;
            public string         Path;

            public TagChange(TagChangedType changeType, string path)
            {
                ChangeType = changeType;
                Path       = path;
            }
        }

        public enum TagChangedType
        {
            Added,
            Deleted,
            Changed
        }
    }
}