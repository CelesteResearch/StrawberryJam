using System;
using System.Collections.Generic;
using Celeste.Core.Scheduling;
using Celeste.Mods.StrawberryJam.Entities.Refills;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Presentation.Hair;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Refills
{
    public sealed class ResettingRefillProcess : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly Hook hair;
        private SceneEntityRegistry trackedEntities;
        private List<object> refills;

        public ResettingRefillProcess(ISourceEngineContext engine)
        {
            this.engine = engine;
            hair = new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.UpdateHair)),
                (Action<Action<RoomPlayerSession, bool>, RoomPlayerSession, bool>)UpdateHair);
        }

        private void UpdateHair(Action<RoomPlayerSession, bool> original, RoomPlayerSession player, bool gravity)
        {
            original(player, gravity);
            if (!ReferenceEquals(player.EngineContext, engine)) return;
            var entities = ((ISourceScene)player.Scene).SourceScene.Entities;
            if (!ReferenceEquals(trackedEntities, entities))
            {
                trackedEntities = entities;
                entities.TryGetTrackedEntities(typeof(ResettingRefill), out refills);
            }

            if (refills == null || refills.Count == 0) return;
            if (player.Data.Dashes == 0 && player.Appearance.LastDashes == player.Data.Dashes &&
                player.Appearance.HairFlashTimer <= 0)
                player.Hair.Color = PlayerHairAppearance.UsedHairColor;
        }

        public void Dispose()
        {
            hair.Dispose();
            trackedEntities = null;
            refills = null;
        }
    }
}
