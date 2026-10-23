using System;
using System.Numerics;
using System.Collections.Generic;
using Celeste.Core.Display;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;

namespace Celeste.Mods.StrawberryJam.Entities.Ascension
{
    public partial class CustomAscendManager
    {
        [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.CustomAscendManager+Streaks")]
        public class Streaks : SceneEntityState
        {
            private sealed class Particle
            {
                public Vector2 Position;
                public float Speed;
                public int Index;
                public int Color;
            }

            public float Alpha = 1f;
            private readonly ISourceEngineContext engine;
            private readonly Particle[] particles = new Particle[80];
            private readonly IReadOnlyList<AtlasEntry> textures;
            private readonly PackedColor[] colors;
            private readonly PackedColor[] alphaColors;
            private readonly CustomAscendManager manager;

            public Streaks(ISourceEngineContext engine, CustomAscendManager manager)
                : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
            {
                this.engine = engine;
                this.manager = manager;
                colors = manager != null
                    ? manager.StreakColors
                    : new[] { PackedColor.White, SourceHexColor.HexToColor("e69ecb") };
                Depth = 20;
                textures =
                    ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.Frames("scenery/launch/slice");
                alphaColors = new PackedColor[colors.Length];
                var random = ((ISourceRandomEnvironment)engine).Random;
                for (int i = 0; i < particles.Length; i++)
                {
                    float x = 160f + SourceRandomMath.Range(random, 24f, 144f) * random.Choose(-1, 1);
                    float y = SourceRandomMath.NextFloat(random, 436f);
                    float speed = ScalarMath.ClampedMap(Math.Abs(x - 160f), 0f, 160f, .25f, 1f) *
                                  SourceRandomMath.Range(random, 600f, 2000f);
                    particles[i] = new Particle
                    {
                        Position = new Vector2(x, y), Speed = speed, Index = random.Next(textures.Count),
                        Color = random.Next(colors.Length)
                    };
                }
            }

            public override void Update()
            {
                base.Update();
                for (int i = 0; i < particles.Length; i++)
                    particles[i].Position.Y += particles[i].Speed * engine.DeltaTime;
            }

            public override void Render(ISourceRenderContext context)
            {
                float amount =
                    ((ISourceEasingEnvironment)engine).EasingArithmetic.SineInOut(
                        (manager != null ? manager.fade : 1f) * Alpha);
                var camera = ((OriginalLevelSceneRoot)Scene).Camera.Position;
                for (int i = 0; i < colors.Length; i++) alphaColors[i] = colors[i] * amount;
                for (int i = 0; i < particles.Length; i++)
                {
                    Vector2 position = particles[i].Position;
                    position.X = ScalarMath.Mod(position.X, 320f);
                    position.Y = -128f + ScalarMath.Mod(position.Y, 436f);
                    position += camera;
                    Vector2 scale = new(ScalarMath.ClampedMap(particles[i].Speed, 600f, 2000f, 1f, .25f),
                        ScalarMath.ClampedMap(particles[i].Speed, 600f, 2000f, 1f, 2f));
                    scale *= ScalarMath.ClampedMap(particles[i].Speed, 600f, 2000f, 1f, 4f);
                    var texture = textures[particles[i].Index];
                    var draw = new AtlasTextureDraw(texture, position,
                        new Vector2(texture.FrameWidth, texture.FrameHeight) * .5f, alphaColors[particles[i].Color],
                        scale);
                    ((ISourceRenderCommandSink<AtlasTextureDraw>)context).Draw(in draw, this);
                }

                if (manager != null && manager.Borders)
                {
                    SourcePrimitiveDrawing.Rect(context, this, camera.X - 10f, camera.Y - 10f, 26f, 200f,
                        alphaColors[0]);
                    SourcePrimitiveDrawing.Rect(context, this, camera.X + 320f - 16f, camera.Y - 10f, 26f, 200f,
                        alphaColors[0]);
                }
            }
        }
    }
}
