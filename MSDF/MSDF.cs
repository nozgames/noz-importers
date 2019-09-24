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

namespace NoZ.Import
{
    partial class MSDF
    {

        public static void RenderGlyph(
            TrueTypeFont.Glyph glyph,
            PixelData output,
            Vector2Int outputPosition,
            Vector2Int outputSize,
            double range,
            in Vector2Double scale,
            in Vector2Double translate
            )
        {
            GenerateSDF(
                output,
                outputPosition,
                outputSize,
                Shape.FromGlyph(glyph, true),
                range,
                scale,
                translate
            );
        }

        private static void GenerateSDF(PixelData output, Vector2Int outputPosition, Vector2Int outputSize, Shape shape, double range, Vector2Double scale, Vector2Double translate)
        {
            int contourCount = shape.contours.Length;
            int w = outputSize.x;
            int h = outputSize.y;

            // Get the windings..
            var windings = new int[contourCount];
            for (int i = 0; i < shape.contours.Length; i++)
                windings[i] = shape.contours[i].Winding();

            var contourSD = new double[contourCount];
            for (int y = 0; y < h; ++y)
            {
                int row = shape.InverseYAxis ? h - y - 1 : y;
                for (int x = 0; x < w; ++x)
                {
                    double dummy = 0;
                    Vector2Double p = new Vector2Double(x + .5, y + .5) / scale - translate;
                    double negDist = -SignedDistance.Infinite.distance;
                    double posDist = SignedDistance.Infinite.distance;
                    int winding = 0;

                    for (int i = 0; i < shape.contours.Length; i++)
                    {
                        SignedDistance minDistance = SignedDistance.Infinite;
                        foreach (var edge in shape.contours[i].edges)
                        {
                            SignedDistance distance = edge.GetSignedDistance(p, out dummy);
                            if (distance < minDistance)
                                minDistance = distance;
                        }
                        contourSD[i] = minDistance.distance;
                        if (windings[i] > 0 && minDistance.distance >= 0 && Math.Abs(minDistance.distance) < Math.Abs(posDist))
                            posDist = minDistance.distance;
                        if (windings[i] < 0 && minDistance.distance <= 0 && Math.Abs(minDistance.distance) < Math.Abs(negDist))
                            negDist = minDistance.distance;
                    }

                    double sd = SignedDistance.Infinite.distance;
                    if (posDist >= 0 && Math.Abs(posDist) <= Math.Abs(negDist))
                    {
                        sd = posDist;
                        winding = 1;
                        for (int i = 0; i < contourCount; ++i)
                            if (windings[i] > 0 && contourSD[i] > sd && Math.Abs(contourSD[i]) < Math.Abs(negDist))
                                sd = contourSD[i];
                    }
                    else if (negDist <= 0 && Math.Abs(negDist) <= Math.Abs(posDist))
                    {
                        sd = negDist;
                        winding = -1;
                        for (int i = 0; i < contourCount; ++i)
                            if (windings[i] < 0 && contourSD[i] < sd && Math.Abs(contourSD[i]) < Math.Abs(posDist))
                                sd = contourSD[i];
                    }
                    for (int i = 0; i < contourCount; ++i)
                        if (windings[i] != winding && Math.Abs(contourSD[i]) < Math.Abs(sd))
                            sd = contourSD[i];

                    // Set the SDF value in the output image
                    sd /= (range * 2.0f);
                    sd = MathEx.Clamp(sd, -0.5, 0.5) + 0.5;

                    // TODO: Set using single value instead of color
                    output.SetPixel(
                        x + outputPosition.x,
                        row + outputPosition.y,
                        Color.FromRgba(0, 0, 0, (byte)(sd * 255.0f)));
                }
            }
        }
    }
}