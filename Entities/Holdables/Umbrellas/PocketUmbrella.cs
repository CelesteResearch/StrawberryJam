using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Startup;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Audio;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Display.Colors;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Enemies.Seeker;
using Celeste.Simulation.Audio.Sources;
using Celeste.Input.Gameplay;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Game.Controls.Source;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Mods.StrawberryJam.Runtime.Holdables.Umbrellas;
using Celeste.Mods.StrawberryJam.Entities.Holdables.Umbrellas;

namespace Celeste.Mods.StrawberryJam.Entities.Holdables.Umbrellas
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.PocketUmbrella")]
    public class PocketUmbrella : SourceActorEntityState
    {
        private float staminaCost;
        private Sprite sprite, sweat;

        public bool destroyed = false, spawning = true;
        public HoldableState Hold;
        private OriginalLevelSceneRoot level;
        private SoundSourceComponentState fallingSfx;
        private RoomPlayerSession player;

        private string musicParam;

        private readonly ISourceEngineContext engine;
        private readonly string barrierRegistryKey;
        private readonly PocketUmbrellaParticles particles;

        public PocketUmbrella(ISourceEngineContext engine, Vector2 position, float cost, string musicParam = "") : base(
            new PixelActor(null, position), engine, ((ISourcePlatformTargetMotionEnvironment)engine).TargetMotionPolicy)
        {
            this.engine = engine;
            barrierRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(SeekerBarrierState))
                .RegistryKey;
            particles = PocketUmbrellaParticles.For(engine);
            staminaCost = cost;
            this.musicParam = musicParam;

            Add(sprite = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<StrawberryJamModule>().Single()
                .SpriteBank.Create("pocketUmbrella"));
            sprite.Visible = false;
            Collider = new HitboxColliderGeometry(8, 10, -4, -10);

            Add(sweat = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<StrawberryJamModule>().Single()
                .SpriteBank.Create("pocketUmbrellaSweat"));
            sweat.Visible = false;
            sweat.CenterOrigin();
            sweat.Play("sweat");
            sweat.Position.Y =
                -2; // adjustment to make it line up with maddies head. *should* work with custom sprites too, unless they have a wack offset

            Add(Hold = new HoldableState(engine, 0.3f));
            Hold.SlowFall = true;
            Hold.SlowRun = false;
            Hold.PickupCollider = new HitboxColliderGeometry(20, 22, -10, -16);
            Hold.OnPickup = new Action(onPickup);

            Add(fallingSfx = new SoundSourceComponentState(engine));
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            level = (OriginalLevelSceneRoot)Scene;
        }

        private void onPickup()
        {
            AddTag(((IOriginalGameTagsEnvironment)engine).Tags.Persistent);
            Depth = 1;
            player = Hold.Holder;
            sprite.Visible = true;
            sprite.Play("spawn", true);
            sprite.OnChange = (_, _) => { spawning = false; };
        }

        public override void Render(Celeste.Core.Rendering.ISourceRenderContext context)
        {
            if (sprite.Visible)
            {
                sprite.DrawSimpleOutline(context);
            }

            base.Render(context);
        }

        public override void Update()
        {
            if (level.OnInterval(0.05f))
            {
                level.Particles.Emit(particles.P_Glow, 1, Center + Vector2.UnitY * -9f, new Vector2(10f, 4f));
            }

            bool climbUpdate = false;

            float target;
            if (Hold.IsHeld)
            {
                climbUpdate = Hold.Holder.Machine.State == 1;
                if (climbUpdate)
                {
                    target = ScalarMath.ClampedMap(400 * (int)Hold.Holder.Facing, -300f, 300f, (float)Math.PI / 4.5f,
                        -(float)Math.PI / 4.5f);
                }
                else if (Hold.Holder.OnGround(1))
                {
                    target = ScalarMath.ClampedMap(Hold.Holder.Speed.X, -300f, 300f, (float)Math.PI / 4.5f,
                        -(float)Math.PI / 4.5f);
                }
                else
                {
                    target = ScalarMath.ClampedMap(Hold.Holder.Speed.X, -300f, 300f, (float)Math.PI / 3f,
                        -(float)Math.PI / 3f);
                }
            }
            else
            {
                target = 0f;
            }

            if (Hold.IsHeld && !player.OnGround() && !climbUpdate)
                ((IOriginalGameplayProcess)engine).AudioSession.SetMusicParam(musicParam, 1);
            else
                ((IOriginalGameplayProcess)engine).AudioSession.SetMusicParam(musicParam, 0);

            sprite.Rotation = ScalarMath.Approach(sprite.Rotation, target, (float)Math.PI * engine.DeltaTime,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);

            if (Hold.IsHeld && !Hold.Holder.OnGround(1) &&
                (sprite.CurrentAnimationID == "fall" || sprite.CurrentAnimationID == "fallLoop"))
            {
                if (!fallingSfx.Playing)
                {
                    ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                        "event:/new_content/game/10_farewell/glider_engage", Position);
                    fallingSfx.Play("event:/new_content/game/10_farewell/glider_movement", null, 0f);
                }

                Vector2 speed = Hold.Holder.Speed;
                Vector2 vector = new Vector2(speed.X * 0.5f, (speed.Y < 0f) ? (speed.Y * 2f) : speed.Y);
                float value = ScalarMath.Map(((ISourceVectorEnvironment)engine).VectorArithmetic.Length(vector), 0f,
                    120f, 0f, 0.7f);
                fallingSfx.Param("glider_speed", value);
            }
            else
            {
                fallingSfx.Stop(true);
            }

            base.Update();
            if (!destroyed)
            {
                foreach (SeekerBarrierState seekerBarrier in ((ISourceScene)Scene).SourceScene.Entities
                         .GetTrackedEntities(barrierRegistryKey))
                {
                    seekerBarrier.Collidable = true;
                    bool flag = SourceEntityCollision.Check(this, seekerBarrier);
                    seekerBarrier.Collidable = false;
                    if (flag)
                    {
                        destroyed = true;
                        Collidable = false;
                        if (Hold.IsHeld)
                        {
                            Hold.Holder.Drop();
                            ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble((int)RumbleStrength.Medium,
                                (int)RumbleLength.Medium);
                        }

                        Add(new SimulationCoroutine(engine, DestroyAnimationRoutine()));
                        return;
                    }
                }

                if (Hold.IsHeld && Hold.Holder.Speed.Y > 20f || level.Wind.Y < 0f)
                {
                    if (level.OnInterval(0.04f))
                    {
                        if (level.Wind.Y < 0f)
                        {
                            level.ParticlesBG.Emit(particles.P_GlideUp, 1, Position - Vector2.UnitY * 20f,
                                new Vector2(6f, 4f));
                        }
                        else
                        {
                            level.ParticlesBG.Emit(particles.P_Glide, 1, Position - Vector2.UnitY * 10f,
                                new Vector2(6f, 4f));
                        }
                    }

                    PlayOpen();
                    ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble((int)RumbleStrength.Climb,
                        (int)RumbleLength.Short);
                }
                else if (!spawning)
                {
                    sprite.Play("held", false, false);
                }

                sprite.Scale.Y = ScalarMath.Approach(sprite.Scale.Y, Vector2.One.Y, engine.DeltaTime * 2f,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                sprite.Scale.X = ScalarMath.Approach(sprite.Scale.X, Math.Sign(sprite.Scale.X) * Vector2.One.X,
                    engine.DeltaTime * 2f, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);

                sweat.Visible = false;
                if (Hold.IsHeld)
                {
                    if (!climbUpdate)
                    {
                        Hold.Holder.Data.Stamina -= staminaCost * engine.DeltaTime;
                        if (!Hold.Holder.OnGround(1))
                        {
                            sweat.Visible = true;
                        }
                    }

                    if (Hold.Holder.Data.Stamina <= 0)
                    {
                        Hold.Holder.Drop();
                    }

                    return;
                }

                if (!destroyed)
                {
                    Collidable = false;
                    Hold.Active = false;
                    destroyed = true;
                    ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble((int)RumbleStrength.Medium,
                        (int)RumbleLength.Medium);
                    Add(new SimulationCoroutine(engine, DestroyAnimationRoutine()));
                }

                return;
            }
        }

        private void PlayOpen()
        {
            if (sprite.CurrentAnimationID != "fall" && sprite.CurrentAnimationID != "fallLoop" && !spawning)
            {
                sprite.Play("fall", false, false);
                sprite.Scale = new Vector2(1.5f, 0.6f);
                level.Particles.Emit(particles.P_Expand, 16,
                    Center + ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(Vector2.UnitY * -12f,
                        sprite.Rotation), new Vector2(8f, 3f), -1.5707964f + sprite.Rotation);
                if (Hold.IsHeld)
                {
                    ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble((int)RumbleStrength.Medium,
                        (int)RumbleLength.Short);
                }
            }
        }

        public IEnumerator DestroyAnimationRoutine()
        {
            sprite.Play("death", true);
            Depth = 1;
            sprite.OnFinish = (_) => { sprite.Visible = false; };
            Vector2 offset = Vector2.Zero;
            if (player is not null)
            {
                offset = Position - player.Position + Vector2.UnitY * 2;
            }
            else
            {
                sprite.Visible = false;
            }

            while (sprite.Visible)
            {
                if (player is not null)
                {
                    Position = player.Position + offset;
                }
                else
                {
                    sprite.Visible = false;
                }

                yield return 0;
            }

            RemoveSelf();
            yield break;
        }
    }
}
