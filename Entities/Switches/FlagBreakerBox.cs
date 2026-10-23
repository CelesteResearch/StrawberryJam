using Celeste.Simulation;
using System;
using System.Numerics;
using System.Collections;
using Celeste.Core.Scheduling;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Rendering;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Scene;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Components.Shaking;
using Celeste.Simulation.Audio.Sources;
using Celeste.Game.Audio;

namespace Celeste.Mods.StrawberryJam.Entities.Switches
{
    [Celeste.Everest.Modules.Entities.Metadata.CustomEntity("SJ2021/FlagBreakerBox")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.FlagBreakerBox")]
    public partial class FlagBreakerBox : SourceSolidEntityState
    {
        private readonly ISourceEngineContext engine;
        private OriginalLevelSceneRoot Level => (OriginalLevelSceneRoot)Scene;
        private IOriginalGameplayProcess Process => (IOriginalGameplayProcess)engine;

        private Celeste.Simulation.Presentation.Particles.Definitions.ParticleTypePresentation P_Smash =>
            Process.ParticleTypes.LightningBreakerBox.P_Smash;

        private Celeste.Simulation.Presentation.Particles.Definitions.ParticleTypePresentation P_Sparks =>
            Process.ParticleTypes.LightningBreakerBox.P_Sparks;

        private Sprite sprite;

        private SineWaveState sine;

        private Vector2 start;

        private float sink;

        private int health = 2;

        private string flag;
        private bool aliveState;

        private float shakeCounter;

        private string music;

        private int musicProgress = -1;

        private bool musicStoreInSession;

        private Vector2 bounceDir;

        private WigglerComponent bounce;

        private ShakerComponentState shaker;

        private bool makeSparks;

        private bool smashParticles;


        private SoundSourceComponentState firstHitSfx;

        private bool spikesLeft;

        private bool spikesRight;

        private bool spikesUp;

        private bool spikesDown;

        public FlagBreakerBox(Vector2 position, bool flipX, ISourceSolidWorldFactory factory,
            ISourceEngineContext engine)
            : base(position, 32f, 32f, true, factory.Create(position, true))
        {
            this.engine = engine;
            SurfaceSoundIndex = 9;
            start = Position;
            var bank = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.SpriteFactory;
            sprite = bank.Create(bank.Has("flagBreakerBox") ? "flagBreakerBox" : "breakerBox");
            Sprite obj = sprite;
            obj.OnLastFrame = (Action<string>)Delegate.Combine(obj.OnLastFrame, (Action<string>)delegate(string anim)
            {
                if (anim == "break")
                {
                    Visible = false;
                }
                else if (anim == "open")
                {
                    makeSparks = false;
                }
            });
            sprite.Position = new Vector2(base.Width, base.Height) / 2f;
            sprite.FlipX = flipX;
            OwnedComponents.Add(sprite);
            sine = new SineWaveState(engine, 0.5f);
            OwnedComponents.Add(sine);
            bounce = ((ISourceWigglerPoolScope)engine).WigglerPool.Create(1f, 0.5f);
            bounce.StartZero = false;
            OwnedComponents.Add(bounce);
            OwnedComponents.Add(shaker = new ShakerComponentState(engine, on: false));
            OnDashCollide = Dashed;
        }

        public FlagBreakerBox(EntityData e, Vector2 levelOffset, ISourceSolidWorldFactory factory,
            ISourceEngineContext engine)
            : this(e.Position + levelOffset, EntityAttributeReader.Bool(e.Values, "flipX", false), factory, engine)
        {
            flag = e.Attr("flag", "");
            aliveState = e.Bool("aliveState", true);
            music = EntityAttributeReader.Attr(e.Values, "music", null);
            musicProgress = EntityAttributeReader.Int(e.Values, "music_progress", -1);
            musicStoreInSession = EntityAttributeReader.Bool(e.Values, "music_session", false);
        }

        public static FlagBreakerBox Load(OriginalLevelSceneRoot level,
            Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room, Vector2 offset,
            Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData data) => new(data, offset,
            ((Celeste.Simulation.Gameplay.Platforms.Collision.ISourcePlatformCollisionScene)level).SolidWorldFactory,
            level.EngineContext);

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (!string.IsNullOrEmpty(flag)) Level.Session.SetFlag(flag, aliveState);
            spikesUp = CollideCheck("Celeste.Spikes", Position - Vector2.UnitY);
            spikesDown = CollideCheck("Celeste.Spikes", Position + Vector2.UnitY);
            spikesLeft = CollideCheck("Celeste.Spikes", Position - Vector2.UnitX);
            spikesRight = CollideCheck("Celeste.Spikes", Position + Vector2.UnitX);
        }
    }
}
