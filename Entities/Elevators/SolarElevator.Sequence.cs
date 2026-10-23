using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Scheduling;
using Celeste.Core.Rendering;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Audio;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Components.Talking;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Rendering;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Riding;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Holdables;

namespace Celeste.Mods.StrawberryJam.Entities.Elevators
{
    public partial class SolarElevator
    {
        private IEnumerator Sequence(RoomPlayerSession player)
        {
            var level = (OriginalLevelSceneRoot)Scene;
            if (player.Holding.Holding != null) player.Drop();
            var components = ((ISourceScene)Scene).SourceScene.Components.GetTrackedComponents(holdableKey);
            foreach (var component in components)
            {
                var hold = (HoldableState)component;
                if (CanCarry(hold)) hold.SetCannotHoldTimerForQuery(delay + time);
            }

            Moving = true;
            interaction.Enabled = false;
            UpdateCollider(false);
            yield return delay;
            sfx.Play(moveSfx);
            level.DirectionalShake(Vector2.UnitY, .15f);
            float start = Y;
            float end = AtGroundFloor ? start - Distance : start + Distance;
            float t = 0;
            while (t < time)
            {
                float amount = t / time;
                float at = start + (end - start) * amount;
                MoveToY(at);
                t = ScalarMath.Approach(t, time, Engine.DeltaTime);
                yield return null;
            }

            MoveToY(end);
            sfx.Stop();
            ((ISourceSpatialAudio)Engine).Play(haltSfx, Position);
            level.DirectionalShake(Vector2.UnitY, .2f);
            UpdateCollider(true);
            AtGroundFloor = !AtGroundFloor;
            if (!oneWay)
            {
                Moving = false;
                interaction.Enabled = true;
            }
        }

        protected override void PushActorV(ISourcePlatformActor actor, int move, float top, float bottom)
        {
            if (Collider is not ColliderListGeometry list)
            {
                base.PushActorV(actor, move, top, bottom);
                return;
            }

            var colliders = list.colliders;
            for (int i = 0; i < colliders.Length; i++)
            {
                var hitbox = (HitboxColliderGeometry)colliders[i];
                if (hitbox.Collide(actor.Collider))
                {
                    float a = Y + hitbox.Top;
                    float b = Y + hitbox.Bottom;
                    int push = move <= 0 ? (int)(a - actor.Bottom) : (int)(b - actor.Top);
                    Collidable = false;
                    actor.MoveVExact(push, actor.SquishCallback, this);
                    actor.LiftSpeed = LiftSpeed;
                    Collidable = true;
                }
            }
        }
    }
}
