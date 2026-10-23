using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.StrawberryJam.Triggers
{
    [CustomEntity("SJ2021/ConditionalFlagTrigger")]
    [SceneTracked]
    public sealed class ConditionalFlagTrigger : SourceTriggerEntityState
    {
        private readonly string flag;
        private readonly string controllerFlag;
        private readonly bool flagValue;
        private readonly bool revertOnLeave;
        private bool priorState;

        public ConditionalFlagTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            flag = data.Attr("flag");
            flagValue = data.Bool("flagValue", true);
            revertOnLeave = data.Bool("revertOnLeave", true);
            controllerFlag = data.Attr("controllerFlag");
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            if (string.IsNullOrEmpty(flag)) return;
            var session = ((OriginalLevelSceneRoot)Scene).Session;
            ConditionalFlagTrigger active = GetActiveTrigger();
            priorState = active == null ? session.GetFlag(flag) : active.priorState;
            session.SetFlag(flag, flagValue);
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            base.OnLeave(player);
            if (string.IsNullOrEmpty(flag) || !revertOnLeave) return;
            if (!FoundActiveTrigger()) ((OriginalLevelSceneRoot)Scene).Session.SetFlag(flag, priorState);
        }

        public override void Update()
        {
            base.Update();
            if (!string.IsNullOrEmpty(controllerFlag))
                Collidable = ((OriginalLevelSceneRoot)Scene).Session.GetFlag(controllerFlag);
        }

        private List<object> TrackedTriggers => ((ISourceScene)Scene).SourceScene.Entities
            .GetTrackedEntities(typeof(ConditionalFlagTrigger));

        private ConditionalFlagTrigger GetActiveTrigger()
        {
            foreach (ConditionalFlagTrigger current in TrackedTriggers)
                if (current != this && current.Triggered && current.flag == flag)
                    return current;
            return null;
        }

        private bool FoundActiveTrigger()
        {
            bool found = false;
            foreach (ConditionalFlagTrigger current in TrackedTriggers)
                if (current != this && current.Triggered && current.flag == flag)
                    found = true;
            return found;
        }
    }
}
