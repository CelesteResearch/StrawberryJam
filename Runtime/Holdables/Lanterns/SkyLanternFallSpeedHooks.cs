// SPDX-License-Identifier: MIT

using System;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.StrawberryJam.Entities.Holdables.Lanterns;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Movement;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Holdables.Lanterns
{
    public sealed class SkyLanternFallSpeedHooks : IDisposable
    {
        private static readonly
            System.Runtime.CompilerServices.ConditionalWeakTable<ISourceEngineContext, SkyLanternFallSpeedHooks>
            owners = new();

        private readonly ISourceEngineContext engine;
        private ILHook fallLiterals;
        private IDisposable phaseLease;


        public SkyLanternFallSpeedHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            owners.Add(engine, this);

            try
            {
                phaseLease = NormalFallPhaseHooks.Acquire(engine);
                if (fallLiterals == null)
                    fallLiterals = new ILHook(typeof(NormalMotion).GetMethod(nameof(NormalMotion.UpdateFallLimit)),
                        PatchFallLiterals);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void PatchFallLiterals(ILContext il)
        {
            // These are the original five held fall literals, now in the extracted numerical phase.
            // Search independently because its conditional-expression IL can arrange branch blocks differently.
            Replace(120f, 0);
            Replace(-32f, 1);
            Replace(24f, 2);
            Replace(0f, 3);
            Replace(40f, 4);

            void Replace(float literal, int index)
            {
                var cursor = new ILCursor(il);
                // ForceMoveXTimer's comparison zero is before the first GliderMoveY read.
                if (literal == 0f && !cursor.TryGotoNext(MoveType.After,
                        instruction =>
                            instruction.MatchLdfld<NormalMotionContext>(nameof(NormalMotionContext.GliderMoveY))))
                    throw new InvalidOperationException("SkyLantern held fall target section was not found.");
                if (!cursor.TryGotoNext(MoveType.After, instruction => instruction.MatchLdcR4(literal)))
                    throw new InvalidOperationException("SkyLantern held fall literal was not found: " + literal);
                cursor.EmitDelegate<Func<float, float>>(value => HeldFallSpeed(value, index));
            }
        }

        private float HeldFallSpeed(float original, int index)
        {
            var player = NormalFallPhaseHooks.ActivePlayer;
            if (!(player?.Holding.Holding?.Entity is SkyLantern lantern) ||
                !ReferenceEquals(player.EngineContext, engine) ||
                !ReferenceEquals(lantern.EngineContext, engine)) return original;
            if (index == 0 && ((OriginalLevelSceneRoot)player.Scene).Wind.Y > 0)
                return lantern.riseSpeeds[0] + 40;
            return lantern.riseSpeeds[index];
        }

        public void Dispose()
        {
            if (!owners.TryGetValue(engine, out var owner) || !ReferenceEquals(owner, this)) return;
            owners.Remove(engine);
            phaseLease?.Dispose();
            phaseLease = null;

            fallLiterals?.Dispose();
            fallLiterals = null;
        }
    }
}
