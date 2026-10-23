using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.StrawberryJam.Backdrops.Meteors;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.StrawberryJam.Triggers.Meteors
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Triggers.HeartCometActivationTrigger")]
    [CustomEntity("SJ2021/HeartCometActivationTrigger")]
    public class HeartCometActivationTrigger : SourceTriggerEntityState
    {
        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new HeartCometActivationTrigger(level.EngineContext, data, offset);

        public HeartCometActivationTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            foreach (var item in ((OriginalLevelSceneRoot)Scene).Background.GetEach<HeartComet>())
                item.Activated = true;
        }
    }
}
