using System;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Rendering;
using Celeste.Core.Rendering.States;
using Celeste.Mods.StrawberryJam.Runtime.Pellets;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Gameplay.Platforms.Cassette;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Platforms.Cassette;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Rendering;
using Celeste.Mods.StrawberryJam.Components.Cassette;
using Celeste.Mods.StrawberryJam.Entities.Audio;
using Celeste.Mods.StrawberryJam.Entities.Hazards.Lasers;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collision.Blockers;
using Celeste.Simulation.Gameplay.Player.Collision.Components;

namespace Celeste.Mods.StrawberryJam.Entities.Hazards.Projectiles
{
    [CustomEntity("SJ2021/PelletEmitterUp = LoadUp", "SJ2021/PelletEmitterDown = LoadDown",
        "SJ2021/PelletEmitterLeft = LoadLeft", "SJ2021/PelletEmitterRight = LoadRight")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.PelletEmitter")]
    public class PelletEmitter : OrientableEntity
    {
        private readonly ISourceEngineContext engine;
        private SingletonAudioController sfx;
        public bool CollideWithSolids { get; }
        public int Count { get; }
        public float Delay { get; }
        public float Speed { get; }
        public int CassetteIndex { get; }
        public float WiggleFrequency { get; }
        public float WiggleAmount { get; }
        public Vector2 Direction { get; }
        public Vector2 Origin { get; }
        public readonly Sprite EmitterSprite;
        public string AnimationKeyPrefix => CassetteIndex switch { 0 => "blue", 1 => "pink", _ => "both" };
        private string IdleAnimationKey => AnimationKeyPrefix + "_idle";
        private string ChargingAnimationKey => AnimationKeyPrefix + "_charging";
        private string FiringAnimationKey => AnimationKeyPrefix + "_firing";

        private static string FireSound(int index) => index == 0
            ? "event:/sj21_mosscairn_sfx/emitter_blue"
            : "event:/sj21_mosscairn_sfx/emitter_pink";

        public static SceneEntityState LoadUp(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            EntityData data) => new PelletEmitter(level.EngineContext, data, offset, Orientations.Up);

        public static SceneEntityState LoadDown(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, EntityData data) => new PelletEmitter(level.EngineContext, data, offset, Orientations.Down);

        public static SceneEntityState LoadLeft(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, EntityData data) => new PelletEmitter(level.EngineContext, data, offset, Orientations.Left);

        public static SceneEntityState LoadRight(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, EntityData data) =>
            new PelletEmitter(level.EngineContext, data, offset, Orientations.Right);

        public PelletEmitter(ISourceEngineContext engine, EntityData data, Vector2 offset, Orientations orientation) :
            base(engine, data, offset, orientation)
        {
            this.engine = engine;
            CassetteIndex = data.Int("cassetteIndex");
            CollideWithSolids = data.Bool("collideWithSolids", true);
            Count = data.Int("pelletCount", 1);
            Delay = data.Float("pelletDelay", .25f);
            Speed = data.Float("pelletSpeed", 100f);
            WiggleFrequency = data.Float("wiggleFrequency", 2f);
            WiggleAmount = data.Float("wiggleAmount", 2f);
            Direction = Orientation.Direction();
            Origin = Orientation.Direction() * 12f;
            Collider = new CircleColliderGeometry(((ISourceVectorEnvironment)engine).VectorArithmetic, 6,
                Direction.X * 2, Direction.Y * 2);
            var runtime = PelletRuntime.For(engine);
            EmitterSprite = runtime.SpriteBank.Create("pelletEmitter");
            EmitterSprite.Rotation = Orientation.Angle() - (float)Math.PI / 2f;
            EmitterSprite.Effects = Orientation is Orientations.Left or Orientations.Down
                ? SourceSpriteEffects.FlipVertically
                : SourceSpriteEffects.None;
            EmitterSprite.Play(IdleAnimationKey);
            Add(EmitterSprite);
            Add(new LedgeBlockerState());
            Add(new PlayerColliderComponentState(OnPlayerCollide));
            Add(new CassetteListener(CassetteIndex) { OnTick = OnTick });
        }

        private void OnTick(int currentIndex, bool isSwap)
        {
            if (!isSwap && (CassetteIndex < 0 || CassetteIndex != currentIndex))
            {
                string key = ChargingAnimationKey;
                var animation = EmitterSprite.Animations[key];
                animation.Delay = GetTickLength() / animation.Frames.Length;
                EmitterSprite.Play(key);
            }
            else if (isSwap && (CassetteIndex < 0 || CassetteIndex == currentIndex))
            {
                EmitterSprite.Play(FiringAnimationKey);
                Fire(currentIndex);
            }
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            sfx = SingletonAudioController.Ensure(scene);
        }

        private float GetTickLength()
        {
            var manager =
                ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<OriginalCassetteBlockManagerState>(
                    "Celeste.CassetteBlockManager");
            float beatLength = (10 / 60f) / manager.TempoMultiplier;
            return beatLength * (manager is InstalledCassetteBlockManagerState installed ? installed.BeatsPerTick : 4);
        }

        public void Fire(int? cassetteIndex = null, Action<PelletShot> action = null)
        {
            for (int i = 0; i < Count; i++)
            {
                var shot = engine.EntityPools.Create<PelletShot>()
                    .Init(this, i * Delay, cassetteIndex ?? CassetteIndex);
                action?.Invoke(shot);
                ((ISourceScene)Scene).SourceScene.QueueSourceEntity(shot);
                if (i == 0) PlayFireSound(cassetteIndex ?? CassetteIndex);
            }
        }

        public void PlayFireSound(int index) => sfx?.Play(FireSound(index), this, .01f);

        private void OnPlayerCollide(RoomPlayerSession player) =>
            player.Die(((ISourceVectorEnvironment)engine).VectorArithmetic.SafeNormalize(player.Center - Position));
    }
}
