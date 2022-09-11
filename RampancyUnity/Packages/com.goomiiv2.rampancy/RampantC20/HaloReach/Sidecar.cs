using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json.Serialization;
using RampantC20.Halo3;
using UnityEngine;

namespace Rampancy.RampantC20.HaloReach
{
    public class Sidecar
    {
        public SidecarMetaData Metadata { get; set; } = new();

        public void Save(string path)
        {
            var       serializer   = new XmlSerializer(typeof(SidecarMetaData));
            using var memoryStream = new MemoryStream();
            var streamWriter = XmlWriter.Create(memoryStream, new()
            {
                Encoding = Encoding.UTF8,
                Indent   = true
            });
            serializer.Serialize(streamWriter, Metadata);
            var result = Encoding.UTF8.GetString(memoryStream.ToArray());
            
            File.WriteAllText(path, result);

            System.Diagnostics.Debug.Write(result);
        }

        public static Sidecar CreateStructureSidecar(string name, string location)
        {
            var sidecar = new Sidecar
            {
                Metadata = new()
                {
                    Asset = new()
                    {
                        Name = name,
                        Type = "scenario",
                        OutputTagCollection = new()
                        {
                            new SidecarOutputTag("scenario", $"{location}\\{name}\\{name}"),
                            new SidecarOutputTag("scenario_lightmap", $"{location}\\{name}\\{name}_faux_lightmap"),
                            new SidecarOutputTag("structure_seams", $"{location}\\{name}\\{name}")
                        }
                    },
                    Folders = new()
                    {
                        SourceBSPs = "\\structure",
                        GameBSPs   = "\\structure"
                    },
                    FaceCollections = new List<SidecarFaceCollection>
                    {
                        new()
                        {
                            Name        = "global materials override",
                            StringTable = "connected_geometry_regions_table",
                            Description = "Global material overrides",
                            FaceCollectionEntries = new()
                            {
                                new SidecarFaceCollectionEntry
                                {
                                    Index  = "0",
                                    Name   = "default",
                                    Active = true
                                }
                            }
                        }
                    },
                    Contents = new List<SidecarContent>
                    {
                        CreateBspSection(name, location, 0)
                    }
                }
            };

            return sidecar;
        }

        public static SidecarContent CreateBspSection(string name, string location, int bspIdx)
        {
            var content = new SidecarContent
            {
                Name = $"{name}_{bspIdx:00#}_bsp",
                Type = "bsp",
                ContentObject = new SidecarContentObject
                {
                    Name = "",
                    Type = "scenario_structure_bsp",
                    ContentNetwork = new()
                    {
                        Name             = "level",
                        Type             = "",
                        InputFile        = $"{location}\\{name}\\structure\\{bspIdx:00#}\\{name}_{bspIdx:00#}.fbx",
                        IntermediateFile = $"{location}\\{name}\\structure\\{bspIdx:00#}\\{name}_{bspIdx:00#}.gr2"
                    },
                    OutputTagCollection = new()
                    {
                        new SidecarOutputTag("scenario_structure_bsp", $"{location}\\{name}\\{name}_{bspIdx:00#}"),
                        new SidecarOutputTag("scenario_structure_lighting_info", $"{location}\\{name}\\{name}_{bspIdx:00#}_bsp")
                    }
                }
            };

            return content;
        }
    }

    [XmlType("Metadata")]
        public class SidecarMetaData
        {
            public            SidecarHeader               Header          { get; set; } = new();
            public            SidecarAsset                Asset           { get; set; } = new();
            public            SidecarFolders              Folders         { get; set; } = new();
            [XmlArray] public List<SidecarFaceCollection> FaceCollections { get; set; } = new();
            [XmlArray] public List<SidecarContent>        Contents        { get; set; } = new();
            public            SidecarMiscData             MiscData        { get; set; } = new();
        }

        [XmlType("Header")]
        public class SidecarHeader
        {
            public int    MainRev       { get; set; } = 0;
            public int    PointRev      { get; set; } = 6;
            public string Description   { get; set; } = $"Created by Rampancy";
            public string Created       { get; set; } = DateTime.Now.ToString("dd/MM/yyyy");
            public string By            { get; set; } = "Rampancy";
            public string DirectoryType { get; set; } = "TAE.Shared.NWOAssetDirectory";
            public int    Schema        { get; set; } = 1;
        }

        [XmlType("Asset")]
        public class SidecarAsset
        {
            [XmlAttribute] public string                 Name                { get; set; }
            [XmlAttribute] public string                 Type                { get; set; }
            [XmlArray]     public List<SidecarOutputTag> OutputTagCollection { get; set; } = new();
        }

        [XmlType("OutputTag")]
        public class SidecarOutputTag
        {
            public SidecarOutputTag()
            {
            }

            public SidecarOutputTag(string type, string path)
            {
                Type = type;
                Path = path;
            }

            [XmlAttribute] public string Type { get; set; }
            [XmlText]      public string Path { get; set; }
        }

        [XmlType("Folders")]
        public class SidecarFolders
        {
            public string Reference             { get; set; }
            public string Temp                  { get; set; }
            public string SourceModels          { get; set; }
            public string GameModels            { get; set; }
            public string GamePhysicsModels     { get; set; }
            public string GameCollisionModels   { get; set; }
            public string ExportModels          { get; set; }
            public string ExportPhysicsModels   { get; set; }
            public string ExportCollisionModels { get; set; }
            public string SourceAnimations      { get; set; }
            public string AnimationRigs         { get; set; }
            public string GameAnimations        { get; set; }
            public string ExportAnimations      { get; set; }
            public string SourceBitmaps         { get; set; }
            public string GameBitmaps           { get; set; }
            public string CinemaSource          { get; set; }
            public string CinemaExport          { get; set; }
            public string ExportBSPs            { get; set; }
            public string GameBSPs              { get; set; }
            public string SourceBSPs            { get; set; }
            public string RigFlags              { get; set; }
            public string RigPoses              { get; set; }
            public string RigRenders            { get; set; }
            public string Scripts               { get; set; }
            public string FacePoses             { get; set; }
            public string CinematicOutsource    { get; set; }
        }

        [XmlType("FaceCollection")]
        public class SidecarFaceCollection
        {
            [XmlAttribute] public string                           Name                  { get; set; }
            [XmlAttribute] public string                           StringTable           { get; set; }
            [XmlAttribute] public string                           Description           { get; set; }
            [XmlArray]     public List<SidecarFaceCollectionEntry> FaceCollectionEntries { get; set; } = new();
        }

        [XmlType("FaceCollectionEntry")]
        public class SidecarFaceCollectionEntry
        {
            [XmlAttribute] public string Index  { get; set; }
            [XmlAttribute] public string Name   { get; set; }
            [XmlAttribute] public bool   Active { get; set; } = true;
        }

        [XmlType("Content")]
        public class SidecarContent
        {
            [XmlAttribute] public string               Name          { get; set; }
            [XmlAttribute] public string               Type          { get; set; }
            public                SidecarContentObject ContentObject { get; set; }
        }

        [XmlType("ContentObject")]
        public class SidecarContentObject
        {
            [XmlAttribute] public string                 Name                { get; set; }
            [XmlAttribute] public string                 Type                { get; set; }
            public                SidecarContentNetwork  ContentNetwork      { get; set; }
            [XmlArray] public     List<SidecarOutputTag> OutputTagCollection { get; set; } = new();
        }

        [XmlType("ContentNetwork")]
        public class SidecarContentNetwork
        {
            [XmlAttribute] public string Name             { get; set; }
            [XmlAttribute] public string Type             { get; set; }
            public                string InputFile        { get; set; }
            public                string IntermediateFile { get; set; }
        }

        [XmlType("MiscData")]
        public class SidecarMiscData
        {
        }
    }