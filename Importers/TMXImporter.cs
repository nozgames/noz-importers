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
            var tilesets = doc.GetElementsByTagName("tileset");
            var layers = doc.GetElementsByTagName("layer");

            if (layers.Count == 0)
                throw new ImportException("Missing layers");

            if (layers.Count > 1)
                throw new ImportException("Multiple layers not yet supported");

            var width = int.Parse(map.Attributes["width"].Value);
            var height = int.Parse(map.Attributes["height"].Value);
            var tileWidth = int.Parse(map.Attributes["tilewidth"].Value);
            var tileHeight = int.Parse(map.Attributes["tileheight"].Value);

            var tileSetFirstId = new int[tilesets.Count];

            writer.Write((ushort)width);
            writer.Write((ushort)height);
            writer.Write((ushort)tileWidth);
            writer.Write((ushort)tileHeight);

            writer.Write((ushort)tileSetFirstId.Length);
            for(int i=0; i< tileSetFirstId.Length; i++)
            {
                XmlNode tileSet = tilesets[i];
                var source = tileSet.Attributes["source"].Value;

                tileSetFirstId[i] = int.Parse(tileSet.Attributes["firstgid"].Value);

                var uri1 = new Uri(Path.Combine(Path.GetDirectoryName(file.TargetFilename), source));
                var uri2 = new Uri(file.TargetDirectory + "\\");
                var name = Path.ChangeExtension(uri2.MakeRelativeUri(uri1).ToString(), null);

                writer.Write(name);
            }

            var layer = layers[0];
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
                for (tileSetId = 0; tileSetId < tileSetFirstId.Length-1 && id >= tileSetFirstId[tileSetId+1]; tileSetId++) ;

                writer.Write((ushort)tileSetId);
                writer.Write((ushort)(id - tileSetFirstId[tileSetId]));
            }
        }
    }
}

