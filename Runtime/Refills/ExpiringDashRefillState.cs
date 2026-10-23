using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Mods.StrawberryJam.Persistence;

namespace Celeste.Mods.StrawberryJam.Runtime.Refills
{
    internal sealed class ExpiringDashRefillState
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, ExpiringDashRefillState> states = new();
        internal Func<StrawberryJamSession> GetSession;
        internal bool ExpireFlash;
        internal StrawberryJamSession Session => GetSession();
        internal static ExpiringDashRefillState For(ISourceEngineContext engine) => states.GetValue(engine, _ => new());
    }
}
