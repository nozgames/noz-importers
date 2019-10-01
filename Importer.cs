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
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

namespace NoZ.Import
{
    public class Importer
    {
        private Dictionary<string, ResourceImporter> _importersByExtension;
        private Dictionary<string, ResourceImporter> _importersByType;
        private Dictionary<string, ImportFile> _files;

        /// <summary>
        /// Import all files from the source directory to the target directory
        /// </summary>
        public void Import (string from, string to)
        {
            Initialize();

            if (string.IsNullOrEmpty(from))
                throw new ArgumentNullException("from");

            if (string.IsNullOrEmpty(to))
                throw new ArgumentNullException("to");

            if (!Directory.Exists(from))
                throw new DirectoryNotFoundException(from);

            // Create the target directory
            Directory.CreateDirectory(to);

            var files = Directory.GetFiles(from, "*.*", SearchOption.AllDirectories);
            foreach(var sourcePath in files)
            {
                // Get the relative filename
                var relativeName = (new Uri(from + "\\")).MakeRelativeUri(new Uri(sourcePath)).ToString();

                // TODO: look up extension and associate impoprter
                // TODO: if extension is yaml then parse yaml and get first entry to determine type

                var name = Path.GetFileNameWithoutExtension(relativeName);
                var targetName = Path.Combine(to, name);
                var targetPath = targetName + ".resource";
                _importersByExtension.TryGetValue(Path.GetExtension(relativeName), out var importer);

                if (!_files.TryGetValue(name, out var file))
                {
                    file = new ImportFile { Filename = sourcePath, Imported = true, Importer = importer };
                    file.Filename = sourcePath;
                    file.TargetFilename = targetPath;
                    file.Name = name;
                    file.TargetDirectory = to;
                    _files[name] = file;
                }
                else if (importer != null)
                {
                    if (file.Importer != null)
                        throw new ImportException($"multiple importers defined for resource {name}");
                    
                    file.Importer = importer;
                    file.Filename = sourcePath;
                }

                if (File.Exists(targetPath))
                    file.Imported &= (File.GetLastWriteTime(targetPath) >= (DateTimeOffset)File.GetLastWriteTime(sourcePath));
                else if (Directory.Exists(targetName))
                    file.Imported &= (Directory.GetLastWriteTime(targetName) >= (DateTimeOffset)File.GetLastWriteTime(sourcePath));
                else
                    file.Imported = false;
            }

            // Import all files
            foreach (var file in _files.Values)
                Import(file);
        }

        private void Import (ImportFile file)
        {
            if (file.Imported)
                return;

            if(file.Importer == null)
            {
                if (Path.GetExtension(file.Filename).ToLower() == ".yaml")
                    ImportYaml(file);

                if(file.Importer == null)
                    throw new ImportException($"{file.Filename}: no importers available");
            }

            try
            {
                // Make sure target directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(file.TargetFilename));

                file.Importer.Import(file);
            }
            catch (Exception e)
            {
                File.Delete(file.TargetFilename);
                throw e;
            }
        }

        private void ImportYaml(ImportFile file)
        {
            if (file.Imported)
                return;

            var deserializer = new YamlDotNet.Serialization.Deserializer();
            using (var reader = new StreamReader(file.Filename))
            {
                var yaml = deserializer.Deserialize(reader) as Dictionary<object,object>;
                if (yaml.Keys.Count != 1)
                    throw new ImportException($"{file.Filename}: invalid yaml format");

                var typeName = yaml.Keys.First() as string;
                if(typeName == null)
                    throw new ImportException($"{file.Filename}: invalid yaml format");

                if(!_importersByType.TryGetValue(typeName, out var importer))
                    throw new ImportException($"{file.Filename}: not importer defined for type {typeName}");

                file.Importer = importer;
            }
        }

        /// <summary>
        /// Initializes the static components of the ImportArchive
        /// </summary>
        private bool _initialized = false;

        private void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;

            _files = new Dictionary<string, ImportFile>();
            _importersByExtension = new Dictionary<string, ResourceImporter>();
            _importersByType = new Dictionary<string, ResourceImporter>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (ReferenceEquals(type, typeof(ResourceImporter)))
                        continue;

                    if (!typeof(ResourceImporter).IsAssignableFrom(type))
                        continue;

                    var importer = (ResourceImporter)Activator.CreateInstance(type);

                    var importTypeAttr = type.GetCustomAttribute<ImportTypeAttribute>();
                    if (importTypeAttr != null)
                    {
                        var importTypeName = type.GetCustomAttribute<ImportTypeAttribute>().TypeName;
                        var importType = Type.GetType(importTypeName);
                        _importersByType[importType.Name] = importer;
                    }

                    foreach(var ext in type.GetCustomAttributes<ImportExtensionAttribute>())
                        _importersByExtension[ext.Extension] = importer;
                }
            }
        }
    }
}
