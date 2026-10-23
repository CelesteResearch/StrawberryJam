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
        public override void Update()
        {
            if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(0.05f, engine))
            {
                level.Particles.Emit(particleGlow, 1, Center + Vector2.UnitY * -9f, new Vector2(10f, 4f));
            }

            float targetAngle = 0;
            if (hold.IsHeld)
            {
                if (hold.Holder.OnGround())
                {
                    targetAngle = ScalarMath.ClampedMap(hold.Holder.Speed.X, -300f, 300f, 0.6981317f, -0.6981317f);
                }
                else
                {
                    targetAngle = ScalarMath.ClampedMap(hold.Holder.Speed.X, -300f, 300f, 1.0471976f, -1.0471976f);
                }
            }

            sprite.Rotation = Approach(sprite.Rotation, targetAngle, (float)Math.PI * engine.DeltaTime);
            if (hold.IsHeld && !hold.Holder.OnGround() && (sprite.CurrentAnimationID.Equals("fall") ||
                                                           sprite.CurrentAnimationID.Equals("fallLoop")))
            {
                if (!risingSFX.Playing)
                {
                    PlaySound("event:/new_content/game/10_farewell/glider_engage", Position);
                    risingSFX.Play("event:/new_content/game/10_farewell/glider_movement", null, 0);
                }

                Vector2 jellySpeed = hold.Holder.Speed;
                Vector2 vector = new Vector2(jellySpeed.X * 0.5f,
                    (jellySpeed.Y > 0f) ? (jellySpeed.Y * 2) : jellySpeed.Y);
                float value = ScalarMath.Map(((ISourceVectorEnvironment)engine).VectorArithmetic.Length(vector), 0, 120,
                    0, 0.7f);
                risingSFX.Param("glider_speed", value);
            }
            else
            {
                risingSFX.Stop(true);
            }

            base.Update();
            if (!destroyed)
            {
                foreach (SeekerBarrierState seekerBarrier in ((ISourceScene)Scene).SourceScene.Entities
                         .GetTrackedEntities("Celeste.SeekerBarrier"))
                {
                    seekerBarrier.Collidable = true;
                    bool collision = SourceEntityCollision.Check(this, seekerBarrier);
                    seekerBarrier.Collidable = false;
                    if (collision)
                    {
                        destroyed = true;
                        Collidable = false;
                        if (hold.IsHeld)
                        {
                            Vector2 newSpeed = hold.Holder.Speed;
                            hold.Holder.Drop();
                            speed = newSpeed * 1f / 3;
                            Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                        }

                        Add(new SimulationCoroutine(engine, DestroyAnimationCoroutine()));
                        return;
                    }
                }

                if (hold.IsHeld)
                {
                    prevLiftSpeed = Vector2.Zero;
                }
                else if (!bubble)
                {
                    if (highFrictionTimer > 0f)
                        highFrictionTimer -= engine.DeltaTime;
                    if (OnGround(-1))
                    {
                        float correction = 0;
                        if (!OnGround(Position + Vector2.UnitX * 3f, -1))
                        {
                            correction = 20;
                        }
                        else if (!OnGround(Position - Vector2.UnitX * 3f, -1))
                        {
                            correction = -20;
                        }

                        speed.X = Approach(speed.X, correction, 800f * engine.DeltaTime);
                        Vector2 liftspeed = LiftSpeed;
                        if (liftspeed == Vector2.Zero && prevLiftSpeed != Vector2.Zero)
                        {
                            speed = liftspeed;
                            prevLiftSpeed = Vector2.Zero;
                            speed.Y = Math.Min(speed.Y * 0.6f, 0);
                            if (speed.X != 0 && speed.Y == 0)
                            {
                                speed.Y = -60;
                            }

                            if (speed.Y < 0)
                            {
                                noGravityTimer = 0.15f;
                            }
                        }
                        else
                        {
                            prevLiftSpeed = liftspeed;
                            if (liftspeed.Y < 0 && speed.Y < 0)
                            {
                                speed.Y = 0;
                            }
                        }
                    }
                    else if (hold.ShouldHaveGravity)
                    {
                        float num = 200f;
                        if (speed.Y <= 30f)
                            num *= 0.5f;
                        float xAxisFriction = (speed.Y < 0 || highFrictionTimer <= 0) ? 40f : 10f;
                        speed.X = Approach(speed.X, 0f, xAxisFriction * engine.DeltaTime);
                        if (noGravityTimer > 0)
                        {
                            noGravityTimer -= engine.DeltaTime;
                        }
                        else if (level.Wind.Y > 0f)
                        {
                            speed.Y = Approach(speed.Y, 0f, num * engine.DeltaTime);
                        }
                        else
                        {
                            speed.Y = Approach(speed.Y, gravity, num * engine.DeltaTime);
                        }
                    }

                    MoveH(speed.X * engine.DeltaTime, onCollideH, null);
                    MoveV(speed.Y * engine.DeltaTime, onCollideV, null);
                    if (Left < level.Bounds.Left)
                    {
                        Left = level.Bounds.Left;
                        onCollideH(new PixelCollision(-Vector2.UnitX, Vector2.Zero, Vector2.Zero));
                    }
                    else if (Right > level.Bounds.Right)
                    {
                        Right = level.Bounds.Right;
                        onCollideH(new PixelCollision(Vector2.UnitX, Vector2.Zero, Vector2.Zero));
                    }

                    if (Bottom < level.Bounds.Top - 32)
                    {
                        RemoveSelf();
                        return;
                    }

                    hold.CheckAgainstColliders();
                }
                else
                {
                    Position = startPosition + Vector2.UnitY * platformSine.Value * 1;
                }

                Vector2 one = Vector2.One;
                if (!hold.IsHeld)
                {
                    if (level.Wind.Y < 0f)
                    {
                        PlayOpen();
                    }
                    else
                    {
                        sprite.Play("idle", false, false);
                    }
                }
                else if (hold.Holder.Speed.Y < -20f || level.Wind.Y < 0f)
                {
                    if (((ISourceScene)level).SourceScene.Clock.OnInterval(0.04f, engine))
                    {
                        if (level.Wind.Y > 0)
                        {
                            level.ParticlesBG.Emit(particleGlideDown, 1, Position - Vector2.UnitY * 20f,
                                new Vector2(6f, 4f));
                        }
                        else
                        {
                            level.ParticlesBG.Emit(particleGlide, 1, Position - Vector2.UnitY * 10f,
                                new Vector2(6f, 4f));
                        }
                    }

                    PlayOpen();
                    if (((ISourceGameInputEnvironment)engine).Input.GliderMoveY.Value > 0)
                    {
                        one.X = 0.7f;
                        one.Y = 1.4f;
                    }
                    else if (((ISourceGameInputEnvironment)engine).Input.GliderMoveY.Value < 0)
                    {
                        one.X = 1.2f;
                        one.Y = 0.8f;
                    }

                    Rumble(RumbleStrength.Climb, RumbleLength.Short);
                }
                else
                {
                    sprite.Play("held", false, false);
                }

                sprite.Scale.Y = Approach(sprite.Scale.Y, one.Y, engine.DeltaTime * 2f);
                sprite.Scale.X = Approach(sprite.Scale.X, Math.Sign(sprite.Scale.X) * one.X, engine.DeltaTime * 2f);
                return;
            }

            Position += speed * engine.DeltaTime;
        }
    }
}
