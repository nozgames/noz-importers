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

using NoZ.Graphics;

namespace NoZ.Import
{
    [ImportType("Image")]
    internal class ImageImporter : ResourceImporter
    {
        public override void Import(Stream source, Stream target, FieldInfo info)
        {
            try
            {
                var image = LoadImage(source);

                // Optional border
                var border = info.GetCustomAttribute<ImageBorderAttribute>();
                if(border != null)
                    image.Border = new Thickness(border.left, border.top, border.right, border.bottom);

                using (var writer = new BinaryWriter(target))
                    image.Save(writer);    
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

        private Image LoadImage (Stream stream)
        {
            var source = new System.Drawing.Bitmap(stream);

            var format = PixelFormat.A8;
            switch (source.PixelFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                {
                    format = PixelFormat.R8G8B8A8;
                    break;
                }

                case System.Drawing.Imaging.PixelFormat.Alpha:
                {
                    format = PixelFormat.A8;
                    break;
                }

                case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                {
                    format = PixelFormat.R8G8B8;
                    break;
                }
            }

            var target = Image.Create(null, source.Width, source.Height, format);
            var targetLocked = target.Lock();
            var targetPixels = targetLocked.Raw;

            var data = source.LockBits(
                new System.Drawing.Rectangle(0, 0, source.Width, source.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                source.PixelFormat);

            // Copy the data into the byte array
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, targetPixels, 0, targetPixels.Length);

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
    }
}
