using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Everest.Gameplay.Platforms.Cassette;
using Celeste.Game.Gameplay.Platforms.Cassette;
using Celeste.Mods.StrawberryJam.Components.Cassette;
using Celeste.Scene.Tracking;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Mono.Cecil.Cil;

namespace Celeste.Mods.StrawberryJam.Runtime.Cassette
{
    public sealed class CassetteListenerProcess : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly string trackerKey;
        private readonly List<IDisposable> hooks = new();

        public CassetteListenerProcess(ISourceEngineContext engine)
        {
            this.engine = engine;
            trackerKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(CassetteListener))
                .RegistryKey;
            foreach (var type in new[]
                         { typeof(OriginalCassetteBlockManagerState), typeof(InstalledCassetteBlockManagerState) })
                Install(type);
        }

        private void Install(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                       BindingFlags.DeclaredOnly;
            hooks.Add(new ILHook(type.GetMethod("AdvanceMusic", flags), il =>
            {
                var c = new ILCursor(il);
                var field = typeof(OriginalCassetteBlockManagerState).GetField("leadBeats",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (!c.TryGotoNext(MoveType.AfterLabel, i => i.MatchLdarg(0), i => i.MatchLdfld(field),
                        i => i.MatchLdcI4(0)))
                    throw new InvalidOperationException("The cassette lead-beat boundary was not found.");
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<OriginalCassetteBlockManagerState>>(OffBeat);
            }));
            hooks.Add(new Hook(type.GetMethod("StopBlocks", flags),
                (Action<Action<OriginalCassetteBlockManagerState>, OriginalCassetteBlockManagerState>)Stop));
            hooks.Add(new Hook(type.GetMethod("SilentUpdateBlocks", flags),
                (Action<Action<OriginalCassetteBlockManagerState>, OriginalCassetteBlockManagerState>)Silent));
            hooks.Add(new Hook(type.GetMethod("SetActiveIndex", flags),
                (Action<Action<OriginalCassetteBlockManagerState, int>, OriginalCassetteBlockManagerState, int>)
                Active));
            hooks.Add(new Hook(type.GetMethod("SetWillActivate", flags),
                (Action<Action<OriginalCassetteBlockManagerState, int>, OriginalCassetteBlockManagerState, int>)Will));
        }

        private List<SourceComponentState> Components(OriginalCassetteBlockManagerState manager) =>
            manager.Scene is ISourceEngineContextScene scoped && ReferenceEquals(scoped.EngineContext, engine)
                ? ((ISourceScene)manager.Scene).SourceScene.Components.GetTrackedComponents(trackerKey)
                : null;

        private void OffBeat(OriginalCassetteBlockManagerState self)
        {
            int beats = self is InstalledCassetteBlockManagerState installed ? installed.BeatsPerTick : 4;
            int ticks = self is InstalledCassetteBlockManagerState manager ? manager.TicksPerSwap : 2;
            if (self.BeatIndex % beats != 0 || self.BeatIndex % (beats * ticks) == 0) return;
            var components = Components(self);
            if (components == null) return;
            foreach (CassetteListener component in components) component.OnTick?.Invoke(self.CurrentIndex, false);
        }

        private void Stop(Action<OriginalCassetteBlockManagerState> original, OriginalCassetteBlockManagerState self)
        {
            original(self);
            var components = Components(self);
            if (components == null) return;
            foreach (CassetteListener component in components) component.OnFinish?.Invoke();
        }

        private void Silent(Action<OriginalCassetteBlockManagerState> original, OriginalCassetteBlockManagerState self)
        {
            original(self);
            var components = Components(self);
            if (components == null) return;
            foreach (CassetteListener component in components)
                component.OnSilentUpdate?.Invoke(component.Index == self.CurrentIndex);
        }

        private void Active(Action<OriginalCassetteBlockManagerState, int> original,
            OriginalCassetteBlockManagerState self, int index)
        {
            original(self, index);
            var components = Components(self);
            if (components == null) return;
            foreach (CassetteListener component in components)
            {
                if (component.Activated && component.Index != index)
                {
                    component.Activated = false;
                    component.OnDeactivated?.Invoke();
                }
                else if (!component.Activated && component.Index == index)
                {
                    component.Activated = true;
                    component.OnActivated?.Invoke();
                }

                component.OnTick?.Invoke(index, true);
            }
        }

        private void Will(Action<OriginalCassetteBlockManagerState, int> original,
            OriginalCassetteBlockManagerState self, int index)
        {
            original(self, index);
            var components = Components(self);
            if (components == null) return;
            foreach (CassetteListener component in components)
                if (component.Index == index || component.Activated)
                    component.OnWillToggle?.Invoke();
        }

        public void Dispose()
        {
            for (int i = 0; i < hooks.Count; i++) hooks[i].Dispose();
            hooks.Clear();
        }
    }
}
