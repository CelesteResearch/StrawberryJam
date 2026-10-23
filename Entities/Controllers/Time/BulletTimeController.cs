using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;

namespace Celeste.Mods.StrawberryJam.Entities.Controllers.Time
{
    [CustomEntity("SJ2021/BTController")]
    [RuntimeTypeAlias("BTController")]
    public class BulletTimeController : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly float timeRate;
        private readonly string flag;
        private bool active;

        public BulletTimeController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.PauseUpdate.Value;
            timeRate = data.Float("speed");
            flag = data.Attr("flag");
        }

        public override void Update()
        {
            base.Update();
            var players = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(typeof(RoomPlayerSession));
            var player = players.Count == 0 ? null : (RoomPlayerSession)players[0];
            if (player != null && !player.IsIntroState && !player.JustRespawned && player.Data.Dashes > 0 &&
                (string.IsNullOrEmpty(flag) || ((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag)))
            {
                engine.Effects.TimeRate = ((OriginalLevelSceneRoot)Scene).Paused ? 1f : timeRate;
                active = true;
            }
            else if (active)
            {
                engine.Effects.TimeRate = 1f;
                active = false;
            }
        }
    }
}
