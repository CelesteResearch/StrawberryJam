// SPDX-License-Identifier: MIT

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Movement;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Holdables
{
    public static class NormalFallPhaseHooks
    {
        [ThreadStatic] private static RoomPlayerSession activePlayer;
        private static readonly ConditionalWeakTable<ISourceEngineContext, Ownership> owners = new();
        private static Hook normalCall;
        private static int leases;
        public static RoomPlayerSession ActivePlayer => activePlayer;

        private sealed class Ownership
        {
            public int Leases;
        }

        public static IDisposable Acquire(ISourceEngineContext engine)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (normalCall == null)
                normalCall = new Hook(
                    typeof(NormalSequence).GetMethod("UpdateCore", BindingFlags.Instance | BindingFlags.NonPublic),
                    (Func<Func<NormalSequence, int>, NormalSequence, int>)UpdateNormal);
            owners.GetValue(engine, _ => new Ownership()).Leases++;
            leases++;
            return new Lease(engine);
        }

        private static int UpdateNormal(Func<NormalSequence, int> original, NormalSequence sequence)
        {
            var previous = activePlayer;
            activePlayer = (sequence.World as RoomPlayerWorld)?.SourcePlayer;
            try
            {
                return original(sequence);
            }
            finally
            {
                activePlayer = previous;
            }
        }

        private sealed class Lease : IDisposable
        {
            private ISourceEngineContext engine;
            public Lease(ISourceEngineContext engine) => this.engine = engine;

            public void Dispose()
            {
                if (engine == null) return;
                if (--owners.GetValue(engine, _ => throw new InvalidOperationException("Missing fall phase owner."))
                        .Leases == 0)
                    owners.Remove(engine);
                engine = null;
                if (--leases != 0) return;
                normalCall?.Dispose();
                normalCall = null;
            }
        }
    }
}
