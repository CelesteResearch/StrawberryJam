using Celeste.Core.Display;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Graphics.Display;
using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Animation;
using Celeste.Core.Components.Timing;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Controls.Source;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Components.Oscillation;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Camera;
using Celeste.Simulation.Gameplay.Collectibles.Strawberry;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Gameplay.Player.Presentation;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Simulation.Presentation.Effects.Trails.Lifecycle;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Rendering;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Profiles;
using Celeste.Game.Entities.BadelineDummy;
using Celeste.Game.Stories.Farewell.FinalLaunch;

namespace Celeste.Mods.StrawberryJam.Entities.Boosters
{
    public partial class CustomBadelineBoost
    {
        private IEnumerator BoostRoutine(RoomPlayerSession player)
        {
            holding = player;
            travelling = true;
            nodeIndex++;
            sprite.Visible = false;
            sprite.Position = Vector2.Zero;
            Collidable = false;
            bool finalBoost = nodeIndex >= nodes.Length;
            OriginalLevelSceneRoot level = (OriginalLevelSceneRoot)player.Scene;
            if (finalBoost) PlaySound("event:/new_content/char/badeline/booster_finalfinal_part1", Position);
            else PlaySound("event:/char/badeline/booster_begin", Position);

            if (player.Holding.Holding != null)
            {
                player.Holding.Drop();
            }

            player.Machine.State = 11;
            if (forceCameraUpdate) player.ForceCameraUpdate = true;
            player.Dummy.AutoAnimate = false;
            player.Dummy.Gravity = false;
            if (((OriginalLevelSceneRoot)player.Scene).Session.Inventory.Dashes > 1)
            {
                player.Data.Dashes = 1;
            }
            else
            {
                player.RefillDash();
            }

            player.RefillStamina();
            player.Speed = Vector2.Zero;
            int num = Math.Sign(player.Position.X - base.X);
            if (num == 0)
            {
                num = -1;
            }

            BadelineDummyPresentation badeline = BadelineDummyPresentation.Create(level, Position);
            badeline.Light.Visible = !finalBoost;
            ((ISourceScene)Scene).SourceScene.QueueSourceEntity(badeline);
            player.Data.Facing = -num;
            badeline.Sprite.Scale.X = num;
            Vector2 playerFrom = player.Position;
            Vector2 playerTo = Position + new Vector2(num * 4, -3f);
            Vector2 badelineFrom = badeline.Position;
            Vector2 badelineTo = Position + new Vector2(-num * 4, 3f);
            for (float p = 0f; p < 1f; p += engine.DeltaTime / 0.2f)
            {
                Vector2 vector = Vector2Interpolation.Lerp(playerFrom, playerTo, p,
                    ((ISourceScalarEnvironment)engine).ScalarArithmetic);
                if (player.Scene != null)
                {
                    player.MoveToX(vector.X);
                }

                if (player.Scene != null)
                {
                    player.MoveToY(vector.Y);
                }

                badeline.Position = Vector2Interpolation.Lerp(badelineFrom, badelineTo, p,
                    ((ISourceScalarEnvironment)engine).ScalarArithmetic);
                yield return null;
            }

            if (finalBoost)
            {
                Vector2 screenSpaceFocusPoint =
                    new Vector2(ScalarMath.Clamp(player.Position.X - level.Camera.Position.X, 120f, 200f),
                        ScalarMath.Clamp(player.Position.Y - level.Camera.Position.Y, 60f, 120f));
                Add(new SimulationCoroutine(engine, level.ZoomTo(screenSpaceFocusPoint, 1.5f, 0.18f)));
                engine.Effects.TimeRate = 0.5f;
            }
            else
            {
                PlaySound("event:/char/badeline/booster_throw", Position);
            }

            badeline.Sprite.Play("boost");
            yield return 0.1f;
            if (!player.Dead)
            {
                player.MoveV(5f);
            }

            yield return 0.1f;
            if (finalBoost)
            {
                ((ISourceScene)Scene).SourceScene.QueueSourceEntity(
                    new Celeste.Mods.StrawberryJam.Cutscenes.Launch.CustomLaunchCutscene(engine, player, this, room,
                        spawnPoint, colorGrade, transitionType, wipeColor));
                player.Active = false;
                badeline.Active = false;
                Active = false;
                yield return null;
                player.Active = true;
                badeline.Active = true;
            }

            Add(SourceAlarmState.Create(engine.AlarmPool, SourceAlarmState.AlarmMode.Oneshot, delegate
            {
                if (player.Data.Dashes < ((OriginalLevelSceneRoot)player.Scene).Session.Inventory.Dashes)
                {
                    player.Data.Dashes++;
                }

                ((ISceneEntityRemovalScene)Scene).RemoveEntity(badeline);
                level.Displacement.AddBurst(badeline.Position, 0.25f, 8f, 32f, 0.5f);
            }, 0.15f, start: true));
            level.Shake(0.3f);
            holding = null;
            if (!finalBoost)
            {
                player.BadelineBoostLaunch(base.CenterX);
                Vector2 from = Position;
                Vector2 to = nodes[nodeIndex];
                float val = VectorArithmetic.Distance(from, to) / 320f;
                val = Math.Min(3f, val);
                stretch.Visible = true;
                stretch.Rotation = Vector2Geometry.Angle(to - from);
                SourceTweenState tween = SourceTweenState.Create(engine.TweenPool, SourceTweenState.TweenMode.Oneshot,
                    ((ISourceEasingEnvironment)engine).EasingArithmetic.SineInOut, val, start: true);
                tween.OnUpdate = delegate(SourceTweenState t)
                {
                    Position = Vector2Interpolation.Lerp(from, to, t.Eased,
                        ((ISourceScalarEnvironment)engine).ScalarArithmetic);
                    stretch.Scale.X = (float)(1d + (double)ScalarMath.YoYo(t.Eased) * 2d);
                    stretch.Scale.Y = (float)(1d - (double)ScalarMath.YoYo(t.Eased) * .75d);
                    if (t.Eased < 0.9f && CurrentLevel.Clock.OnInterval(0.03f, engine))
                    {
                        TrailManagerState.Add(engine, (ISourceRenderEnvironment)engine, this,
                            global::Celeste.Simulation.Gameplay.Player.Presentation.Hair.PlayerHairAppearance
                                .TwoDashesHairColor, 0.5f);
                        level.ParticlesFG.Emit(Process.ParticleTypes.BadelineBoost.P_Move, 1, base.Center,
                            Vector2.One * 4f);
                    }
                };
                tween.OnComplete = delegate
                {
                    if (base.X >= (float)level.Bounds.Right)
                    {
                        RemoveSelf();
                    }
                    else
                    {
                        travelling = false;
                        stretch.Visible = false;
                        sprite.Visible = true;
                        Collidable = true;
                        PlaySound("event:/char/badeline/booster_reappear", Position);
                    }
                };
                Add(tween);
                relocateSfx.Play("event:/char/badeline/booster_relocate");
                ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble(2, 1);
                level.DirectionalShake(-Vector2.UnitY);
                level.Displacement.AddBurst(base.Center, 0.4f, 8f, 32f, 0.5f);
            }
            else
            {
                FinalBoostSfx = PlaySound("event:/new_content/char/badeline/booster_finalfinal_part2", Position);
                engine.Effects.FreezeTimer = 0.1f;
                yield return null;
                ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble(2, 2);
                level.Flash(PackedColor.White * .5f, drawPlayerOver: true);
                level.DirectionalShake(-Vector2.UnitY, 0.6f);
                level.Displacement.AddBurst(base.Center, 0.6f, 8f, 64f, 0.5f);
                level.ResetZoom();
                player.SummitLaunch(base.X);
                engine.Effects.TimeRate = 1f;
                Finish();
            }

            if (forceCameraUpdate) player.ForceCameraUpdate = false;
        }
    }
}
