using System;
using Celeste.Core.Scheduling;
using Celeste.Mods.StrawberryJam.Entities.Holdables.Flowers;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Movement;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Holdables.Flowers
{
    public sealed class TripleBoostFlowerFallSpeedHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private IDisposable phase;
        private ILHook literals;

        public TripleBoostFlowerFallSpeedHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            try
            {
                phase = NormalFallPhaseHooks.Acquire(engine);
                literals = new ILHook(typeof(NormalMotion).GetMethod(nameof(NormalMotion.UpdateFallLimit)), Patch);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void Patch(ILContext il)
        {
            PatchLiteral(il, 120f, Fast);
            PatchLiteral(il, 24f, Slow);
            PatchLiteral(il, 40f, Normal);
        }

        private static void PatchLiteral(ILContext il, float literal, Func<float, float> replacement)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instruction => instruction.MatchLdcR4(literal)))
                throw new InvalidOperationException("Missing Player.NormalUpdate held fall constant " + literal);
            cursor.EmitDelegate(replacement);
        }

        private TripleBoostFlower HeldFlower()
        {
            RoomPlayerSession player = NormalFallPhaseHooks.ActivePlayer;
            return player != null && ReferenceEquals(player.EngineContext, engine)
                ? player.Holding?.Holding?.Entity as TripleBoostFlower
                : null;
        }

        private float Fast(float speed) => HeldFlower()?.FastFallSpeed ?? speed;
        private float Slow(float speed) => HeldFlower()?.SlowFallSpeed ?? speed;
        private float Normal(float speed) => HeldFlower()?.FallSpeed ?? speed;

        public void Dispose()
        {
            try
            {
                literals?.Dispose();
                literals = null;
            }
            finally
            {
                phase?.Dispose();
                phase = null;
            }
        }
    }
}
