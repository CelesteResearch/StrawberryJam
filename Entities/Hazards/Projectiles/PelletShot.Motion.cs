using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Mods.StrawberryJam.Runtime.Pellets.Motion;

namespace Celeste.Mods.StrawberryJam.Entities.Hazards.Projectiles
{
    public sealed partial class PelletShot
    {
        private void Move() => ContinuousProjectileCollisionMotion.Move(new CollisionMotionState(this));

        private readonly struct CollisionMotionState(PelletShot owner) : IContinuousProjectileCollisionState
        {
            public Vector2 Speed
            {
                get => owner.Speed;
                set => owner.Speed = value;
            }

            public Vector2 Position
            {
                get => owner.Position;
                set => owner.Position = value;
            }

            public ISourceVectorArithmetic Vectors => owner.vectors;
            public float DeltaTime => owner.engine.DeltaTime;
            public ColliderGeometry Collider => owner.Collider;
            public bool CollideWithSolids => owner.CollideWithSolids;

            public bool SolidOutsideAt(Vector2 target) =>
                SourceTrackedEntityCollision.FirstOutsideAt<object, PelletShot>(
                    ((ISourceScene)owner.Scene).SourceScene.Entities, "Celeste.Solid", owner, target) != null;

            public object FirstSolidAt(Vector2 position) =>
                SourceTrackedEntityCollision.FirstAt<object>(((ISourceScene)owner.Scene).SourceScene.Entities,
                    "Celeste.Solid", owner, position);

            public void Impact(object solid) => owner.Impact(solid is not ISourceSolidTilesEntity);
        }
    }
}
