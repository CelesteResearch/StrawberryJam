using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Gameplay.Platforms.Cassette;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Platforms.Cassette;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content.Atlas;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Mods.StrawberryJam.Components.Cassette;
using Celeste.Mods.StrawberryJam.Entities.Hazards.Lasers;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Mods.StrawberryJam.Presentation.Paintbrushes;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Gameplay.Collision.Blockers;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;

namespace Celeste.Mods.StrawberryJam.Entities.Hazards.Paintbrushes
{
    [CustomEntity("SJ2021/PaintbrushUp = LoadUp",
        "SJ2021/PaintbrushDown = LoadDown",
        "SJ2021/PaintbrushLeft = LoadLeft",
        "SJ2021/PaintbrushRight = LoadRight")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.Paintbrush")]
    public partial class Paintbrush : OrientableEntity
    {
        #region Static Loader Methods

        public static SceneEntityState LoadUp(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, EntityData data) =>
            new Paintbrush(level.EngineContext, data, offset, Orientations.Up);

        public static SceneEntityState LoadDown(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, EntityData data) =>
            new Paintbrush(level.EngineContext, data, offset, Orientations.Down);

        public static SceneEntityState LoadLeft(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, EntityData data) =>
            new Paintbrush(level.EngineContext, data, offset, Orientations.Left);

        public static SceneEntityState LoadRight(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, EntityData data) =>
            new Paintbrush(level.EngineContext, data, offset, Orientations.Right);

        #endregion

        #region Ahorn Properties

        public bool CollideWithSolids { get; }
        public bool KillPlayer { get; }
        public int CassetteIndex { get; }
        public bool HalfLength { get; }

        protected int DataWidth { get; }
        protected int DataHeight { get; }

        protected int Size => Orientation.Vertical() ? DataWidth : DataHeight;
        protected int Tiles => Size / tileSize;

        #endregion

        private string animationPrefix => CassetteIndex == 0 ? "blue" : "pink";
        private readonly string chargingAnimation;
        private readonly string firingAnimation;
        private readonly string burstAnimation;
        private readonly string idleAnimation;
        private readonly string cooldownAnimation;
        private Vector2 beamOffset => Orientation.Normal() * beamOffsetMultiplier;
        private PackedColor telegraphColor => CassetteListener.ColorFromCassetteIndex(CassetteIndex);

        private PackedColor beamFillColor =>
            CassetteIndex == 0 ? new PackedColor(115, 239, 232) : new PackedColor(255, 142, 174);

        private readonly string laserFireSound;

        private readonly Sprite largeBrushSprite;
        private readonly Sprite smallBrushSprite;
        private readonly Sprite beamSprite;
        private readonly Sprite paintParticlesSprite;
        private readonly Sprite paintBackSprite;
        private readonly SoundSourceComponentState rampUpSource;
        private readonly SoundSourceComponentState fireSource;
        private readonly int[] smallBrushFrames;

        private readonly ColliderGeometry[] brushHitboxes;
        private readonly HitboxColliderGeometry[] beamHitboxes;
        private readonly ColliderListGeometry inactiveColliderList;
        private readonly ColliderListGeometry activeColliderList;
        private readonly CassetteListener cassetteListener;
        private LaserState laserState;

        private static readonly object impactParticleInterval = 0.1f;

        private const float chargeDelayFraction = 0.25f;
        private const float collisionDelaySeconds = 5f / 60f;
        private const float burstTimeSeconds = 0.2f;
        private const float fireSoundDelaySeconds = 10f / 60f;
        private const int beamOffsetMultiplier = 4;
        private const int beamThickness = 12;
        private const float mediumRumbleEffectRange = 8f * 12;
        private const float strongRumbleEffectRange = 8f * 8;
        private const int tileSize = 8;

        private float collisionDelayRemaining;
        private float chargeDelayRemaining;
        private float burstTimeRemaining;
        private float fireSoundDelayRemaining;

        private readonly ISourceEngineContext engine;
        private readonly IRumbleSession rumble;
        private readonly PaintbrushParticles particles;
        private readonly ReusableAtlasSlice backTop = new();
        private readonly ReusableAtlasSlice backBottom = new();
        private readonly ReusableAtlasSlice beamSlice = new();

        private void setAnimationSpeed(string key, float totalRunTime)
        {
            if (largeBrushSprite.Animations.TryGetValue(key, out var emitterAnimation))
                emitterAnimation.Delay = totalRunTime / emitterAnimation.Frames.Length;
            if (paintParticlesSprite.Animations.TryGetValue(key, out var paintAnimation))
                paintAnimation.Delay = totalRunTime / paintAnimation.Frames.Length;
        }

        public LaserState State
        {
            get => laserState;
            set => setState(value);
        }

        private void setState(LaserState state, bool force = false)
        {
            if (!force && laserState == state) return;
            laserState = state;

            switch (State)
            {
                case LaserState.Idle:
                    largeBrushSprite.Play(idleAnimation);
                    Collider = inactiveColliderList;
                    break;

                case LaserState.Precharge:
                    largeBrushSprite.Play(idleAnimation);
                    Collider = inactiveColliderList;
                    PlayIfInBounds(rampUpSource, "event:/sj21_mosscairn_sfx/paintbrush_laser_ramp_up");
                    break;

                case LaserState.Charging:
                    largeBrushSprite.Play(chargingAnimation);
                    paintParticlesSprite.Play(chargingAnimation);
                    Collider = inactiveColliderList;
                    break;

                case LaserState.Burst:
                    largeBrushSprite.Play(burstAnimation);
                    paintParticlesSprite.Play(burstAnimation);
                    beamSprite.Play(burstAnimation);
                    Collider = inactiveColliderList;
                    collisionDelayRemaining = collisionDelaySeconds;
                    burstTimeRemaining = burstTimeSeconds;
                    playNearbyEffects();
                    break;

                case LaserState.Firing:
                    largeBrushSprite.Play(firingAnimation);
                    paintParticlesSprite.Play(firingAnimation);
                    paintBackSprite.Play(firingAnimation);
                    beamSprite.Play(firingAnimation);
                    collisionDelayRemaining = 0;
                    burstTimeRemaining = 0;
                    Collider = activeColliderList;
                    break;

                case LaserState.Cooldown:
                    largeBrushSprite.Play(cooldownAnimation);
                    paintParticlesSprite.Play(cooldownAnimation);
                    paintBackSprite.Play(cooldownAnimation);
                    Collider = inactiveColliderList;
                    emitCooldownParticles();
                    fireSource.Param("end", 1f);
                    break;
            }
        }

        private void playNearbyEffects()
        {
            if (((ISourceScene)Scene).SourceScene.Entities.TryGetTrackedEntities(typeof(RoomPlayerSession),
                    out var players) &&
                players.Count > 0 && players[0] is RoomPlayerSession player)
            {
                float distanceSquared = (player.Position - Position).LengthSquared();
                if (distanceSquared <= strongRumbleEffectRange * strongRumbleEffectRange)
                {
                    ((OriginalLevelSceneRoot)Scene).Shake(0.2f);
                    rumble.Rumble(2, 0);
                }
                else if (distanceSquared <= mediumRumbleEffectRange * mediumRumbleEffectRange)
                {
                    ((OriginalLevelSceneRoot)Scene).Shake(0.1f);
                    rumble.Rumble(1, 0);
                }
                else
                {
                    rumble.Rumble(0, 0);
                }
            }
        }

        public Paintbrush(ISourceEngineContext engine, EntityData data, Vector2 offset, Orientations orientation)
            : base(engine, data, offset, orientation)
        {
            this.engine = engine;
            rumble = ((ISourceRumbleEnvironment)engine).RumbleSession;
            particles = PaintbrushParticles.For(engine);
            var bank = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<StrawberryJamModule>().Single()
                .SpriteBank;
            CollideWithSolids = data.Bool("collideWithSolids", true);
            KillPlayer = data.Bool("killPlayer", true);
            CassetteIndex = data.Int("cassetteIndex", 0);
            HalfLength = data.Bool("halfLength");
            chargingAnimation = $"{animationPrefix}_charging";
            firingAnimation = $"{animationPrefix}_firing";
            burstAnimation = $"{animationPrefix}_burst";
            idleAnimation = $"{animationPrefix}_idle";
            cooldownAnimation = $"{animationPrefix}_cooldown";
            laserFireSound = $"event:/sj21_mosscairn_sfx/paintbrush_laser_{animationPrefix}";
            DataWidth = data.Width;
            DataHeight = data.Height;

            largeBrushSprite = configureSprite(bank.Create("paintbrushLargeBrush"));
            smallBrushSprite = configureSprite(bank.Create("paintbrushSmallBrush"));
            paintParticlesSprite = configureSprite(bank.Create("paintbrushPaintParticles"));
            paintBackSprite = configureSprite(bank.Create("paintbrushPaintBack"));
            beamSprite = configureSprite(bank.Create("paintbrushBeam"));

            largeBrushSprite.Play(idleAnimation);
            smallBrushSprite.Play(idleAnimation);

            beamSprite.Position = beamOffset;

            if (smallBrushSprite.Animations.TryGetValue(idleAnimation, out var smallBrushAnimation))
            {
                var rnd = new Random((int)Position.LengthSquared());
                smallBrushFrames = Enumerable.Range(0, smallBrushAnimation.Frames.Length).ToArray();
                for (int i = 0; i < smallBrushFrames.Length - 1; i++)
                {
                    int swapIndex = rnd.Next(i, smallBrushFrames.Length);
                    if (swapIndex == i) continue;
                    int oldValue = smallBrushFrames[i];
                    smallBrushFrames[i] = smallBrushFrames[swapIndex];
                    smallBrushFrames[swapIndex] = oldValue;
                }
            }

            Add(cassetteListener = new CassetteListener(CassetteIndex)
                {
                    OnActivated = () => { State = LaserState.Burst; },
                    OnDeactivated = () =>
                    {
                        State = State == LaserState.Firing ? LaserState.Cooldown : LaserState.Idle;
                    },
                    OnWillToggle = () =>
                    {
                        if (State == LaserState.Charging)
                        {
                            fireSoundDelayRemaining = fireSoundDelaySeconds;
                        }
                    },
                    OnSilentUpdate = activated =>
                    {
                        cassetteListener.Activated = activated;
                        setState(activated && !HalfLength ? LaserState.Firing : LaserState.Idle, true);
                    },
                    OnTick = (_, isSwap) =>
                    {
                        if (isSwap) return;
                        if (State == LaserState.Firing && HalfLength)
                        {
                            State = LaserState.Cooldown;
                        }
                        else if (State == LaserState.Idle && !cassetteListener.Activated)
                        {
                            var cbm = ((ISourceScene)Scene).SourceScene.Entities
                                .GetTrackedEntity<OriginalCassetteBlockManagerState>("Celeste.CassetteBlockManager");
                            var beatLength = (10 / 60f) / cbm.TempoMultiplier;
                            var tickLength = beatLength * (cbm is InstalledCassetteBlockManagerState installed
                                ? installed.BeatsPerTick
                                : 4);
                            chargeDelayRemaining = chargeDelayFraction * tickLength;
                            setAnimationSpeed(chargingAnimation, tickLength * (1 - chargeDelayFraction));
                            State = LaserState.Precharge;
                        }
                    },
                },
                new PlayerColliderComponentState(onPlayerCollide),
                new LedgeBlockerState(_ => KillPlayer),
                beamSprite,
                paintBackSprite,
                smallBrushSprite,
                largeBrushSprite,
                paintParticlesSprite,
                rampUpSource = new SoundSourceComponentState(engine),
                fireSource = new SoundSourceComponentState(engine)
            );

            var brushHitboxColliderGeometryList = new List<ColliderGeometry>();
            var colliderOffset = Orientation.Vertical() ? new Vector2(tileSize, 0) : new Vector2(0, tileSize);
            for (int i = 1; i < Tiles; i += 2)
            {
                var coll = (ColliderGeometry)new CircleColliderGeometry(
                    ((ISourceVectorEnvironment)engine).VectorArithmetic, 6);
                coll.Position = Orientation.Normal() * 2f + colliderOffset * i;
                brushHitboxColliderGeometryList.Add(coll);
            }

            brushHitboxes = brushHitboxColliderGeometryList.ToArray();
            inactiveColliderList = new ColliderListGeometry(brushHitboxes);

            var components = CreateLaserColliders().ToArray();
            beamHitboxes = components.Select(c => c.Collider).ToArray();
            foreach (var component in components) Add(component);
            activeColliderList = new ColliderListGeometry(brushHitboxes.Concat(beamHitboxes).ToArray());

            Get<SourceStaticMoverState>().OnMove = v =>
            {
                Position += v;
                foreach (var collider in components)
                    collider.UpdateBeam();
            };
        }

        private Sprite configureSprite(Sprite sprite)
        {
            sprite.Scale = Orientation == Orientations.Left || Orientation == Orientations.Up
                ? new Vector2(-1, 1)
                : Vector2.One;
            sprite.Rotation = Orientation == Orientations.Up || Orientation == Orientations.Down
                ? (float)Math.PI / 2f
                : 0f;
            return sprite;
        }

        protected virtual IEnumerable<LaserColliderComponent> CreateLaserColliders()
        {
            var offset = Orientation.Vertical() ? new Vector2(tileSize, 0) : new Vector2(0, tileSize);

            if (Tiles == 2)
            {
                return new[]
                {
                    new LaserColliderComponent
                    {
                        CollideWithSolids = CollideWithSolids, Thickness = beamThickness, Offset = offset + beamOffset,
                    }
                };
            }

            var start = offset / 2;
            return Enumerable.Range(0, Tiles).Select(i => new LaserColliderComponent
            {
                CollideWithSolids = CollideWithSolids, Offset = start + offset * i, Thickness = tileSize,
            });
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            Add(new SimulationCoroutine(engine, impactParticlesSequence()));
        }

        private void onPlayerCollide(RoomPlayerSession player)
        {
            if (KillPlayer)
            {
                Vector2 direction;
                if (Orientation == Orientations.Left || Orientation == Orientations.Right)
                    direction = player.Center.Y <= Position.Y ? -Vector2.UnitY : Vector2.UnitY;
                else
                    direction = player.Center.X <= Position.X ? -Vector2.UnitX : Vector2.UnitX;

                player.Die(direction, false, true);
            }
        }

        public override void Update()
        {
            base.Update();

            if (State == LaserState.Precharge && chargeDelayRemaining > 0)
            {
                chargeDelayRemaining -= engine.DeltaTime;
                if (chargeDelayRemaining <= 0)
                    State = LaserState.Charging;
            }

            if (collisionDelayRemaining > 0)
            {
                collisionDelayRemaining -= engine.DeltaTime;
                if (collisionDelayRemaining <= 0)
                    Collider = activeColliderList;
            }

            if (fireSoundDelayRemaining > 0)
            {
                fireSoundDelayRemaining -= engine.DeltaTime;
                if (fireSoundDelayRemaining <= 0 && State >= LaserState.Charging && State <= LaserState.Firing)
                {
                    PlayIfInBounds(fireSource, laserFireSound);
                }
            }

            if (State == LaserState.Burst && burstTimeRemaining > 0)
            {
                burstTimeRemaining -= engine.DeltaTime;
                if (burstTimeRemaining <= 0)
                    State = LaserState.Firing;
            }

            if (State == LaserState.Cooldown && !paintParticlesSprite.Animating)
            {
                State = LaserState.Idle;
            }
        }

        private void PlayIfInBounds(SoundSourceComponentState source, string path)
        {
            var camera = ((OriginalLevelSceneRoot)Scene).Camera;
            if (Position.X + activeColliderList.Right >= camera.Left - 32f &&
                Position.X + activeColliderList.Left <= camera.Right + 32f &&
                Position.Y + activeColliderList.Bottom >= camera.Top - 32f &&
                Position.Y + activeColliderList.Top <= camera.Bottom + 32f)
            {
                source.Play(path);
            }
        }
    }
}
