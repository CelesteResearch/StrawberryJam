using System.Collections;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Levels.Cutscenes;
using Celeste.Game.UI.Dialogue.Textboxes;
using Celeste.Scene.Tracking;
using Celeste.Simulation;

namespace Celeste.Mods.StrawberryJam.Cutscenes
{
    public sealed class AutoSkipDialogCutscene : OriginalCutsceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly RoomPlayerSession player;
        private readonly string dialogId;
        private readonly bool endLevel;

        public AutoSkipDialogCutscene(ISourceEngineContext engine, string dialogId, RoomPlayerSession player,
            bool endLevel)
            : base(engine)
        {
            this.engine = engine;
            this.player = player;
            this.dialogId = dialogId;
            this.endLevel = endLevel;
        }

        public override void OnBegin(OriginalLevelSceneRoot level) =>
            Add(new SimulationCoroutine(engine, Cutscene(level)));

        private IEnumerator Cutscene(OriginalLevelSceneRoot level)
        {
            player.Machine.State = 11;
            player.Machine.Locked = true;
            player.ForceCameraUpdate = true;
            yield return TextboxState.Say(dialogId, engine, StartSkipping, StopSkipping);
            EndCutscene(level);
        }

        private IEnumerator StartSkipping()
        {
            ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<TextboxState>().AutoPressContinue = true;
            yield break;
        }

        private IEnumerator StopSkipping()
        {
            ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<TextboxState>().AutoPressContinue = false;
            yield break;
        }

        public override void OnEnd(OriginalLevelSceneRoot level)
        {
            player.Machine.Locked = false;
            player.Machine.State = 0;
            player.ForceCameraUpdate = false;
            if (WasSkipped) level.Camera.Position = player.CameraTarget;
            if (!endLevel) return;
            level.CompleteArea(true, false);
            player.Machine.State = 11;
            RemoveSelf();
        }
    }
}
