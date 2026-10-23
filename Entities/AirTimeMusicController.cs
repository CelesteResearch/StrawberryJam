using Celeste.Core.Reflection.Runtime;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;

namespace Celeste.Mods.StrawberryJam.Entities
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.AirTimeMusicController")]
    [CustomEntity("SJ2021/AirTimeMusicController")]
    public sealed class AirTimeMusicController : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly float airtimeThreshold;
        private readonly string parameter;
        private float lastGroundTime;
        private RoomPlayerSession player;

        public AirTimeMusicController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            airtimeThreshold = data.Float("activationThreshold");
            parameter = data.Attr("musicParam", "");
        }

        public override void Update()
        {
            base.Update();
            if (player == null)
            {
                var players = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(typeof(RoomPlayerSession));
                if (players.Count != 0) player = (RoomPlayerSession)players[0];
            }

            if (player == null) return;
            var timeActive = ((OriginalLevelSceneRoot)Scene).TimeActive;
            if (player.OnSafeGround) lastGroundTime = timeActive;
            ((IOriginalGameplayProcess)engine).AudioSession.SetMusicParam(parameter,
                timeActive - lastGroundTime > airtimeThreshold ? 1 : 0);
        }
    }
}
