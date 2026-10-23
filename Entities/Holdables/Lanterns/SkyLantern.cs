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
    [CustomEntity("SJ2021/AntiGravJelly")]
    public partial class SkyLantern : SourceActorEntityState
    {
        private readonly ISourceEngineContext engine;

        private readonly ParticleTypePresentation particleGlow,
            particleExpand,
            particleGlideDown,
            particleGlide,
            particlePlatform;

        public bool canBoostUp { get; private set; }
        public float[] riseSpeeds { get; private set; }
        private bool bubble, destroyed;

        private float highFrictionTimer,
            noGravityTimer,
            downThrowMultiplier,
            diagThrowXMultiplier,
            diagThrowYMultiplier,
            gravity,
            lastDroppedTime;

        private Vector2 speed, startPosition, prevLiftSpeed;
        private SourceCollisionHandler onCollideH, onCollideV;
        private Sprite sprite;
        private WigglerComponent wiggler;
        private HoldableState hold;
        private SineWaveState platformSine;
        private SoundSourceComponentState risingSFX;
        private OriginalLevelSceneRoot level;
        private Celeste.Simulation.Presentation.Lighting.VertexLightState light;
        public ISourceEngineContext EngineContext => engine;

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new SkyLantern(level.EngineContext, data, offset);

        public SkyLantern(ISourceEngineContext engine, EntityData data, Vector2 offset) : this(engine,
            data.Position + offset, data.Bool("bubble", false), data.Float("downThrowMultiplier", 1.8f),
            data.Float("diagThrowXMultiplier", 1.6f), data.Float("diagThrowYMultiplier", 1.8f),
            data.Float("gravity", -30), data.Bool("canBoostUp", true),
            data.Attr("riseSpeeds", "-24.0, -176.0, -120.0, -80.0, -40.0"))
        {
        }

        private float Approach(float value, float target, float step) => ScalarMath.Approach(value, target, step,
            ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);

        private void Rumble(RumbleStrength strength, RumbleLength length) =>
            ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble((int)strength, (int)length);

        private void PlaySound(string sound, Vector2 position)
        {
            var p = (IOriginalGameplayProcess)engine;
            p.AudioSession.Play(sound, position, p.SaveContext);
        }

        public SkyLantern(ISourceEngineContext engine, Vector2 position, bool bubble, float downThrowMultiplier,
            float diagThrowXMultiplier, float diagThrowYMultiplier, float gravity, bool canBoostUp,
            string riseSpeeds) : base(new PixelActor(null, position), engine,
            ((ISourcePlatformTargetMotionEnvironment)engine).TargetMotionPolicy)
        {
            this.engine = engine;
            var runtime = SkyLanternRuntime.For(engine);
            this.bubble = bubble;
            this.downThrowMultiplier = downThrowMultiplier;
            this.diagThrowYMultiplier = diagThrowYMultiplier;
            this.diagThrowXMultiplier = diagThrowXMultiplier;
            this.gravity = gravity;
            startPosition = Position;
            this.canBoostUp = canBoostUp;

            string[] speeds = riseSpeeds.Split(',');
            this.riseSpeeds = new float[speeds.Length];

            for (int i = 0; i < speeds.Length; i++)
            {
                this.riseSpeeds[i] = float.Parse(speeds[i]);
            }

            lastDroppedTime = -2;

            Collider = new HitboxColliderGeometry(8, 10, -4, -10);
            onCollideH = new SourceCollisionHandler(CollideHandlerH);
            onCollideV = new SourceCollisionHandler(CollideHandlerV);
            Add(sprite = runtime.Module.SpriteBank.Create("skyLantern"));
            Add(wiggler = ((ISourceWigglerPoolScope)engine).WigglerPool.Create(0.25f, 4));
            Depth = -5;
            Add(hold = new HoldableState(engine, 0.3f));
            hold.PickupCollider = new HitboxColliderGeometry(20, 22, -10, -16);
            hold.SlowFall = true;
            hold.SlowRun = false;
            hold.OnPickup = new Action(PickupHandler);
            hold.OnRelease = new Action<Vector2>(ReleaseHandler);
            hold.SpeedGetter = () => { return speed; };
            hold.OnHitSpring = value => SpringHandler((ISourceSpringEntity)value);
            Add(platformSine = new SineWaveState(engine, 0.3f));
            platformSine.Randomize();
            Add(risingSFX = new SoundSourceComponentState(engine));
            Add(new WindMoverState(WindHandler));
            Add(light = new Celeste.Simulation.Presentation.Lighting.VertexLightState(engine, new Vector2(0, -15),
                PackedColor.Orange, 1f, 32, 64));
            runtime.LoadParticles();
            particleGlow = runtime.ParticleGlow;
            particleExpand = runtime.ParticleExpand;
            particleGlideDown = runtime.ParticleGlideDown;
            particleGlide = runtime.ParticleGlide;
            particlePlatform = runtime.ParticlePlatform;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            level = (OriginalLevelSceneRoot)Scene;
        }
    }
}
