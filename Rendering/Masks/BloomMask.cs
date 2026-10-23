using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.StrawberryJam.Rendering.Masks
{
    [SceneTracked, CustomEntity("SJ2021/BloomMask")]
    public sealed class BloomMask : Mask
    {
        public float BaseFrom, BaseTo, StrengthFrom, StrengthTo;

        public BloomMask(ISourceEngineContext engine, Vector2 position, float width, float height) : base(engine,
            position, width, height)
        {
        }

        public BloomMask(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(engine, data, offset)
        {
            BaseFrom = data.Float("baseFrom", -1f);
            BaseTo = data.Float("baseTo", -1f);
            StrengthFrom = data.Float("strengthFrom", -1f);
            StrengthTo = data.Float("strengthTo", -1f);
        }
    }
}
