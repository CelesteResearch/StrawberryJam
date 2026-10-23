using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution;
using Celeste.Game.Execution.Graphics;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;
using Celeste.Simulation;

namespace Celeste.Mods.StrawberryJam.Presentation
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Cutscenes.CS_PrologueOutro+TitleLogo")]
    public sealed class PrologueTitleLogo : SceneEntityState
    {
        private const int ParticleCount = 13;
        private readonly ISourceEngineContext engine;
        private readonly PrologueLogoParticle[] particles;
        private readonly Sprite sprite;
        private readonly Vector2 initialPosition = new(960, 0);
        private float opacity;
        private float size;
        private int renderPhase = 1;
        public Vector2 DrawPosition;

        public PrologueTitleLogo(ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = engine;
            var random = new Random();
            var atlas = ((OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics).GuiAtlas;
            sprite = new Sprite(engine, atlas, "StrawberryJam2021/logo/");
            sprite.AddLoop("wave", "logo", 0.08f);
            sprite.AddLoop("idle", "logoIdle", 1.5f);
            sprite.Play("idle");
            sprite.OnLoop = _ =>
            {
                renderPhase++;
                if (renderPhase == 2) sprite.Play("idle");
                else if (renderPhase == 3)
                {
                    renderPhase = 0;
                    sprite.Play("wave");
                }
            };
            Tags = OriginalSceneTags.HUD;
            DrawPosition = initialPosition;
            particles = new PrologueLogoParticle[ParticleCount];
            for (int i = 0; i < ParticleCount; i++)
                particles[i] = new PrologueLogoParticle(engine, random, atlas.Entries["StrawberryJam2021/logo/sparkle"],
                    (float)random.NextDouble() * PrologueLogoParticle.DefaultParticleLifetime);
        }

        public override void Render(ISourceRenderContext context)
        {
            SourceAtlasDrawing.DrawCentered(context, this, sprite.Texture, DrawPosition, PackedColor.White * opacity,
                size);
            foreach (var particle in particles)
                SourceAtlasDrawing.DrawCentered(context, this, particle.Texture,
                    DrawPosition + particle.Offset * size, PackedColor.White * particle.Opacity * opacity,
                    particle.Size * size * 0.15f);
        }

        public override void Update()
        {
            base.Update();
            sprite.Update();
            foreach (var particle in particles) particle.Update();
        }

        public IEnumerator EaseIn()
        {
            var easing = ((ISourceEasingEnvironment)engine).EasingArithmetic;
            for (float p = 0f; p < 1f; p += engine.DeltaTime / 2.5f)
            {
                DrawPosition = initialPosition + (GameRuntime.TargetCenter - initialPosition) * easing.CubeOut(p);
                opacity = easing.CubeOut(p);
                size = easing.CubeOut(p) / 2 + 1 / 2f;
                yield return null;
            }

            opacity = 1f;
            size = 1f;
        }
    }
}
