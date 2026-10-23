using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Animation;
using Celeste.Core.Components.Timing;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Collectibles.Refills;
using Celeste.Mods.StrawberryJam.Runtime.Refills.Shards;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;

namespace Celeste.Mods.StrawberryJam.Entities.Refills.Shards
{
    [CustomEntity("SJ2021/RefillShard")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.RefillShardController")]
    public class RefillShardController : SceneEntityState
    {
        private readonly IOriginalRefillProcess engine;
        private string playerTrackerKey;

        internal IOriginalRefillProcess Engine => engine;

        public static RefillShardController Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new(data, offset, (IOriginalRefillProcess)level.EngineContext);

        public const float RespawnTime = 3600f;

        public List<RefillShard> Shards;
        public OriginalRefillState Refill;

        private bool spawnRefill;
        private bool twoDashes;
        private bool resetOnGround;
        private bool oneUse;
        private int collectAmount;
        private Vector2[] nodes;

        private bool finished;

        public RefillShardController(EntityData data, Vector2 offset, IOriginalRefillProcess engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            spawnRefill = data.Bool("spawnRefill");
            twoDashes = data.Bool("twoDashes");
            resetOnGround = data.Bool("resetOnGround");
            oneUse = data.Bool("oneUse");
            collectAmount = data.Int("collectAmount");

            // old flcc behavior
            if (!spawnRefill && !data.Has("collectAmount"))
                oneUse = true;

            nodes = spawnRefill ? data.NodesOffset(offset) : data.NodesWithPosition(offset);
        }

        public override void Added(object scene)
        {
            base.Added(scene);

            playerTrackerKey = ((ISourceScene)scene).SourceScene.Catalog.Find(typeof(RoomPlayerSession)).RegistryKey;
            Shards = new List<RefillShard>(nodes.Length);

            for (int i = 0; i < nodes.Length; i++)
            {
                var shard = new RefillShard(this, nodes[i], i, twoDashes, resetOnGround);
                Shards.Add(shard);
                ((ISourceScene)scene).SourceScene.AddEntity(shard);
            }

            if (spawnRefill)
            {
                Refill = new OriginalRefillState(Position, twoDashes, oneUse, engine);
                ((ISourceScene)scene).SourceScene.AddEntity(Refill);

                Refill.Collidable = false;
                Refill.Depth = 8999;
                RefillShardRefillAccess.HideComponents(Refill);
                RefillShardRefillAccess.SetRespawnTimer(Refill, RespawnTime);
            }
        }

        public override void Update()
        {
            base.Update();
            if (!finished && spawnRefill)
                RefillShardRefillAccess.SetRespawnTimer(Refill, RespawnTime);
        }

        public void CheckCollection()
        {
            int collectedShards = 0;
            foreach (var shard in Shards)
                if (shard.Follower.HasLeader)
                    collectedShards++;
            if (!finished && collectedShards >= (collectAmount > 0 ? collectAmount : Shards.Count))
            {
                if (spawnRefill || (oneUse && collectedShards == Shards.Count))
                    finished = true;

                if (!spawnRefill)
                {
                    List<RefillShard> toRemove = new List<RefillShard>();
                    foreach (RefillShard shard in Shards)
                    {
                        if (shard.Follower.HasLeader)
                        {
                            shard.Collect(!oneUse);
                            if (oneUse)
                                toRemove.Add(shard);
                        }
                    }

                    foreach (RefillShard shard in toRemove)
                        Shards.Remove(shard);

                    RoomPlayerSession player =
                        ((ISourceScene)Scene).SourceScene.Entities
                        .GetTrackedEntity<RoomPlayerSession>(playerTrackerKey);
                    engine.SpatialAudio.Play(
                        twoDashes
                            ? "event:/new_content/game/10_farewell/pinkdiamond_touch"
                            : "event:/game/general/diamond_touch", player.Position);
                    player.UseRefill(twoDashes);
                    engine.RumbleSession.Rumble(1, 1);
                    engine.Freeze(0.05f);
                    ((OriginalLevelSceneRoot)Scene).Shake();
                }
                else
                {
                    float maxDist = 0f;

                    foreach (RefillShard shard in Shards)
                    {
                        shard.Finished = true;
                        if (shard.Follower.HasLeader)
                            shard.Follower.Leader.LoseFollower(shard.Follower);

                        Vector2 startPos = shard.Position;
                        Vector2 targetPos = Refill.Position;

                        float dist = ((ISourceVectorEnvironment)engine).VectorArithmetic.Length(targetPos - startPos);
                        maxDist = Math.Max(dist, maxDist);

                        var tween = SourceTweenState.Create(engine.TweenPool, SourceTweenState.TweenMode.Oneshot,
                            ((ISourceEasingEnvironment)engine).EasingArithmetic.CubeInOut, dist / 200f, true);
                        tween.OnUpdate = (t) => shard.Position = Vector2Math.Lerp(startPos, targetPos, t.Eased,
                            ((ISourceScalarEnvironment)engine).ScalarArithmetic);
                        shard.OwnedComponents.Add(tween);
                    }

                    OwnedComponents.Add(SourceAlarmState.Create(engine.AlarmPool, SourceAlarmState.AlarmMode.Oneshot,
                        () =>
                        {
                            ((ISourceScene)Scene).SourceScene.RemoveEntities(Shards);
                            Shards.Clear();
                            SpawnRefill();
                        }, maxDist / 200f, true));
                }
            }
        }

        public void SpawnRefill()
        {
            RefillShardRefillAccess.SetRespawnTimer(Refill, RespawnTime);
            RefillShardRefillAccess.Respawn(Refill);
        }
    }
}
