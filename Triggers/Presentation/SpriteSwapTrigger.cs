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
using Celeste.Scene.Entities;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.StrawberryJam.Triggers.Presentation
{
    [CustomEntity("SJ2021/SpriteSwapTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Triggers.SpriteSwapTrigger")]
    public class SpriteSwapTrigger : SourceTriggerEntityState
    {
        public SpriteSwapTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
            data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new SpriteSwapTrigger(level.EngineContext, data, offset);
    }
}
