using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.StrawberryJam.Rendering.Masks
{
    [SceneTracked, CustomEntity("SJ2021/LightingMask")]
    public sealed class LightingMask : Mask
    {
        public float LightingFrom, LightingTo;
        public bool AddBase;
        public int BufferIndex;

        public LightingMask(ISourceEngineContext engine, Vector2 position, float width, float height) : base(engine,
            position, width, height)
        {
        }

        public LightingMask(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(engine, data, offset)
        {
            LightingFrom = data.Float("lightingFrom", -1f);
            LightingTo = data.Float("lightingTo", 0f);
            AddBase = data.Bool("addBase", true);
        }
    }
}
