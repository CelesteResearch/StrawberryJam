using System;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Interaction.Springs;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Interaction.Springs;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Gameplay.Hazards.Puffer;
using Celeste.Mods.StrawberryJam.Entities.Holdables.Lanterns;

namespace Celeste.Mods.StrawberryJam.Entities.Springs
{
    [CustomEntity("SJ2021/UpsidedownSpring")]
    public sealed class UpsidedownSpring : OriginalSpringState
    {
        public readonly float strength, xAxisFriction;

        public UpsidedownSpring(ISourceEngineContext engine, EntityData data, Vector2 offset) :
            this(engine, data.Position + offset, data.Float("strength", 1), data.Float("xAxisFriction", .5f))
        {
        }

        public UpsidedownSpring(ISourceEngineContext engine, Vector2 position, float strength, float xAxisFriction) :
            base(position, SpringOrientation.Floor, false, (IOriginalSpringProcess)engine)
        {
            this.strength = strength;
            this.xAxisFriction = xAxisFriction;
            Remove(Get<PlayerColliderComponentState>());
            Remove(Get<PufferColliderState>());
            Remove(Get<HoldableColliderState>());
            Add(new HoldableColliderState(OnHoldable));
            SpringSprite.Position += Vector2.UnitY * 7;
            SpringSprite.Rotation = (float)Math.PI;
            Collider = new HitboxColliderGeometry(16, 6, -8, 7);
            SpringStaticMover.SolidChecker =
                solid => SourceEntityCollision.CheckAt(this, solid, position - Vector2.UnitY);
            SpringStaticMover.JumpThruChecker = jumpThru => false;
        }

        private void OnHoldable(HoldableState holdable)
        {
            if (holdable.Entity is SkyLantern)
            {
                holdable.HitSpring(this);
                AnimateSpringBounce();
            }
        }

        public static UpsidedownSpring Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new(level.EngineContext, data, offset);
    }
}
