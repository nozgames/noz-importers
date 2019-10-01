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
using System.Collections.Generic;

namespace NoZ.Import
{
    [ImportType("NoZ.Font, NoZ")]
    [ImportExtension(".ttf")]
    class FontImporter : ResourceImporter
    {
        private class ImportedGlyph
        {
            public TrueTypeFont.Glyph ttf;
            public Vector2Int size;
            public Vector2Double scale;
            public Vector2Int packedSize;
            public RectInt packedRect;
            public Vector2Int bearing;
            public char ascii;
        }

        public class YamlDefinition
        {
            public class FontDefinition
            {
                public string Chars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.+- ";
                public int Resolution { get; set; } = 64;
                public int Range { get; set; } = 8;
                public int Padding { get; set; } = 2;
            }

            public FontDefinition Font{ get; set; }
        }

        public object StringBuidler { get; private set; }

        private Vector2Int RoundToNearest(in Vector2 v) => new Vector2Int((int)(v.x + 0.5f), (int)(v.y + 0.5f));
        private Vector2Int RoundToNearest(in Vector2Double v) => new Vector2Int((int)(v.x + 0.5f), (int)(v.y + 0.5f));

        public override void Import(ImportFile file)
        {
            YamlDefinition.FontDefinition meta = null;

            var yamlPath = Path.ChangeExtension(file.Filename, ".yaml");
            try
            {
                if (File.Exists(yamlPath)) {
                    using (var yamlStream = File.OpenRead(yamlPath))
                    using (var yamlReader = new StreamReader(yamlStream))
                    {
                        var def = (new YamlDotNet.Serialization.Deserializer()).Deserialize<YamlDefinition>(yamlReader);
                        meta = def.Font;
                    }
                }
                else
                    meta = new YamlDefinition.FontDefinition();
            }
            catch
            {
                throw new ImportException($"{yamlPath}: invalid format");
            }


            using (var targetFile = new ResourceWriter(File.OpenWrite(file.TargetFilename), typeof(Font)))
            using (var sourceFile = File.OpenRead(file.Filename))
                Import(sourceFile, targetFile, meta); ;
        }


        private void Import(Stream source, ResourceWriter target, YamlDefinition.FontDefinition meta)
        {
            // Alwasy add a space into the character list
            if (-1 == meta.Chars.IndexOf(' '))
                meta.Chars = meta.Chars + " ";

            var ttf = TrueTypeFont.Load(source, meta.Resolution, meta.Chars);

            // Build the imported glyph list.
            var importedGlyphs = new List<ImportedGlyph>();
            for (int i = 0; i < meta.Chars.Length; i++)
            {
                var ttfGlyph = ttf.GetGlyph(meta.Chars[i]);
                if (ttfGlyph == null)
                    continue;

                var iglyph = new ImportedGlyph();
                iglyph.ascii = meta.Chars[i];
                iglyph.ttf = ttfGlyph;

                if (iglyph.ttf.contours != null)
                {
                    iglyph.size = RoundToNearest(ttfGlyph.size);
                    iglyph.scale = iglyph.size.ToVector2Double() / ttfGlyph.size;
                    iglyph.packedSize = iglyph.size + (meta.Padding + meta.Range) * 2;
                    iglyph.bearing = RoundToNearest(ttfGlyph.bearing);
                }
                importedGlyphs.Add(iglyph);
            }

            // Pack the glyphs
            int minHeight = (int)MathEx.NextPow2((uint)(meta.Resolution + 2 + meta.Range * 2 + meta.Padding * 2));
            BinPacker packer = new BinPacker(minHeight, minHeight);

            while (packer.IsEmpty)
            {
                foreach (var iglyph in importedGlyphs)
                {
                    if (iglyph.ttf.contours == null)
                        continue;

                    if (-1 == packer.Insert(iglyph.packedSize, BinPacker.Method.BestLongSideFit, out iglyph.packedRect))
                    {
                        Vector2Int size = packer.Size;
                        if (size.x <= size.y)
                        {
                            size.x <<= 1;
                        }
                        else
                        {
                            size.y <<= 1;
                        }
                        packer.Resize(size.x, size.y);
                        break;
                    }
                }
            }

            var image = Image.Create(null, packer.Size.x, packer.Size.y, PixelFormat.A8);
            var locked = image.Lock();
            locked.Clear(Color.Transparent);

            var imageSize = image.Size.ToVector2();

            // Render the glyphs
            Font.Glyph[] glyphs = new Font.Glyph[importedGlyphs.Count];
            for (int i = 0; i < importedGlyphs.Count; i++)
            {
                var iglyph = importedGlyphs[i];
                if (iglyph.ttf.contours == null)
                {
                    glyphs[i] = new Font.Glyph(
                        iglyph.ascii,
                        (float)iglyph.ttf.advance,
                        Vector2.Zero,
                        new Vector2((float)iglyph.ttf.advance, (float)ttf.Height),
                        Vector2.Zero,
                        Vector2.Zero
                    );
                    continue;
                }

                MSDF.RenderGlyph(
                    iglyph.ttf,
                    locked,
                    iglyph.packedRect.TopLeft + meta.Padding,
                    iglyph.packedRect.Size - meta.Padding * 2,
                    meta.Range,
                    iglyph.scale,
                    new Vector2Double(
                        -iglyph.ttf.bearing.x + meta.Range,
                        (iglyph.ttf.size.y - iglyph.ttf.bearing.y) + meta.Range
                    )
                );

                glyphs[i] = new Font.Glyph(
                    iglyph.ascii,
                    (float)iglyph.ttf.advance,
                    iglyph.bearing.ToVector2(),
                    iglyph.size.ToVector2(),
                    (iglyph.packedRect.TopLeft + meta.Padding).ToVector2() / imageSize,
                    (iglyph.packedRect.TopLeft + iglyph.packedRect.Size - meta.Padding).ToVector2() / imageSize
                );
            }

            image.Unlock();
            //texture.SignedDistanceFieldRange = meta.Range;

            Dictionary<ushort, float> kerning = null;
            if (ttf._kerning != null)
            {
                kerning = new Dictionary<ushort, float>();
                foreach (var kern in ttf._kerning)
                {
                    kerning[kern.Item1] = kern.Item2;
                }
            }

            var font = Font.Create(meta.Resolution, (float)ttf.Height, (float)ttf.Ascent, image, glyphs, kerning);
            font.Save(target);
        }
    }
}
