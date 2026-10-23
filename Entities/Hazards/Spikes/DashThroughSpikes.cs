// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collision.Blockers;
using Celeste.Simulation.Gameplay.Hazards.Spikes;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Gameplay.Player.Collision.Components;

namespace Celeste.Mods.StrawberryJam.Entities.Hazards.Spikes
{
    [CustomEntity(
        "SJ2021/DashThroughSpikesUp = LoadUp",
        "SJ2021/DashThroughSpikesDown = LoadDown",
        "SJ2021/DashThroughSpikesLeft = LoadLeft",
        "SJ2021/DashThroughSpikesRight = LoadRight"
    )]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.DashThroughSpikes")]
    public class DashThroughSpikes : SceneEntityState
    {
        public static SceneEntityState LoadUp(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, EntityData data)
        {
            SpikeDirection dir = SpikeDirection.Up;
            return new DashThroughSpikes(data.Position + offset, GetSize(data, dir), dir, level.EngineContext,
                EntityAttributeReader.Attr(data.Values, "spikeType",
                    "objects/StrawberryJam2021/dashThroughSpikes/dream"));
        }

        public static SceneEntityState LoadDown(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, EntityData data)
        {
            SpikeDirection dir = SpikeDirection.Down;
            return new DashThroughSpikes(data.Position + offset, GetSize(data, dir), dir, level.EngineContext,
                EntityAttributeReader.Attr(data.Values, "spikeType",
                    "objects/StrawberryJam2021/dashThroughSpikes/dream"));
        }

        public static SceneEntityState LoadLeft(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, EntityData data)
        {
            SpikeDirection dir = SpikeDirection.Left;
            return new DashThroughSpikes(data.Position + offset, GetSize(data, dir), dir, level.EngineContext,
                EntityAttributeReader.Attr(data.Values, "spikeType",
                    "objects/StrawberryJam2021/dashThroughSpikes/dream"));
        }

        public static SceneEntityState LoadRight(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, EntityData data)
        {
            SpikeDirection dir = SpikeDirection.Right;
            return new DashThroughSpikes(data.Position + offset, GetSize(data, dir), dir, level.EngineContext,
                EntityAttributeReader.Attr(data.Values, "spikeType",
                    "objects/StrawberryJam2021/dashThroughSpikes/dream"));
        }

        public SpikeDirection Direction;
        private readonly ISourceEngineContext engine;
        private Vector2 directionVector;
        private Vector2 imageOffset;
        private int size;
        private string spikeType;

        public DashThroughSpikes(Vector2 position, int size, SpikeDirection direction, ISourceEngineContext engine,
            string spikeType) :
            base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            Position = position;
            this.engine = engine;
            Depth = -1;
            Direction = direction;
            directionVector = SpikeDirToVector(direction);
            this.size = size;
            this.spikeType = spikeType;
            switch (direction)
            {
                case SpikeDirection.Up:
                    Collider = new HitboxColliderGeometry(size, 3f, 0f, -3f);
                    OwnedComponents.Add(new LedgeBlockerState());
                    break;
                case SpikeDirection.Down:
                    Collider = new HitboxColliderGeometry(size, 3f, 0f, 0f);
                    break;
                case SpikeDirection.Left:
                    Collider = new HitboxColliderGeometry(3f, size, -3f, 0f);
                    OwnedComponents.Add(new LedgeBlockerState());
                    break;
                case SpikeDirection.Right:
                    Collider = new HitboxColliderGeometry(3f, size, 0f, 0f);
                    OwnedComponents.Add(new LedgeBlockerState());
                    break;
            }

            OwnedComponents.Add(new PlayerColliderComponentState(OnCollide));

            OwnedComponents.Add(new SourceStaticMoverState
            {
                OnShake = new Action<Vector2>(OnShake),
                SolidChecker = new Func<SourceSolidEntityState, bool>(IsRiding),
                JumpThruChecker = new Func<ISourceJumpThruEntity, bool>(IsRiding),
            });
        }

        public override void Added(object scene)
        {
            base.Added(scene);

            string dir = Direction.ToString().ToLower();

            for (int i = 0; i < size / 8; i++)
            {
                SpriteImage image =
                    new SpriteImage(
                        ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[$"{spikeType}_{dir}00"]);

                switch (Direction)
                {
                    case SpikeDirection.Up:
                        image.JustifyOrigin(0.5f, 1f);
                        image.Position = Vector2.UnitX * (i + 0.5f) * 8f + Vector2.UnitY;
                        break;
                    case SpikeDirection.Down:
                        image.JustifyOrigin(0.5f, 0f);
                        image.Position = Vector2.UnitX * (i + 0.5f) * 8f - Vector2.UnitY;
                        break;
                    case SpikeDirection.Left:
                        image.JustifyOrigin(1f, 0.5f);
                        image.Position = Vector2.UnitY * (i + 0.5f) * 8f + Vector2.UnitX;
                        break;
                    case SpikeDirection.Right:
                        image.JustifyOrigin(0f, 0.5f);
                        image.Position = Vector2.UnitY * (i + 0.5f) * 8f - Vector2.UnitX;
                        break;
                }

                OwnedComponents.Add(image);
            }
        }

        private void OnShake(Vector2 amount)
        {
            imageOffset += amount;
        }

        public override void Render(ISourceRenderContext context)
        {
            Vector2 position = Position;
            Position += imageOffset;
            base.Render(context);
            Position = position;
        }

        private void OnCollide(RoomPlayerSession player)
        {
            if (DashingIntoSpikes(player))
            {
                return;
            }

            switch (Direction)
            {
                case SpikeDirection.Up:
                    if (player.Data.Motion.Normal.Speed.Y >= 0f && player.Bottom <= Bottom)
                    {
                        player.Die(new Vector2(0f, -1f), false, true);
                        return;
                    }

                    break;
                case SpikeDirection.Down:
                    if (player.Data.Motion.Normal.Speed.Y <= 0f)
                    {
                        player.Die(new Vector2(0f, 1f), false, true);
                        return;
                    }

                    break;
                case SpikeDirection.Left:
                    if (player.Data.Motion.Normal.Speed.X >= 0f)
                    {
                        player.Die(new Vector2(-1f, 0f), false, true);
                        return;
                    }

                    break;
                case SpikeDirection.Right:
                    if (player.Data.Motion.Normal.Speed.X <= 0f)
                    {
                        player.Die(new Vector2(1f, 0f), false, true);
                    }

                    break;
                default:
                    return;
            }
        }

        private Vector2 SpikeDirToVector(SpikeDirection dir)
        {
            return dir switch
            {
                SpikeDirection.Up => -Vector2.UnitY,
                SpikeDirection.Down => Vector2.UnitY,
                SpikeDirection.Right => Vector2.UnitX,
                _ => -Vector2.UnitX, //left or default case
            };
        }

        //returns bool based on if the player is still dash type state, 
        //and their direction is toward the spikes in at least 1 direction
        private bool DashingIntoSpikes(RoomPlayerSession player)
        {
            return (player.DashAttacking
                    || player.Machine.State == 2
                    || player.Machine.State == 5
                    || player.Machine.State == 9)
                   && (Math.Sign(player.Data.Direction.X) == -Math.Sign(directionVector.X)
                       || Math.Sign(player.Data.Direction.Y) == -Math.Sign(directionVector.Y));
        }

        private static int GetSize(EntityData data, SpikeDirection dir)
        {
            if (dir > SpikeDirection.Down)
            {
                return data.Height;
            }

            return data.Width;
        }

        private bool IsRiding(SourceSolidEntityState solid)
        {
            switch (Direction)
            {
                case SpikeDirection.Up:
                    return !SourceEntityCollision.Check(this, solid) &&
                           SourceEntityCollision.CheckAt(this, solid, Position + Vector2.UnitY);
                case SpikeDirection.Down:
                    return !SourceEntityCollision.Check(this, solid) &&
                           SourceEntityCollision.CheckAt(this, solid, Position - Vector2.UnitY);
                case SpikeDirection.Left:
                    return !SourceEntityCollision.Check(this, solid) &&
                           SourceEntityCollision.CheckAt(this, solid, Position + Vector2.UnitX);
                case SpikeDirection.Right:
                    return !SourceEntityCollision.Check(this, solid) &&
                           SourceEntityCollision.CheckAt(this, solid, Position - Vector2.UnitX);
                default:
                    return false;
            }
        }

        private bool IsRiding(ISourceJumpThruEntity jumpThru)
        {
            SpikeDirection direction = Direction;
            return direction == SpikeDirection.Up &&
                   SourceEntityCollision.CheckAt(this, jumpThru, Position + Vector2.UnitY);
        }
    }
}
