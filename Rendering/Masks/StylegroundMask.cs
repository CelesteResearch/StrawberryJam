using System;
using System.Numerics;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Presentation.Backdrops.HeatWave;

namespace Celeste.Mods.StrawberryJam.Rendering.Masks
{
    [SceneTracked, CustomEntity("SJ2021/StylegroundMask")]
    public sealed class StylegroundMask : Mask
    {
        private MaskRenderProcess process;
        public string[] RenderTags = Array.Empty<string>();
        public bool Foreground, EntityRenderer, BehindForeground;
        public float AlphaFrom, AlphaTo;

        public StylegroundMask(ISourceEngineContext engine, Vector2 position, float width, float height)
            : base(engine, position, width, height)
        {
            Depth = 2000000;
        }

        public StylegroundMask(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(engine, data,
            offset)
        {
            Depth = 2000000;
            RenderTags = data.Attr("tag").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            EntityRenderer = data.Bool("entityRenderer");
            BehindForeground = data.Bool("behindFg");
            AlphaFrom = data.Float("alphaFrom", 0f);
            AlphaTo = data.Float("alphaTo", 1f);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            process = MaskRenderProcess.Get(engine);
            if (EntityRenderer && !Foreground)
                Level.Scheduler.QueueSourceEntity(new StylegroundMask(engine, Position, Width, Height)
                {
                    Depth = -2000000, Foreground = true, Fade = Fade, Flag = Flag, NotFlag = NotFlag,
                    ScrollX = ScrollX, ScrollY = ScrollY, RenderTags = RenderTags, EntityRenderer = EntityRenderer,
                    BehindForeground = BehindForeground, AlphaFrom = AlphaFrom, AlphaTo = AlphaTo
                });
            if (!Foreground)
                foreach (var backdrop in Level.Foreground.Backdrops)
                    if (backdrop is HeatWaveBackdropPresentation && backdrop.GetType().Name != "HeatWaveNoColorGrade")
                    {
                        bool matches = false;
                        foreach (var tag in RenderTags)
                            if (backdrop.Tags.Contains(StylegroundMaskRenderer.TagPrefix + tag))
                            {
                                matches = true;
                                break;
                            }

                        if (!matches) continue;
                        Level.Scheduler.QueueSourceEntity(new ColorGradeMask(engine, Position, Width, Height)
                        {
                            Fade = Fade, Flag = Flag, NotFlag = NotFlag, ScrollX = ScrollX, ScrollY = ScrollY,
                            ColorGradeTo = "(core)"
                        });
                        break;
                    }
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            if (!EntityRenderer) return;

            var buffers = process.GetStylegrounds(Level).GetBuffers(Foreground);
            foreach (var tag in RenderTags)
                if (buffers.TryGetValue(tag, out var buffer))
                    foreach (var slice in GetMaskSlices())
                        process.DrawBuffer(buffer, slice.Position, slice.Source,
                            Celeste.Core.Display.PackedColor.White * slice.GetValue(AlphaFrom, AlphaTo));
        }
    }
}
