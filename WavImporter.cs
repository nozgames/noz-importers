using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace NoZ.Import
{
    [ImportTypeAttribute(".wav")]
    internal class WavImporter : ResourceImporter
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct WavHeader
        {
            public uint chunkId;
            public uint chunkSize;
            public uint format;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct WavFormat
        {
            public ushort type;
            public ushort channels;
            public uint samplesPerSec;
            public uint avgBytesPerSec;
            public ushort blockAlign;
            public ushort bitsPerSample;
        };

        private void Import(BinaryReader reader, BinaryWriter writer)
        {
            // Read the header.
            WavHeader header = new WavHeader();
            header.chunkId = reader.ReadUInt32();
            header.chunkSize = reader.ReadUInt32();
            header.format = reader.ReadUInt32();

            // Ensure the file is a valid wav file
            if (header.chunkId != 0x46464952 || header.format != 0x45564157L)
                throw new ImportException("not a valid .wav file");

            // Read the block type
            WavFormat format = new WavFormat();

            uint dataSize = 0;
            long dataPosition = 0;

            // Read header blocks..
            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                uint blockId = reader.ReadUInt32();

                // Read the size of the block
                uint blockSize = reader.ReadUInt32();

                // Next block position
                long next = reader.BaseStream.Position + blockSize;

                switch (blockId)
                {
                    // FMT 
                    case 0x20746d66:
                    {
                        format.type = reader.ReadUInt16();
                        format.channels = reader.ReadUInt16();
                        format.samplesPerSec = reader.ReadUInt32();
                        format.avgBytesPerSec = reader.ReadUInt32();
                        format.blockAlign = reader.ReadUInt16();
                        format.bitsPerSample = reader.ReadUInt16();
                        break;
                    }

                    // DATA
                    case 0x61746164:
                    {
                        dataSize = blockSize;
                        dataPosition = reader.BaseStream.Position;
                        break;
                    }
                }

                // Skip to next block
                reader.BaseStream.Position = next;
            }

            if (dataSize == 0 || dataPosition == 0)
                throw new ImportException("invalid or corrupt .wav file");

            // 8-bit format not allowed
            if (format.bitsPerSample != 16)
                throw new ImportException("only 16-bit PCM data supported");

            var bytesPerSample = format.channels * (format.bitsPerSample >> 3);
            var sampleCount = (int)(dataSize / bytesPerSample);

            writer.Write((byte)(format.channels == 1 ? AudioChannelFormat.Mono : AudioChannelFormat.Stereo));
            writer.Write((int)format.samplesPerSec);
            writer.Write(sampleCount);

            reader.BaseStream.Position = dataPosition;
            writer.Write(reader.ReadBytes((int)dataSize));

#if false
            // Create the clip
            var clip = AudioClip.Create(sampleCount, format.channels == 1 ? AudioChannelFormat.Mono : AudioChannelFormat.Stereo, (int)format.samplesPerSec);

            // Read the data
            reader.BaseStream.Position = dataPosition;
            byte[] data = reader.ReadBytes((int)dataSize);

            short[] pcm = new short[(int)dataSize / 2];
            Buffer.BlockCopy(data, 0, pcm, 0, (int)dataSize);
            clip.SetData(pcm, 0);
#endif
        }

        public override void Import(Stream source, Stream target, FieldInfo info)
        {
            try
            {
                using (var reader = new BinaryReader(source))
                using (var writer = new BinaryWriter(target))
                {
                    Import(reader, writer);
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
    }
}
