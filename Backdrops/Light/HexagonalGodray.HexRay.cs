using System;
using System.Numerics;
using Celeste.Core.Randomness;

namespace Celeste.Mods.StrawberryJam.Backdrops.Light
{
    public partial class HexagonalGodray
    {
        private struct HexRay
        {
            public float X, Y, Percent, Duration, angle;
            public int length;
            public Vector2[] points;

            public void Reset(ISourceRandom random, float minRotation, float maxRotation)
            {
                Percent = 0f;
                X = SourceRandomMath.NextFloat(random, 384f);
                Y = SourceRandomMath.NextFloat(random, 244f);
                Duration = 4f + SourceRandomMath.NextFloat(random) * 8f;
                float degrees = minRotation - maxRotation + SourceRandomMath.NextFloat(random) * maxRotation * 2f;
                angle = (float)(Math.PI / 180.0 * degrees);
                length = random.Next(15, 22);
                if (points == null) points = new Vector2[3];
                points[0] = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                points[1] = new Vector2((float)Math.Cos(angle + Math.PI / 3.0), (float)Math.Sin(angle + Math.PI / 3.0));
                points[2] = new Vector2((float)Math.Cos(angle + Math.PI * 2.0 / 3.0),
                    (float)Math.Sin(angle + Math.PI * 2.0 / 3.0));
            }
        }
    }
}
