using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Scheduling;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Rendering;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Controls.Source;
using Celeste.Input.Gameplay;
using Celeste.Game.Gameplay.Mechanisms.DashSwitches;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Content;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Interaction.Springs;
using Celeste.Simulation.Gameplay.Environment.Wind;
using Celeste.Simulation.Gameplay.Enemies.Seeker;
using Celeste.Simulation.Gameplay.Player.Movement;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Mods.StrawberryJam.Runtime.Holdables.Lanterns;
using Celeste.Scene.Entities;

namespace Celeste.Mods.StrawberryJam.Entities.Holdables.Lanterns
{
    public partial class SkyLantern
    {
        internal IEnumerator PlayerPickupCoroutine(PlayerHoldingController holding, RoomPlayerSession player)
        {
            Vector2 carryOffsetTarget = new Vector2(0f, -12f);
            player.Play("event:/char/madeline/crystaltheo_lift");
            Rumble(RumbleStrength.Medium, RumbleLength.Short);
            if (holding.Holding != null && holding.Holding.SlowFall &&
                player.Data.Motion.GliderBoostTimer - .16f > 0f && player.Data.Motion.GliderBoostDirection.Y > 0f ||
                (((ISourceVectorEnvironment)engine).VectorArithmetic.Length(player.Speed) > 180f &&
                 player.Speed.Y <= 0f))
                PlaySound("event:/new_content/game/10_farewell/glider_platform_dissipate", player.Position);
            Vector2 oldSpeed = player.Speed;
            float varJump = player.Data.Motion.Normal.VariableJumpTimer;
            player.Speed = Vector2.Zero;
            Vector2 begin = ((ISourceEntityPosition)holding.Holding.Entity).Position - player.Position;
            Vector2 control = new Vector2(begin.X + Math.Sign(begin.X) * 2, carryOffsetTarget.Y - 2f);
            var curve = new SimpleCurve(begin, carryOffsetTarget, control);
            SkyLanternRuntime.WriteCarryOffset(holding, begin);
            var tween = Celeste.Core.Components.Animation.SourceTweenState.Create(engine.TweenPool,
                Celeste.Core.Components.Animation.SourceTweenState.TweenMode.Oneshot,
                ((Celeste.Core.Mathematics.Easing.ISourceEasingEnvironment)engine).EasingArithmetic.CubeInOut,
                .16f, true);
            tween.OnUpdate = t => SkyLanternRuntime.WriteCarryOffset(holding,
                curve.GetPoint(t.Eased, ((ISourceCurveEnvironment)engine).CurveArithmetic));
            player.Components.Add(tween);
            yield return tween.Wait();
            player.Speed = oldSpeed;
            player.Data.Motion.Normal.VariableJumpTimer = varJump;
            player.Machine.State = 0;
            if (holding.Holding != null && holding.Holding.SlowFall)
            {
                if (player.Data.Motion.GliderBoostTimer > 0f && player.Data.Motion.GliderBoostDirection.Y > 0f)
                {
                    Rumble(RumbleStrength.Medium, RumbleLength.Short);
                    player.Data.Motion.GliderBoostTimer = 0f;
                    player.Data.Motion.Normal.Speed.Y = ((ISourceExtremaEnvironment)engine).ExtremaArithmetic.Max(
                        player.Data.Motion.Normal.Speed.Y, 240f * player.Data.Motion.GliderBoostDirection.Y);
                }
                else if (player.Data.Motion.GliderBoostTimer > 0f && player.Data.Motion.GliderBoostDirection.Y < 0f &&
                         ((SkyLantern)holding.Holding.Entity).canBoostUp)
                {
                    Rumble(RumbleStrength.Medium, RumbleLength.Short);
                    player.Data.Motion.GliderBoostTimer = 0f;
                    player.Data.Motion.Normal.Speed.Y = ((ISourceExtremaEnvironment)engine).ExtremaArithmetic.Min(
                        player.Data.Motion.Normal.Speed.Y, -240f * Math.Abs(player.Data.Motion.GliderBoostDirection.Y));
                }
                else if (player.Speed.Y > 0f && player.Data.Motion.GliderBoostTimer <= 0f)
                {
                    float pickupTimeDiff = ((ISourceScene)player.Scene).SourceScene.Clock.TimeActive - lastDroppedTime;
                    if (pickupTimeDiff < 1.5f) player.Data.Motion.Normal.Speed.Y = -player.Speed.Y * 1.2f;
                    else
                        player.Data.Motion.Normal.Speed.Y = ((ISourceExtremaEnvironment)engine).ExtremaArithmetic.Max(
                            player.Speed.Y, -105f);
                }
                else player.Data.Motion.Normal.Speed.Y = player.Speed.Y / 2f;

                if (player.Data.OnGround && holding.Input.MoveY.Value == 1)
                    player.Data.Motion.Normal.HoldCannotDuck = true;
            }
        }

        private IEnumerator DestroyAnimationCoroutine()
        {
            PlaySound("event:/new_content/game/10_farewell/glider_emancipate", Position);
            sprite.Play("death", false, false);
            var tween = Celeste.Core.Components.Animation.SourceTweenState.Create(engine.TweenPool,
                Celeste.Core.Components.Animation.SourceTweenState.TweenMode.Oneshot,
                ((Celeste.Core.Mathematics.Easing.ISourceEasingEnvironment)engine).EasingArithmetic.Linear, 1f);
            tween.Start(true);
            tween.OnUpdate = t => light.Alpha = t.Eased;
            Add(tween);
            yield return 1f;
            RemoveSelf();
        }
    }
}
