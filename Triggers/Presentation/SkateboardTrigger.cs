using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.StrawberryJam.Runtime.Skateboards;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.StrawberryJam.Triggers.Presentation
{
    [CustomEntity("SJ2021/SkateboardTrigger")]
    [SceneTracked(false)]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Triggers.SkateboardTrigger")]
    public sealed class SkateboardTrigger : SourceTriggerEntityState
    {
        private enum TriggerMode
        {
            Enable,
            Disable,
            Toggle
        }

        private readonly TriggerMode triggerMode;
        private readonly SkateboardHooks hooks;

        public SkateboardTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
            data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            triggerMode = data.Enum("mode", TriggerMode.Enable);
            hooks = SkateboardHooks.For(engine);
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new SkateboardTrigger(level.EngineContext, data, offset);

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            hooks.SetEnabled(triggerMode switch
            {
                TriggerMode.Enable => true, TriggerMode.Disable => false, TriggerMode.Toggle => !hooks.Enabled,
                _ => hooks.Enabled
            });
        }
    }
}
