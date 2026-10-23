using System;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Physics.Collision.Shapes;

namespace Celeste.Mods.StrawberryJam.Runtime.Pellets.Motion
{
    public interface IContinuousProjectileCollisionState
    {
        ISourceVectorArithmetic Vectors { get; }
        Vector2 Speed { get; set; }
        Vector2 Position { get; set; }
        float DeltaTime { get; }
        ColliderGeometry Collider { get; }
        bool CollideWithSolids { get; }
        bool SolidOutsideAt(Vector2 target);
        object FirstSolidAt(Vector2 position);
        void Impact(object solid);
    }
}
