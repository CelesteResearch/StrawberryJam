using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.StrawberryJam.Runtime.Holdables.Dashes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.StrawberryJam.Triggers.Holdables
{
    [CustomEntity("SJ2021/HoldableDashTrigger")]
    [SceneTracked(false)]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Triggers.HoldableDashTrigger")]
    public class HoldableDashTrigger : SourceTriggerEntityState
    {
        public enum Modes
        {
            EnableOnStay,
            DisableOnStay,
            EnableToggle,
            DisableToggle
        }

        public Modes Mode;
        public const string DashWithHoldableFlag = "sj2021_canDashWithHoldable";

        public HoldableDashTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
            data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            Mode = data.Enum("mode", Modes.EnableOnStay);
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new HoldableDashTrigger(level.EngineContext, data, offset);

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            var session = ((OriginalLevelSceneRoot)Scene).Session;
            if (Mode == Modes.EnableToggle) session.SetFlag(DashWithHoldableFlag, true);
            else if (Mode == Modes.DisableToggle) session.SetFlag(DashWithHoldableFlag, false);
        }

        public static bool CanDashWithHoldable(RoomPlayerSession player) =>
            HoldableDashHooks.For(player.EngineContext).CanDashWithHoldable(player);
    }
}
