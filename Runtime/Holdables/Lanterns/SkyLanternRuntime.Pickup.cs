using System;
using System.Collections;
using System.Numerics;
using System.Reflection;
using Celeste.Mods.StrawberryJam.Entities.Holdables.Lanterns;
using Celeste.Simulation.Gameplay.Holdables;

namespace Celeste.Mods.StrawberryJam.Runtime.Holdables.Lanterns
{
    public sealed partial class SkyLanternRuntime
    {
        internal static readonly Action<PlayerHoldingController, Vector2> WriteCarryOffset =
            (Action<PlayerHoldingController, Vector2>)typeof(PlayerHoldingController)
                .GetProperty(nameof(PlayerHoldingController.CarryOffset))
                .GetSetMethod(true).CreateDelegate(typeof(Action<PlayerHoldingController, Vector2>));

        private IEnumerator OnPickupCoroutine(Func<PlayerHoldingController, IEnumerator> original,
            PlayerHoldingController self)
        {
            var player = Player(self);
            IEnumerator routine;
            if (ReferenceEquals(player.EngineContext, Engine) && self.Holding.Entity is SkyLantern lantern)
                routine = lantern.PlayerPickupCoroutine(self, player);
            else routine = original(self);
            while (routine.MoveNext()) yield return routine.Current;
        }
    }
}
