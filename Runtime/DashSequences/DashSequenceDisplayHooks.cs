// SPDX-License-Identifier: MIT

using System;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.StrawberryJam.Configuration;
using Celeste.Mods.StrawberryJam.Entities.Controllers.DashSequences;
using Celeste.Simulation.Gameplay.Player.Intro;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.DashSequences
{
    public sealed class DashSequenceDisplayHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly Func<StrawberryJamSettings> settings;
        private readonly Hook hook;

        public DashSequenceDisplayHooks(ISourceEngineContext engine, Func<StrawberryJamSettings> settings)
        {
            this.engine = engine;
            this.settings = settings;
            hook = new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.LoadLevel)),
                (Action<Action<OriginalLevelSceneRoot, PlayerIntroType, bool>, OriginalLevelSceneRoot, PlayerIntroType,
                    bool>)LoadLevel);
        }

        private void LoadLevel(Action<OriginalLevelSceneRoot, PlayerIntroType, bool> orig, OriginalLevelSceneRoot level,
            PlayerIntroType intro, bool fromLoader)
        {
            orig(level, intro, fromLoader);
            if (!ReferenceEquals(level.EngineContext, engine)) return;
            var display = level.Scheduler.Entities.FindFirst<DashSequenceDisplay>();
            if (display == null) level.AddEntity(display = new DashSequenceDisplay(engine, settings));
            display.InitializeDashCodes(level);
        }

        public void Dispose() => hook.Dispose();
    }
}
