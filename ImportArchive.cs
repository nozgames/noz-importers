/*
  NozEngine Library

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
using System.Reflection;

namespace NoZ.Import
{
    public class ImportArchive : ResourceArchive
    {
        /// <summary>
        /// Dictionary of importers by the extension of the file they import
        /// </summary>
        private static Dictionary<string, ResourceImporter> _importers;

        public DirectoryInfo SourceDirectory { get; private set; }
        public DirectoryInfo TargetDirectory { get; private set; }
        
        public ImportArchive(string sourceDirectory, string targetDirectory) : base(ResourceArchiveMode.ReadOnly)
        {
            if (string.IsNullOrEmpty(sourceDirectory))
                throw new ArgumentNullException("sourceDirectory");

            if (string.IsNullOrEmpty(targetDirectory))
                throw new ArgumentNullException("targetDirectory");

            if (!Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException($"SourceDirectory '{sourceDirectory}' not found.");

            // Create the target directory
            Directory.CreateDirectory(targetDirectory);

            SourceDirectory = new DirectoryInfo(sourceDirectory);
            TargetDirectory = new DirectoryInfo(targetDirectory);
        }

        private void Import(string name, string sourcePath, string targetPath, FieldInfo info)
        {
            if (!_importers.TryGetValue(info.FieldType.Name, out var importer))
                throw new ImportException($"importer not found for type '{info.FieldType.Name}'");

            Console.WriteLine($"Importing {name}");

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

            using (var readStream = File.OpenRead(sourcePath))
            using (var writeStream = File.OpenWrite(targetPath))
            {
                using (var writer = new BinaryWriter(writeStream, System.Text.Encoding.Default, true))
                    writer.Write(info.FieldType.FullName);

                importer.Import(readStream, writeStream, info);
            }
        }

        public override Stream OpenRead(string name, FieldInfo info)
        {
            var files = Directory.GetFiles(SourceDirectory.FullName, $"{name}.*");
            if (files.Length == 0)
                throw new FileNotFoundException($"{SourceDirectory.FullName}/{name}.*");
            if (files.Length > 1)
                throw new InvalidOperationException($"Multiple files found named '{SourceDirectory.FullName}/{name}'");

            var targetPath = Path.Combine(TargetDirectory.FullName, name);
            var sourcePath = Path.Combine(SourceDirectory.FullName, files[0]);

            if(File.GetLastWriteTime(targetPath) <= (DateTimeOffset)File.GetLastWriteTime(sourcePath))
            {
                Import(name, sourcePath, targetPath, info);
            }

            return File.OpenRead(targetPath);
        }

        /// <summary>
        /// Initializes the static components of the ImportArchive
        /// </summary>
        static ImportArchive()
        {
            _importers = new Dictionary<string, ResourceImporter>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (ReferenceEquals(type, typeof(ResourceImporter)))
                        continue;

                    if (!typeof(ResourceImporter).IsAssignableFrom(type))
                        continue;

                    _importers[type.GetCustomAttribute<ImportTypeAttribute>().TypeName] = (ResourceImporter)Activator.CreateInstance(type);

                }
            }
        }
    }
}
