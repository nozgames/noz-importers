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

namespace NoZ.Import
{
    partial class MSDF
    {
        private class Contour
        {
            public Edge[] edges;

            public void Bounds(ref double l, ref double b, ref double r, ref double t)
            {
                foreach (Edge edge in edges)
                    edge.Bounds(ref l, ref b, ref r, ref t);
            }

            public int Winding()
            {
                if (edges.Length == 0)
                    return 0;

                double total = 0;
                if (edges.Length == 1)
                {
                    var a = edges[0].GetPoint(0);
                    var b = edges[0].GetPoint(1 / 3.0);
                    var c = edges[0].GetPoint(2 / 3.0);
                    total += ShoeLace(a, b);
                    total += ShoeLace(b, c);
                    total += ShoeLace(c, a);
                }
                else if (edges.Length == 2)
                {
                    var a = edges[0].GetPoint(0);
                    var b = edges[0].GetPoint(0.5);
                    var c = edges[1].GetPoint(0);
                    var d = edges[1].GetPoint(.5);
                    total += ShoeLace(a, b);
                    total += ShoeLace(b, c);
                    total += ShoeLace(c, d);
                    total += ShoeLace(d, a);
                }
                else
                {
                    var prev = edges.Last().GetPoint(0);
                    foreach (var edge in edges)
                    {
                        var cur = edge.GetPoint(0);
                        total += ShoeLace(prev, cur);
                        prev = cur;
                    }
                }
                return Sign(total);
            }
        }
    }
}
