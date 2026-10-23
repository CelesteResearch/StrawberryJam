using System.Collections.Generic;
using Celeste.Core.Geometry;

namespace Celeste.Mods.StrawberryJam.Rendering.Masks
{
    internal sealed class BloomMaskPassState
    {
        public float Strength;
        public readonly List<IntRect> Rectangles = new(320);
    }
}
