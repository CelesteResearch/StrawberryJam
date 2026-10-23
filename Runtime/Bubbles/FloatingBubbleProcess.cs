using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Gameplay.Interaction.Springs;
using Celeste.Game.Gameplay.Mechanisms.Switches;
using Celeste.Simulation.Gameplay.Interaction.Springs;
using Celeste.Mods.MaxHelpingHand.Entities.Switches;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Mods.StrawberryJam.Components.Bubbles;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Bubbles
{
    public sealed class FloatingBubbleProcess : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, FloatingBubbleProcess> Owners = new();
        public ISourceEngineContext Engine { get; }
        public StrawberryJamModule Module { get; }
        private Hook touch, spring, flagSwitch;

        public FloatingBubbleProcess(ISourceEngineContext engine, StrawberryJamModule module)
        {
            Engine = engine;
            Module = module;
            Owners.Add(engine, this);
            touch = new Hook(
                typeof(OriginalTouchSwitchState).GetConstructor(new[]
                    { typeof(Vector2), typeof(IOriginalTouchSwitchProcess) }),
                (Action<Action<OriginalTouchSwitchState, Vector2, IOriginalTouchSwitchProcess>, OriginalTouchSwitchState
                    , Vector2, IOriginalTouchSwitchProcess>)OnTouch);
            spring = new Hook(
                typeof(OriginalSpringState).GetConstructor(new[]
                    { typeof(Vector2), typeof(SpringOrientation), typeof(bool), typeof(IOriginalSpringProcess) }),
                (Action<Action<OriginalSpringState, Vector2, SpringOrientation, bool, IOriginalSpringProcess>,
                    OriginalSpringState, Vector2, SpringOrientation, bool, IOriginalSpringProcess>)OnSpring);
            flagSwitch = new Hook(
                typeof(FlagTouchSwitch).GetConstructor(new[]
                    { typeof(ISourceEngineContext), typeof(EntityData), typeof(Vector2) }),
                (Action<Action<FlagTouchSwitch, ISourceEngineContext, EntityData, Vector2>, FlagTouchSwitch,
                    ISourceEngineContext, EntityData, Vector2>)OnFlag);
        }

        public static FloatingBubbleProcess For(ISourceEngineContext engine) =>
            Owners.TryGetValue(engine, out var owner)
                ? owner
                : throw new InvalidOperationException("Strawberry Jam bubble hooks are not loaded in this engine.");

        private void OnTouch(Action<OriginalTouchSwitchState, Vector2, IOriginalTouchSwitchProcess> original,
            OriginalTouchSwitchState self, Vector2 position, IOriginalTouchSwitchProcess process)
        {
            original(self, position, process);
            if (ReferenceEquals(process, Engine)) self.Add(new BubbleCollider());
        }

        private void OnSpring(
            Action<OriginalSpringState, Vector2, SpringOrientation, bool, IOriginalSpringProcess> original,
            OriginalSpringState self, Vector2 position, SpringOrientation orientation, bool playerCanUse,
            IOriginalSpringProcess process)
        {
            original(self, position, orientation, playerCanUse, process);
            if (ReferenceEquals(process, Engine)) self.Add(new BubbleCollider());
        }

        private void OnFlag(Action<FlagTouchSwitch, ISourceEngineContext, EntityData, Vector2> original,
            FlagTouchSwitch self, ISourceEngineContext engine, EntityData data, Vector2 offset)
        {
            original(self, engine, data, offset);
            if (ReferenceEquals(engine, Engine)) self.Add(new BubbleCollider());
        }

        public void Dispose()
        {
            touch.Dispose();
            spring.Dispose();
            flagSwitch.Dispose();
            Owners.Remove(Engine);
        }
    }
}
