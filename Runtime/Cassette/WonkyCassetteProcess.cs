using System;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Runtime.Events.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.StrawberryJam.Entities.Platforms.Cassette;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Player.Intro;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Cassette
{
    public sealed class WonkyCassetteProcess : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly StrawberryJamModule module;
        private readonly Hook levelLoadHook;

        private delegate void LevelLoad(OriginalLevelSceneRoot self, PlayerIntroType intro, bool fromLoader);

        public WonkyCassetteProcess(StrawberryJamModule module, ISourceEngineContext engine)
        {
            this.module = module;
            this.engine = engine;
            levelLoadHook = new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.LoadLevel),
                    new[] { typeof(PlayerIntroType), typeof(bool) }),
                (Action<LevelLoad, OriginalLevelSceneRoot, PlayerIntroType, bool>)OnLevelLoad);
            EverestLevelFreezeEvents.OnBeforeFreezeFrameUpdate += OnFreezeUpdate;
        }

        private void OnLevelLoad(LevelLoad orig, OriginalLevelSceneRoot level, PlayerIntroType intro, bool fromLoader)
        {
            if (!ReferenceEquals(engine, level.EngineContext))
            {
                orig(level, intro, fromLoader);
                return;
            }

            WonkyCassetteConnectionRegistry.Clear(engine);
            orig(level, intro, fromLoader);
            var session = module.Session;
            var main = level.SourceScene.Entities.GetTrackedEntity<WonkyCassetteBlockController>();
            var minorControllers =
                level.SourceScene.Entities.GetTrackedEntities(typeof(WonkyMinorCassetteBlockController));
            foreach (WonkyCassetteBlock block in level.SourceScene.Entities.GetTrackedEntities(
                         typeof(WonkyCassetteBlock)))
            {
                if (block.ControllerIndex == 0)
                    block.SetActivatedSilently(main != null && !session.CassetteBlocksDisabled &&
                                               Array.IndexOf(block.OnAtBeats,
                                                   session.CassetteWonkyBeatIndex / (16 / main.beatLength) %
                                                   main.barLength) >= 0);
                else
                    foreach (WonkyMinorCassetteBlockController minor in minorControllers)
                        if (block.ControllerIndex == minor.ControllerIndex)
                            block.SetActivatedSilently(!session.CassetteBlocksDisabled &&
                                                       Array.IndexOf(block.OnAtBeats,
                                                           minor.CassetteWonkyBeatIndex / (16 / minor.beatLength) %
                                                           minor.barLength) >= 0);
            }
        }

        private void OnFreezeUpdate(EverestLevelFreezeContext context)
        {
            if (context.NativeLevel is not OriginalLevelSceneRoot level ||
                !ReferenceEquals(level.EngineContext, engine)) return;
            level.SourceScene.Entities.GetTrackedEntity<WonkyCassetteBlockController>()
                ?.AdvanceMusic(engine.DeltaTime, level, module.Session);
            foreach (WonkyCassetteBlock block in level.SourceScene.Entities.GetTrackedEntities(
                         typeof(WonkyCassetteBlock))) block.Update();
        }

        public void Dispose()
        {
            EverestLevelFreezeEvents.OnBeforeFreezeFrameUpdate -= OnFreezeUpdate;
            levelLoadHook.Dispose();
            WonkyCassetteConnectionRegistry.Remove(engine);
        }
    }
}
