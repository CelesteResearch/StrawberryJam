using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;
using Celeste.Mods.StrawberryJam.Entities.Bubbles;

namespace Celeste.Mods.StrawberryJam.Triggers.Bubbles
{
    [CustomEntity("SJ2021/bubbleEmitterFireTrigger")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias(
        "Celeste.Mod.StrawberryJam2021.Triggers.BubbleEmitterFireTrigger")]
    public class BubbleEmitterFireTrigger : SourceTriggerEntityState
    {
        private readonly string flag;
        private readonly bool onlyOnce;

        public BubbleEmitterFireTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
            data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            flag = data.Attr("flag");
            onlyOnce = data.Bool("onlyOnce");
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            foreach (FloatingBubbleEmitter emitter in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(FloatingBubbleEmitter)))
                if (flag == "" || flag == emitter.flag)
                    emitter.Fire();
            if (onlyOnce) RemoveSelf();
        }

        public static BubbleEmitterFireTrigger Load(OriginalLevelSceneRoot level,
            Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room, Vector2 offset,
            Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData data) => new(level.EngineContext, data, offset);
    }
}
