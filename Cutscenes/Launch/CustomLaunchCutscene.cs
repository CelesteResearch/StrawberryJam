using Celeste.Core.Display;
using Celeste.Game.Presentation.Backdrops.Blackhole;
using Celeste.Graphics.Display;
using System;
using Celeste.Core.Randomness;
using Celeste.Game.UI.Dialogue.Textboxes;
using System.Collections;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Rendering;
using Celeste.Core.Mathematics.Easing;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Levels.Cutscenes;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Profiles;
using Celeste.Game.Entities.BadelineBoosts;
using Celeste.Game.Entities.Birds;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collectibles.Strawberry;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Presentation.Backdrops.Blackhole;
using Celeste.Simulation.Presentation.Backdrops.Parallax;
using Celeste.Game.Entities.Ascension;
using Celeste.Simulation.Presentation.Scene.Transitions;
using Celeste.Scene.Rendering;
using Celeste.Graphics.Content;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Display.Colors;

namespace Celeste.Mods.StrawberryJam.Cutscenes.Launch
{
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Cutscenes.CS_CustomLaunch")]
    public sealed partial class CustomLaunchCutscene : OriginalCutsceneEntityState
    {
        private RoomPlayerSession player;

        private Celeste.Mods.StrawberryJam.Entities.Boosters.CustomBadelineBoost boost;

        private BirdNpcState bird;

        private float fadeToColor;

        private Vector2 birdScreenPosition;

        private Celeste.Mods.StrawberryJam.Entities.Ascension.CustomAscendManager.Streaks streaks;

        private Vector2 cameraWaveOffset;

        private Vector2 cameraOffset;

        private float timer;

        private SimulationCoroutine wave;
        private readonly ISourceEngineContext engine;

        private string room;
        private Vector2 spawnPoint;
        private PlayerIntroType transitionType;
        private string colorGrade;
        private PackedColor wipeColor;

        public CustomLaunchCutscene(ISourceEngineContext engine, RoomPlayerSession player,
            Celeste.Mods.StrawberryJam.Entities.Boosters.CustomBadelineBoost boost, string room, Vector2 spawnPoint,
            string colorGrade, PlayerIntroType transitionType, PackedColor wipeColor)
            : base(engine, fadeInOnSkip: false)
        {
            this.engine = engine;
            this.player = player;
            this.boost = boost;
            Depth = 10010;
            this.room = room;
            this.colorGrade = colorGrade;
            this.spawnPoint = spawnPoint;
            this.transitionType = transitionType;
            this.wipeColor = wipeColor;
        }

        public override void OnBegin(OriginalLevelSceneRoot level)
        {
            ((IOriginalGameplayProcess)engine).AudioSession.ChannelOutput.SetMusic(null);
            ((ISourceScreenWipeEnvironment)engine).ProcessWipe.WipeColor = wipeColor;
            OwnedComponents.Add(new SimulationCoroutine(engine, Cutscene()));
        }

        private Celeste.Mods.StrawberryJam.Entities.Ascension.CustomAscendManager GetAscendManager(
            RoomPlayerSession player)
        {
            var entities =
                ((Celeste.Scene.Tracking.ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                    typeof(Celeste.Mods.StrawberryJam.Entities.Ascension.CustomAscendManager));
            Celeste.Mods.StrawberryJam.Entities.Ascension.CustomAscendManager manager = null;
            float best = 0f;
            foreach (Celeste.Mods.StrawberryJam.Entities.Ascension.CustomAscendManager item in entities)
            {
                float distance =
                    ((Celeste.Core.Mathematics.ISourceVectorEnvironment)engine).VectorArithmetic.DistanceSquared(
                        player.Position, item.Position);
                if (manager == null || distance < best)
                {
                    manager = item;
                    best = distance;
                }
            }

            return manager;
        }
    }
}
