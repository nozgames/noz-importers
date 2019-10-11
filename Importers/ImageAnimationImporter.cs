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

namespace NoZ.Import
{
    /// <summary>
    /// Imports SpriteAnimations
    /// </summary>
    [ImportType("NoZ.ImageAnimation, NoZ")]
    internal class ImageAnimationImporter : ResourceImporter
    {
        /// <summary>
        /// Definition of yaml file
        /// </summary>
        public class YamlDefinition
        {
            public class FrameYaml
            {
                public float Duration { get; set; } = 0.1f;
                public string Image { get; set; }
                public string[] Events { get; set; }
            }

            public class ImageAnimationYaml
            {
                public FrameYaml[] Frames { get; set; }
            }

            public ImageAnimationYaml ImageAnimation { get; set; }
        }

        /// <summary>
        /// Import the sprite animation
        /// </summary>
        public override void Import(ImportFile file)
        {
            using (var sourceFile = File.OpenRead(file.Filename))
            using (var targetWriter = new ResourceWriter(File.OpenWrite(file.TargetFilename), typeof(ImageAnimation)))
                Import(sourceFile, targetWriter);
        }

        private void Import(Stream source, ResourceWriter writer)
        {            
            using (var reader = new StreamReader(source))
            {
                try
                {
                    var yaml = (new YamlDotNet.Serialization.Deserializer()).Deserialize<YamlDefinition>(reader);

                    writer.Write(yaml.ImageAnimation.Frames.Length);
                    for (int i = 0; i < yaml.ImageAnimation.Frames.Length; i++)
                    {
                        ref var frame = ref yaml.ImageAnimation.Frames[i];
                        writer.Write(frame.Image);
                        writer.Write(frame.Duration);

                        writer.Write((byte)(frame.Events?.Length ?? 0));
                        if (frame.Events != null)
                            for (int e = 0; e < frame.Events.Length; e++)
                                writer.Write(frame.Events[e]);
                    }
                }
                catch
                {
                    Console.WriteLine();
                }                
            }
        }
    }
}
