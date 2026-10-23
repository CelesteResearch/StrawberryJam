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
        private void OnCollideH(CollisionData data)
        {
            if (data.Hit is DashSwitch)
            {
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
            }

            if (data.Hit is SwitchCrateHolder)
            {
                SwitchCrateHolder batterySwitch = data.Hit as SwitchCrateHolder;
                batterySwitch.Hit(this, Vector2.UnitX * Math.Sign(Speed.X));
            }

            if (Math.Abs(Speed.X) > 100f)
            {
                ImpactParticles(data.Direction);
            }

            Speed.X *= -0.4f;
        }

        private void OnCollideV(CollisionData data)
        {
            if (data.Hit is DashSwitch)
            {
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitY * Math.Sign(Speed.Y));
            }

            if (data.Hit is SwitchCrateHolder)
            {
                SwitchCrateHolder batterySwitch = data.Hit as SwitchCrateHolder;
                batterySwitch.Hit(this, Vector2.UnitY * (float)Math.Sign(Speed.Y));
            }

            if (Speed.Y > 160f)
            {
                ImpactParticles(data.Direction);
            }

            if (Speed.Y > 40f && !(data.Hit is SwapBlock) && !(data.Hit is DashSwitch))
            {
                Speed.Y *= -0.25f;
            }
            else
            {
                Speed.Y = 0f;
            }
        }

        private bool OnFloorBooster(Vector2 at) =>
            SourceTrackedEntityCollision.CheckAt(((Level)Scene).Scheduler.Entities, floorBoosterRegistryKey, this, at);

        public bool OnSolidTile(int downCheck = 1)
        {
            if (OnFloorBooster(Position + Vector2.UnitY * downCheck))
            {
                return false;
            }

            if (SourceTrackedEntityCollision.FirstOutsideAt<object, SwitchCrate>(((Level)Scene).Scheduler.Entities,
                    "Celeste.JumpThru", this, Position + Vector2.UnitY * downCheck) != null)
            {
                if (DepleteOnJumpThru)
                {
                    return true;
                }

                return false;
            }

            if (!CollideCheck("Celeste.SolidTiles", Position + Vector2.UnitY * downCheck))
            {
                return false;
            }


            return true;
        }
    }
}
