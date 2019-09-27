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
            public class SpriteAnimationDefinition
            {
                public List<string> Frames { get; set; } = new List<string>();
                public int FPS { get; set; }
                public bool Looping { get; set; }
            }

            public SpriteAnimationDefinition SpriteAnimation { get; set; }
        }

        /// <summary>
        /// Import the sprite animation
        /// </summary>
        public override void Import(string filename, Stream target)
        {
            using (var source = File.OpenRead(filename))
                Import(source, target);
        }

        private void Import(Stream source, Stream target)
        {
            var d = new YamlDotNet.Serialization.Deserializer();
            using (var reader = new StreamReader(source))
            {
                try
                {
                    var yaml = d.Deserialize<YamlDefinition>(reader);

                    using (var writer = new BinaryWriter(target))
                    {
                        writer.Write(yaml.SpriteAnimation.FPS);
                        writer.Write(yaml.SpriteAnimation.Looping);
                        writer.Write(yaml.SpriteAnimation.Frames.Count);
                        for (int i = 0; i < yaml.SpriteAnimation.Frames.Count; i++)
                            writer.Write(yaml.SpriteAnimation.Frames[i]);
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
