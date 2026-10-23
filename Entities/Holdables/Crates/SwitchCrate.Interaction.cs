using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Maps;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Audio;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Controls.Source;
using Celeste.Game.Gameplay.Effects.Slashes;
using Celeste.Game.Gameplay.Mechanisms.DashSwitches;
using Celeste.Game.Gameplay.Enemies.Seekers;
using Celeste.Game.Entities.Platforms.SwapBlocks;
using Celeste.Graphics.Animation;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Interaction.Springs;
using Celeste.Simulation.Gameplay.Walls.Concealment;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.Effects.Mirrors;
using Celeste.Simulation.Presentation.Player.Death;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Mods.Batteries.Modules;
using Celeste.Mods.Batteries.Runtime.Power;
using Celeste.Mods.Batteries.Components.Power;
using Celeste.Mods.Batteries.Entities.Gates;
using Color = Celeste.Core.Display.PackedColor;
using ParticleType = Celeste.Simulation.Presentation.Particles.Definitions.ParticleTypePresentation;
using Holdable = Celeste.Simulation.Gameplay.Holdables.HoldableState;
using HoldableCollider = Celeste.Simulation.Gameplay.Holdables.HoldableColliderState;
using Collision = Celeste.Physics.Collision.SourceCollisionHandler;
using CollisionData = Celeste.Physics.Collision.PixelCollision;
using EntityID = Celeste.Core.Maps.MapEntityId;
using VertexLight = Celeste.Simulation.Presentation.Lighting.VertexLightState;
using MirrorReflection = Celeste.Simulation.Presentation.Effects.Mirrors.MirrorReflectionState;
using Entity = Celeste.Scene.Entities.SceneEntityState;
using Player = Celeste.Simulation.RoomPlayerSession;
using Level = Celeste.Game.Execution.Levels.OriginalLevelSceneRoot;
using Hitbox = Celeste.Physics.Collision.Shapes.HitboxColliderGeometry;
using Seeker = Celeste.Game.Gameplay.Enemies.Seekers.OriginalSeekerState;
using SeekerBarrier = Celeste.Simulation.Gameplay.Enemies.Seeker.SeekerBarrierState;
using DashSwitch = Celeste.Game.Gameplay.Mechanisms.DashSwitches.OriginalDashSwitchState;
using SwapBlock = Celeste.Game.Entities.Platforms.SwapBlocks.OriginalSwapBlockState;
using Solid = Celeste.Physics.Collision.ISourceEntityCollider;
using System.Linq;
using System.Collections;
using Celeste.Core.Components.Timing;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Game.Gameplay.Doors.TempleGates;
using Celeste.Graphics.Content.Atlas;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Mods.StrawberryJam.Entities.Switches;
using Celeste.Mods.StrawberryJam.Entities.Holdables.Crates;
using Celeste.Mods.StrawberryJam.Runtime.Holdables.Crates;

namespace Celeste.Mods.StrawberryJam.Entities.Holdables.Crates
{
    public partial class SwitchCrate
    {
        private void OnPickup()
        {
            Speed = Vector2.Zero;
            AddTag(((IOriginalGameTagsEnvironment)engine).Tags.Persistent.Value);
            IsHeld = true;
            if (!HasStarted)
            {
                HasStarted = true;
                sprite.Play("idle");
            }
        }

        private void OnRelease(Vector2 force)
        {
            RemoveTag(((IOriginalGameTagsEnvironment)engine).Tags.Persistent.Value);
            if (force.X != 0f && force.Y == 0f)
            {
                force.Y = -0.4f;
            }

            Speed = force * 200f;
            if (Speed != Vector2.Zero)
            {
                noGravityTimer = 0.1f;
            }

            IsHeld = false;
        }

        public void HitSeeker(Seeker seeker)
        {
            if (!Hold.IsHeld)
            {
                Speed = ((ISourceVectorEnvironment)engine).VectorArithmetic.SafeNormalize(Center - seeker.Center, 120f);
            }
        }

        public void HitSpinner(Entity spinner)
        {
            if (!Hold.IsHeld && ((ISourceVectorEnvironment)engine).VectorArithmetic.Length(Speed) < 0.01f &&
                ((ISourceVectorEnvironment)engine).VectorArithmetic.Length(LiftSpeed) < 0.01f &&
                ((ISourceVectorEnvironment)engine).VectorArithmetic.Length(previousPosition - ExactPosition) < 0.01f &&
                OnGround())
            {
                int num = Math.Sign(X - spinner.X);
                if (num == 0)
                {
                    num = 1;
                }

                Speed.X = (float)num * 120f;
                Speed.Y = -30f;
            }
        }

        public bool HitSpring(ISourceSpringEntity spring)
        {
            if (!Hold.IsHeld)
            {
                if (spring.Orientation == SpringOrientation.Floor && Speed.Y >= 0f)
                {
                    Speed.X *= 0.5f;
                    Speed.Y = -160f;
                    noGravityTimer = 0.15f;
                    return true;
                }

                if (spring.Orientation == SpringOrientation.WallLeft && Speed.X <= 0f)
                {
                    MoveTowardsY(spring.CenterY + 5f, 4f);
                    Speed.X = 220f;
                    Speed.Y = -80f;
                    noGravityTimer = 0.1f;
                    return true;
                }

                if (spring.Orientation == SpringOrientation.WallRight && Speed.X >= 0f)
                {
                    MoveTowardsY(spring.CenterY + 5f, 4f);
                    Speed.X = -220f;
                    Speed.Y = -80f;
                    noGravityTimer = 0.1f;
                    return true;
                }
            }

            return false;
        }

        protected override void OnSquish(CollisionData data)
        {
            if (!TrySquishWiggle(data, 3, 3) &&
                !((IOriginalGameplayProcess)engine).SaveContext.CurrentSave.Assists.Invincible)
            {
                Die(forceKillMaddy: true);
            }
        }

        public override bool IsRiding(Solid solid)
        {
            if (Speed.Y == 0f)
            {
                return base.IsRiding(solid);
            }

            return false;
        }

        public void Die(bool forceKillMaddy)
        {
            if (!dead)
            {
                dead = true;
                ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/char/madeline/death", Position);
                Add(new DeathEffect(Color.Gray, engine, base.Center - Position));
                sprite.Visible = false;
                Depth = -1000000;
                AllowPushing = false;

                if (KillMaddyOnExplosion || forceKillMaddy)
                {
                    ((Level)Scene).Scheduler.Entities.GetTrackedEntity<Player>(playerRegistryKey)?.Die(Vector2.Zero);
                }

                RemoveSelf();
            }
        }

        public bool Dangerous(HoldableCollider holdableCollider)
        {
            if (!Hold.IsHeld && Speed != Vector2.Zero)
            {
                return hitSeeker != holdableCollider;
            }

            return false;
        }

        public void Swat(HoldableCollider hc, int dir)
        {
            if (Hold.IsHeld && hitSeeker == null)
            {
                swatTimer = 0.1f;
                hitSeeker = hc;
                Hold.Holder.Swat(dir);
            }
        }

        public void Use()
        {
            ((Level)Scene).Session.SetFlag(FlagName);
        }

        private void MoveOnConveyor(float amount)
        {
            float accY = 800f;
            if (Math.Abs(Speed.Y) <= 30f)
            {
                accY *= 0.5f;
            }

            Speed.Y = ScalarMath.Approach(Speed.Y, 300f, accY * engine.DeltaTime);
            Speed.X = ScalarMath.Approach(Speed.X, amount, 200f * engine.DeltaTime);
            MoveH((Speed.X + 36.3f) * engine.DeltaTime, OnCollideH);
            MoveV(Speed.Y * engine.DeltaTime, OnCollideV);
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
                position = CenterRight - Vector2.UnitX * 12f;
                positionRange = Vector2.UnitY * (Height - 6f) * 0.5f;
                num = (int)(Height / 8f) * 4;
            }
            else if (dir == -Vector2.UnitX)
            {
                direction = (float)Math.PI;
                position = CenterLeft + Vector2.UnitX * 12f;
                positionRange = Vector2.UnitY * (Height - 6f) * 0.5f;
                num = (int)(Height / 8f) * 4;
            }
            else if (dir == Vector2.UnitY)
            {
                direction = (float)Math.PI / 2f;
                position = BottomCenter - Vector2.UnitY * 12f;
                positionRange = Vector2.UnitX * (Width - 6f) * 0.5f;
                num = (int)(Width / 8f) * 4;
            }
            else
            {
                direction = -(float)Math.PI / 2f;
                position = TopCenter + Vector2.UnitY * 12f;
                positionRange = Vector2.UnitX * (Width - 6f) * 0.5f;
                num = (int)(Width / 8f) * 4;
            }

            num += 2;
            ((Level)Scene).Particles.Emit(P_Smash, num, position, positionRange, direction);
        }

        private void ImpactParticles(Vector2 dir)
        {
            float direction;
            Vector2 position;
            Vector2 positionRange;
            if (dir.X > 0f)
            {
                direction = (float)Math.PI;
                position = new Vector2(Right, Y - 4f);
                positionRange = Vector2.UnitY * 6f;
            }
            else if (dir.X < 0f)
            {
                direction = 0f;
                position = new Vector2(Left, Y - 4f);
                positionRange = Vector2.UnitY * 6f;
            }
            else if (dir.Y > 0f)
            {
                direction = -(float)Math.PI / 2f;
                position = new Vector2(X, Bottom);
                positionRange = Vector2.UnitX * 6f;
            }
            else
            {
                direction = (float)Math.PI / 2f;
                position = new Vector2(X, Top);
                positionRange = Vector2.UnitX * 6f;
            }

            Level.Particles.Emit(P_Impact, 12, position, positionRange, direction);
        }
    }
}
