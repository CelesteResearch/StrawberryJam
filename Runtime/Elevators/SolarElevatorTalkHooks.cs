using System;
using Celeste.Core.Scheduling;
using Celeste.Game.Components.Talking;
using Celeste.Scene.Tracking;
using Celeste.Mods.StrawberryJam.Entities.Elevators;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Elevators
{
    internal sealed class SolarElevatorTalkHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly Hook hook;

        internal SolarElevatorTalkHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            hook = new Hook(typeof(OriginalTalkComponent).GetMethod(nameof(OriginalTalkComponent.Update)),
                (Action<Action<OriginalTalkComponent>, OriginalTalkComponent>)Update);
        }

        private void Update(Action<OriginalTalkComponent> orig, OriginalTalkComponent self)
        {
            if (self.Entity is SolarElevator elevator && ReferenceEquals(elevator.Engine, engine) &&
                elevator.RequiresHoldable && self.UI == null)
                ((ISourceScene)self.Scene).SourceScene.QueueSourceEntity(self.UI =
                    new HintTalkComponentUI(self, elevator));
            orig(self);
        }

        public void Dispose() => hook.Dispose();
    }
}
