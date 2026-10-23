using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Input.Gameplay;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Rendering;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Interaction.Springs;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Movement;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Mods.StrawberryJam.Runtime.Holdables;

namespace Celeste.Mods.StrawberryJam.Entities.Holdables.Flowers
{
    [CustomEntity("SJ2021/TripleBoostFlower")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.TripleBoostFlower")]
    public class TripleBoostFlower : SourceActorEntityState
    {
        private int charges, lastFacing;

        private float boostCooldown,
            boostDelay,
            boostSpeed,
            boostDuration,
            boostDurationMax,
            boostThreshold,
            gravity,
            highFrictionTimer,
            noGravityTimer;

        private bool destroyed;
        private Vector2 speed, customCarryOffset, prevLiftSpeed;
        private HoldableState hold;
        private SourceCollisionHandler onCollideH, onCollideV;
        private RoomPlayerSession player;
        private OriginalLevelSceneRoot level;
        private Sprite sprite;

        private readonly SoundSourceComponentState boostSfx, moveSfx;

        public float FallSpeed { get; private set; }
        public float FastFallSpeed { get; private set; }
        public float SlowFallSpeed { get; private set; }

        private readonly ISourceEngineContext engine;

        public TripleBoostFlower(ISourceEngineContext engine, EntityData data, Vector2 offset) : this(engine,
            data.Position + offset, data.Float("boostDelay", 0.2f), data.Float("boostSpeed", -160f),
            data.Float("boostDuration", 0.5f), data.Float("fastFallSpeed", 120f), data.Float("slowFallSpeed", 24f),
            data.Float("normalFallSpeed", 40f))
        {
        }


        public TripleBoostFlower(ISourceEngineContext engine, Vector2 position, float boostDelay, float boostSpeed,
            float boostDuration, float fastFall, float slowFall, float fall) : base(new PixelActor(null, position),
            engine, ((ISourcePlatformTargetMotionEnvironment)engine).TargetMotionPolicy)
        {
            this.engine = engine;
            Depth = -5;
            FallSpeed = fall;
            FastFallSpeed = fastFall;
            SlowFallSpeed = slowFall;
            this.boostDelay = boostDelay;
            this.boostSpeed = boostSpeed;
            boostDurationMax = boostDuration;
            boostThreshold = -300f;
            charges = 3;
            destroyed = false;
            TripleBoostFlowerParticles.For(engine);
            customCarryOffset = Vector2.UnitY * -8f;
            gravity = 30f;
            prevLiftSpeed = Vector2.Zero;

            Add(sprite = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<StrawberryJamModule>().Single()
                .SpriteBank.Create("roseGlider"));
            sprite.CenterOrigin();
            sprite.Origin.Y += 9;
            sprite.Play("idle_3");

            Collider = new HitboxColliderGeometry(6, 10, -3, -5); // todo adjust
            onCollideH = collideHandlerH;
            onCollideV = collideHandlerV;


            Add(hold = new HoldableState(engine, 0.3f));
            hold.PickupCollider = new HitboxColliderGeometry(10, 20, -5, -15); // todo adjust
            hold.SlowFall = true;
            hold.SlowRun = false; // todo ask
            hold.SpeedGetter = () => speed;
            hold.OnPickup = new Action(onPickup);
            hold.OnRelease = new Action<Vector2>(onRelease);
            hold.OnHitSpring = value => onHitSpring((ISourceSpringEntity)value);

            Add(boostSfx = new SoundSourceComponentState(engine));
            Add(moveSfx =
                new SoundSourceComponentState(engine).Play(
                    "event:/strawberry_jam_2021/game/triple_boost_flower/glider_movement"));
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            level = (OriginalLevelSceneRoot)Scene;
        }

        private void AdvanceGroundSpeed()
        {
            speed.X = ScalarMath.Approach(speed.X, 0f, 800f * engine.DeltaTime,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            Vector2 liftSpeed = LiftSpeed;
            if (liftSpeed == Vector2.Zero && prevLiftSpeed != Vector2.Zero)
            {
                speed = prevLiftSpeed;
                prevLiftSpeed = Vector2.Zero;
                speed.Y = ((ISourceExtremaEnvironment)engine).ExtremaArithmetic.Min(speed.Y * .6f, 0);
                if (speed.X != 0 && speed.Y == 0) speed.Y = -60;
                if (speed.Y < 0)
                {
                    noGravityTimer += .15f;
                }
            }
            else
            {
                prevLiftSpeed = liftSpeed;
                if (liftSpeed.Y < 0 && speed.Y < 0) speed.Y = 0;
            }
        }

        public override void Update()
        {
            base.Update();

            if (boostCooldown > 0)
                boostCooldown -= engine.DeltaTime;
            if (boostDuration > 0 && hold.IsHeld)
            {
                player.Speed = new Vector2(player.Speed.X, boostSpeed);
                boostDuration -= engine.DeltaTime;
                if (boostDuration <= 0)
                    boostCooldown = boostDelay;
            }
            else if (boostDuration <= 0 && charges == 0 && hold.IsHeld)
            {
                destroySelf();
            }

            if (hold.IsHeld)
            {
                if (((OriginalGameInputProcess)((ISourceGameInputEnvironment)engine).Input).Dash.Pressed)
                {
                    consumeBoost();
                    if (destroyed)
                        return;
                }

                // SFX might be stopped by room transition
                if (!moveSfx.Playing)
                    moveSfx.Play("event:/strawberry_jam_2021/game/triple_boost_flower/glider_movement");

                float intensity = hold.Holder.OnGround()
                    ? 0
                    : ScalarMath.ClampedMap(hold.Holder.Speed.Length(), 0, 160);
                moveSfx.Param("speed", intensity);
                moveSfx.Param("fadeout", 0);
            }
            else
            {
                moveSfx.Param("fadeout", 1);

                if (highFrictionTimer >= 0)
                {
                    highFrictionTimer -= engine.DeltaTime;
                }

                if (OnGround(1))
                {
                    AdvanceGroundSpeed();
                }
                else
                {
                    if (hold.ShouldHaveGravity)
                    {
                        float gravityCoefficient = 100;
                        if (speed.Y < -30f)
                        {
                            gravityCoefficient *= 2;
                        }

                        float xAdjustSpeed = 10;
                        if (speed.Y < 0 || highFrictionTimer <= 0)
                        {
                            xAdjustSpeed = 40;
                        }

                        speed.X = ScalarMath.Approach(speed.X, 0f, xAdjustSpeed * engine.DeltaTime,
                            ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                        if (noGravityTimer > 0f)
                        {
                            noGravityTimer -= engine.DeltaTime;
                        }
                        else
                        {
                            speed.Y = ScalarMath.Approach(speed.Y, gravity, gravityCoefficient * engine.DeltaTime,
                                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                        }
                    }
                }

                MoveH(speed.X * engine.DeltaTime, onCollideH, null);
                MoveV(speed.Y * engine.DeltaTime, onCollideV, null);

                // boundary enforcing
                if (Left < level.Bounds.Left)
                {
                    Left = level.Bounds.Left;
                    onCollideH(new PixelCollision
                    {
                        Direction = -Vector2.UnitX
                    });
                }
                else if (Right > level.Bounds.Right)
                {
                    Right = level.Bounds.Right;
                    onCollideH(new PixelCollision
                    {
                        Direction = Vector2.UnitX
                    });
                }

                if (Top > level.Bounds.Bottom + 16)
                {
                    RemoveSelf();
                }

                hold.CheckAgainstColliders();
            }
        }

        private void consumeBoost()
        {
            if (canBoost())
            {
                hold.Holder.Launch.Begin();
                hold.Holder.Speed = new Vector2(hold.Holder.Speed.X, boostSpeed);
                // todo particles
                //level.ParticlesBG.Emit(boostParticles, 8, Position - Vector2.UnitY * 10, Vector2.UnitX * 5 + Vector2.UnitY * 3, (float) Math.PI);
                if (charges > 0)
                {
                    sprite.Play($"boost_{charges}");
                }

                charges--;
                boostDuration = boostDurationMax;
                ((OriginalGameInputProcess)((ISourceGameInputEnvironment)engine).Input).Dash.ConsumeBuffer();

                //sfx
                boostSfx.Play("event:/strawberry_jam_2021/game/triple_boost_flower/boost_" + (3 - charges));
            }
            else if (shouldBeDestroyed())
            {
                destroySelf();
                ((OriginalGameInputProcess)((ISourceGameInputEnvironment)engine).Input).Dash.ConsumeBuffer();
                return;
            }
        }

        private bool shouldBeDestroyed()
        {
            return hold.Holder.Speed.Y > boostThreshold && boostCooldown <= 0f && charges == 0 && boostDuration <= 0;
        }

        private bool canBoost()
        {
            return hold.Holder.Speed.Y > boostThreshold && boostCooldown <= 0f && charges > 0 && boostDuration <= 0;
        }

        private bool onHitSpring(ISourceSpringEntity spring)
        {
            if (!hold.IsHeld)
            {
                if (spring.Orientation == SpringOrientation.Floor && speed.Y >= 0f)
                {
                    speed.X = speed.X * 0.5f;
                    speed.Y = -160f;
                    noGravityTimer = 0.15f;
                    return true;
                }

                MoveTowardsY(spring.Center.Y + 5f, 4f, null);
                speed.X = spring.Orientation == SpringOrientation.WallRight ? -160f : 160;
                speed.Y = -80f;
                noGravityTimer = 0.1f;
                return true;
            }

            return false;
        }

        private void onRelease(Vector2 direction)
        {
            player.Holding.CarryOffsetTargetValue = new Vector2(0f, -12f);
            RemoveTag(((IOriginalGameTagsEnvironment)engine).Tags.Persistent.Value);

            highFrictionTimer = 0.5f;

            if (charges == 0)
            {
                destroySelf();
                return;
            }

            player = null;
            direction.Y *= 0.5f;
            if (direction.Y == 0f && direction.X != 0f)
            {
                direction.Y = -0.4f;
            }

            speed = direction * 100f;
        }

        private void destroySelf()
        {
            destroyed = true;
            speed = player.Speed;
            if (hold.IsHeld)
            {
                hold.Holder.Drop();
            }

            Collidable = false;
            hold.Active = false;
            Add(new SimulationCoroutine(engine, destroySelfCoroutine()));
        }

        private IEnumerator destroySelfCoroutine()
        {
            // todo audio effect
            yield return 1f;
            RemoveSelf();
            yield break;
        }

        private void onPickup()
        {
            player = hold.Holder;
            AddTag(((IOriginalGameTagsEnvironment)engine).Tags.Persistent.Value);
            lastFacing = player.Data.Facing;
            player.Holding.CarryOffsetTargetValue = customCarryOffset;
        }

        private void collideHandlerV(PixelCollision data)
        {
            // todo sfx when landing fast enough?
            if (speed.Y < 0f)
            {
                speed.Y *= -0.5f;
                return;
            }

            speed.Y = 0f;
        }

        private void collideHandlerH(PixelCollision data)
        {
            speed.X *= -1f;
        }

        public override void Render(global::Celeste.Core.Rendering.ISourceRenderContext context)
        {
            if (sprite.Visible) sprite.DrawOutline(context);
            base.Render(context);
        }
    }
}
