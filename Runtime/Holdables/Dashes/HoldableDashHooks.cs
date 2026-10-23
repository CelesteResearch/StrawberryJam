using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.StrawberryJam.Triggers.Holdables;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Movement;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Holdables.Dashes
{
    public sealed class HoldableDashHooks : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, HoldableDashHooks> owners = new();
        private readonly ISourceEngineContext engine;
        private readonly string triggerRegistryKey;
        private readonly Hook normal;

        public static HoldableDashHooks For(ISourceEngineContext engine) =>
            owners.TryGetValue(engine, out var hooks) ? hooks : null;

        public HoldableDashHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            triggerRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(HoldableDashTrigger))
                .RegistryKey;
            normal = new Hook(
                typeof(NormalSequence).GetMethod("UpdateCore", BindingFlags.Instance | BindingFlags.NonPublic),
                (Func<Func<NormalSequence, int>, NormalSequence, int>)NormalUpdate);
            owners.Add(engine, this);
        }

        public bool CanDashWithHoldable(RoomPlayerSession player)
        {
            var trigger =
                SourceTrackedEntityCollision.First<HoldableDashTrigger>(
                    ((ISourceScene)player.Scene).SourceScene.Entities, triggerRegistryKey, player);
            if (trigger != null)
            {
                if (trigger.Mode == HoldableDashTrigger.Modes.EnableOnStay) return true;
                if (trigger.Mode == HoldableDashTrigger.Modes.DisableOnStay) return false;
            }

            return ((OriginalLevelSceneRoot)player.Scene).Session.GetFlag(HoldableDashTrigger.DashWithHoldableFlag);
        }

        private int NormalUpdate(Func<NormalSequence, int> original, NormalSequence sequence)
        {
            int result = original(sequence);
            if (!ReferenceEquals(sequence.EngineContext, engine) || sequence.World is not RoomPlayerWorld world)
                return result;
            var player = world.SourcePlayer;
            if (player.Holding.Holding != null && CanDashWithHoldable(player) && player.Motor.Dash.CanDash)
            {
                player.Data.Motion.Normal.Speed += JumpMotion.LiftBoost(player.LiftSpeed);
                return player.StartDash();
            }

            return result;
        }

        public void Dispose()
        {
            owners.Remove(engine);
            normal.Dispose();
        }
    }
}
