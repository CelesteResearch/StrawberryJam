using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;

namespace Celeste.Mods.StrawberryJam.Runtime.Cassette
{
    internal sealed class WonkyCassetteConnectionRegistry
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, WonkyCassetteConnectionRegistry> Registries =
            new();

        public Dictionary<string, WonkyCassetteConnectionGrid> Connections { get; } = new(StringComparer.Ordinal);

        public static WonkyCassetteConnectionRegistry For(ISourceEngineContext engine) =>
            Registries.GetValue(engine, _ => new WonkyCassetteConnectionRegistry());

        public static void Clear(ISourceEngineContext engine) => For(engine).Connections.Clear();

        public static void Remove(ISourceEngineContext engine)
        {
            if (Registries.TryGetValue(engine, out var value)) value.Connections.Clear();
            Registries.Remove(engine);
        }
    }
}
