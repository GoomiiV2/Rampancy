using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RampantC20
{
    public class DebugGeoData
    {
        public List<DebugGeoMarker> Items = new();

        // If you want a nice proper wrl loader, then this isn't it
        // this is just to load the debug info and is far from right :D
        public void LoadFromWrl(string path)
        {
            try {
                var lines       = File.ReadAllLines(path);
                var currentItem = DebugGeoMarker.Create();
                var ignoreCase  = StringComparison.InvariantCultureIgnoreCase;

                var vertsRegex   = new Regex(@".*?point\[(.*)]");
                var colorRegex   = new Regex(@".*diffuseColor\[([\d.]*) ([\d.]*) ([\d.]*).*\].*transparency\[([\d.]*).*\]");
                var indicesRegex = new Regex(@".*\[(.*)\]");

                foreach (var srcLine in lines) {
                    var line = srcLine.Trim(' ', '\t');

                    if (line.StartsWith("Coordinate3", ignoreCase)) {
                        var vertsStr = vertsRegex.Match(line).Groups[1].ToString();
                        var verts    = vertsStr.Split(' ').Select(x => float.Parse(x.Trim(' ', ','))).ToArray();
                        for (int i = 0; i < verts.Length; i += 3) {
                            currentItem.Verts.Add(new Vector3(verts[i], verts[i + 1], verts[i + 2]));
                        }
                    }
                    else if (line.StartsWith("MaterialBinding", ignoreCase)) {
                        var showAsFace = line.ToUpper().Contains("PER_FACE");
                        currentItem.Flags = showAsFace ? DebugGeoMarker.ItemFlags.Tri : DebugGeoMarker.ItemFlags.Line;
                    }
                    else if (line.StartsWith("Material", StringComparison.InvariantCultureIgnoreCase)) {
                        var colors = colorRegex.Match(line).Groups;
                        currentItem.Color = new Color(float.Parse(colors[1].Value), float.Parse(colors[2].Value), float.Parse(colors[3].Value), 1f - float.Parse(colors[4].Value));
                    }
                    else if (line.StartsWith("IndexedFaceSet", StringComparison.InvariantCultureIgnoreCase) || line.StartsWith("IndexedLineSet", StringComparison.InvariantCultureIgnoreCase)) {
                        var indices = indicesRegex.Match(line).Groups[1].Value.Split(',').Select(short.Parse).Where(x => x != -1);
                        currentItem.Indices.AddRange(indices);
                    }
                    else if (line.StartsWith("}")) {
                        // Try and name
                        if (currentItem.Color.r == 255 && currentItem.Color.g == 0 && currentItem.Color.b == 0 && currentItem.Flags == DebugGeoMarker.ItemFlags.Tri) {
                            currentItem.Name = "Degenerate Tri";
                        }

                        Items.Add(currentItem);
                        currentItem = DebugGeoMarker.Create();
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        public void Clear()
        {
            Items?.Clear();
        }
    }
}