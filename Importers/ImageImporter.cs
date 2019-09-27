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

namespace NoZ.Import
{
    [ImportType("NoZ.Graphics.Image, NoZ")]
    [ImportExtension(".png")]
    [ImportExtension(".jpg")]
    [ImportExtension(".tga")]
    internal class ImageImporter : ResourceImporter
    {
        public override void Import(string filename, Stream target)
        {
            using (var source = File.OpenRead(filename))
                Import(source, target);
        }

        private void Import(Stream source, Stream target)
        {
            try
            {
                var image = Image.Load(source);
                var format = PixelFormat.A8;
                byte[] bytes;

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

                using(var writer = new BinaryWriter(target))
                {
                    writer.Write((short)image.Width);
                    writer.Write((short)image.Height);
                    writer.Write((byte)format);
                    writer.Write(new Thickness(0));
                    writer.Write(bytes, 0, bytes.Length);
                }
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

#if false
        private NoZ.Graphics.Image LoadImage (Stream stream)
        {

            switch (format)
            {
                case PixelFormat.R8G8B8A8:
                {
                    for (int i = 0; i < targetPixels.Length; i += 4)
                    {
                        byte temp = targetPixels[i + 0];
                        targetPixels[i + 0] = targetPixels[i + 2];
                        targetPixels[i + 2] = temp;
                    }
                    break;
                }

                case PixelFormat.R8G8B8:
                {
                    for (int i = 0; i < targetPixels.Length; i += 3)
                    {
                        byte temp = targetPixels[i + 0];
                        targetPixels[i + 0] = targetPixels[i + 2];
                        targetPixels[i + 2] = temp;
                    }
                    break;
                }
            }
            return target;
    }
#endif
    }
}
