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

namespace NoZ.Import.Importers
{
    /// <summary>
    /// Imports SpriteAnimations
    /// </summary>
    [ImportType("NoZ.SpriteAnimation, NoZ")]
    internal class SpriteAnimationImporter : ResourceImporter
    {
        /// <summary>
        /// Definition of yaml file
        /// </summary>
        public class YamlDefinition
        {
            public class Frame
            {
                public float Duration { get; set; } = 0.1f;
                public string Image { get; set; }
            }

            public class SpriteAnimationDefinition
            {
                public Frame[] Frames { get; set; }
                public int FPS { get; set; }
                public bool Looping { get; set; }
            }

            public SpriteAnimationDefinition SpriteAnimation { get; set; }
        }

        /// <summary>
        /// Import the sprite animation
        /// </summary>
        public override void Import(string source, string target)
        {
            using (var sourceFile = File.OpenRead(source))
            using (var targetWriter = new ResourceWriter(File.OpenWrite(target), typeof(SpriteAnimation)))
                Import(sourceFile, targetWriter);
        }

        private void Import(Stream source, ResourceWriter writer)
        {            
            using (var reader = new StreamReader(source))
            {
                try
                {
                    var yaml = (new YamlDotNet.Serialization.Deserializer()).Deserialize<YamlDefinition>(reader);

                    writer.Write(yaml.SpriteAnimation.FPS);
                    writer.Write(yaml.SpriteAnimation.Looping);
                    writer.Write(yaml.SpriteAnimation.Frames.Length);
                    for (int i = 0; i < yaml.SpriteAnimation.Frames.Length; i++)
                    {
                        writer.Write(yaml.SpriteAnimation.Frames[i].Image);
                        writer.Write(yaml.SpriteAnimation.Frames[i].Duration);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine();
                }                
            }
        }
    }
}
