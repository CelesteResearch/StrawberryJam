using System;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Physics.Collision.Shapes;

namespace Celeste.Mods.StrawberryJam.Runtime.Pellets.Motion
{
    public static class ContinuousProjectileCollisionMotion
    {
        public static void Move<TState>(TState state) where TState : struct, IContinuousProjectileCollisionState
        {
            Vector2 delta = state.Speed * state.DeltaTime;
            Vector2 target = state.Position + delta;
            if (state.CollideWithSolids && state.SolidOutsideAt(target))
            {
                Vector2 normal = state.Vectors.SafeNormalize(delta);
                if (normal.X != 0)
                    state.Position =
                        new Vector2(
                            normal.X < 0 ? (float)Math.Ceiling(state.Position.X) : (float)Math.Floor(state.Position.X),
                            state.Position.Y);
                if (normal.Y != 0)
                    state.Position = new Vector2(state.Position.X,
                        normal.Y < 0 ? (float)Math.Ceiling(state.Position.Y) : (float)Math.Floor(state.Position.Y));
                int safety = 50;
                while (safety-- > 0)
                {
                    object solid = state.FirstSolidAt(state.Position + normal);
                    if (solid != null)
                    {
                        if (normal.X < 0)
                        {
                            state.Position = new Vector2((float)Math.Floor(state.Collider.AbsoluteLeft),
                                state.Position.Y);
                            state.Position = new Vector2(state.Position.X,
                                (float)Math.Round(state.Collider.CenterY + state.Position.Y));
                        }
                        else if (normal.X > 0)
                        {
                            state.Position = new Vector2((float)Math.Ceiling(state.Collider.AbsoluteRight),
                                state.Position.Y);
                            state.Position = new Vector2(state.Position.X,
                                (float)Math.Round(state.Collider.CenterY + state.Position.Y));
                        }
                        else if (normal.Y < 0)
                        {
                            state.Position = new Vector2(state.Position.X,
                                (float)Math.Floor(state.Collider.AbsoluteTop));
                            state.Position = new Vector2((float)Math.Round(state.Collider.CenterX + state.Position.X),
                                state.Position.Y);
                        }
                        else if (normal.Y > 0)
                        {
                            state.Position = new Vector2(state.Position.X,
                                (float)Math.Ceiling(state.Collider.AbsoluteBottom));
                            state.Position = new Vector2((float)Math.Round(state.Collider.CenterX + state.Position.X),
                                state.Position.Y);
                        }

                        state.Speed = Vector2.Zero;
                        state.Impact(solid);
                        return;
                    }

                    state.Position += normal;
                }
            }

            state.Position = target;
        }
    }
}
