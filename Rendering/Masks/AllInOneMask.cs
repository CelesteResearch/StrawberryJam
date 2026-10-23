using Celeste.Core.Scheduling;
using Celeste.Scene.Entities;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Everest.Modules.Entities.Metadata;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mods.StrawberryJam.Rendering.Masks
{
    [CustomEntity("SJ2021/AllInOneMask")]
    public class AllInOneMask : Mask
    {
        private List<SceneEntityState> masks;

        public AllInOneMask(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(engine, data, offset)
        {
            float Width = data.Width;
            float Height = data.Height;

            masks = new List<SceneEntityState>();

            string stylemaskTag = data.Attr("stylemaskTag");
            float styleAlphaFrom = data.Float("styleAlphaFrom");
            float styleAlphaTo = data.Float("styleAlphaTo", 1f);
            bool entityRenderer = data.Bool("entityRenderer");
            bool styleBehindFg = data.Bool("styleBehindFg", true);
            if (!string.IsNullOrEmpty(stylemaskTag))
            {
                masks.Add(new StylegroundMask(engine, Position, Width, Height)
                {
                    Fade = Fade, FadeMask = FadeMask, Flag = Flag, NotFlag = NotFlag, ScrollX = ScrollX,
                    ScrollY = ScrollY,
                    RenderTags = stylemaskTag.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                    AlphaFrom = styleAlphaFrom,
                    AlphaTo = styleAlphaTo,
                    EntityRenderer = entityRenderer,
                    BehindForeground = styleBehindFg
                });
            }

            string colorGradeFrom = data.Attr("colorGradeFrom", "(current)");
            string colorGradeTo = data.Attr("colorGradeTo", "(current)");
            float colorGradeFadeFrom = data.Float("colorGradeFadeFrom", 0f);
            float colorGradeFadeTo = data.Float("colorGradeFadeTo", 1f);
            if (colorGradeFrom != "(current)" || colorGradeTo != "(current)")
            {
                masks.Add(new ColorGradeMask(engine, Position, Width, Height)
                {
                    Fade = Fade, FadeMask = FadeMask, Flag = Flag, NotFlag = NotFlag, ScrollX = ScrollX,
                    ScrollY = ScrollY,
                    ColorGradeFrom = colorGradeFrom,
                    ColorGradeTo = colorGradeTo,
                    FadeFrom = colorGradeFadeFrom,
                    FadeTo = colorGradeFadeTo
                });
            }

            float bloomBaseFrom = data.Float("bloomBaseFrom", -1f);
            float bloomBaseTo = data.Float("bloomBaseTo", -1f);
            float bloomStrengthFrom = data.Float("bloomStrengthFrom", -1f);
            float bloomStrengthTo = data.Float("bloomStrengthTo", -1f);
            if (bloomBaseFrom >= 0f || bloomBaseTo >= 0f || bloomStrengthFrom >= 0f || bloomStrengthTo >= 0f)
            {
                masks.Add(new BloomMask(engine, Position, Width, Height)
                {
                    Fade = Fade, FadeMask = FadeMask, Flag = Flag, NotFlag = NotFlag, ScrollX = ScrollX,
                    ScrollY = ScrollY,
                    BaseFrom = bloomBaseFrom,
                    BaseTo = bloomBaseTo,
                    StrengthFrom = bloomStrengthFrom,
                    StrengthTo = bloomStrengthTo
                });
            }

            float lightingFrom = data.Float("lightingFrom", -1f);
            float lightingTo = data.Float("lightingTo", -1f);
            bool addBaseLighting = data.Bool("addBaseLight", true);
            if (lightingFrom >= 0f || lightingTo >= 0f)
            {
                masks.Add(new LightingMask(engine, Position, Width, Height)
                {
                    Fade = Fade, FadeMask = FadeMask, Flag = Flag, NotFlag = NotFlag, ScrollX = ScrollX,
                    ScrollY = ScrollY,
                    LightingFrom = lightingFrom,
                    LightingTo = lightingTo,
                    AddBase = addBaseLighting
                });
            }
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            foreach (SceneEntityState mask in masks)
                ((Celeste.Scene.Tracking.ISourceScene)scene).SourceScene.QueueSourceEntity(mask);
        }
    }
}
