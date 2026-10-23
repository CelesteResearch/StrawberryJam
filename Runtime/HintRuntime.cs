using System;
using System.Linq;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Everest.Runtime.Events;
using Celeste.Game.Content.Dialogs;
using Celeste.Game.Execution.Levels;
using Celeste.Game.UI.Dialogue.Textboxes;
using Celeste.Mods.StrawberryJam.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.UI.Menus.Text;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime
{
    public sealed class HintRuntime : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly EverestGameEventsProcess events;
        private readonly ILHook renderHook;
        private readonly ILHook updateHook;
        internal bool ShowingHint { get; set; }

        public HintRuntime(ISourceEngineContext engine)
        {
            this.engine = engine;
            events = ((IEverestGameEventsEnvironment)engine).GameEvents;
            renderHook = new ILHook(typeof(TextboxState).GetMethod("Render"), HookTextbox);
            try
            {
                updateHook = new ILHook(typeof(TextboxState).GetMethod("UpdateCurrentFrame",
                    BindingFlags.NonPublic | BindingFlags.Instance), HookTextbox);
                events.OnCreatePauseMenuButtons += CreatePauseMenuButtons;
            }
            catch
            {
                renderHook.Dispose();
                throw;
            }
        }

        private void HookTextbox(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction => instruction.MatchCallvirt<OriginalLevelSceneRoot>("get_FrozenOrPaused")))
                throw new InvalidOperationException(
                    $"HintController cannot locate FrozenOrPaused in {il.Method.FullName}.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, typeof(TextboxState).GetField("engine",
                BindingFlags.NonPublic | BindingFlags.Instance));
            cursor.EmitDelegate<Func<bool, ISourceEngineContext, bool>>((paused, actualEngine) =>
                !(ReferenceEquals(actualEngine, engine) && ShowingHint) && paused);
        }

        private void CreatePauseMenuButtons(OriginalLevelSceneRoot level, TextMenuPresentation menu, bool minimal)
        {
            if (!ReferenceEquals(level.EngineContext, engine)) return;
            var text = ((IOriginalGameTextEnvironment)engine).Text;
            var retryIndex = menu.Items.FindIndex(item =>
                item.GetType() == typeof(TextMenuPresentation.Button) &&
                ((TextMenuPresentation.Button)item).Label == text.CleanDialog("menu_pause_retry"));
            if (retryIndex < 0) return;
            var controller = ((ISourceScene)level).SourceScene.Entities.Entities.OfType<HintController>()
                .FirstOrDefault();
            if (controller == null) return;
            menu.Insert(retryIndex + 1, new TextMenuPresentation.Button(text.CleanDialog("StrawberryJam2021_Menu_Hint"))
            {
                OnPressed = () =>
                {
                    menu.OnCancel();
                    controller.ShowHint(this);
                },
                Disabled = controller.SingleUse && controller.UsedFlagValue
            });
        }

        public void Dispose()
        {
            events.OnCreatePauseMenuButtons -= CreatePauseMenuButtons;
            ShowingHint = false;
            updateHook.Dispose();
            renderHook.Dispose();
        }
    }
}
