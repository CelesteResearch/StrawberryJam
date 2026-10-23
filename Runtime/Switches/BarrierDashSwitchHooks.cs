using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Holdables.Gliders;
using Celeste.Mods.StrawberryJam.Entities.Switches;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Switches
{
    public sealed class BarrierDashSwitchHooks : IDisposable
    {
        private static readonly Action<OriginalGliderState, bool> SetDestroyed = CreateDestroyedSetter();

        private static readonly Func<OriginalGliderState, IEnumerator> DestroyAnimationRoutine =
            (Func<OriginalGliderState, IEnumerator>)typeof(OriginalGliderState)
                .GetMethod("DestroyAnimationRoutine", BindingFlags.Instance | BindingFlags.NonPublic)
                .CreateDelegate(typeof(Func<OriginalGliderState, IEnumerator>));

        private readonly ISourceEngineContext engine;
        private readonly ILHook hook;

        public BarrierDashSwitchHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            hook = new ILHook(
                typeof(OriginalGliderState).GetMethod("OnCollideH", BindingFlags.Instance | BindingFlags.NonPublic),
                PatchCollision);
        }

        private static Action<OriginalGliderState, bool> CreateDestroyedSetter()
        {
            var self = Expression.Parameter(typeof(OriginalGliderState), "self");
            var value = Expression.Parameter(typeof(bool), "value");
            return Expression.Lambda<Action<OriginalGliderState, bool>>(
                Expression.Assign(
                    Expression.Field(self,
                        typeof(OriginalGliderState).GetField("destroyed",
                            BindingFlags.Instance | BindingFlags.NonPublic)), value), self, value).Compile();
        }

        private void PatchCollision(ILContext il)
        {
            var cursor = new ILCursor(il);
            var invoke =
                typeof(Func<RoomPlayerSession, System.Numerics.Vector2, DashCollisionResult>).GetMethod("Invoke");
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt(invoke), i => i.MatchPop()))
                throw new InvalidOperationException("The real Glider DashCollision.Invoke/pop anchor is missing.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Ldfld, typeof(PixelCollision).GetField(nameof(PixelCollision.Hit)));
            cursor.EmitDelegate<Action<OriginalGliderState, ISourceEntityCollider>>(DestroyIfBarrierDashSwitch);
        }

        private void DestroyIfBarrierDashSwitch(OriginalGliderState glider, ISourceEntityCollider hit)
        {
            if (glider.Scene is ISourceEngineContextScene scene && ReferenceEquals(scene.EngineContext, engine) &&
                hit is BarrierDashSwitch)
            {
                SetDestroyed(glider, true);
                glider.Collidable = false;
                glider.Add(new SimulationCoroutine(engine, DestroyAnimationRoutine(glider)));
            }
        }

        public void Dispose() => hook.Dispose();
    }
}
