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
    [CustomEntity("SJ2021/SwitchCrate")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.SwitchCrate")]
    public partial class SwitchCrate : SourceActorEntityState
    {
        private ParticleType P_Smash => ((IOriginalGameplayProcess)engine).ParticleTypes.LightningBreakerBox.P_Smash;

        private ParticleType P_Impact => ((IOriginalGameplayProcess)engine).ParticleTypes.TheoCrystal.P_Impact;
        private readonly ISourceEngineContext engine;
        private readonly string floorBoosterRegistryKey;
        private readonly string playerRegistryKey;

        public Vector2 Speed;

        public Holdable Hold;

        private bool dead;

        private Level Level;

        private Collision onCollideH;

        private Collision onCollideV;

        private float noGravityTimer;

        private Vector2 prevLiftSpeed;

        private Vector2 previousPosition;

        private HoldableCollider hitSeeker;

        private float swatTimer;

        float TimeToExplode;

        bool IsMounted = false;

        private EntityID id;

        private Player player;

        private string FlagName;


        Sprite sprite;

        bool IsHeld = false;

        bool DepleteOnJumpThru = false;

        private bool KillMaddyOnExplosion;

        bool IsFirstFrame = true;

        bool HasStarted = false;

        public SwitchCrate(EntityData data, Vector2 offset, EntityID id, ISourceEngineContext engine)
            : base(new PixelActor(null, data.Position + offset), engine,
                ((ISourcePlatformTargetMotionEnvironment)engine).TargetMotionPolicy)
        {
            this.engine = engine;
            playerRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(Player)).RegistryKey;
            floorBoosterRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog
                .Find(typeof(Celeste.Mods.VortexHelper.Entities.Boosters.FloorBooster)).Name;
            var conveyorMover = new Celeste.Mods.FactoryHelper.Components.Conveyors.ConveyorMover();
            Add(conveyorMover);
            conveyorMover.OnMove = MoveOnConveyor;

            TimeToExplode = data.Float("TimeToExplode", 1);
            DepleteOnJumpThru = data.Bool("DepleteOnJumpThru");
            KillMaddyOnExplosion = data.Bool("KillMaddyOnExplosion", defaultValue: true);

            FlagName = "battery_" + data.ID;
            this.id = id;
            previousPosition = data.Position + offset;
            Depth = 100;
            Collider = new Hitbox(8f, 11f, -4f, -2f);

            sprite = new Sprite(engine,
                ((Celeste.Graphics.Content.ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas,
                "objects/StrawberryJam2021/SwitchCrate/");
            float t = 1f / 6f;
            sprite.Add("idle", "idle", t);
            sprite.Rate = 1 / TimeToExplode;
            Add(sprite);
            sprite.Play("idle", true);

            sprite.CenterOrigin();
            Add(Hold = new Holdable(engine, 0.1f));
            Hold.PickupCollider = new Hitbox(20f, 19f, -10f, -10f);
            Hold.SlowFall = false;
            Hold.SlowRun = true;
            Hold.OnPickup = OnPickup;
            Hold.OnRelease = OnRelease;
            Hold.DangerousCheck = value => Dangerous((HoldableCollider)value);
            Hold.OnHitSeeker = value => HitSeeker((Seeker)value);
            Hold.OnSwat = (value, dir) => Swat((HoldableCollider)value, dir);
            Hold.OnHitSpring = value => HitSpring((ISourceSpringEntity)value);
            Hold.OnHitSpinner = value => HitSpinner((Entity)value);
            Hold.SpeedGetter = () => Speed;
            Hold.SpeedSetter = value => Speed = value;
            onCollideH = OnCollideH;
            onCollideV = OnCollideV;
            Hold.OnCarry = pos => Position = pos + new Vector2(0f, -8f);
            LiftSpeedGraceTime = 0.1f;
            Add(new VertexLight(engine, Collider.Center, Color.White, 1f, 32, 64));
            Add(new MirrorReflection());

            sprite.OnFinish = delegate { Die(forceKillMaddy: false); };
        }

        public static SwitchCrate Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) =>
            new SwitchCrate(data, offset, new MapEntityId(room.Name, data.ID), level.EngineContext);
    }
}
