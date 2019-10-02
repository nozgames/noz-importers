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
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace NoZ.Import
{
    /// <summary>
    /// Importer for Tiled tileset files
    /// </summary>
    [ImportExtension(".tsx")]
    [ImportExtension(".png", IsSecondary = true)]
    class TSXImporter : ResourceImporter
    {
        public override void Import(ImportFile file)
        {
            var doc = new XmlDocument();
            try
            {
                doc.Load(file.Filename);
            }
            catch
            {
                throw new ImportException("failed to parse XML");
            }

            using(var writer = new ResourceWriter(File.Create(file.TargetFilename), typeof(TileSet)))
                Import(file, doc, writer);
        }

        private void WriteProperties(ResourceWriter writer, XmlNode properties)
        {
            if (properties == null)
            {
                writer.Write((ushort)0);
                return;
            }

            writer.Write((ushort)properties.ChildNodes.Count);
            foreach (XmlNode prop in properties)
            {
                writer.Write(prop.Attributes["name"].Value);
                writer.Write(prop.Attributes["value"].Value);
            }
        }

        private void Import (ImportFile file, XmlDocument doc, ResourceWriter writer)
        {
            var tileset = doc.GetElementsByTagName("tileset")[0];
            var images = doc.GetElementsByTagName("image");
            var tiles = doc.GetElementsByTagName("tile");
            var columns = int.Parse(tileset.Attributes["columns"].Value);
            var spacing = int.Parse(tileset.Attributes["spacing"]?.Value ?? "0");
            var tilewidth = int.Parse(tileset.Attributes["tilewidth"].Value);
            var tileheight = int.Parse(tileset.Attributes["tileheight"].Value);
            var tilecount = int.Parse(tileset.Attributes["tilecount"].Value);

            if (images.Count == 0)
                throw new ImportException("TileSet has no images");
            if (images.Count > 1)
                throw new ImportException("Multiple TileSet images not supported");

            var image = images[0].Attributes["source"].Value;
            if (Path.ChangeExtension(image, null) != Path.GetFileName(file.Name))
                throw new ImportException("TileSet image must be same name as the tile set");

            image = Path.ChangeExtension(file.Filename, Path.GetExtension(image));

            writer.Write((ushort)tilewidth);
            writer.Write((ushort)tileheight);
            writer.Write((ushort)tilecount);
            writer.Write((ushort)tiles.Count);

            // Embed the tile images into the tile set
            foreach (XmlNode tile in tiles)
            {
                var id = int.Parse(tile.Attributes["id"].Value);
                var x = id % columns;
                var y = id / columns;

                x = (x * tilewidth) + x * spacing;
                y = (y * tileheight) + y * spacing;

                writer.Write((ushort)id);

                using (var source = File.OpenRead(image))
                    ImageImporter.Import(source, writer, Thickness.Empty, new SixLabors.Primitives.Rectangle(x, y, tilewidth, tileheight));

                WriteProperties(writer, tile["properties"]);

                var objectGroup = tile["objectgroup"];
                if (objectGroup != null)
                {
                    writer.Write((ushort)objectGroup.ChildNodes.Count);

                    foreach (XmlNode obj in objectGroup)
                    {
                        var objx = int.Parse(obj.Attributes["x"].Value);
                        var objy = int.Parse(obj.Attributes["y"].Value);

                        var polygon = obj["polygon"];
                        if(null != polygon)
                        {
                            var points = polygon.Attributes["points"].Value.Split(new char[] { ' ' });
                            writer.Write((ushort)points.Length);
                            for (int i = 0; i < points.Length; i++)
                                writer.Write(Vector2.Parse(points[i]) + new Vector2(objx,objy));
                        }
                        else
                        {
                            writer.Write((ushort)0);
                        }

                        WriteProperties(writer, obj["properties"]);
                    }
                }
                else
                {
                    writer.Write((ushort)0);
                }
            }
        }
    }
}

