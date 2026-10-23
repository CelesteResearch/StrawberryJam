using Celeste.Core.Display;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Graphics.Display;
using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Animation;
using Celeste.Core.Components.Timing;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Controls.Source;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Components.Oscillation;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Camera;
using Celeste.Simulation.Gameplay.Collectibles.Strawberry;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Gameplay.Player.Presentation;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Simulation.Presentation.Effects.Trails.Lifecycle;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Rendering;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Profiles;
using Celeste.Game.Entities.BadelineDummy;
using Celeste.Game.Stories.Farewell.FinalLaunch;

namespace Celeste.Mods.StrawberryJam.Entities.Boosters
{
    [Celeste.Everest.Modules.Entities.Metadata.CustomEntity("SJ2021/CustomBadelineBoost")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.CustomBadelineBoost")]
    public partial class CustomBadelineBoost : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly Sprite sprite;
        private readonly SpriteImage stretch;
        private readonly WigglerComponent wiggler;
        private readonly VertexLightState light;
        private readonly BloomPointState bloom;
        private Vector2 spawnPoint;
        private string room;
        private string colorGrade;
        private Celeste.Simulation.Gameplay.Player.Intro.PlayerIntroType transitionType;
        private PackedColor wipeColor;
        private bool forceCameraUpdate;
        private readonly Vector2[] nodes;
        private int nodeIndex;
        private bool travelling;
        private RoomPlayerSession holding;
        private readonly SoundSourceComponentState relocateSfx;
        public global::Celeste.Audio.Fmod.Events.FmodEventReference FinalBoostSfx;
        private OriginalLevelSceneRoot CurrentLevel => (OriginalLevelSceneRoot)Scene;
        private IOriginalGameplayProcess Process => (IOriginalGameplayProcess)engine;
        private ISourceGameGraphicsResources Graphics => ((ISourceGameGraphicsEnvironment)engine).GameGraphics;
        private ISourceVectorArithmetic VectorArithmetic => ((ISourceVectorEnvironment)engine).VectorArithmetic;

        private CustomBadelineBoost(ISourceEngineContext engine, Vector2[] nodes)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = engine;
            Position = nodes[0];
            Depth = -1000000;
            this.nodes = nodes;
            Collider = new global::Celeste.Physics.Collision.Shapes.CircleColliderGeometry(
                ((ISourceVectorEnvironment)engine).VectorArithmetic, 16f);
            OwnedComponents.Add(new PlayerColliderComponentState(OnPlayer));
            sprite = Graphics.SpriteFactory.Create("badelineBoost");
            OwnedComponents.Add(sprite);
            stretch = new SpriteImage(Graphics.GameAtlas.Entries["objects/badelineboost/stretch"]);
            OwnedComponents.Add(stretch);
            stretch.Visible = false;
            stretch.CenterOrigin();
            light = new VertexLightState(engine, Vector2.Zero, PackedColor.White, .7f, 12, 20);
            OwnedComponents.Add(light);
            bloom = new BloomPointState(.5f, 12f);
            OwnedComponents.Add(bloom);
            wiggler = ((ISourceWigglerPoolScope)engine).WigglerPool.Create(.4f, 3f,
                _ => sprite.Scale = Vector2.One * (float)(1d + (double)wiggler.Value * .4d));
            OwnedComponents.Add(wiggler);
            relocateSfx = ((ISourceSoundSourceEnvironment)engine).SoundSources.Create();
            OwnedComponents.Add(relocateSfx);
        }

        public CustomBadelineBoost(ISourceEngineContext engine, Celeste.Formats.Map.Entities.Structure.EntityData data,
            Vector2 offset)
            : this(engine, data.NodesWithPosition(offset))
        {
            room = data.Attr("room", "");
            colorGrade = data.Attr("colorGrade", "");
            wipeColor = Celeste.Graphics.Display.Colors.SourceHexColor.HexToColor(data.Attr("wipeColor", "ffffff"));
            spawnPoint = new Vector2(data.Int("spawnPointX", 0), data.Int("spawnPointY", 0));
            transitionType = data.Enum<Celeste.Simulation.Gameplay.Player.Intro.PlayerIntroType>("transitionType");
            forceCameraUpdate = data.Bool("forceCameraUpdate", false);
        }

        public override void Awake(object actualScene)
        {
            base.Awake(actualScene);
            if (SourceTrackedEntityCollision.First<object>(((ISourceScene)Scene).SourceScene.Entities,
                    "Celeste.FakeWall",
                    this) != null) Depth = -12500;
        }

        private void OnPlayer(RoomPlayerSession player) => Add(new SimulationCoroutine(engine, BoostRoutine(player)));
        private void Add(SimulationCoroutine coroutine) => OwnedComponents.Add(coroutine);
        private void Add(SourceAlarmState alarm) => OwnedComponents.Add(alarm);
        private void Add(SourceTweenState tween) => OwnedComponents.Add(tween);

        private global::Celeste.Audio.Fmod.Events.FmodEventReference PlaySound(string path, Vector2 position)
            => Process.AudioSession.Play(path, position, Process.SaveContext);

        private RoomPlayerSession FirstPlayer()
        {
            var list = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                typeof(global::Celeste.Simulation.RoomPlayerSession).FullName);

            return list.Count == 0 ? null : (RoomPlayerSession)list[0];
        }
    }
}
