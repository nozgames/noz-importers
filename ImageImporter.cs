using System;
using System.IO;
using System.Reflection;

namespace NoZ.Import
{
    internal class ImageImporter : Importer
    {
        public override void Import(Stream source, Stream target, FieldInfo info)
        {
            try
            {
                using (var reader = new BinaryReader(source))
                using (var writer = new BinaryWriter(target))
                {
                    // Import(reader, writer);

                    // TODO: if there is an imageatlas then open that atlas, add to it, and resave
                    // TODO: files are saved in the atlas using their original name for lookup at load time
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
