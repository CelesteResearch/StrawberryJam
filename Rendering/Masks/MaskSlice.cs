using System.Numerics;
using Celeste.Core.Geometry;

namespace Celeste.Mods.StrawberryJam.Rendering.Masks
{
    public struct MaskSlice
    {
        public Vector2 Position;
        public IntRect Source;
        public float Value;

        public MaskSlice(Vector2 position, IntRect source, float value = 1f)
        {
            Position = position;
            Source = source;
            Value = value;
        }

        public float GetValue(float from, float to) =>
            from + (to - from) * System.Math.Max(0f, System.Math.Min(1f, Value));
    }
}
