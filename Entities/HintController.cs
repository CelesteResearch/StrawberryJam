using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Game.UI.Dialogue.Textboxes;
using Celeste.Mods.StrawberryJam.Runtime;
using Celeste.Scene.Entities;

namespace Celeste.Mods.StrawberryJam.Entities
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.HintController")]
    [CustomEntity("SJ2021/HintController")]
    public sealed class HintController : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        public string DialogId1 { get; }
        public string DialogId2 { get; }
        public bool SingleUse1 { get; }
        public bool SingleUse2 { get; }
        public string ToggleFlag { get; }
        public string DialogId => ToggleFlagValue ? DialogId2 : DialogId1;
        public bool SingleUse => ToggleFlagValue ? SingleUse2 : SingleUse1;
        private OriginalLevelSceneRoot CurrentLevel => engine.Frame.Scenes.CurrentScene as OriginalLevelSceneRoot;
        private bool ToggleFlagValue => CurrentLevel?.Session.GetFlag(ToggleFlag) ?? false;

        private string UsedFlag =>
            $"HintControllerUsed:{CurrentLevel?.Session.Level ?? string.Empty}:{ToggleFlagValue}";

        public bool UsedFlagValue => CurrentLevel?.Session.GetFlag(UsedFlag) ?? false;

        public HintController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            DialogId1 = data.Attr("dialogId1");
            DialogId2 = data.Attr("dialogId2");
            SingleUse1 = data.Bool("singleUse1");
            SingleUse2 = data.Bool("singleUse2");
            ToggleFlag = data.Attr("toggleFlag");
            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.PauseUpdate.Value;
        }

        internal void ShowHint(HintRuntime runtime)
        {
            if (Scene is OriginalLevelSceneRoot level)
            {
                level.Paused = true;
                level.Session.SetFlag(UsedFlag);
            }

            Add(new SimulationCoroutine(engine, ShowHintSequence(runtime)));
        }

        private IEnumerator ShowHintSequence(HintRuntime runtime)
        {
            runtime.ShowingHint = true;
            yield return TextboxState.Say(DialogId, engine);
            runtime.ShowingHint = false;
            if (Scene is OriginalLevelSceneRoot level) level.Paused = false;
        }
    }
}
