using Celeste.Core.Reflection.Runtime;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Holdables.TheoCrystal;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.StrawberryJam.Entities
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.TheoRespawn")]
    [CustomEntity("SJ2021/TheoRespawn")]
    [SceneTracked]
    public sealed class TheoRespawn : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        public readonly string Flag;

        public TheoRespawn(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            Flag = data.Attr("flag");
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            if (!string.IsNullOrWhiteSpace(Flag) && !((OriginalLevelSceneRoot)Scene).Session.GetFlag(Flag))
                RemoveSelf();
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            var level = (OriginalLevelSceneRoot)Scene;
            var distance = Vector2.Distance(Position, level.Session.RespawnPoint.Value);
            foreach (TheoRespawn respawn in ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(TheoRespawn)))
            {
                if (Vector2.Distance(respawn.Position, level.Session.RespawnPoint.Value) < distance)
                {
                    RemoveSelf();
                    return;
                }
            }

            ((ISourceScene)scene).SourceScene.AddEntity(new TheoCrystalState(Position, engine));
            RemoveSelf();
        }
    }
}
