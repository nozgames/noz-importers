/*
  NoZ Game Engine

  Copyright(c) 2019 NoZ Games, LLC

  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files(the "Software"), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions :

  The above copyright notice and this permission notice shall be included in all
  copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  SOFTWARE.
*/

using System;
using System.IO;
using System.Xml;

namespace NoZ.Import
{
    /// <summary>
    /// Importer for Tiled map files
    /// </summary>
    [ImportExtension(".tmx")]
    class TMXImporter  : ResourceImporter
    {
        public override void Import(ImportFile file)
        {
            var doc = new XmlDocument();
            doc.Load(file.Filename);

            using (var writer = new ResourceWriter(File.Create(file.TargetFilename), typeof(TileMap)))
                Import(file, doc, writer);
        }

        private void Import(ImportFile file, XmlDocument doc, ResourceWriter writer)
        {
            var map = doc.GetElementsByTagName("map")[0];
            var width = int.Parse(map.Attributes["width"].Value);
            var height = int.Parse(map.Attributes["height"].Value);

            writer.Write((ushort)width);
            writer.Write((ushort)height);
            writer.Write((ushort)int.Parse(map.Attributes["tilewidth"].Value));
            writer.Write((ushort)int.Parse(map.Attributes["tileheight"].Value));

            var layers = map.SelectNodes("objectgroup | layer");
            var tilesets = map.SelectNodes("tileset");

            // Write tile sets
            var tileSetFirstId = new int[tilesets.Count];
            writer.Write((ushort)tileSetFirstId.Length);
            for (int i = 0; i < tileSetFirstId.Length; i++)
            {
                XmlNode tileSet = tilesets[i];
                var source = tileSet.Attributes["source"].Value;

                tileSetFirstId[i] = int.Parse(tileSet.Attributes["firstgid"].Value);

                var uri1 = new Uri(Path.Combine(Path.GetDirectoryName(file.TargetFilename), source));
                var uri2 = new Uri(file.TargetDirectory + "\\");
                var name = Path.ChangeExtension(uri2.MakeRelativeUri(uri1).ToString(), null);

                writer.Write(name);
            }

            // Write layers
            writer.Write((ushort)layers.Count);

            foreach (XmlNode layer in layers)
            {
                writer.Write(layer.Attributes["name"]?.Value ?? "");
                writer.Write((ushort)width);
                writer.Write((ushort)height);

                // Properties
                var objprops = layer.SelectNodes("properties/property");
                writer.Write((ushort)objprops.Count);

                foreach (XmlNode objprop in objprops)
                {
                    writer.Write(objprop.Attributes["name"].Value);
                    writer.Write(objprop.Attributes["value"].Value);
                }

                switch (layer.Name)
                {
                    case "layer":
                    {
                        // Write tiles
                        var data = layer["data"];
                        var encoding = data.Attributes["encoding"].Value;
                        if (encoding != "csv")
                            throw new ImportException($"encoding '{encoding}' not supported");

                        var ids = data.InnerText.Split(',');
                        writer.Write((ushort)ids.Length);
                        for (int i = 0; i < ids.Length; i++)
                        {
                            var id = int.Parse(ids[i]);
                            if (id == 0)
                            {
                                writer.Write((ushort)0xFFFF);
                                continue;
                            }

                            int tileSetId;
                            for (tileSetId = 0; tileSetId < tileSetFirstId.Length - 1 && id >= tileSetFirstId[tileSetId + 1]; tileSetId++) ;

                            writer.Write((ushort)tileSetId);
                            writer.Write((ushort)(id - tileSetFirstId[tileSetId]));
                        }

                        // Write objects
                        writer.Write((ushort)0);

                        break;
                    }

                    case "objectgroup":
                    {
                        // No tiles
                        writer.Write((ushort)0);

                        var properties = layer.SelectNodes("properties/property");

                        // Write objects
                        var objects = layer.SelectNodes("object");
                        writer.Write((ushort)objects.Count);

                        foreach(XmlNode o in objects)
                        {
                            XmlNode obj = o;
                            var template = obj.Attributes["template"]?.Value;
                            if (template != null)
                            {
                                var templateDoc = new XmlDocument();
                                templateDoc.Load(Path.Combine(Path.GetDirectoryName(file.Filename), template));
                                obj = templateDoc.SelectSingleNode("/template/object");
                            }

                            writer.Write(o.Attributes["name"]?.Value ?? obj.Attributes["name"]?.Value ?? "");
                            writer.Write(o.Attributes["type"]?.Value ?? obj.Attributes["type"]?.Value ?? "");
                            writer.Write(new Vector2(float.Parse(o.Attributes["x"].Value), float.Parse(o.Attributes["y"].Value)));

                            // Point
                            if (obj.SelectSingleNode("point") != null)
                            {
                                writer.Write((byte)TileMap.ShapeType.Point);
                            } 
                            // Polygon
                            else if (obj.SelectSingleNode("polygon") != null)
                            {
                                writer.Write((byte)TileMap.ShapeType.Polygon);

                                var points = obj.SelectSingleNode("polygon").Attributes["points"].Value.Split(new char[] { ' ' });
                                writer.Write((ushort)points.Length);
                                for (int i = 0; i < points.Length; i++)
                                    writer.Write(Vector2.Parse(points[i]));
                            } 
                            // Polyline
                            else if (obj.SelectSingleNode("polyline") != null)
                            {
                                writer.Write((byte)TileMap.ShapeType.PolyLine);
                                
                                var points = obj.SelectSingleNode("polyline").Attributes["points"].Value.Split(new char[] { ' ' });
                                writer.Write((ushort)points.Length);
                                for (int i = 0; i < points.Length; i++)
                                    writer.Write(Vector2.Parse(points[i]));
                            }
                            // Ellipse
                            else if (obj.SelectSingleNode("ellipse") != null)
                            {
                                writer.Write((byte)TileMap.ShapeType.Circle);
                                writer.Write(float.Parse(o.Attributes["width"]?.Value ?? obj.Attributes["width"]?.Value ?? "0"));
                                writer.Write(float.Parse(o.Attributes["height"]?.Value ?? obj.Attributes["height"]?.Value ?? "0"));
                            }
                            // Box
                            else
                            {
                                writer.Write((byte)TileMap.ShapeType.Box);
                                writer.Write(float.Parse(o.Attributes["width"]?.Value ?? obj.Attributes["width"]?.Value ?? "0"));
                                writer.Write(float.Parse(o.Attributes["height"]?.Value ?? obj.Attributes["height"]?.Value ?? "0"));
                            }

                            // Properties
                            var layerprops = obj.SelectNodes("properties/property");
                            writer.Write((ushort)layerprops.Count);

                            foreach(XmlNode layerProp in layerprops)
                            {
                                writer.Write(layerProp.Attributes["name"].Value);
                                writer.Write(layerProp.Attributes["value"].Value);
                            }
                        }

                        break;
                    }
                }
            }
        }
    }
}

