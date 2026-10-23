// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Mods.Crystalline.Runtime;

namespace Celeste.Mods.StrawberryJam.Runtime.Audio
{
    internal sealed class TimeFreezeMusicBinding
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, TimeFreezeMusicBinding> owners = new();
        public TimeStopState State;

        public static TimeFreezeMusicBinding For(ISourceEngineContext engine) =>
            owners.GetValue(engine, _ => new TimeFreezeMusicBinding());
    }
}
