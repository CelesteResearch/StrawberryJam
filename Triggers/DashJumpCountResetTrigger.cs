using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.ExtendedVariants.Configuration;
using Celeste.Mods.ExtendedVariants.Runtime;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.StrawberryJam.Triggers
{
    [CustomEntity("SJ2021/DashJumpCountResetTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Triggers.DashJumpCountResetTrigger")]
    public sealed class DashJumpCountResetTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;

        public DashJumpCountResetTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            var session = ((OriginalLevelSceneRoot)Scene).Session;
            VariantRuntime.For(engine).Enter(Variant.JumpCount, 1, false, false, true);
            session.Inventory.Dashes = 1;
        }
    }
}
