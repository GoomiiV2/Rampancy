using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Rampancy.RampantC20.HaloReach
{
    public class Sidecar
    {
        public SidecarMetaData MetaData { get; set; } = new();

        public void Save(string path)
        {
            var       serializer   = new XmlSerializer(typeof(Sidecar));
            using var memoryStream = new MemoryStream();
            var streamWriter = XmlWriter.Create(memoryStream, new()
            {
                Encoding = Encoding.UTF8,
                Indent   = true
            });
            serializer.Serialize(streamWriter, this);
            var result = Encoding.UTF8.GetString(memoryStream.ToArray());

            System.Diagnostics.Debug.Write(result);
        }

        public static Sidecar CreateStructureSidecar()
        {
            var sidecar = new Sidecar();
            sidecar.MetaData.Asset.Name = "rampancytest";
            sidecar.MetaData.Asset.Type = "scenario";

            return sidecar;
        }
    }

    public class SidecarMetaData
    {
        public SidecarHeader Header { get; set; } = new();
        public SidecarAsset  Asset  { get; set; } = new();
    }

    public class SidecarHeader
    {
        public int    MainRev       { get; set; } = 0;
        public int    PointRev      { get; set; } = 6;
        public string Description   { get; set; } = $"Created by Rampancy";
        public string Created       { get; set; } = DateTime.Now.ToString("dd/MM/yyyy");
        public string By            { get; set; } = "";
        public string DirectoryType { get; set; } = "TAE.Shared.NWOAssetDirectory";
        public int    Schema        { get; set; } = 1;
    }

    public class SidecarAsset
    {
        [XmlAttribute] public string Name { get; set; }
        [XmlAttribute] public string Type { get; set; }
    }
}