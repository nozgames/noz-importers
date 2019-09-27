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
using System.Reflection;

namespace NoZ.Import
{
    /// <summary>
    /// Abstract definition for an importer of a resource
    /// </summary>
    public abstract class ResourceImporter
    {
        /// <summary>
        /// Import the file with the given name and output it to the given stream
        /// </summary>
        /// <param name="filename">Input filename</param>
        /// <param name="target">Output binary stream</param>
        public abstract void Import(string filename, Stream target);

        /// <summary>
        /// Return the type that the importer is responsible for importing
        /// </summary>
        public Type ImportType {
            get {
                var a = GetType().GetCustomAttribute<ImportTypeAttribute>();
                if (a == null)
                    return null;

                var result = Type.GetType(a.TypeName);
                return result;
            }
        }
    }
}
