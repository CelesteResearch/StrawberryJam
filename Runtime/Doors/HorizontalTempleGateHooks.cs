using System;
using System.Linq.Expressions;
using System.Reflection;
using Celeste.Core.Maps;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Doors.TempleGates;
using Celeste.Game.Gameplay.Mechanisms.DashSwitches;
using Celeste.Mods.StrawberryJam.Entities.Doors;
using Celeste.Scene.Tracking;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Doors
{
    public sealed class HorizontalTempleGateHooks : IDisposable
    {
        private static readonly Func<OriginalDashSwitchState, MapEntityId> GetId = CreateIdGetter();
        private readonly ISourceEngineContext engine;
        private readonly string registryKey;
        private readonly ILHook hook;

        public HorizontalTempleGateHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            registryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(HorizontalTempleGate))
                .RegistryKey;
            hook = new ILHook(
                typeof(OriginalDashSwitchState).GetMethod("OnDashed",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), PatchDashed);
        }

        private static Func<OriginalDashSwitchState, MapEntityId> CreateIdGetter()
        {
            var self = Expression.Parameter(typeof(OriginalDashSwitchState), "self");
            return Expression.Lambda<Func<OriginalDashSwitchState, MapEntityId>>(
                Expression.Field(self,
                    typeof(OriginalDashSwitchState).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic)),
                self).Compile();
        }

        private void PatchDashed(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(i =>
                    i.MatchLdfld(typeof(OriginalDashSwitchState).GetField("allGates",
                        BindingFlags.Instance | BindingFlags.NonPublic))))
                throw new InvalidOperationException("DashSwitch actual allGates field anchor missing.");
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchBrfalse(out _)))
                throw new InvalidOperationException("DashSwitch actual allGates conditional branch missing.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<OriginalDashSwitchState>>(OpenAll);
            cursor.Index = 0;
            if (!cursor.TryGotoNext(MoveType.After,
                    i => i.MatchCallOrCallvirt(typeof(OriginalDashSwitchState).GetMethod("GetGate",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))))
                throw new InvalidOperationException("DashSwitch actual GetGate call missing.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<OriginalTempleGateState, OriginalDashSwitchState, OriginalTempleGateState>>(
                ChooseNearest);
        }

        private bool Owns(OriginalDashSwitchState self) => self.Scene is ISourceEngineContextScene scene &&
                                                           ReferenceEquals(scene.EngineContext, engine);

        private void OpenAll(OriginalDashSwitchState self)
        {
            if (!Owns(self)) return;
            foreach (HorizontalTempleGate gate in ((ISourceScene)self.Scene).SourceScene.Entities.GetTrackedEntities(
                         registryKey))
            {
                if (gate.Type == HorizontalTempleGate.Types.NearestSwitch && gate.LevelID == GetId(self).Level)
                    gate.SwitchOpen();
            }
        }

        private OriginalTempleGateState ChooseNearest(OriginalTempleGateState templeGate, OriginalDashSwitchState self)
        {
            if (!Owns(self)) return templeGate;
            var entities = ((ISourceScene)self.Scene).SourceScene.Entities.GetTrackedEntities(registryKey);
            HorizontalTempleGate horizontal = null;
            float dist = 0f;
            foreach (HorizontalTempleGate item in entities)
            {
                if (item.Type == HorizontalTempleGate.Types.NearestSwitch && !item.ClaimedByASwitch &&
                    item.LevelID == GetId(self).Level)
                {
                    float currentDist =
                        ((ISourceVectorEnvironment)engine).VectorArithmetic.DistanceSquared(self.Position,
                            item.Position);
                    if (horizontal == null || currentDist < dist)
                    {
                        horizontal = item;
                        dist = currentDist;
                    }
                }
            }

            if (horizontal != null && (templeGate == null ||
                                       dist < ((ISourceVectorEnvironment)engine).VectorArithmetic.DistanceSquared(
                                           self.Position, templeGate.Position)))
            {
                if (templeGate != null)
                {
                    templeGate.ClaimedByASwitch = false;
                    templeGate = null;
                }

                horizontal.ClaimedByASwitch = true;
                horizontal.SwitchOpen();
            }

            return templeGate;
        }

        public void Dispose() => hook.Dispose();
    }
}
