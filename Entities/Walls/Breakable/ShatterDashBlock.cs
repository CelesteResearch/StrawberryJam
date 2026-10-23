using System;
using System.Numerics;
using Celeste.Core.Maps;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Audio.Surfaces;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Engine.Actions;
using Celeste.Game.Presentation.Walls.Breakable;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;

namespace Celeste.Mods.StrawberryJam.Entities.Walls.Breakable
{
    [CustomEntity("SJ2021/ShatterDashBlock")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.ShatterDashBlock")]
    public class ShatterDashBlock : SourceSolidEntityState
    {
        private bool permanent;
        private MapEntityId id;
        private char tileType;
        private float width;
        private float height;
        private bool blendIn;
        private float speedReq;
        private float delay;
        private float speedDec;
        private float shakeTime;

        public ShatterDashBlock(EntityData data, Vector2 offset, MapEntityId id, ISourceSolidWorldFactory worlds) :
            base(data.Position + offset, data.Width, data.Height, true, worlds.Create(data.Position + offset, true))
        {
            Depth = -12999;
            this.id = id;
            permanent = EntityAttributeReader.Bool(data.Values, "permanent");
            width = data.Width;
            height = data.Height;
            blendIn = EntityAttributeReader.Bool(data.Values, "blendin");
            tileType = EntityAttributeReader.Char(data.Values, "tiletype", '3');
            delay = ScalarMath.Clamp(EntityAttributeReader.Float(data.Values, "FreezeTime", .1f), 0, .5f,
                ((Celeste.Core.Mathematics.Extrema.ISourceExtremaEnvironment)SourceWorld.EngineContext)
                .ExtremaArithmetic);
            speedReq = Math.Max(0f, EntityAttributeReader.Float(data.Values, "SpeedRequirement", 0f));
            speedDec = Math.Max(0f, EntityAttributeReader.Float(data.Values, "SpeedDecrease", 80f));
            shakeTime = Math.Max(0f, EntityAttributeReader.Float(data.Values, "ShakeTime", .3f));
            OnDashCollide = OnDashed;
            SurfaceSoundIndex =
                ((IOriginalTileSurfaceEnvironment)SourceWorld.EngineContext).SurfaceIndices.TileToIndex[tileType];
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new ShatterDashBlock(data, offset, level.CreateEntityId(room, data),
            ((ISourcePlatformCollisionScene)level).SolidWorldFactory);

        public override void Awake(object scene)
        {
            base.Awake(scene);
            BreakableTileBlockVisuals.AddTiles(this, SourceWorld, ref tileType, blendIn, ref width, ref height);
            if (SourceTrackedEntityCollision.Check(((ISourceScene)Scene).SourceScene.Entities,
                    typeof(RoomPlayerSession).FullName, this)) RemoveSelf();
        }

        public void Break(RoomPlayerSession player, Vector2 direction, bool playSound = true,
            bool playDebrisSound = true)
        {
            if (playSound) BreakableTileBlockEffects.PlayBreakSound(SourceWorld, tileType, this);
            BreakableTileBlockEffects.EmitDebris(this, SourceWorld, ref tileType, playDebrisSound, player);
            OriginalCelesteFreeze.Freeze(SourceWorld.EngineContext, delay);
            Collidable = false;
            ((OriginalLevelSceneRoot)Scene).DirectionalShake(direction * -1, shakeTime);
            player.Speed -=
                ((ISourceVectorEnvironment)SourceWorld.EngineContext).VectorArithmetic.RotateTowards(Vector2.UnitX,
                    Vector2Geometry.Angle(direction), 6.3f) * speedDec;
            if (permanent) RemoveAndFlagAsGone();
            else RemoveSelf();
        }

        public void RemoveAndFlagAsGone()
        {
            RemoveSelf();
            ((OriginalLevelSceneRoot)Scene).Session.DoNotLoad.Add(id);
        }

        private DashCollisionResult OnDashed(RoomPlayerSession player, Vector2 direction)
        {
            if (Math.Abs(player.Speed.X) > speedReq)
            {
                Break(player, direction, true);
                return DashCollisionResult.Ignore;
            }

            return DashCollisionResult.NormalCollision;
        }

        public void Break(RoomPlayerSession player, Vector2 direction, bool playSound = true) =>
            Break(player, direction, playSound, true);
    }
}
