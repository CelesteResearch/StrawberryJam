// SPDX-License-Identifier: MIT

using System;
using System.Reflection;
using System.Linq;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Enemies.AngryOshiro;
using Celeste.Simulation.Runtime.Session;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mods.StrawberryJam.Runtime.Enemies
{
    public sealed class OshiroAttackTimeHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly Func<bool> enabled;
        private readonly ILHook hook;

        public OshiroAttackTimeHooks(ISourceEngineContext engine, Func<bool> enabled)
        {
            this.engine = engine;
            this.enabled = enabled;
            hook = new ILHook(
                typeof(OriginalAngryOshiroState)
                    .GetMethod("ChaseCoroutine", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetStateMachineTarget(), Patch);
        }

        private void Patch(ILContext il)
        {
            var cursor = new ILCursor(il);
            var owner = il.Body.Instructions.Select(instruction => instruction.Operand)
                .OfType<Mono.Cecil.FieldReference>().First(field =>
                    field.FieldType.FullName == typeof(OriginalAngryOshiroState).FullName);
            cursor.GotoNext(MoveType.After, instruction => instruction.MatchLdfld<ChapterCompletionSession>("Mode"));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, owner);
            cursor.EmitDelegate<Func<int, OriginalAngryOshiroState, int>>(ReadMode);
        }

        private int ReadMode(int original, OriginalAngryOshiroState oshiro) =>
            oshiro.Scene is OriginalLevelSceneRoot level && ReferenceEquals(level.EngineContext, engine) && enabled()
                ? 1
                : original;

        public void Dispose() => hook.Dispose();
    }
}
