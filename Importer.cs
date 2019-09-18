using System;
using System.IO;
using System.Reflection;

namespace NoZ.Import
{
    public abstract class Importer
    {
        public abstract void Import(Stream source, Stream target, FieldInfo info);
    }
}
