using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Core.Rendering.States;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.StrawberryJam.Entities.Platforms.SwapBlocks
{
    [SceneTracked]
    public sealed class ToggleBlockLaser : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly AtlasEntry laserSprite;
        private float angle;
        public PackedColor Color;
        public Vector2 Start;
        public Vector2 End;
        private float timer;
        private float sineTimer;

        public ToggleBlockLaser(ISourceEngineContext engine) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, Vector2.Zero)
        {
            this.engine = engine;
            laserSprite =
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[
                    "objects/StrawberryJam2021/toggleSwapBlock/laser"];
            Depth = 8999;
            timer = SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random);
        }

        public ToggleBlockLaser Init(Vector2 currentNode, Vector2 nextNode)
        {
            Start = currentNode;
            End = nextNode;
            angle = Vector2Geometry.Angle(nextNode - currentNode);
            return this;
        }

        public override void Update()
        {
            base.Update();
            timer += engine.DeltaTime * 4f;
            sineTimer += engine.DeltaTime * 12f;
        }

        public override void Render(ISourceRenderContext context)
        {
            float opacity = .5f * (.5f + ((float)Math.Sin(timer) + 1f) * .25f);
            float distance = ((ISourceVectorEnvironment)engine).VectorArithmetic.Distance(Start, End);
            SineTextureDrawing.DrawHorizontal(context, laserSprite, Start, Vector2.Zero,
                new Vector2(distance / 128f, 1.5f), angle, Color * opacity, SourceSpriteEffects.None, sineTimer, 1f, 1,
                .04f);
        }
    }
}
