using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Timing;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Collectibles.Refills;
using Celeste.Game.Gameplay.Collectibles.Strawberries;
using Celeste.Graphics.Animation;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Scene.Following;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Entities.Refills.Shards
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.RefillShard")]
    public partial class RefillShard : SceneEntityState
    {
        private readonly IOriginalRefillProcess engine;
        public const float LoseTime = 0.15f;

        public bool Finished;
        public FollowerState Follower;

        private int index;
        private bool twoDashes;
        private bool resetOnGround;
        private RefillShardController controller;

        private Vector2 start;
        private ISourcePlatformEntity attached;
        private float canLoseTimer;
        private float loseTimer;
        private bool losing;

        private Sprite sprite;
        private Sprite flash;
        private Sprite outlineSprite;
        private ParticleTypePresentation p_shatter;
        private ParticleTypePresentation p_regen;
        private ParticleTypePresentation p_glow;
        private WigglerComponent wiggler;
        private BloomPointState bloom;
        private VertexLightState light;
        private SineWaveState sine;

        private bool
            renderShard; //we can't use the standard Visible modifier without removing the dotted outline after grabbing a shard

        public RefillShard(RefillShardController controller, Vector2 position, int index, bool two, bool groundReset)
            : base(((ISourceEntitySourceDataEnvironment)controller.Engine).EntitySourceDataRegistration, position)
        {
            engine = controller.Engine;
            this.index = index;
            this.controller = controller;

            twoDashes = two;
            resetOnGround = groundReset;

            start = Position;

            OwnedComponents.Add(Follower = new FollowerState(engine, OriginalSceneTags.Persistent,
                onGainLeader: OnGainLeader, onLoseLeader: OnLoseLeader));
            Follower.FollowDelay = 0.2f;
            Follower.PersistentFollow = false;
            OwnedComponents.Add(new PlayerColliderComponentState(OnPlayer));
            OwnedComponents.Add(new SourceStaticMoverState
            {
                SolidChecker = solid => SourceEntityCollision.Check(solid, this),
                OnAttach = platform =>
                {
                    Depth = -1000000;
                    Collider = new HitboxColliderGeometry(18f, 18f, -9f, -9f);
                    attached = platform;
                    start = Position - platform.Position;
                }
            });

            p_shatter = two ? engine.RefillParticles.P_ShatterTwo : engine.RefillParticles.P_Shatter;
            p_regen = two ? engine.RefillParticles.P_RegenTwo : engine.RefillParticles.P_Regen;
            p_glow = two ? engine.RefillParticles.P_GlowTwo : engine.RefillParticles.P_Glow;

            OwnedComponents.Add(sprite = new Sprite(engine, engine.GameGraphics.GameAtlas,
                $"objects/StrawberryJam2021/refillShard/{(two ? "two" : "one")}"));
            sprite.AddLoop("idle", "", 0.1f);
            sprite.Play("idle");
            sprite.CenterOrigin();

            OwnedComponents.Add(flash = new Sprite(engine, engine.GameGraphics.GameAtlas,
                "objects/StrawberryJam2021/refillShard/flash"));
            flash.Add("flash", "", 0.05f);
            flash.OnFinish = (_) => flash.Visible = false;
            flash.CenterOrigin();

            OwnedComponents.Add(outlineSprite = new Sprite(engine, engine.GameGraphics.GameAtlas,
                $"objects/StrawberryJam2021/refillShard/outline"));
            outlineSprite.AddLoop("idle", "", 0.1f);
            outlineSprite.Play("idle");
            outlineSprite.CenterOrigin();

            outlineSprite.Rotation = sprite.Rotation = flash.Rotation = engine.Random.Next(4) * ((float)Math.PI / 2f);

            OwnedComponents.Add(wiggler = ((ISourceWigglerPoolScope)engine).WigglerPool.Create(1f, 4f,
                value => sprite.Scale = outlineSprite.Scale = flash.Scale = Vector2.One * (1f + value * 0.2f)));

            OwnedComponents.Add(bloom = new BloomPointState(0.8f, 8f));
            OwnedComponents.Add(light = new VertexLightState(engine, Vector2.Zero, PackedColor.White, 1f, 8, 32));
            OwnedComponents.Add(sine = new SineWaveState(engine, 0.6f).Randomize());

            UpdateY();
            Depth = -100;
            Collider = new HitboxColliderGeometry(16f, 16f, -8f, -8f);

            renderShard = true;
        }
    }
}
