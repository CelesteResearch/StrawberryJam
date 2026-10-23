using System.Collections.Generic;
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

namespace Celeste.Mods.StrawberryJam.Entities.Switches
{
    [CustomEntity("SJ2021/SwitchCrateHolder")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.SwitchCrateHolder")]
    public class SwitchCrateHolder : SourceSolidEntityState
    {
        public enum Sides
        {
            Up,
            Down,
            Left,
            Right
        }

        private ParticleType P_PressA => ((IOriginalGameplayProcess)engine).ParticleTypes.DashSwitch.P_PressA;

        private ParticleType P_PressB => ((IOriginalGameplayProcess)engine).ParticleTypes.DashSwitch.P_PressB;

        private readonly ISourceEngineContext engine;
        private readonly string gateRegistryKey;
        private readonly string playerRegistryKey;

        private Sides side;

        private bool pressed;

        private Vector2 pressDirection;

        private bool persistent;

        private EntityID id;

        private bool allGates;

        private Sprite sprite;

        private bool alwaysFlag;

        private string FlagName;

        public SwitchCrateHolder(Vector2 position, Sides side, bool persistent, bool allGates, bool alwaysFlag,
            EntityID id, ISourceSolidWorld world)
            : base(position, 0f, 0f, true, world)
        {
            engine = world.EngineContext;
            playerRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(Player)).RegistryKey;
            gateRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(BatteryGate)).Name;
            FlagName = "batterySwitch_" + id.Key;
            this.side = side;
            this.persistent = persistent;
            this.allGates = allGates;
            this.id = id;
            this.alwaysFlag = alwaysFlag;
            Add(sprite = ((Celeste.Everest.Modules.Runtime.ISourceEverestModulesEnvironment)engine).Modules
                .OfType<StrawberryJamModule>().Single().SpriteBank.Create("switchCrateHolder"));
            if (side == Sides.Up || side == Sides.Down)
            {
                base.Collider.Width = 16f;
                base.Collider.Height = 8f;
            }
            else
            {
                base.Collider.Width = 8f;
                base.Collider.Height = 16f;
            }

            switch (side)
            {
                case Sides.Up:
                    sprite.Position = new Vector2(-2f, -2f);
                    sprite.Rotation = (float)Math.PI / 2f;
                    pressDirection = Vector2.UnitY;
                    break;
                case Sides.Down:
                    sprite.Position = new Vector2(18f, 10f);
                    sprite.Rotation = -(float)Math.PI / 2f;
                    pressDirection = -Vector2.UnitY;
                    break;
                case Sides.Right:
                    sprite.Position = new Vector2(10f, 18f);
                    sprite.FlipX = true;
                    pressDirection = -Vector2.UnitX;
                    break;
                case Sides.Left:
                    sprite.Position = new Vector2(-2f, 18f);
                    sprite.FlipX = false;
                    pressDirection = Vector2.UnitX;
                    break;
            }
        }

        public SwitchCrateHolder(EntityData data, Vector2 offset, EntityID id, ISourceSolidWorldFactory factory)
            : this(data.Position + offset, data.Enum<Sides>("direction"), data.Bool("persistent"),
                data.Bool("allGates"), data.Bool("alwaysFlag"), id, factory.Create(data.Position + offset, true))
        {
        }

        public static void SetupParticles(ISourceEngineContext engine)
        {
            var signal = ((IOriginalGameplayProcess)engine).ParticleTypes.DashSwitch.P_PressAMirror;
            signal.Color = Color.Aqua;
            signal.ColorMode = ParticleType.ColorModes.Choose;
            signal.LifeMax = 0.6f;
            signal.LifeMin = 0.3f;
            signal.SpeedMultiplier = 0.1f;
            signal.DirectionRange = (float)Math.PI / 5f;
        }

        public static SwitchCrateHolder Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) =>
            new SwitchCrateHolder(data, offset, new MapEntityId(room.Name, data.ID),
                ((ISourcePlatformCollisionScene)level).SolidWorldFactory);

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (!persistent || !((Level)Scene).Session.GetFlag(FlagName))
            {
                return;
            }

            sprite.Play("buzz");
            pressed = true;
            Collidable = true;
            LitState();
            foreach (BatteryGate entity in GetGate())
            {
                if (entity.closes)
                {
                    entity.StartClosed();
                }
                else
                {
                    entity.StartOpen();
                }
            }
        }

        public override void Update()
        {
            base.Update();
            Level level = ((Level)Scene);
            if (!((Level)Scene).OnInterval(0.3f))
            {
                return;
            }

            if (pressed)
            {
                level.ParticlesFG.Emit(P_PressA, 1, base.Collider.AbsolutePosition + base.Collider.Center,
                    Vector2Geometry.Perpendicular(pressDirection) * 6f, Vector2Geometry.Angle(pressDirection * -1f));
                return;
            }
        }

        private void LitState()
        {
            switch (side)
            {
                case Sides.Up:
                    base.Collider.Width = 16f;
                    base.Collider.Height = 12f;
                    base.Collider.Position.Y -= 4f;
                    break;
                case Sides.Down:
                    base.Collider.Width = 16f;
                    base.Collider.Height = 12f;
                    break;
                case Sides.Left:
                    base.Collider.Width = 12f;
                    base.Collider.Height = 16f;
                    base.Collider.Position.X -= 4f;
                    break;
                case Sides.Right:
                    base.Collider.Width = 12f;
                    base.Collider.Height = 16f;
                    break;
            }

            Vector2 offset = Vector2.Zero;
            switch (side)
            {
                case Sides.Down:
                    offset = base.Collider.BottomCenter + new Vector2(0f, 1f);
                    break;
                case Sides.Up:
                    offset = base.Collider.TopCenter + new Vector2(0f, -1f);
                    break;
                case Sides.Right:
                    offset = base.Collider.CenterRight + new Vector2(1f, 0f);
                    break;
                case Sides.Left:
                    offset = base.Collider.CenterLeft + new Vector2(-1f, 0f);
                    break;
            }

            Add(new VertexLight(engine, offset, Color.Aqua, 1f, 12, 24));
        }

        public void Hit(SwitchCrate Crate, Vector2 direction)
        {
            if (pressed)
            {
                return;
            }

            Crate.Use();
            Crate.Collidable = false;
            Add(new SimulationCoroutine(engine, InsertRoutine(Crate)));
            LitState();
            pressed = true;
            Player player = ((Level)Scene).Scheduler.Entities.GetTrackedEntity<Player>(playerRegistryKey);
            if (player != null && base.Collider.Collide(player.Collider))
            {
                player.Position += -5f * pressDirection;
            }

            ((Level)Scene).ParticlesFG.Emit(P_PressA, 10, base.Collider.AbsolutePosition + base.Collider.Center,
                Vector2Geometry.Perpendicular(direction) * 6f, Vector2Geometry.Angle(pressDirection * -1f));
            ((Level)Scene).ParticlesFG.Emit(P_PressB, 4, base.Collider.AbsolutePosition + base.Collider.Center,
                Vector2Geometry.Perpendicular(direction) * 6f, Vector2Geometry.Angle(pressDirection * -1f));
            if (allGates)
            {
                foreach (BatteryGate entity in ((Level)Scene).Scheduler.Entities.GetTrackedEntities(gateRegistryKey))
                {
                    if (entity.entityID.Level == id.Level)
                    {
                        entity.SwitchOpen();
                    }
                }
            }
            else
            {
                foreach (BatteryGate gate in GetGate())
                {
                    gate.SwitchOpen();
                }
            }

            if (persistent || alwaysFlag)
            {
                ((Level)Scene).Session.SetFlag(FlagName);
            }
        }

        private IEnumerator InsertRoutine(SwitchCrate crate)
        {
            ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Medium,
                RumbleLength.Medium);
            ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/game/05_mirror_temple/button_activate",
                Position);
            Vector2 target = Center;
            switch (side)
            {
                case Sides.Down:
                    target = new Vector2(Center.X, Bottom + crate.Height / 2f);
                    break;
                case Sides.Up:
                    target = new Vector2(Center.X, Top - crate.Height / 2f);
                    break;
                case Sides.Right:
                    target = new Vector2(Right + crate.Width / 2f, Center.Y);
                    break;
                case Sides.Left:
                    target = new Vector2(Left - crate.Width / 2f, Center.Y);
                    break;
            }

            crate.Depth = Depth - 1;

            while (crate.Position != target)
            {
                crate.Position =
                    ((ISourceVectorEnvironment)engine).VectorArithmetic.Approach(crate.Position, target, 1f);
                yield return null;
            }

            crate.RemoveSelf();
            sprite.Play("insert");
            yield break;
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            if (!persistent && alwaysFlag)
            {
                ((Level)scene).Session.SetFlag(FlagName, false);
            }
        }

        private List<BatteryGate> GetGate()
        {
            List<object> entities = ((Level)Scene).Scheduler.Entities.GetTrackedEntities(gateRegistryKey);
            List<BatteryGate> batteryGates = new List<BatteryGate>();
            float num = 0f;
            foreach (BatteryGate item2 in entities)
            {
                if (!item2.ClaimedByASwitch && item2.bound && item2.openWith == id.ID)
                {
                    batteryGates.Add(item2);
                }
            }

            if (batteryGates.Count != 0)
            {
                return batteryGates;
            }

            BatteryGate batteryGate = null;
            foreach (BatteryGate item in entities)
            {
                if (!item.ClaimedByASwitch && item.entityID.Level == id.Level && !item.bound)
                {
                    float num2 = Vector2.DistanceSquared(Position, item.Position);
                    if (batteryGate == null || num2 < num)
                    {
                        batteryGate = item;
                        num = num2;
                    }
                }
            }

            if (batteryGate != null)
            {
                batteryGate.ClaimedByASwitch = true;
                batteryGates.Add(batteryGate);
            }

            return batteryGates;
        }
    }
}
