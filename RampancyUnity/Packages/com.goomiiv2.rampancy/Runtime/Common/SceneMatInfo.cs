using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rampancy.Common
{
    public class SceneMatInfo
    {
        public string   Name       = null;
        public string   MatPath    = null;
        public Material Mat        = null;
        public bool     IsInPrefab = false;
        public string   PrefabPath = null;

        public virtual string GetDisplayName() => Name;

        // Get the path where this copy would go
        public string GetCopyPath(string name)
        {
            var basePath   = IsInPrefab ? Path.GetDirectoryName(PrefabPath) : Path.GetDirectoryName(SceneManager.GetActiveScene().path);
            var newMatPath = Path.Combine(basePath, "mats", $"{name}.asset");

            return newMatPath;
        }

        public bool DoesCopyExist(string name)
        {
            var matPath    = GetCopyPath(name);
            var fileExists = File.Exists(matPath);

            return fileExists;
        }
        
        public Material MakeCopy(string name)
        {
            var newMat = new Material(Mat);
            newMat.name = name;

            var newMatPath = GetCopyPath(name);

            //AssetDatabase.CopyAsset(MatPath, newMatPath);
            AssetDatabase.CreateAsset(newMat, newMatPath);
            CreateOrCopyBaseMetaProps(newMatPath);

            return newMat;
        }

        protected virtual void CreateOrCopyBaseMetaProps(string path)
        {
        }

        protected bool Equals(SceneMatInfo other)
        {
            return Name == other.Name && MatPath == other.MatPath && Equals(Mat, other.Mat) && IsInPrefab == other.IsInPrefab && PrefabPath == other.PrefabPath;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SceneMatInfo) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, MatPath, Mat, IsInPrefab, PrefabPath);
        }
    }
}