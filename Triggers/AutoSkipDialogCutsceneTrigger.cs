using Celeste.Core.Reflection.Runtime;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Maps;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.StrawberryJam.Cutscenes;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.StrawberryJam.Triggers
{
    [RuntimeTypeAlias("Celeste.Mod.Entities.AutoSkipDialogCutsceneTrigger")]
    [CustomEntity("SJ2021/AutoSkipDialogCutsceneTrigger")]
    public sealed class AutoSkipDialogCutsceneTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly string dialogEntry;
        private readonly MapEntityId id;
        private readonly bool onlyOnce;
        private readonly bool endLevel;
        private readonly int deathCount;
        private bool triggered;

        public AutoSkipDialogCutsceneTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset,
            MapEntityId id)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            this.id = id;
            dialogEntry = data.Attr("dialogId");
            onlyOnce = data.Bool("onlyOnce", true);
            endLevel = data.Bool("endLevel", false);
            deathCount = data.Int("deathCount", -1);
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            var level = (OriginalLevelSceneRoot)Scene;
            if (triggered || level.Session.GetFlag("DoNotLoad" + id) ||
                (deathCount >= 0 && level.Session.DeathsInCurrentLevel != deathCount)) return;
            triggered = true;
            ((ISourceScene)Scene).SourceScene.AddEntity(new AutoSkipDialogCutscene(engine, dialogEntry, player,
                endLevel));
            if (onlyOnce) level.Session.SetFlag("DoNotLoad" + id, true);
        }
    }
}
