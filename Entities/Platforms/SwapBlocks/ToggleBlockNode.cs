using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Components;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Scene.Tracking;
using Celeste.Scene.Components.Oscillation;

namespace Celeste.Mods.StrawberryJam.Entities.Platforms.SwapBlocks
{
    [SceneTracked]
    public sealed class ToggleBlockNode : SceneEntityState
    {
        private readonly AtlasEntry nodeTexture;
        public PackedColor Color = PackedColor.Red;
        private readonly SineWaveState sine;
        private float positionOffset;

        public ToggleBlockNode(ISourceEngineContext engine) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, Vector2.Zero)
        {
            nodeTexture =
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[
                    "objects/StrawberryJam2021/toggleSwapBlock/node"];
            Depth = 8999;
            Add(sine = new SineWaveState(engine, .6f, 0f));
        }

        public ToggleBlockNode Init(Vector2 position)
        {
            Position = position;
            return this;
        }

        public override void Update()
        {
            base.Update();
            positionOffset = sine.Value - 2f;
        }

        public override void Render(ISourceRenderContext context)
            => SourceAtlasDrawing.DrawCentered(context, this, nodeTexture, Position + Vector2.UnitY * positionOffset,
                Color);
    }
}
