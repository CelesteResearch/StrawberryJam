using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.StrawberryJam.Triggers
{
    [SceneTracked]
    [CustomEntity("SJ2021/DashCountTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Triggers.DashCountTrigger")]
    public sealed class DashCountTrigger : SourceTriggerEntityState
    {
        private int origDashCount;
        private readonly int newDashCount;
        private readonly string playerRegistryKey;
        private bool entered;

        public DashCountTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            newDashCount = data.Int("NumberOfDashes", 1);
            playerRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(RoomPlayerSession))
                .RegistryKey;
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            if (!entered)
            {
                origDashCount = player.Inventory.Dashes;
                ((OriginalLevelSceneRoot)Scene).Session.Inventory.Dashes = newDashCount;
                player.Data.Dashes = newDashCount;
                entered = true;
            }
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            if (entered)
            {
                if (((ISourceScene)scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(playerRegistryKey) is
                    RoomPlayerSession
                    player && player.Inventory.Dashes == newDashCount)
                {
                    ((OriginalLevelSceneRoot)scene).Session.Inventory.Dashes = origDashCount;
                    player.Data.Dashes = origDashCount;
                }
            }
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            if (entered) ((OriginalLevelSceneRoot)scene).Session.Inventory.Dashes = origDashCount;
        }
    }
}
