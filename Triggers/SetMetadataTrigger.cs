using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Maps.Processing;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.StrawberryJam.Triggers
{
    [CustomEntity("SJ2021/SetMetadataTrigger")]
    public sealed class SetMetadataTrigger : SourceTriggerEntityState
    {
        private readonly IEverestChapterMapProcessingEnvironment processing;
        private readonly bool theoInBooster;

        public SetMetadataTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            processing = (IEverestChapterMapProcessingEnvironment)engine;
            theoInBooster = data.Bool("theoInBooster", false);
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            var map = (OriginalChapterMapData)((OriginalLevelSceneRoot)Scene).Session.MapData;
            processing.MapProcessing.GetModeMeta(map).TheoInBubble = theoInBooster;
        }
    }
}
