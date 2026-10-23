// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Runtime.Startup;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Runtime;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Rendering;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.Particles;
using Celeste.Graphics.Particles;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.StrawberryJam.Entities.Collectibles.Cassettes
{
    [CustomEntity("SJ2021/FakeCassette")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.FakeCassette")]
    public partial class FakeCassette : SceneEntityState
    {
        private readonly GameApplication engine;

        public Celeste.Simulation.Presentation.Particles.Definitions.ParticleTypePresentation P_Shine =>
            engine.ParticleTypes.Cassette.P_Shine;

        public Celeste.Simulation.Presentation.Particles.Definitions.ParticleTypePresentation P_Collect =>
            engine.ParticleTypes.Cassette.P_Collect;

        private Sprite sprite;

        private SineWaveState hover;

        private BloomPointState bloom;

        private VertexLightState light;

        private WigglerComponent scaleWiggler;

        private bool collected;

        private bool collecting;

        private string collectAudioEventName, flagOnCollect;

        private ISourceAudioEventReference collectAudioEvent;

        private FakeCassetteShatterMessage message;

        public FakeCassette(ISourceEngineContext engine, Vector2 position)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = (GameApplication)engine;
            base.Collider = new HitboxColliderGeometry(16f, 16f, -8f, -8f);
            Add(new PlayerColliderComponentState(OnPlayer));
        }

        public FakeCassette(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data.Position + offset)
        {
            collectAudioEventName = data.Attr("remixEvent");
            flagOnCollect = data.Attr("flagOnCollect");
        }

        public override void Added(object scene)
        {
            if ((scene as OriginalLevelSceneRoot).Session.GetFlag(flagOnCollect))
            {
                RemoveSelf();
                return;
            }

            base.Added(scene);
            Add(sprite = ((OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics)
                .SpriteBank.Create("cassette"));
            sprite.Play("idle");
            Add(scaleWiggler = ((ISourceWigglerPoolScope)engine).WigglerPool.Create(0.25f, 4f,
                delegate(float f) { sprite.Scale = Vector2.One * (1f + f * 0.25f); }));
            Add(bloom = new BloomPointState(0.25f, 16f));
            Add(light = new VertexLightState(engine, PackedColor.White, 0.4f, 32, 64));
            Add(hover = new SineWaveState(engine, 0.5f, 0f));
            hover.OnUpdate = delegate(float f)
            {
                Sprite obj = sprite;
                VertexLightState vertexLight = light;
                float num2 = (bloom.Y = f * 2f);
                float num5 = (obj.Y = (vertexLight.Y = num2));
            };
        }

        public override void Update()
        {
            base.Update();
            if (!collecting && Scene != null && ((ISourceScene)Scene).SourceScene.Clock.OnInterval(0.1f, engine))
            {
                ((OriginalLevelSceneRoot)Scene).Particles.Emit(P_Shine, 1, base.Center, new Vector2(12f, 10f));
            }
        }

        private void OnPlayer(RoomPlayerSession player)
        {
            if (!collected)
            {
                player?.RefillStamina();
                collectAudioEvent =
                    ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(collectAudioEventName, Position);
                collected = true;
                ((ISourceFreezeEnvironment)engine).Freeze(0.1f);
                (Scene as OriginalLevelSceneRoot).StartCutscene((level) => SkipCutscene(level, player),
                    fadeInOnSkip: false);
                Add(new SimulationCoroutine(engine, CollectRoutine(player)));
            }
        }
    }
}
