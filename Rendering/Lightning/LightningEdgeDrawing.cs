using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Framework;
using Celeste.Graphics.Rendering.Vertices;

namespace Celeste.Mods.StrawberryJam.Rendering.Lightning
{
    public static class LightningEdgeDrawing
    {
        public static void Draw(ref int index, ref SourceVertexPositionColor[] verts, uint seed, Vector2 pos, Vector2 a,
            Vector2 b, PackedColor color, ISourceVectorArithmetic vectors, float thickness = 1f)
        {
            seed += (uint)(SourceSingleHashMath.GetVector2HashCodeOriginal(a) +
                           SourceSingleHashMath.GetVector2HashCodeOriginal(b));
            a += pos;
            b += pos;
            Vector2 diff = b - a;
            float length = vectors.Length(diff);
            Vector2 direction = vectors.Divide(b - a, length);
            Vector2 right = Vector2Geometry.Perpendicular(direction);
            a += right;
            b += right;
            Vector2 previous = a;
            int sign = PseudoRand(ref seed) % 2 != 0 ? 1 : -1;
            float unused = PseudoRandRange(ref seed, 0f, (float)Math.PI * 2f);
            float distance = 0f;
            float limit = index + (vectors.Length(b - a) / 4f + 1f) * 6f;
            while (limit >= verts.Length) Array.Resize(ref verts, verts.Length * 2);
            for (int i = index; i < limit; i++) verts[i].Color = color * .75f;
            do
            {
                float wiggle = PseudoRandRange(ref seed, 0f, 4f);
                unused += .1f;
                distance += 4f + wiggle;
                Vector2 next = a + direction * distance;
                next = distance < length ? next + (sign * right * wiggle - right) : b;
                verts[index++].Position = new Vector3(previous - right * thickness, 0f);
                verts[index++].Position = new Vector3(next - right * thickness, 0f);
                verts[index++].Position = new Vector3(next + right * thickness, 0f);
                verts[index++].Position = new Vector3(previous - right * thickness, 0f);
                verts[index++].Position = new Vector3(next + right * thickness, 0f);
                verts[index++].Position = new Vector3(previous, 0f);
                previous = next;
                sign = -sign;
            } while (distance < length);
        }

        private static uint PseudoRand(ref uint seed)
        {
            seed ^= seed << 13;
            seed ^= seed >> 17;
            return seed;
        }

        public static float PseudoRandRange(ref uint seed, float min, float max) =>
            min + (float)(double)(PseudoRand(ref seed) & 0x3ff) / 1024f * (max - min);
    }
}
