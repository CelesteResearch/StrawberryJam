using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Mods.StrawberryJam.Entities.Platforms.SwapBlocks;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.SwapBlocks
{
    public sealed class ToggleSwapBlockRuntime : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly List<IDisposable> registrations = new();

        public ToggleSwapBlockRuntime(ISourceEngineContext engine)
        {
            this.engine = engine;
            try
            {
                registrations.Add(engine.EntityPools.RegisterScoped(() => new ToggleBlockLaser(engine)));
                registrations.Add(engine.EntityPools.RegisterScoped(() => new ToggleBlockNode(engine)));
                registrations.Add(new Hook(
                    typeof(RoomPlayerWorld).GetMethod(nameof(RoomPlayerWorld.MatchingSwapBlockAhead)),
                    (AheadHook)MatchingAhead));
                registrations.Add(new Hook(typeof(RoomPlayerWorld).GetMethod(nameof(RoomPlayerWorld.SwapCancel)),
                    (CancelHook)Cancel));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private bool Owns(RoomPlayerWorld world) => ReferenceEquals(world.SourcePlayer.EngineContext, engine);

        private bool MatchingAhead(AheadOriginal original, RoomPlayerWorld world, int direction)
        {
            if (original(world, direction)) return true;
            if (!Owns(world)) return false;
            var player = world.SourcePlayer;
            if (!(player.Data.Direction.X != 0f && player.Input.GrabCheck)) return false;
            var scene = ((ISourceScene)player.Scene).SourceScene;
            var block = SourceTrackedEntityCollision.FirstAt<ToggleSwapBlock>(
                scene.Entities,
                scene.Catalog.Find(typeof(ToggleSwapBlock)).Name, player,
                player.Position + Vector2.UnitX * Math.Sign(player.Data.Direction.X));
            return block != null && !block.AllowDashSliding &&
                   Math.Sign(block.Direction.X) == Math.Sign(player.Data.Direction.X);
        }

        private Vector2 Cancel(CancelOriginal original, RoomPlayerWorld world, Vector2 direction)
        {
            Vector2 mask = original(world, direction);
            if (!Owns(world)) return mask;
            var player = world.SourcePlayer;
            foreach (var entity in ((ISourceScene)player.Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(ToggleSwapBlock)))
            {
                var block = (ToggleSwapBlock)entity;
                if (!block.AllowDashSliding && block.Moving && ReferenceEquals(block.GetPlayerRider(), player))
                {
                    if (direction.X != 0f && Math.Sign(block.Direction.X) == Math.Sign(direction.X))
                        player.Data.Motion.Normal.Speed.X = mask.X = 0f;
                    if (direction.Y != 0f && Math.Sign(block.Direction.Y) == Math.Sign(direction.Y))
                        player.Data.Motion.Normal.Speed.Y = mask.Y = 0f;
                }
            }

            return mask;
        }

        public void Dispose()
        {
            for (int index = registrations.Count - 1; index >= 0; index--) registrations[index].Dispose();
            registrations.Clear();
        }

        private delegate bool AheadOriginal(RoomPlayerWorld world, int direction);

        private delegate bool AheadHook(AheadOriginal original, RoomPlayerWorld world, int direction);

        private delegate Vector2 CancelOriginal(RoomPlayerWorld world, Vector2 direction);

        private delegate Vector2 CancelHook(CancelOriginal original, RoomPlayerWorld world, Vector2 direction);
    }
}
