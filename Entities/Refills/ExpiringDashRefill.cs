using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Collectibles.Refills;
using Celeste.Mods.StrawberryJam.Runtime.Refills;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;

namespace Celeste.Mods.StrawberryJam.Entities.Refills
{
    [SceneTracked]
    [CustomEntity("SJ2021/ExpiringDashRefill = Create")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.ExpiringDashRefill")]
    public class ExpiringDashRefill : OriginalRefillState
    {
        private readonly float dashExpirationTime;
        private readonly float hairFlashTime;
        private readonly ExpiringDashRefillState state;

        public static ExpiringDashRefill Create(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new((IOriginalRefillProcess)level.EngineContext, data, offset);

        public ExpiringDashRefill(IOriginalRefillProcess engine, EntityData data, Vector2 offset)
            : base(data.Position + offset, false, data.Bool("oneUse"), engine)
        {
            state = ExpiringDashRefillState.For(engine);
            dashExpirationTime = engine.ExtremaArithmetic.Max(data.Float("dashExpirationTime"), 0.01f);
            hairFlashTime = dashExpirationTime * ScalarMath.Clamp(data.Float("hairFlashThreshold"), 0f, 1f,
                engine.ExtremaArithmetic);
            Remove(Get<PlayerColliderComponentState>());
            OwnedComponents.Add(new PlayerColliderComponentState(OnPlayer));
        }

        private void OnPlayer(RoomPlayerSession player)
        {
            var session = state.Session;
            if (session.ExpiringDashRemainingTime >= dashExpirationTime && player.Data.Stamina >= 20f) return;
            int playerRealDashes = player.Data.Dashes - (session.ExpiringDashRemainingTime > 0 ? 1 : 0);
            state.ExpireFlash = false;
            player.Data.Dashes = playerRealDashes + 1;
            session.ExpiringDashRemainingTime = dashExpirationTime;
            session.ExpiringDashFlashThreshold = hairFlashTime;
            player.RefillStamina();
            ((IOriginalGameplayProcess)engine).AudioSession.Play("event:/game/general/diamond_touch");
            engine.RumbleSession.Rumble(1, 1);
            Collidable = false;
            OwnedComponents.Add(new SimulationCoroutine(engine, RefillRoutine(player)));
            respawnTimer = 2.5f;
        }
    }
}
