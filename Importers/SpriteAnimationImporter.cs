using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace NoZ.Import.Importers
{
    [ImportType("SpriteAnimation")]
    internal class SpriteAnimationImporter : ResourceImporter
    {
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

        public override void Import(Stream source, Stream target, FieldInfo info)
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
