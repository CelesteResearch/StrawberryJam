using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Rendering;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.StrawberryJam.Backdrops.Meteors
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Effects.HeartComet")]
    public class HeartComet : BackdropPresentation
    {
        private readonly ISourceEngineContext engine;
        private readonly List<AtlasEntry> texture;
        private float timer;
        private readonly float rateFPS = 10f;
        private readonly Vector2 position;
        public bool Activated;

        public HeartComet(ISourceEngineContext engine, int x, int y)
        {
            this.engine = engine;
            texture = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas
                .GetAtlasSubtextures("bgs/StrawberryJam2021/meteors/heartComet");
            position = new Vector2(x, y);
        }

        public override void Render(ISourceRenderContext context, ISourceBackdropScene scene)
        {
            base.Render(context, scene);
            if (!Activated) return;
            timer += engine.Frame.DeltaTime;
            var frame = (int)(timer * rateFPS);
            if (frame >= texture.Count) return;
            var entry = texture[frame];
            var draw = new AtlasTextureDraw(entry, position, entry.Center, PackedColor.White, Vector2.One);
            ((ISourceRenderCommandSink<AtlasTextureDraw>)context).Draw(in draw, null);
        }
    }
}
