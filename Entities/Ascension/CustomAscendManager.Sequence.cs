using System.Collections;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Randomness;
using Celeste.Core.Timing;
using Celeste.Game.Presentation.Effects.SpeedRings;
using Celeste.Core.Audio;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Scene.Tracking;
using Celeste.Simulation;

namespace Celeste.Mods.StrawberryJam.Entities.Ascension
{
    public partial class CustomAscendManager
    {
        private IEnumerator Routine()
        {
            var player =
                ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(playerRegistryKey);
            while (player == null || player.Y > Y)
            {
                player = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(
                    playerRegistryKey);
                yield return null;
            }

            launchStarted = true;
            ((ISourceScene)Scene).SourceScene.QueueSourceEntity(new Streaks(engine, this));
            player.Sprite.Play("launch");
            player.Speed = Vector2.Zero;
            player.Machine.State = 11;
            player.Dummy.Gravity = false;
            player.Dummy.AutoAnimate = false;
            if (introLaunch)
            {
                FadeSnapTo(1f);
                level.Camera.Position = player.Center + new Vector2(-160f, -90f);
                yield return 2.3f;
            }
            else
            {
                yield return FadeTo(1f);
                yield return .5f;
            }

            level.CanRetry = false;
            player.Sprite.Play("launch");
            ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/char/madeline/summit_flytonext",
                player.Position);
            yield return .25f;
            Vector2 from = player.Position;
            for (float p = 0f; p < 1f; p += engine.DeltaTime / 1f)
            {
                player.Position = Vector2Interpolation.Lerp(from, from + new Vector2(0f, 60f),
                                      ((ISourceEasingEnvironment)engine).EasingArithmetic.CubeInOut(p),
                                      ((ISourceScalarEnvironment)engine).ScalarArithmetic) +
                                  RandomShakeVector.Next(((ISourceRandomEnvironment)engine).Random);
                ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble(0, 0);
                yield return null;
            }

            var fader = new Fader(engine, this);
            ((ISourceScene)Scene).SourceScene.QueueSourceEntity(fader);
            player.X = from.X;
            from = player.Position;
            ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble(2, 1);
            for (float p = 0f; p < 1f; p += engine.DeltaTime / .5f)
            {
                float y = player.Y;
                player.Position = Vector2Interpolation.Lerp(from, from + new Vector2(0f, -160f),
                    ((ISourceEasingEnvironment)engine).EasingArithmetic.SineIn(p),
                    ((ISourceScalarEnvironment)engine).ScalarArithmetic);
                if (p == 0f || IntervalQueries.OnInterval(player.Y, y, 16f))
                    level.SourceScene.QueueSourceEntity(engine.EntityPools.Create<OriginalSpeedRingState>().Init(
                        player.Center,
                        Vector2Geometry.Angle(new Vector2(0f, -1f)), PackedColor.White));
                fader.Fade = p >= .5f ? (p - .5f) * 2f : 0f;
                yield return null;
            }

            level.CanRetry = true;
            launchCompleted = true;
            player.Y = level.Bounds.Top;
            player.SummitLaunch(player.X);
            player.Dummy.Gravity = true;
            player.Dummy.AutoAnimate = true;
            level.NextTransitionDuration = .05f;
        }
    }
}
