using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Mods.ExtendedVariants.Configuration;
using Celeste.Mods.ExtendedVariants.Runtime;
using Celeste.Mods.StrawberryJam.Persistence;

namespace Celeste.Mods.StrawberryJam.Runtime.Gravity
{
    public sealed class ZeroGRuntime
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, ZeroGRuntime> Instances = new();
        private readonly VariantRuntime variants;
        private Func<StrawberryJamSession> session;
        internal int SoftlockFrames;
        public ISourceEngineContext Engine { get; }
        public StrawberryJamSession Session => session();

        private ZeroGRuntime(ISourceEngineContext engine)
        {
            Engine = engine;
            variants = VariantRuntime.For(engine);
        }

        public static ZeroGRuntime For(ISourceEngineContext engine) =>
            Instances.GetValue(engine, owner => new ZeroGRuntime(owner));

        public void Bind(Func<StrawberryJamSession> provider) => session = provider;

        public void SetZeroG(bool on)
        {
            variants.Enter(Variant.Gravity, on ? 0f : 1f, false, false, false);
            variants.Enter(Variant.AirFriction, on ? 0f : 1f, false, false, false);
            Session.ZeroG = on;
        }
    }
}
