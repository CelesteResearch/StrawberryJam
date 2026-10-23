using System;
using Celeste.Core.Reflection.Runtime;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Core.Rendering;
using Celeste.Core.Mathematics;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Rendering;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Components.Oscillation;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Gameplay.Collision.Blockers;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;

namespace Celeste.Mods.StrawberryJam.Entities.Hazards.Lasers
{
    /// <summary>
    /// Calculates the collider for the beam part of a laser emitter.
    /// </summary>
    /// <remarks>
    /// The size of the laser hitbox is calculated using a binary search algorithm for performance.
    /// </remarks>
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.LaserColliderComponent")]
    public class LaserColliderComponent : SourceComponentState
    {
        public float Thickness { get; set; }
        public bool CollideWithSolids { get; set; }
        public HitboxColliderGeometry Collider { get; } = new HitboxColliderGeometry(0, 0);
        public Vector2 Offset { get; set; }
        public bool CollidedWithScreenBounds { get; private set; }

        public LaserColliderComponent() : this(Vector2.Zero)
        {
        }

        public LaserColliderComponent(Vector2 offset) : base(true, false)
        {
            Offset = offset;
        }

        public override void EntityAdded(object scene)
        {
            base.EntityAdded(scene);
            UpdateBeam(true);
        }

        public override void EntityAwake()
        {
            base.EntityAwake();
            UpdateBeam(true);
        }

        public override void Update()
        {
            base.Update();
            UpdateBeam();
        }

        private void resizeHitboxColliderGeometry(float size)
        {
            if (!(Entity is OrientableEntity orientableEntity)) return;

            switch (orientableEntity.Orientation)
            {
                case OrientableEntity.Orientations.Up:
                    Collider.Width = Thickness;
                    Collider.Height = size;
                    Collider.BottomCenter = Offset;
                    break;

                case OrientableEntity.Orientations.Down:
                    Collider.Width = Thickness;
                    Collider.Height = size;
                    Collider.TopCenter = Offset;
                    break;

                case OrientableEntity.Orientations.Left:
                    Collider.Width = size;
                    Collider.Height = Thickness;
                    Collider.CenterRight = Offset;
                    break;

                case OrientableEntity.Orientations.Right:
                    Collider.Width = size;
                    Collider.Height = Thickness;
                    Collider.CenterLeft = Offset;
                    break;
            }
        }

        public void UpdateBeam(bool fromEntityAdded = false)
        {
            if (!(Entity is OrientableEntity orientableEntity)) return;
            var level = SceneAs<OriginalLevelSceneRoot>();

            float high = orientableEntity.Orientation switch
            {
                OrientableEntity.Orientations.Up => orientableEntity.Position.Y + Offset.Y - level.Bounds.Top,
                OrientableEntity.Orientations.Down => level.Bounds.Bottom - orientableEntity.Position.Y - Offset.Y,
                OrientableEntity.Orientations.Left => orientableEntity.Position.X + Offset.X - level.Bounds.Left,
                OrientableEntity.Orientations.Right => level.Bounds.Right - orientableEntity.Position.X - Offset.X,
                _ => 0
            };

            int low = 0, safety = 1000;

            // force non-collidable invisible barriers to be collidable if our entity was just added
            List<SceneEntityState> barriers = null;
            if (fromEntityAdded && CollideWithSolids)
            {
                barriers = new List<SceneEntityState>();
                barriers.AddRange(((ISourceScene)level).SourceScene.Entities
                    .GetTrackedEntities("Celeste.InvisibleBarrier").Cast<SceneEntityState>()
                    .Where(ib => !ib.Collidable));
                barriers.ForEach(ib => ib.Collidable = true);
            }

            // first check if the laser hits the edge of the screen
            resizeHitboxColliderGeometry(high);
            CollidedWithScreenBounds = !CollideWithSolids || !solidCollideCheck();
            if (!CollidedWithScreenBounds)
            {
                // perform a binary search to hit the nearest solid
                while (safety-- > 0)
                {
                    int pivot = (int)(low + (high - low) / 2f);
                    resizeHitboxColliderGeometry(pivot);
                    if (pivot == low)
                        break;
                    if (solidCollideCheck())
                    {
                        high = pivot;
                    }
                    else
                    {
                        low = pivot;
                    }
                }
            }

            // reset collidable for those we modified
            barriers?.ForEach(ib => ib.Collidable = false);
        }

        private bool solidCollideCheck()
        {
            var oldCollider = ((SceneEntityState)Entity).Collider;
            ((SceneEntityState)Entity).Collider = Collider;
            bool didCollide = SourceTrackedEntityCollision.Check(((ISourceScene)Scene).SourceScene.Entities,
                "Celeste.Solid", (ISourceEntityCollider)Entity);
            ((SceneEntityState)Entity).Collider = oldCollider;
            return didCollide;
        }
    }
}
