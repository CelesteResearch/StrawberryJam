using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Simulation;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Runtime.Fields.Dashes
{
    internal sealed class DashBoostFieldRuntimeState
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, DashBoostFieldRuntimeState> states = new();
        internal float CurrentTimeRateMult = 1f;
        internal ParticleTypePresentation P_RedRefill;
        internal readonly ConditionalWeakTable<RoomPlayerSession, DashBoostPlayerState> Players = new();

        internal static DashBoostFieldRuntimeState For(ISourceEngineContext engine) =>
            states.GetValue(engine, _ => new());
    }
}
