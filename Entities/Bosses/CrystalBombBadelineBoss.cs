using System;
using System.Numerics;
using System.Collections.Generic;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Core.Mathematics;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Gameplay.Enemies.BadelineBoss;
using Celeste.Game.Gameplay.Enemies.BadelineBoss.Construction;
using Celeste.Game.Gameplay.Enemies.BadelineBoss.Progression;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.CavernHelper.Entities.Explosives;
using Celeste.Mods.StrawberryJam.Runtime.Bosses;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;

namespace Celeste.Mods.StrawberryJam.Entities.Bosses
{
    [CustomEntity("SJ2021/CrystalBombBadelineBoss")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.CrystalBombBadelineBoss")]
    [EverestTrackedAs(typeof(FinalBossState))]
    public class CrystalBombBadelineBoss : FinalBossState
    {
        private readonly string music;
        private readonly ISourceEngineContext engine;
        private readonly List<CrystalBomb> bombs = new();
        internal string Music => music;
        internal ISourceEngineContext SourceEngine => engine;

        public CrystalBombBadelineBoss(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(data, offset,
                CrystalBombBossBindings.Progression(((IOriginalFinalBossEnvironment)engine).FinalBosses), engine)
        {
            this.engine = engine;
            OwnedComponents.Remove(OwnedComponents.Get<PlayerColliderComponentState>());
            OwnedComponents.Add(new PlayerColliderComponentState(OnPlayerCBBB,
                new CircleColliderGeometry(((ISourceVectorEnvironment)engine).VectorArithmetic, 8f, 0f, -6f)));
            music = data.Attr("music", "");
            if (data.Bool("disableCameraLock", false)) OwnedComponents.Remove(CameraLocker);
        }

        public override void Update()
        {
            base.Update();
            AvoidPosition = Vector2.Zero;
            if (!Collidable) return;
            bombs.Clear();
            var entities = ((OriginalLevelSceneRoot)Scene).SourceScene.Entities.Entities;
            for (int i = 0; i < entities.Count; i++)
                if (entities[i] is CrystalBomb bomb)
                    bombs.Add(bomb);
            // The original FindAll list captures membership before any explosion callback.
            for (int i = 0; i < bombs.Count; i++)
                if (SourceEntityCollision.Check(this, bombs[i]))
                    CrystalBombBossBindings.Explode(bombs[i]);
            bombs.Clear();
        }

        private void OnPlayerCBBB(RoomPlayerSession player) =>
            player.Die(((ISourceVectorEnvironment)engine).VectorArithmetic.SafeNormalize(player.Center - Center), false,
                true);

        internal void OnHit() => OnPlayer(null);
    }
}
