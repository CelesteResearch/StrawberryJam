using System.Collections;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Levels.Cutscenes;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Rendering;
using Celeste.Mods.StrawberryJam.Entities;
using Celeste.Mods.StrawberryJam.Presentation;
using Celeste.Scene.Tracking;
using Celeste.Simulation;

namespace Celeste.Mods.StrawberryJam.Cutscenes
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Cutscenes.CS_PrologueOutro")]
    public sealed class PrologueOutroCutscene : OriginalCutsceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly RoomPlayerSession player;
        private readonly PrologueBasket basket;
        private PrologueTitleLogo logo;
        private readonly Vector2 buttonTarget = new(1728, 972);
        private readonly Vector2 buttonOffScreen = new(1728, 1188);
        private Vector2 buttonPosition;

        public PrologueOutroCutscene(ISourceEngineContext engine, RoomPlayerSession player, PrologueBasket basket)
            : base(engine)
        {
            this.engine = engine;
            this.player = player;
            this.basket = basket;
            Tags = OriginalSceneTags.HUD;
            buttonPosition = buttonOffScreen;
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            var input = ((IOriginalGameInputProcessEnvironment)engine).InputSource;
            var confirmButton = input.GuiButton(input.MenuConfirm, fallback: "controls/keyboard/oemquestion");
            SourceAtlasDrawing.DrawCentered(context, this, confirmButton, buttonPosition, PackedColor.White);
        }

        public override void OnBegin(OriginalLevelSceneRoot level) =>
            Add(new SimulationCoroutine(engine, Cutscene(level)));

        private IEnumerator Cutscene(OriginalLevelSceneRoot level)
        {
            player.Machine.State = 11;
            player.Data.Dashes = 1;
            ((IOriginalGameplayProcess)engine).AudioSession.SetMusicParam("outro", 1);
            yield return 0.5f;
            yield return player.DummyWalkTo(basket.X - 12f);
            yield return 0.4f;
            player.Data.Facing = 1;
            yield return 0.3f;
            player.Dummy.AutoAnimate = false;
            player.Sprite.Play("sitDown");
            yield return 2f;
            Add(new SimulationCoroutine(engine, PanCamera(level)));
            yield return 3f;
            logo = new PrologueTitleLogo(engine);
            ((ISourceScene)Scene).SourceScene.AddEntity(logo);
            yield return logo.EaseIn();
            yield return 4f;
            yield return ShowConfirmButton();
            while (!((IOriginalGameInputProcessEnvironment)engine).InputSource.MenuConfirm.Pressed) yield return null;
            EndCutscene(level);
        }

        private IEnumerator ShowConfirmButton()
        {
            Vector2 source = buttonPosition;
            Vector2 destination = buttonTarget;
            var easing = ((ISourceEasingEnvironment)engine).EasingArithmetic;
            for (float p = 0f; p < 1f; p += engine.DeltaTime * 3)
            {
                buttonPosition = (destination - source) * easing.CubeOut(p) + source;
                yield return null;
            }
        }

        private IEnumerator PanCamera(OriginalLevelSceneRoot level)
        {
            Vector2 from = level.Camera.Position;
            Vector2 to = from - Vector2.UnitY * 3000f;
            var easing = ((ISourceEasingEnvironment)engine).EasingArithmetic;
            var arithmetic = ((ISourceScalarEnvironment)engine).ScalarArithmetic;
            for (float p = 0f; p < 1f; p += engine.DeltaTime / 4.5f)
            {
                level.Camera.Position = Vector2Interpolation.Lerp(from, to, easing.CubeInOut(p), arithmetic);
                yield return null;
            }
        }

        public override void OnEnd(OriginalLevelSceneRoot level) => level.CompleteArea(false, false, true);
    }
}
