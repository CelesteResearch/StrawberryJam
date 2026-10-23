using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Levels.Cutscenes;
using Celeste.Game.Runtime;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Presentation.Scene.Transitions;
using Celeste.Game.Entities.StarJump;

namespace Celeste.Mods.StrawberryJam.Cutscenes.Pinball
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.PinballFallCutscene")]
    internal class PinballFallCutscene : OriginalCutsceneEntityState
    {
        private readonly GameApplication engine;

        public PinballFallCutscene(GameApplication engine) : base(engine, true, false)
        {
            this.engine = engine;
        }

        public override void OnBegin(OriginalLevelSceneRoot level) =>
            Add(new SimulationCoroutine(engine, Cutscene(level), true));

        private IEnumerator Cutscene(OriginalLevelSceneRoot level)
        {
            level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Bottom));
            var player = level.SourceScene.Entities.FindFirst<RoomPlayerSession>();
            engine.Effects.Glitch = .2f;
            yield return .4f;
            engine.Effects.Glitch = 0f;
            while (!player.OnGround(1)) yield return null;
            engine.MInput.Disabled = false;
            EndCutscene(level, true);
        }

        public override void Update()
        {
            engine.MInput.Disabled = false;
            if (Level.CanPause && (engine.InputSource.Pause.Pressed || engine.InputSource.ESC.Pressed))
            {
                engine.InputSource.Pause.ConsumeBuffer();
                engine.InputSource.ESC.ConsumeBuffer();
                Level.Pause(0, false, false);
            }

            engine.MInput.Disabled = true;
            base.Update();
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            engine.MInput.Disabled = false;
        }

        public override void OnEnd(OriginalLevelSceneRoot level)
        {
            level.OnEndOfFrame += () =>
            {
                if (WasSkipped)
                {
                    ((ISceneEntityRemovalScene)level).RemoveEntity(this);
                    level.UnloadLevel();
                    level.EndCutscene();
                    level.Session.Level = "Tutorial";
                    level.Session.RespawnPoint =
                        level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Bottom));
                    level.LoadLevel((PlayerIntroType)8, false);
                    engine.MInput.Disabled = false;
                    OriginalFallEffectsState.Show(engine, false);
                    level.Wipe.Cancel();
                }
                else
                {
                    engine.MInput.Disabled = false;
                    ((ISceneEntityRemovalScene)level).RemoveEntity(this);
                }
            };
        }
    }
}
