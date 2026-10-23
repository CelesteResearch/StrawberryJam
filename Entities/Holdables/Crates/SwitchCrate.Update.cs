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
        public override void Added(object scene)
        {
            base.Added(scene);
            Level = ((Level)Scene);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            player = ((Level)Scene).Scheduler.Entities.GetTrackedEntity<Player>(playerRegistryKey);
        }

        void Restartanim()
        {
            sprite.Play("idle", true);
        }

        public override void Update()
        {
            base.Update();
            if (!IsMounted)
            {
                if (swatTimer > 0f)
                {
                    swatTimer -= engine.DeltaTime;
                }

                if (Hold.IsHeld)
                {
                    prevLiftSpeed = Vector2.Zero;
                }
                else
                {
                    if (OnGround())
                    {
                        if (!OnSolidTile() || !HasStarted)
                        {
                            Restartanim();
                        }

                        CarryableActorMotionSteps.AdvanceGroundSpeed(this, engine, ref Speed, ref prevLiftSpeed,
                            ref noGravityTimer);
                    }
                    else
                    {
                        if (IsFirstFrame && !HasStarted)
                        {
                            HasStarted = true;
                        }

                        Restartanim();
                        if (Hold.ShouldHaveGravity)
                        {
                            CarryableActorMotionSteps.AdvanceAirSpeed(engine, ref Speed, ref noGravityTimer, 300f);
                        }
                    }

                    previousPosition = ExactPosition;
                    MoveH(Speed.X * engine.DeltaTime, onCollideH);
                    MoveV(Speed.Y * engine.DeltaTime, onCollideV);
                    if (base.Center.X > (float)Level.Bounds.Right)
                    {
                        MoveH(32f * engine.DeltaTime);
                        if (Left - 8f > (float)Level.Bounds.Right)
                        {
                            RemoveSelf();
                        }
                    }
                    else if (Left < (float)Level.Bounds.Left)
                    {
                        Left = Level.Bounds.Left;
                        Speed.X *= -0.4f;
                    }
                    else if (Top < (float)(Level.Bounds.Top - 4))
                    {
                        Top = Level.Bounds.Top + 4;
                        Speed.Y = 0f;
                    }
                    else if (Bottom > (float)Level.Bounds.Bottom &&
                             ((IOriginalGameplayProcess)engine).SaveContext.CurrentSave.Assists.Invincible)
                    {
                        Bottom = Level.Bounds.Bottom;
                        Speed.Y = -300f;
                        ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                            "event:/game/general/assist_screenbottom", Position);
                    }
                    else if (Top > (float)Level.Bounds.Bottom)
                    {
                        Die(forceKillMaddy: true);
                    }

                    if (X < (float)(Level.Bounds.Left + 10))
                    {
                        MoveH(32f * engine.DeltaTime);
                    }

                    OriginalTempleGateState templeGate = CollideFirst<OriginalTempleGateState>("Celeste.TempleGate");
                    if (templeGate != null && player != null)
                    {
                        templeGate.Collidable = false;
                        MoveH((float)(Math.Sign(player.X - X) * 32) * engine.DeltaTime);
                        templeGate.Collidable = true;
                    }
                }

                if (!dead)
                {
                    Hold.CheckAgainstColliders();
                }

                if (hitSeeker != null && swatTimer <= 0f && !hitSeeker.Check(Hold))
                {
                    hitSeeker = null;
                }
            }

            if (IsHeld)
            {
                Restartanim();
            }

            IsFirstFrame = false;
        }
    }
}
