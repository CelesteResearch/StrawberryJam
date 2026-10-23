using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Runtime.Boosters
{
    public sealed partial class WormholeBoosterProcess : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, WormholeBoosterProcess> Owners = new();
        private readonly List<IDisposable> hooks = new();
        public ISourceEngineContext Engine { get; }

        public StrawberryJamModule Module
        {
            get
            {
                foreach (var module in ((ISourceEverestModulesEnvironment)Engine).Modules)
                    if (module is StrawberryJamModule owner)
                        return owner;
                throw new InvalidOperationException("The Strawberry Jam module is not registered in this engine.");
            }
        }

        public bool TeleDeath, CanTeleport;
        public ParticleTypePresentation PTeleporting { get; private set; }
        public ParticleTypePresentation PBurst { get; private set; }
        public ParticleTypePresentation PAppear { get; private set; }
        private WormholeBoosterProcess(ISourceEngineContext engine) => Engine = engine;

        public static WormholeBoosterProcess For(ISourceEngineContext engine) =>
            Owners.GetValue(engine, owner => new WormholeBoosterProcess(owner));

        public static WormholeBoosterProcess Install(ISourceEngineContext engine)
        {
            var owner = For(engine);
            try
            {
                owner.InstallHooks();
                return owner;
            }
            catch
            {
                owner.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
        }
    }
}
