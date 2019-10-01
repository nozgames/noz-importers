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
using System.Runtime.InteropServices;
using System.IO.Compression;

namespace NoZ.Import.Importers
{
    /// <summary>
    /// Importer for Asesprite files
    /// </summary>
    [ImportExtension(".aseprite")]
    [ImportExtension(".ase")]
    class AsepriteImporter : ResourceImporter
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 128)]
        struct Header
        {
            public uint fileSize;
            public ushort magicNumber;
            public ushort frames;
            public ushort width;
            public ushort height;
            public ushort depth;
            public uint flags;
            public ushort speed;
            public uint reserved0;
            public uint reserved1;
            public byte paletteEntry;
            public byte reserved2;
            public byte reserved3;
            public byte reserved4;
            public ushort colors;
            public byte pixelWidth;
            public byte pixelHeight;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
        struct FrameHeader
        {
            public uint size;
            public ushort magic;
            public ushort chunksOld;
            public ushort duration;
            public ushort reserved;
            public uint chunks;
        };

        struct Tag
        {
            public string name;
            public int from;
            public int to;
            public int loopdir;
        }

        struct Frame
        {
            public string name;
            public float duration;
            public byte[] data;
        }

        public override void Import(ImportFile file)
        {
            using (var reader = new BinaryReader(File.OpenRead(file.Filename)))
                Import(reader, file);
        }

        private void Import (BinaryReader reader, ImportFile file)
        {
            var targetDir = Path.ChangeExtension(file.TargetFilename, null);

            reader.ReadStruct<Header>(out var header);

            if (header.depth != 32)
                throw new ImportException($"{header.depth} bit not supported");
            
            var tags = new List<Tag>();
            var frames = new List<Frame>();

            for (int i=0; i<header.frames; i++)
            {
                reader.ReadStruct<FrameHeader>(out var frame);

                var celData = new byte[header.width * header.height * 4];

                for (int j=0; j<frame.chunks; j++)
                {                    
                    var size = reader.ReadUInt32();
                    var type = reader.ReadUInt16();
                    var next = reader.BaseStream.Position + size - 6;

                    switch(type)
                    {
                        // Old palette chunk
                        case 0x0011:
                        case 0x0004:
                            break;

                        // Layer chunk
                        case 0x2004:
                            break;

                        // Cel chunk
                        case 0x2005:
                        {
                            var layerIndex = reader.ReadUInt16();
                            var x = reader.ReadInt16();
                            var y = reader.ReadInt16();
                            var opacity = reader.ReadByte();
                            var celType = reader.ReadUInt16();
                            reader.BaseStream.Position += 7;

                            if (opacity != 255)
                                throw new ImportException("opacity not implemented");

                            if (layerIndex != 0)
                                throw new ImportException("layers not implemented");

                            switch (celType)
                            {
                                case 0: throw new ImportException("raw cels are not supported");
                                case 1: throw new ImportException("linked cels are not supported");
                                case 2:
                                {
                                    var w = reader.ReadUInt16();
                                    var h = reader.ReadUInt16();

                                    reader.ReadUInt16();

                                    using (var deflate = new DeflateStream(reader.BaseStream, CompressionMode.Decompress, true))
                                    {
                                        try
                                        {
                                            for(int yy=0;yy<h;yy++)
                                                deflate.Read(celData, x * 4 + (yy + y) * header.width * 4, w * 4);                                           
                                        }
                                        catch
                                        {
                                            throw new ImportException("cel data corrupt");
                                        }
                                    }

                                    break;
                                }
                            }
                            break;
                        }

                        // Cel extra chunk
                        case 0x2006:
                            break;

                        // Color profile chunk
                        case 0x2007:
                            break;

                        // Mask chunk
                        case 0x2016:
                            break;

                        // Path chunk
                        case 0x2017:
                            break;

                        // Frame Tags Chunk
                        case 0x2018:
                        {
                            var tagCount = reader.ReadUInt16();
                            reader.BaseStream.Position += 8;

                            for(var tagIndex = 0; tagIndex < tagCount; tagIndex++)
                            {
                                var fromFrame = reader.ReadUInt16();
                                var toFrame = reader.ReadUInt16();
                                var loopDir = reader.ReadByte();

                                if (loopDir == 2)
                                    throw new ImportException("Ping-Pong loop direction not implemented");

                                reader.BaseStream.Position += 12;

                                tags.Add(new Tag {
                                    from = fromFrame,
                                    to = toFrame,
                                    name = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16())),
                                    loopdir = loopDir });
                            }
                            break;
                        }
                    }

                    reader.BaseStream.Position = next;
                }

                // Add the frame
                frames.Add(new Frame { duration = frame.duration / 1000.0f, name = $"{file.Name}/{i}", data = celData });
            }

            if (frames.Count == 1 && tags.Count == 0)
            {
                if(Directory.Exists(targetDir))
                    Directory.Delete(targetDir, true);

                using (var writer = new ResourceWriter(File.Create(file.TargetFilename), typeof(Image)))
                {
                    writer.Write((short)header.width);
                    writer.Write((short)header.height);
                    writer.Write((byte)PixelFormat.R8G8B8A8);
                    writer.Write(Thickness.Empty);
                    writer.Write(frames[0].data, 0, frames[0].data.Length);
                }
            }
            else
            {
                if(File.Exists(file.TargetFilename))
                    File.Delete(file.TargetFilename);

                Directory.CreateDirectory(targetDir);

                for (var frameIndex = 0; frameIndex < frames.Count; frameIndex++)
                    using (var writer = new ResourceWriter(File.Create(Path.Combine(file.TargetDirectory, $"{frames[frameIndex].name}.resource")), typeof(Image)))
                    {
                        writer.Write((short)header.width);
                        writer.Write((short)header.height);
                        writer.Write((byte)PixelFormat.R8G8B8A8);
                        writer.Write(Thickness.Empty);
                        writer.Write(frames[frameIndex].data, 0, frames[frameIndex].data.Length);
                    }

                // If the file has tags then write each tag as an ImageAnimation in "name/Tag"
                foreach (var tag in tags)
                {
                    using (var writer = new ResourceWriter(File.Create(Path.Combine(Path.ChangeExtension(file.TargetFilename, null), $"{tag.name}.resource")), typeof(ImageAnimation)))
                    {
                        writer.Write(tag.to - tag.from + 1);
                        if (tag.loopdir == 0)
                        {
                            for (int i = tag.from; i <= tag.to; i++)
                            {
                                writer.Write(frames[i].name);
                                writer.Write(frames[i].duration);
                            }
                        }
                        else
                        {
                            for (int i = tag.to; i >= tag.from; i--)
                            {
                                writer.Write(frames[i].name);
                                writer.Write(frames[i].duration);
                            }
                        }
                    }                    
                }

                Directory.SetLastWriteTime(Path.ChangeExtension(file.TargetFilename, null), File.GetLastWriteTime(file.Filename));
            }
        }
    }
}
