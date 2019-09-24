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

namespace NoZ.Import
{
    partial class TrueTypeFont
    {
        public enum CurveType : byte
        {
            None,
            Cubic,
            Conic
        }

        public struct Point
        {
            public Vector2Double xy;
            public CurveType curve;
        }

        public struct Contour
        {
            public int start;
            public int length;
        }

        public class Glyph
        {
            public ushort id;
            public char ascii;
            public Point[] points;
            public Contour[] contours;
            public double advance;
            public Vector2Double size;
            public Vector2Double bearing;
        }

        public double Ascent { get; private set; }
        public double Descent { get; private set; }
        public double Height { get; private set; }

        private Glyph[] _glyphs;
        internal List<Tuple<ushort, float>> _kerning;

        private partial class Reader { };

        public Glyph GetGlyph(char c) => _glyphs[c];

        public static TrueTypeFont Load(string path, int requestedSize, string filter)
        {
            using (var stream = File.OpenRead(path))
            {
                return Load(stream, requestedSize, filter);
            }
        }

        public static TrueTypeFont Load(Stream stream, int requestedSize, string filter)
        {
            using (var reader = new Reader(stream, requestedSize, filter))
            {
                return reader.Read();
            }
        }
    }
}
