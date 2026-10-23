using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Maps;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Mechanisms.DashSwitches;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;

namespace Celeste.Mods.StrawberryJam.Entities.Switches
{
    [CustomEntity("SJ2021/FlagDashSwitch")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.FlagDashSwitch")]
    public class FlagDashSwitch : OriginalDashSwitchState
    {
        private readonly string flag;
        private readonly bool target;
        private readonly OriginalDashSwitchSide side;
        private readonly string playerKey;
        private readonly ISourceEngineContext engine;
        private Vector2 spriteOffset;
        private SourceStaticMoverState mover;
        internal ISourceEngineContext EngineContext => engine;
        internal void SetTargetFlag() => ((OriginalLevelSceneRoot)Scene).Session.SetFlag(flag, target);

        internal void TriggerAttachedPlatform(Vector2 direction)
        {
            if (!SourcePressed && direction == SourcePressDirection && mover != null) mover.TriggerPlatform();
        }

        public FlagDashSwitch(ISourceEngineContext engine, EntityData data, Vector2 offset, MapEntityId id,
            ISourceSolidWorld world) : base(data.Position + offset,
            ChooseFacing(EntityAttributeReader.Enum(data.Values, "orientation", OriginalDashSwitchSide.Up)),
            data.Bool("persistent", false), false, id, data.Attr("sprite", "default"), engine, world)
        {
            this.engine = engine;
            side = ChooseFacing(EntityAttributeReader.Enum(data.Values, "orientation", OriginalDashSwitchSide.Up));
            target = data.Bool("flagTargetValue", true);
            flag = data.Attr("flag", "");
            playerKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(RoomPlayerSession))
                .RegistryKey;
            if (data.Bool("attach", false))
                Add(mover = new SourceStaticMoverState
                {
                    OnMove = StaticMoverMove, OnEnable = OnEnable,
                    OnAttach = p => Depth = ((SourcePlatformEntityState)p).Depth + 1, OnShake = OnMoverShake,
                    SolidChecker = CheckAttachedSolid
                });
            if (side is OriginalDashSwitchSide.Up or OriginalDashSwitchSide.Down)
            {
                Collider.Width = 16f;
                Collider.Height = 6f;
            }
            else
            {
                Collider.Width = 7f;
                Collider.Height = 16f;
            }

            if (side == OriginalDashSwitchSide.Left) Collider.Position += Vector2.UnitX;
            else if (side == OriginalDashSwitchSide.Up) Collider.Position += Vector2.UnitY;
            else if (side == OriginalDashSwitchSide.Down) Collider.Height -= 1f;
        }

        public static FlagDashSwitch Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new(level.EngineContext, data, offset, new MapEntityId(room.Name, data.ID),
            ((ISourcePlatformCollisionScene)level).SolidWorldFactory.Create(data.Position + offset, true));

        public override void Added(object scene)
        {
            base.Added(scene);
            var session = ((OriginalLevelSceneRoot)Scene).Session;
            if (session.GetFlag(flag) == target)
            {
                if (!SourcePersistent) session.SetFlag(flag, false);
                else SourcePressed = true;
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            var position = Position;
            Position += spriteOffset;
            base.Render(context);
            Position = position;
        }

        private static OriginalDashSwitchSide ChooseFacing(OriginalDashSwitchSide side) => side switch
        {
            OriginalDashSwitchSide.Up => OriginalDashSwitchSide.Down,
            OriginalDashSwitchSide.Down => OriginalDashSwitchSide.Up,
            OriginalDashSwitchSide.Left => OriginalDashSwitchSide.Right,
            OriginalDashSwitchSide.Right => OriginalDashSwitchSide.Left,
            _ => throw new Exception("Unknown FlagDashSwitch direction!")
        };

        private bool CheckAttachedSolid(SourceSolidEntityState solid)
        {
            Vector2 at = side switch
            {
                OriginalDashSwitchSide.Down => Position + Vector2.UnitY * 4f,
                OriginalDashSwitchSide.Up => Position - Vector2.UnitY * 4f,
                OriginalDashSwitchSide.Left => Position - Vector2.UnitX * 2f,
                OriginalDashSwitchSide.Right => Position + Vector2.UnitX * 2f, _ => Position
            };
            return !SourceEntityCollision.Check(this, solid) && SourceEntityCollision.CheckAt(this, solid, at);
        }

        private void OnMoverShake(Vector2 amount) => spriteOffset += amount;

        private void StaticMoverMove(Vector2 amount)
        {
            if (side == OriginalDashSwitchSide.Down && !SourcePressed) SourceStartY += amount.Y;
            Position += amount;
            SourcePressedTarget += amount;
            var rider = GetPlayerRider();
            if (rider != null)
            {
                rider.MoveV(amount.Y);
                rider.MoveH(amount.X);
            }
        }

        private void OnEnable()
        {
            Collidable = !SourcePressed;
            Active = Visible = true;
            Speed = Vector2.Zero;
            var player = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(playerKey);
            if (player != null && SourceEntityCollision.Check(player, this)) StopClip(player);
        }

        private float StopClip(RoomPlayerSession player, int offset = 0)
        {
            switch (side)
            {
                case OriginalDashSwitchSide.Down: return player.Bottom = Top + offset * Math.Sign(Top);
                case OriginalDashSwitchSide.Up: return player.Top = Bottom + offset * Math.Sign(Bottom);
                case OriginalDashSwitchSide.Left: return player.Left = Right + offset * Math.Sign(Right);
                case OriginalDashSwitchSide.Right: return player.Right = Left + offset * Math.Sign(Left);
                default: return 0f;
            }
        }
    }
}
