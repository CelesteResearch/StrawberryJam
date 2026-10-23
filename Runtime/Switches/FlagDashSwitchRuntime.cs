using System;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Mechanisms.DashSwitches;
using Celeste.Game.Gameplay.Doors.TempleGates;
using Celeste.Mods.StrawberryJam.Entities.Switches;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Switches
{
    public sealed class FlagDashSwitchRuntime : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly Hook gateHook;
        private readonly Hook dashedHook;

        public FlagDashSwitchRuntime(ISourceEngineContext engine)
        {
            this.engine = engine;
            gateHook = new Hook(
                typeof(OriginalDashSwitchState).GetMethod("GetGate", BindingFlags.Instance | BindingFlags.NonPublic),
                (Func<Func<OriginalDashSwitchState, OriginalTempleGateState>, OriginalDashSwitchState,
                    OriginalTempleGateState>)GetGate);
            dashedHook = new Hook(typeof(OriginalDashSwitchState).GetMethod(nameof(OriginalDashSwitchState.OnDashed)),
                (Func<Func<OriginalDashSwitchState, RoomPlayerSession, Vector2, DashCollisionResult>,
                    OriginalDashSwitchState, RoomPlayerSession, Vector2, DashCollisionResult>)OnDashed);
        }

        private OriginalTempleGateState GetGate(Func<OriginalDashSwitchState, OriginalTempleGateState> orig,
            OriginalDashSwitchState self)
        {
            if (self is FlagDashSwitch flag && ReferenceEquals(flag.EngineContext, engine))
            {
                flag.SetTargetFlag();
                return null;
            }

            return orig(self);
        }

        private DashCollisionResult OnDashed(
            Func<OriginalDashSwitchState, RoomPlayerSession, Vector2, DashCollisionResult> orig,
            OriginalDashSwitchState self, RoomPlayerSession player, Vector2 direction)
        {
            if (self is FlagDashSwitch flag && ReferenceEquals(flag.EngineContext, engine))
                flag.TriggerAttachedPlatform(direction);
            return orig(self, player, direction);
        }

        public void Dispose()
        {
            dashedHook.Dispose();
            gateHook.Dispose();
        }
    }
}
