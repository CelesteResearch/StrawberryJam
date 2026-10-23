// SPDX-License-Identifier: MIT

using System;
using System.Reflection;
using Celeste.Mods.Crystalline.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Game.Content.Maps;
using Celeste.Game.Execution.LevelLoader;
using Celeste.Mods.StrawberryJam.Entities.Audio;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Audio
{
    public sealed class TimeFreezeMusicHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly ILHook hook;
        private readonly Hook stateConstructor;
        private readonly TimeFreezeMusicBinding binding;

        public TimeFreezeMusicHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            binding = TimeFreezeMusicBinding.For(engine);
            binding.State = TimeCrystalRuntime.Get(engine).State;
            stateConstructor =
                new Hook(typeof(TimeCrystalRuntime).GetConstructor(new[] { typeof(ISourceEngineContext) }),
                    (Action<Action<TimeCrystalRuntime, ISourceEngineContext>, TimeCrystalRuntime, ISourceEngineContext>)
                    ConstructState);
            try
            {
                hook = new ILHook(
                    typeof(OriginalLevelLoaderScene).GetMethod("LoadingThread",
                        BindingFlags.Instance | BindingFlags.NonPublic), Patch);
            }
            catch
            {
                stateConstructor.Dispose();
                throw;
            }
        }

        private void ConstructState(Action<TimeCrystalRuntime, ISourceEngineContext> original, TimeCrystalRuntime self,
            ISourceEngineContext owner)
        {
            original(self, owner);
            if (ReferenceEquals(owner, engine)) binding.State = self.State;
        }

        private void Patch(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.Before, instruction => instruction.MatchLdarg(0),
                instruction => instruction.MatchLdcI4(1),
                instruction => instruction.MatchCall<OriginalLevelLoaderScene>("set_Loaded"));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<OriginalLevelLoaderScene>>(LoadingThreadModifier);
        }

        public void LoadingThreadModifier(OriginalLevelLoaderScene loader)
        {
            if (!ReferenceEquals(loader.EngineContext, engine)) return;
            foreach (var room in ((OriginalChapterMapData)loader.Level.Session.MapData)?.Levels)
            {
                foreach (var data in room.Entities)
                {
                    if (data.Name == "SJ2021/TimeFreezeMusicController")
                    {
                        loader.Level.SourceScene.QueueSourceEntity(
                            new TimeFreezeMusicController(engine, data, room.Position));
                        return;
                    }
                }
            }
        }

        public void Dispose()
        {
            hook.Dispose();
            stateConstructor.Dispose();
        }
    }
}
