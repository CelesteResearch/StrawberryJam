// SPDX-License-Identifier: MIT

using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Mods.StrawberryJam.Runtime.Enemies;
using Celeste.Mods.StrawberryJam.Persistence;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.StrawberryJam.Triggers.Enemies
{
    [CustomEntity("SJ2021/OshiroAttackTimeTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Triggers.OshiroAttackTimeTrigger")]
    public class OshiroAttackTimeTrigger : SourceTriggerEntityState
    {
        private readonly OshiroAttackTimeBinding binding;
        private bool Enable = true;

        public OshiroAttackTimeTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            binding = OshiroAttackTimeBinding.For(engine);
            Enable = data.Bool("Enable", true);
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            ((StrawberryJamSession)binding.Module._Session).OshiroBSideMode = Enable;
        }
    }
}
