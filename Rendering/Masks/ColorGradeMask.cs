using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Environment.Core;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Content;
using Celeste.Game.Execution.Graphics;

namespace Celeste.Mods.StrawberryJam.Rendering.Masks
{
    [SceneTracked, CustomEntity("SJ2021/ColorGradeMask")]
    public sealed class ColorGradeMask : Mask
    {
        public string ColorGradeFrom, ColorGradeTo;
        public float FadeFrom, FadeTo;
        public int BufferIndex;

        public ColorGradeMask(ISourceEngineContext engine, Vector2 position, float width, float height) : base(engine,
            position, width, height)
        {
        }

        public ColorGradeMask(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(engine, data, offset)
        {
            ColorGradeFrom = data.Attr("colorGradeFrom", "(current)");
            ColorGradeTo = data.Attr("colorGradeTo", data.Attr("colorGrade", "(current)"));
            FadeFrom = data.Float("fadeFrom", 0f);
            FadeTo = data.Float("fadeTo", 1f);
        }

        public AtlasEntry GetColorGrade(bool from = false)
        {
            var name = from ? ColorGradeFrom : ColorGradeTo;
            if (name == "(current)") name = from ? Level.ColorGrade.LastGrade : Level.Session.ColorGrade;
            else if (name == "(core)")
                name = Level.CoreMode switch
                {
                    RoomCoreMode.Cold => "cold", RoomCoreMode.Hot => "hot", _ => "none"
                };
            var atlas = ((OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics)
                .ColorGradesAtlas;
            return atlas.GetOrDefault(name, atlas["none"]);
        }
    }
}
