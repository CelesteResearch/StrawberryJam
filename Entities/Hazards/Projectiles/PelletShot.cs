using System;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Graphics.Animation;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Mods.StrawberryJam.Runtime.Pellets;
using Celeste.Core.Mathematics;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Entities.Hazards.Projectiles
{
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.PelletEmitter+PelletShot")]
    public sealed partial class PelletShot : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly PelletRuntime runtime;
        private readonly ISourceVectorArithmetic vectors;
        private readonly Sprite projectileSprite;
        private readonly Sprite impactSprite;
        private float delayTimeRemaining;
        private string projectileAnimationKey;
        private string impactAnimationKey;
        private readonly HitboxColliderGeometry killHitbox = new(8, 8);
        private readonly SineWaveState travelSineWave;
        private readonly WigglerComponent hitWiggler;
        private ParticleTypePresentation particleType;
        private Vector2 hitDir;
        private int firedCassetteIndex;
        private float wiggleAmount = 2f;
        private PelletEmitter parentEmitter;
        public bool Dead { get; set; }
        public Vector2 Speed { get; set; }
        public bool CollideWithSolids { get; set; }

        public PelletShot(ISourceEngineContext engine, PelletRuntime runtime) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, Vector2.Zero)
        {
            this.engine = engine;
            this.runtime = runtime;
            vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            Depth = -8500;
            Add(projectileSprite = runtime.SpriteBank.Create("pelletProjectile"));
            Add(impactSprite = runtime.SpriteBank.Create("pelletImpact"));
            Add(new PlayerColliderComponentState(OnPlayerCollide));
            Add(travelSineWave = new SineWaveState(engine, 2f));
            Add(hitWiggler = WigglerComponent.Create(((ISourceWigglerPoolScope)engine).WigglerPool, 1.2f, 2f));
            hitWiggler.StartZero = true;
        }

        public PelletShot Init(PelletEmitter emitter, float delay, int cassetteIndex)
        {
            parentEmitter = emitter;
            delayTimeRemaining = delay;
            Dead = false;
            Speed = emitter.Direction * emitter.Speed;
            Position = emitter.Position + emitter.Origin;
            CollideWithSolids = emitter.CollideWithSolids;
            Collider = killHitbox;
            Collidable = true;
            particleType = cassetteIndex == 0 ? runtime.BlueTrail : runtime.PinkTrail;
            impactSprite.Rotation = projectileSprite.Rotation = emitter.EmitterSprite.Rotation;
            impactSprite.Effects = projectileSprite.Effects = emitter.EmitterSprite.Effects;
            firedCassetteIndex = cassetteIndex;
            projectileAnimationKey = impactAnimationKey = cassetteIndex == 0 ? "blue" : "pink";
            impactSprite.Visible = false;
            impactSprite.Stop();
            projectileSprite.Visible = delay == 0;
            projectileSprite.Stop();
            travelSineWave.Frequency = emitter.WiggleFrequency;
            travelSineWave.Active = true;
            travelSineWave.Reset();
            wiggleAmount = emitter.WiggleAmount;
            hitWiggler.StopAndClear();
            hitDir = Vector2.Zero;
            return this;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            if (delayTimeRemaining <= 0) projectileSprite.Play(projectileAnimationKey, randomizeFrame: true);
        }

        public void Destroy(float delay = 0)
        {
            parentEmitter = null;
            projectileSprite.Stop();
            impactSprite.Stop();
            Dead = true;
            RemoveSelf();
        }
    }
}
