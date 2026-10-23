using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Mods.StrawberryJam.Runtime.Weather;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.StrawberryJam.Triggers.Weather
{
    [CustomEntity("SJ2021/RainDensityTrigger")]
    public sealed class RainDensityTrigger : SourceTriggerEntityState
    {
        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new RainDensityTrigger(level.EngineContext, data, offset);

        private readonly IRainDensityEnvironment environment;
        private readonly float triggerEndDensity;
        private readonly float triggerDuration;

        public RainDensityTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            environment = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<IRainDensityEnvironment>().Single();
            var extrema = ((ISourceExtremaEnvironment)engine).ExtremaArithmetic;
            triggerEndDensity = ScalarMath.Clamp(data.Float("density", 0f), 0f, 1f, extrema);
            triggerDuration = extrema.Max(data.Float("duration", 0f), .01f);
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            var data = environment.RainDensityData;
            data.StartDensity = data.Density;
            data.EndDensity = triggerEndDensity;
            data.Duration = triggerDuration;
        }
    }
}
