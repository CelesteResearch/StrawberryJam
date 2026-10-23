using System.Collections;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Controls.Source;
using Celeste.Game.Entities.Platforms.Wood;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.StrawberryJam.Entities.Platforms
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.LaunchPlatform")]
    [CustomEntity("SJ2021/LaunchPlatform")]
    public sealed class LaunchPlatform : SourceJumpThruEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly Vector2 start;
        private readonly Vector2 end;
        private readonly SoundSourceComponentState sound;
        private float addY;
        private float sinkTimer;
        private AtlasEntry[] textures;
        private LaunchPlatformState state = LaunchPlatformState.Start;

        public LaunchPlatform(ISourceEngineContext engine, Vector2 position, int width, Vector2 node,
            ISourceSolidWorld world)
            : base(position, width, false, world)
        {
            this.engine = engine;
            start = Position;
            end = node;
            SurfaceSoundIndex = 5;
            Add(sound = new SoundSourceComponentState(engine));
            Add(new LightOccludeState(0.2f));
            Add(new SimulationCoroutine(engine, Sequence()));
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, OriginalLevelEntityData data)
            => new LaunchPlatform(level.EngineContext, data.Position + offset, data.Width, data.Nodes[0] + offset,
                ((ISourcePlatformCollisionScene)level).SolidWorldFactory.CreateJumpThru(data.Position + offset, false));

        public override void Added(object scene)
        {
            base.Added(scene);
            var texture = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas
                .Entries["objects/StrawberryJam2021/launchPlatform/default"];
            textures = new AtlasEntry[texture.Width / 8];
            for (int i = 0; i < textures.Length; i++) textures[i] = texture.GetSubtexture(i * 8, 0, 8, 8);
            var offset = new Vector2(Width, Height + 4f) / 2f;
            ((ISourceScene)scene).SourceScene.AddEntity(
                new OriginalMovingPlatformLineState(engine, start + offset, end + offset));
        }

        public override void Render(ISourceRenderContext context)
        {
            SourceAtlasDrawing.Draw(context, this, textures[0], Position);
            for (int x = 8; x < Width - 8; x += 8)
                SourceAtlasDrawing.Draw(context, this, textures[1], Position + new Vector2(x, 0f));
            SourceAtlasDrawing.Draw(context, this, textures[3], Position + new Vector2(Width - 8f, 0f));
            SourceAtlasDrawing.Draw(context, this, textures[2], Position + new Vector2((float)(Width / 2.0 - 4.0), 0f));
        }

        public override void Update()
        {
            base.Update();
            var extrema = ((ISourceExtremaEnvironment)engine).ExtremaArithmetic;
            if (HasPlayerRider())
            {
                sinkTimer = 0.2f;
                addY = ScalarMath.Approach(addY, 3f, 50f * engine.DeltaTime, extrema);
            }
            else if (sinkTimer > 0f)
            {
                sinkTimer -= engine.DeltaTime;
                addY = ScalarMath.Approach(addY, 3f, 50f * engine.DeltaTime, extrema);
            }
            else
            {
                addY = ScalarMath.Approach(addY, 0f, 20f * engine.DeltaTime, extrema);
            }

            if (state is not (LaunchPlatformState.Start or LaunchPlatformState.End)) return;
            var target = (state == LaunchPlatformState.Start ? start : end) + Vector2.UnitY * addY;
            if (target != Position) MoveTo(target);
        }

        // ReSharper disable IteratorNeverReturns
        private IEnumerator Sequence()
        {
            var extrema = ((ISourceExtremaEnvironment)engine).ExtremaArithmetic;
            var arithmetic = ((ISourceScalarEnvironment)engine).ScalarArithmetic;
            var easing = ((ISourceEasingEnvironment)engine).EasingArithmetic;
            var rumble = ((ISourceRumbleEnvironment)engine).RumbleSession;
            while (true)
            {
                state = LaunchPlatformState.Start;
                while (!HasPlayerRider()) yield return null;
                sound.Play("event:/game/01_forsaken_city/zip_mover");
                rumble.Rumble((int)RumbleStrength.Medium, (int)RumbleLength.Short);
                StartShaking(0.1f);
                yield return 0.1f;
                state = LaunchPlatformState.Launching;
                float at = 0f;
                while (at < 1f)
                {
                    yield return null;
                    at = ScalarMath.Approach(at, 1f, 2f * engine.DeltaTime, extrema);
                    MoveTo(Vector2Interpolation.Lerp(start, end, easing.CubeIn(at), arithmetic) + Vector2.UnitY * addY);
                }

                state = LaunchPlatformState.End;
                StartShaking(0.2f);
                rumble.Rumble((int)RumbleStrength.Strong, (int)RumbleLength.Medium);
                ((OriginalLevelSceneRoot)Scene).Shake();
                yield return 0.5f;
                state = LaunchPlatformState.Returning;
                at = 0f;
                while (at < 1f)
                {
                    yield return null;
                    at = ScalarMath.Approach(at, 1f, 0.5f * engine.DeltaTime, extrema);
                    MoveTo(Vector2Interpolation.Lerp(end, start, at, arithmetic) + Vector2.UnitY * addY);
                }

                state = LaunchPlatformState.Start;
                StartShaking(0.2f);
                yield return 0.5f;
            }
        }
        // ReSharper restore IteratorNeverReturns
    }
}
