using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Plugins.Rampancy.Runtime;
using Unity.VisualScripting;
using UnityEngine;
using Utils = RampantC20.Utils;

namespace Plugins.Rampancy.RampantC20
{
    public class DebugGeoData
    {
        public List<Item> Items = new();

        // If you want a nice proper wrl loader, then this isn't it
        // this is just to load the debug info and is far from right :D
        public void LoadFromWrl(string path)
        {
            try {
                var lines       = File.ReadAllLines(path);
                var currentItem = Item.Create();
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
                        var showAsFace = line.ToUpper().Contains("PER_FACE") /* || Utils.CalcAreaOfTri(currentItem.Verts[0], currentItem.Verts[1], currentItem.Verts[2]) > 0.001f */;
                        currentItem.Flags = showAsFace ? Item.ItemFlags.Tri : Item.ItemFlags.Line;
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
                        Items.Add(currentItem);
                        currentItem = Item.Create();
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

        public struct Item
        {
            public ItemFlags     Flags;
            public List<Vector3> Verts;
            public Color32       Color;
            public List<short>   Indices; // may not be needed

            public static Item Create()
            {
                var data = new Item
                {
                    Flags   = ItemFlags.Line,
                    Verts   = new(),
                    Color   = new Color32(255, 255, 0, 0),
                    Indices = new()
                };

                return data;
            }

            [Flags]
            public enum ItemFlags : byte
            {
                Line,
                Tri
            }
        }
    }
}