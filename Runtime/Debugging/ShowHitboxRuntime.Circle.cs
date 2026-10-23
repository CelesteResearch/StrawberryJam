using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Drawing;

namespace Celeste.Mods.StrawberryJam.Runtime.Debugging
{
    internal sealed partial class ShowHitboxRuntime
    {
        private static void CircleOctant(ISourceDrawScope scope, Vector2 center, float radius, PackedColor color,
            float flipX, float flipY, bool interchangeXy)
        {
            // when flipX = flipY = 1 and interchangeXY = false, we are drawing the [0, pi/4] octant.

            float cx, cy;
            if (interchangeXy)
            {
                cx = center.Y;
                cy = center.X;
            }
            else
            {
                cx = center.X;
                cy = center.Y;
            }

            float x, y;
            if (flipX > 0)
            {
                x = (float)Math.Ceiling(cx + radius - 1);
            }
            else
            {
                x = (float)Math.Floor(cx - radius + 1);
            }

            if (flipY > 0)
            {
                y = (float)Math.Floor(cy);
            }
            else
            {
                y = (float)Math.Ceiling(cy);
            }

            float starty = y;
            float e = (x - cx) * (x - cx) + (y - cy) * (y - cy) - radius * radius;
            float yc = flipY * 2 * (y - cy) + 1;
            float xc = flipX * -2 * (x - cx) + 1;
            while (flipY * (y - cy) <= flipX * (x - cx))
            {
                // Slower than using DrawLine, but more obviously correct:
                // DrawPoint((int)x + (flipX < 0 ? -1 : 0), (int)y + (flipY < 0 ? -1 : 0), interchangeXY, color);
                e += yc;
                y += flipY;
                yc += 2;
                if (e >= 0)
                {
                    // We would have a 1px correction for flipY here (as we do for flipX) except for
                    // the fact that our lines always include the top pixel and exclude the bottom one.
                    // Because of this we would have to make two corrections which cancel each other out,
                    // so we just don't do either of them.
                    DrawLine(scope, (int)x + (flipX < 0 ? -1 : 0), (int)starty, (int)y, interchangeXy, color);
                    starty = y;
                    e += xc;
                    x -= flipX;
                    xc += 2;
                }
            }

            DrawLine(scope, (int)x + (flipX < 0 ? -1 : 0), (int)starty, (int)y, interchangeXy, color);
        }

        private static void DrawLine(ISourceDrawScope scope, int x, int y0, int y1, bool interchangeXy,
            PackedColor color)
        {
            // x, y0, and y1 must all be integers
            int length = (int)(y1 - y0);
            IntRect rect;
            if (interchangeXy)
            {
                rect.X = (int)y0;
                rect.Y = (int)x;
                rect.Width = length;
                rect.Height = 1;
            }
            else
            {
                rect.X = (int)x;
                rect.Y = (int)y0;
                rect.Width = 1;
                rect.Height = length;
            }

            SourcePrimitiveDrawing.Rect(scope, rect, color);
        }
    }
}
