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
        public DashCollisionResult Dashed(RoomPlayerSession player, Vector2 dir)
        {
            if (!Process.SaveContext.CurrentSave.Assists.Invincible)
            {
                if (dir == Vector2.UnitX && spikesLeft)
                {
                    return DashCollisionResult.NormalCollision;
                }

                if (dir == -Vector2.UnitX && spikesRight)
                {
                    return DashCollisionResult.NormalCollision;
                }

                if (dir == Vector2.UnitY && spikesUp)
                {
                    return DashCollisionResult.NormalCollision;
                }

                if (dir == -Vector2.UnitY && spikesDown)
                {
                    return DashCollisionResult.NormalCollision;
                }
            }

            Level.DirectionalShake(dir);
            sprite.Scale = new Vector2(1f + Math.Abs(dir.Y) * 0.4f - Math.Abs(dir.X) * 0.4f,
                1f + Math.Abs(dir.X) * 0.4f - Math.Abs(dir.Y) * 0.4f);
            health--;
            if (health > 0)
            {
                OwnedComponents.Add(firstHitSfx = ((ISourceSoundSourceEnvironment)engine).SoundSources.Create()
                    .Play("event:/new_content/game/10_farewell/fusebox_hit_1"));
                ((ISourceFreezeEnvironment)engine).Freeze(0.1f);
                shakeCounter = 0.2f;
                shaker.On = true;
                bounceDir = dir;
                bounce.Start();
                smashParticles = true;
                ((Celeste.Input.Gameplay.Feedback.ISourceRumbleEnvironment)engine).RumbleSession.Rumble(1, 1);
            }
            else
            {
                if (firstHitSfx != null)
                {
                    firstHitSfx.Stop();
                }

                ((Celeste.Core.Audio.ISourceSpatialAudio)engine).Play(
                    "event:/new_content/game/10_farewell/fusebox_hit_2",
                    Position);
                ((ISourceFreezeEnvironment)engine).Freeze(0.2f);
                player.RefillDash();
                Break();
                ((Celeste.Input.Gameplay.Feedback.ISourceRumbleEnvironment)engine).RumbleSession.Rumble(2, 2);
                SmashParticles(Vector2Geometry.Perpendicular(dir));
                SmashParticles(-Vector2Geometry.Perpendicular(dir));
            }

            return DashCollisionResult.Rebound;
        }

        private void SmashParticles(Vector2 dir)
        {
            float direction;
            Vector2 position;
            Vector2 positionRange;
            int num;
            if (dir == Vector2.UnitX)
            {
                direction = 0f;
                position = base.CenterRight - Vector2.UnitX * 12f;
                positionRange = Vector2.UnitY * (base.Height - 6f) * 0.5f;
                num = (int)(base.Height / 8f) * 4;
            }
            else if (dir == -Vector2.UnitX)
            {
                direction = (float)Math.PI;
                position = base.CenterLeft + Vector2.UnitX * 12f;
                positionRange = Vector2.UnitY * (base.Height - 6f) * 0.5f;
                num = (int)(base.Height / 8f) * 4;
            }
            else if (dir == Vector2.UnitY)
            {
                direction = (float)Math.PI / 2f;
                position = base.BottomCenter - Vector2.UnitY * 12f;
                positionRange = Vector2.UnitX * (base.Width - 6f) * 0.5f;
                num = (int)(base.Width / 8f) * 4;
            }
            else
            {
                direction = -(float)Math.PI / 2f;
                position = base.TopCenter + Vector2.UnitY * 12f;
                positionRange = Vector2.UnitX * (base.Width - 6f) * 0.5f;
                num = (int)(base.Width / 8f) * 4;
            }

            num += 2;
            Level.Particles.Emit(P_Smash, num, position, positionRange, direction);
        }
    }
}
