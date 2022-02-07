using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace RampantC20.Halo3
{
    public class ShaderCollection
    {
        public         Dictionary<string, string> Mapping = new();
        private static Regex                      Reggor  = new (@"(\w+)\s+([\w\\]+)");

        public ShaderCollection(string path, bool onlyLevelShaders = false)
        {
            Load(path, onlyLevelShaders);
        }

        public void Load(string path, bool onlyLevelShaders = false)
        {
            var sr = new StreamReader(path);

            while (sr.BaseStream.CanRead) {
                var line  = sr.ReadValidLine();

                if (line == null)
                    break;
                
                //var split = line.Split(new[] {"\t\t", "\t"}, StringSplitOptions.None);
                var match = Reggor.Match(line);

                var name     = match.Groups[1].Value;
                var filePath = match.Groups[2].Value;

                if (!Mapping.ContainsKey(name)) {
                    if (!onlyLevelShaders)
                        Mapping.Add(name, filePath);
                    else if (filePath.StartsWith("levels") || filePath.StartsWith("scenarios"))
                        Mapping.Add(name, filePath);
                }
            }
        }
    }
}