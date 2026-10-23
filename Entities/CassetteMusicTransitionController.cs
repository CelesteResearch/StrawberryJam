using Celeste.Core.Reflection.Runtime;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Gameplay.Platforms.Cassette;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;

namespace Celeste.Mods.StrawberryJam.Entities
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.CassetteMusicTransitionController")]
    [CustomEntity("SJ2021/CassetteMusicTransitionController")]
    public sealed class CassetteMusicTransitionController : SceneEntityState
    {
        public CassetteMusicTransitionController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            var manager = ((ISourceScene)scene).SourceScene.Entities
                .GetTrackedEntity<OriginalCassetteBlockManagerState>();
            var listener = manager?.Components.Get<RoomTransitionListener>();
            if (listener == null) return;
            listener.OnOut = _ =>
            {
                if (manager.Scene != null) manager.Update();
            };
        }
    }
}
