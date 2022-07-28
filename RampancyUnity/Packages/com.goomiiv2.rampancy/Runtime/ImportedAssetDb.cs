using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Rampancy
{
    // Keep track of any assets that were imported and need to be imported on change etc
    public class ImportedAssetDb
    {
        [JsonIgnore] private string                  Path = string.Empty;
        private              Db                      Imported;
        private              Task                    DelayedSave;
        private              CancellationTokenSource SaveCancelToken = new();
        private              PostponableAction       PostponableSave;

        public ImportedAssetDb(string path)
        {
            Imported = new();
            Path     = path;

            PostponableSave = new(TimeSpan.FromSeconds(5), Save);
        }

        public void Load()
        {
            try {
                var txt = File.ReadAllText(Path);
                Imported = JsonConvert.DeserializeObject<Db>(txt) ?? new Db();
            }
            catch (Exception) {
            }
        }

        public void Save()
        {
            var txt = JsonConvert.SerializeObject(Imported, Formatting.Indented);
            File.WriteAllText(Path, txt);
        }

        public void MarkForSave() => PostponableSave.Invoke();

        // Add a new tag of a type to be tracked as imported
        public void Add(ImportedAsset importedAsset, string type)
        {
            lock (Imported.Assets) {
                if (Imported.Assets.TryGetValue(type, out var tags)) {
                    if (!tags.Any(x => x.Path == importedAsset.Path)) {
                        tags.Add(importedAsset);
                    }
                }
                else {
                    var tagList = new List<ImportedAsset>();
                    tagList.Add(importedAsset);
                    Imported.Assets.Add(type, tagList);
                }
            }

            PostponableSave.Invoke();
        }

        public void Add(string path, string type)
        {
            var asset = new ImportedAsset(path);
            Add(asset, type);
        }

        public ImportedAsset Get(string tagPath, string type)
        {
            lock (Imported.Assets) {
                if (Imported.Assets.TryGetValue(type, out var tags)) {
                    return tags.FirstOrDefault(x => x.Path == tagPath);
                }
            }

            return null;
        }

        // remove a tag path of a type from the db
        // TODO: return a list of tags that can be removed since nothing else referances them
        public void Remove(string tagPath, string type)
        {
            var fullTagPath = $"{tagPath}.{type}";
            
            lock (Imported.Assets) {
                if (Imported.Assets.TryGetValue(type, out var tags)) {
                    var removedCount = tags.RemoveAll(x => x.Path == fullTagPath);
                }
            }

            PostponableSave.Invoke();
        }

        public List<ImportedAssetRef> GetAssetsReferencing(string inTagPath, string inTagType)
        {
            var refs = new List<ImportedAssetRef>();

            foreach (var (tagType, tags) in Imported.Assets) {
                foreach (var tagData in tags) {
                    foreach (var tagRef in tagData.Refs) {
                        if (tagRef.TagType == inTagType && tagRef.TagPath == inTagPath) {
                            refs.Add(new ImportedAssetRef
                            {
                                TagType = tagType,
                                TagPath = System.IO.Path.GetFileNameWithoutExtension(tagData.Path)
                            });
                        }
                    }
                }
            }
            
            return refs;
        }

        public bool IsAssetReferanced(string tagType, string tagPath) => GetAssetsReferencing(tagType, tagPath).Count > 0;

        // Check if a tag of a type is imported
        public bool IsImported(string tagPath, string type = null)
        {
            if (type == null) {
                type = System.IO.Path.GetExtension(tagPath)[1..];
            }

            lock (Imported.Assets) {
                if (Imported.Assets.TryGetValue(type, out var tags)) {
                    return tags.Any(x => x.Path == tagPath);
                }
            }

            return false;
        }

        public void AddRefToEntry(ImportedAsset parent, string refPath, string refType) => AddRefToEntry(parent.Path, System.IO.Path.GetExtension(parent.Path)[1..], refPath, refType);

        public void AddRefToEntry(string parentPath, string parentType, string refPath, string refType)
        {
            lock (Imported.Assets) {
                if (Imported.Assets.TryGetValue(parentType, out var tagsOfType)) {
                    var entry = tagsOfType.FirstOrDefault(x => x.Path == parentPath);
                    if (entry != null) {
                        if (!entry.Refs.Any(x => x.TagPath == refPath && x.TagType == refType)) {
                            entry.Refs.Add(new ImportedAssetRef
                            {
                                TagType = refType,
                                TagPath = refPath
                            });

                            MarkForSave();
                        }
                    }
                }
            }
        }

        public class Db
        {
            // type, path
            public Dictionary<string, List<ImportedAsset>> Assets = new();
        }

        public class ImportedAsset
        {
            public string                 Path;
            public List<ImportedAssetRef> Refs;

            public ImportedAsset(string path)
            {
                Path = path;
                Refs = new();
            }

            public void AddRef(string tagPath, string tagType)
            {
                if (Refs.Any(x => x.TagPath != tagPath)) {
                    Refs.Add(new ImportedAssetRef
                    {
                        TagType = tagType,
                        TagPath = tagPath
                    });
                }
            }
        }


        public class ImportedAssetRef
        {
            public string TagType;
            public string TagPath;

            [JsonIgnore] public string FullTagPath => $"{TagPath}.{TagType}";
        }
    }
}