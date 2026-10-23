using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Environment.Core;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.StrawberryJam.Triggers
{
    [CustomEntity("SJ2021/CoreModeTriggerNoFlash")]
    public sealed class CoreModeTriggerNoFlash : SourceTriggerEntityState
    {
        public RoomCoreMode Mode { get; }

        public CoreModeTriggerNoFlash(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            Mode = data.Enum("mode", RoomCoreMode.None);
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            var level = (OriginalLevelSceneRoot)Scene;
            if (level.CoreMode == Mode) return;
            level.CoreMode = Mode;
        }
    }
}
