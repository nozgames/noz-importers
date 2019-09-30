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

using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using NoZ;
using System.Collections.Generic;

namespace NoZ.Import
{
    [ImportType("NoZ.Image, NoZ")]
    [ImportExtension(".png")]
    [ImportExtension(".jpg")]
    [ImportExtension(".tga")]
    internal class ImageImporter : ResourceImporter
    {
        public class MetaDefinition
        {
            public class ImageDefinition
            {
                public string Border { get; set; }
            }

            public struct AtlasRect
            {
                public int x { get; set; }
                public int y { get; set; }
                public int w { get; set; }
                public int h { get; set; }
            }

            public class AtlasImage
            {
                public string Name { get; set; }
                public AtlasRect Rect { get; set; }
            }

            public class ImageAtlasDefinition
            {
                public AtlasImage[] Images { get; set; }
            }

            public ImageDefinition Image { get; set; }
            public ImageAtlasDefinition ImageAtlas { get; set; }
        }

        public override void Import(string source, string target)
        {
            MetaDefinition meta = null;
            var yamlPath = Path.ChangeExtension(source, ".yaml");
            if (File.Exists(yamlPath))
            {
                using (var yamlStream = File.OpenRead(yamlPath))
                using (var yamlReader = new StreamReader(yamlStream))
                {
                    meta = (new YamlDotNet.Serialization.Deserializer()).Deserialize<MetaDefinition>(yamlReader);
                }
            }

            if(meta != null && meta.ImageAtlas != null)
            {
                var targetPath = Path.ChangeExtension(target,null);

                // Create a directory with the same name as the resource
                Directory.CreateDirectory(targetPath);

                for (var i = 0; i < meta.ImageAtlas.Images.Length; i++)
                {
                    using (var sourceFile = File.OpenRead(source))
                    using (var targetWriter = new ResourceWriter(File.OpenWrite($"{targetPath}/{meta.ImageAtlas.Images[i].Name}.resource"), typeof(Image)))
                        Import(sourceFile, targetWriter, meta,
                            new SixLabors.Primitives.Rectangle(
                                meta.ImageAtlas.Images[i].Rect.x,
                                meta.ImageAtlas.Images[i].Rect.y,
                                meta.ImageAtlas.Images[i].Rect.w,
                                meta.ImageAtlas.Images[i].Rect.h));

                    // TODO: write the atlas out as a resource
                }
            }
            else
            {
                using (var sourceFile = File.OpenRead(source))
                using (var targetWriter = new ResourceWriter(File.OpenWrite(target), typeof(NoZ.Image)))
                    Import(sourceFile, targetWriter, meta, SixLabors.Primitives.Rectangle.Empty);
            }
        }

        private void Import(Stream source, ResourceWriter writer, MetaDefinition meta, SixLabors.Primitives.Rectangle crop)
        {
            try
            {
                var image = SixLabors.ImageSharp.Image.Load(source);
                var format = PixelFormat.A8;
                byte[] bytes;

                if (crop != SixLabors.Primitives.Rectangle.Empty)
                    image.Mutate(x => x.Crop(crop));

                switch (image.PixelType.BitsPerPixel)
                {
                    case 32:
                    {
                        bytes = MemoryMarshal.AsBytes((image as Image<Rgba32>).GetPixelSpan()).ToArray();
                        format = PixelFormat.R8G8B8A8;
                        break;
                    }

                    case 8:
                    {
                        bytes = MemoryMarshal.AsBytes((image as Image<Alpha8>).GetPixelSpan()).ToArray();
                        format = PixelFormat.A8;
                        break;
                    }

                    case 24:
                    {
                        bytes = MemoryMarshal.AsBytes((image as Image<Rgb24>).GetPixelSpan()).ToArray();
                        format = PixelFormat.R8G8B8;
                        break;
                    }

                    default:
                        throw new ImportException("unsupported image format");
                }

                writer.Write((short)image.Width);
                writer.Write((short)image.Height);
                writer.Write((byte)format);

                if (meta?.Image != null)
                    writer.Write(Thickness.Parse(meta.Image.Border));
                else
                    writer.Write(new Thickness(0));

                writer.Write(bytes, 0, bytes.Length);
            }
            catch (ImportException)
            {
                throw;
            }
            catch
            {
                throw new ImportException("failed to open file for read");
            }
        }
    }
}
