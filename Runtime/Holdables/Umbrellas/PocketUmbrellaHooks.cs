using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Engine.Update;
using Celeste.Mods.StrawberryJam.Entities.Holdables.Umbrellas;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Player.Movement;
using Celeste.Simulation.Gameplay.Player.State;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Numerics;

namespace Celeste.Mods.StrawberryJam.Runtime.Holdables.Umbrellas
{
    public sealed class PocketUmbrellaHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly string controllerRegistryKey;
        private readonly string playerRegistryKey;
        private readonly List<IDisposable> hooks = new();

        public PocketUmbrellaHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            playerRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(RoomPlayerSession))
                .RegistryKey;
            controllerRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog
                .Find(typeof(PocketUmbrellaController)).RegistryKey;
            try
            {
                hooks.Add(new Hook(typeof(PlayerHoldingController).GetMethod(nameof(PlayerHoldingController.Drop)),
                    (Action<Action<PlayerHoldingController>, PlayerHoldingController>)Drop));
                hooks.Add(new Hook(typeof(PlayerHoldingController).GetMethod(nameof(PlayerHoldingController.Throw)),
                    (Action<Action<PlayerHoldingController>, PlayerHoldingController>)Throw));
                hooks.Add(new ILHook(
                    typeof(OriginalEngineFrame).GetMethod("UpdateSourceBody",
                        BindingFlags.Instance | BindingFlags.NonPublic), PatchFrame));
                hooks.Add(new ILHook(
                    typeof(NormalSequence).GetMethod("UpdateCore", BindingFlags.Instance | BindingFlags.NonPublic),
                    PatchNormal));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void CheckDrop(RoomPlayerSession player)
        {
            var controller =
                ((ISourceScene)player.Scene).SourceScene.Entities.GetTrackedEntity<PocketUmbrellaController>(
                    controllerRegistryKey);
            if (controller != null && controller.Enabled) controller.CheckDrop(player);
        }

        private void Drop(Action<PlayerHoldingController> original, PlayerHoldingController holding)
        {
            var player = (RoomPlayerSession)holding.Body.SourceColliderOwner;
            if (ReferenceEquals(player.EngineContext, engine) && holding.Holding?.Entity is PocketUmbrella)
                CheckDrop(player);
            original(holding);
        }

        private void Throw(Action<PlayerHoldingController> original, PlayerHoldingController holding)
        {
            var player = (RoomPlayerSession)holding.Body.SourceColliderOwner;
            if (!ReferenceEquals(player.EngineContext, engine) || holding.Holding?.Entity is not PocketUmbrella)
            {
                original(holding);
                return;
            }

            CheckDrop(player);
            if (holding.Input.MoveY.Value == 1) holding.Drop();
            else
            {
                ((Celeste.Input.Gameplay.Feedback.ISourceRumbleEnvironment)engine).RumbleSession.Rumble(2, 0);
                holding.Holding.Release(Vector2.UnitX * player.Facing);
                player.Play("event:/char/madeline/crystaltheo_throw", null, 0f);
                player.Sprite.Play("throw", false, false);
            }

            holding.SetHolding(null);
        }

        private void PatchFrame(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.Before,
                    i => i.MatchLdfld<SourceEngineEffectsState>(nameof(SourceEngineEffectsState.FreezeTimer))))
                throw new InvalidOperationException("Missing Engine.Update first FreezeTimer read.");
            // Effects receiver is already on the stack; injected call leaves it untouched.
            cursor.MoveAfterLabels();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<OriginalEngineFrame>>(frame =>
            {
                if (ReferenceEquals(frame.State, engine.Frame))
                    PocketUmbrellaController.FrozenUpdate(engine, controllerRegistryKey, playerRegistryKey);
            });
        }

        private void PatchNormal(ILContext il)
        {
            var cursor = new ILCursor(il);
            // Shared NormalSequence combines the original right/left wall jump branches into one wall direction.
            if (!cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdfld<PlayerState>(nameof(PlayerState.Stamina)),
                    i => i.MatchLdcR4(0f),
                    i => i.OpCode == OpCodes.Ble_Un || i.OpCode == OpCodes.Ble_Un_S,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<NormalSequence>("data"),
                    i => i.MatchCallvirt<PlayerState>("get_Holding")))
                throw new InvalidOperationException("Missing NormalUpdate post-stamina holding test.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<bool, NormalSequence, bool>>(FilterHolding);
        }

        private bool FilterHolding(bool holding, NormalSequence sequence)
        {
            if (!ReferenceEquals(sequence.EngineContext, engine)) return holding;
            return sequence.World is not RoomPlayerWorld world ||
                   world.SourcePlayer.Holding.Holding?.Entity is not PocketUmbrella
                ? holding
                : false;
        }

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
        }
    }
}
