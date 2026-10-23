// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Mods.StrawberryJam.Modules;

namespace Celeste.Mods.StrawberryJam.Runtime.Enemies
{
    internal sealed class OshiroAttackTimeBinding
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, OshiroAttackTimeBinding> owners = new();
        public StrawberryJamModule Module;

        public static OshiroAttackTimeBinding For(ISourceEngineContext engine) =>
            owners.GetValue(engine, _ => new OshiroAttackTimeBinding());

        public static void Bind(ISourceEngineContext engine, StrawberryJamModule module) => For(engine).Module = module;
    }
}
