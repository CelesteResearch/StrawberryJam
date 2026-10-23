using Celeste.Simulation;
using System;
using System.Numerics;
using System.Collections;
using Celeste.Core.Scheduling;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Rendering;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Scene;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Components.Shaking;
using Celeste.Simulation.Audio.Sources;
using Celeste.Game.Audio;

namespace Celeste.Mods.StrawberryJam.Entities.Switches
{
    public partial class FlagBreakerBox
    {
        public override void Update()
        {
            base.Update();
            if (makeSparks && ((Celeste.Scene.Tracking.ISourceScene)Scene).SourceScene.Clock.OnInterval(0.03f, engine))
            {
                Level.ParticlesFG.Emit(P_Sparks, 1, base.Center, Vector2.One * 12f);
            }

            if (shakeCounter > 0f)
            {
                shakeCounter -= engine.DeltaTime;
                if (shakeCounter <= 0f)
                {
                    shaker.On = false;
                    sprite.Scale = Vector2.One * 1.2f;
                    sprite.Play("open");
                }
            }

            if (Collidable)
            {
                bool flag = HasPlayerRider();
                sink = ScalarMath.Approach(sink, flag ? 1 : 0, 2f * engine.DeltaTime,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                sine.Rate = ScalarMath.Lerp(1f, 0.5f, sink, ((ISourceScalarEnvironment)engine).ScalarArithmetic);
                Vector2 vector = start;
                vector.Y += sink * 6f + sine.Value *
                    ScalarMath.Lerp(4f, 2f, sink, ((ISourceScalarEnvironment)engine).ScalarArithmetic);
                vector += bounce.Value * bounceDir * 12f;
                MoveToX(vector.X);
                MoveToY(vector.Y);
                if (smashParticles)
                {
                    smashParticles = false;
                    SmashParticles(Vector2Geometry.Perpendicular(bounceDir));
                    SmashParticles(-Vector2Geometry.Perpendicular(bounceDir));
                }
            }

            sprite.Scale.X = ScalarMath.Approach(sprite.Scale.X, 1f, engine.DeltaTime * 4f,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            sprite.Scale.Y = ScalarMath.Approach(sprite.Scale.Y, 1f, engine.DeltaTime * 4f,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            LiftSpeed = Vector2.Zero;
        }

        public override void Render(ISourceRenderContext context)
        {
            Vector2 position = sprite.Position;
            sprite.Position += shaker.Value;
            base.Render(context);
            sprite.Position = position;
        }

        private void Break()
        {
            var session = Level.Session;
            Celeste.Game.Entities.Mechanisms.RumbleWalls.OriginalRumbleTriggerState.ManuallyTrigger(engine,
                base.Center.X, 1.2f);
            base.Tags = ((Celeste.Game.Execution.Startup.IOriginalGameTagsEnvironment)engine).Tags.Persistent.Value;
            shakeCounter = 0f;
            shaker.On = false;
            sprite.Play("break");
            Collidable = false;
            DestroyStaticMovers();
            if (!string.IsNullOrEmpty(flag)) session.SetFlag(flag, !aliveState);

            if (musicStoreInSession)
            {
                if (!string.IsNullOrEmpty(music))
                {
                    session.Audio.Music.Event = ((IOriginalSfxEnvironment)engine).Sfx.EventnameByHandle(music);
                }

                if (musicProgress >= 0)
                {
                    session.Audio.Music.SetProgress(musicProgress);
                }

                OriginalAudioSession.Apply(session.Audio, Process.AudioSession.ChannelOutput, Process.AudioApplyPolicy);
            }
            else
            {
                if (!string.IsNullOrEmpty(music))
                {
                    Process.AudioSession.SetMusic(((IOriginalSfxEnvironment)engine).Sfx.EventnameByHandle(music),
                        startPlaying: false);
                }

                if (musicProgress >= 0)
                {
                    Process.AudioSession.SetMusicParam("progress", musicProgress);
                }

                if (!string.IsNullOrEmpty(music) && Process.AudioSession.CurrentMusicEventInstance != null)
                {
                    Process.AudioSession.CurrentMusicEventInstance.Start();
                }
            }
        }
    }
}
