using Celeste.Physics.Collision.Shapes;
using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using Celeste.Core.Randomness;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.StrawberryJam.Entities.Hazards.Lasers;

namespace Celeste.Mods.StrawberryJam.Entities.Hazards.Paintbrushes
{
    public partial class Paintbrush
    {
        private void emitCooldownParticles()
        {
            int amount = beamHitboxes.Length == 1 ? 3 : 1;

            foreach (var laserHitbox in beamHitboxes)
            {
                var level = ((OriginalLevelSceneRoot)Scene);
                int length = (int)Orientation.LengthOfHitbox(laserHitbox) - beamOffsetMultiplier;
                var offset = Orientation.Normal();
                float angle = Orientation.Angle() - (float)Math.PI / 2f;
                var startPos = Position + Orientation.OriginOfHitbox(laserHitbox) + beamOffset * 2;
                var particle = CassetteIndex == 0 ? particles.blueCooldownParticle : particles.pinkCooldownParticle;

                for (int i = 0; i < length; i += ((ISourceRandomEnvironment)engine).Random.Next(8, 16))
                {
                    level.ParticlesBG.Emit(particle, amount, startPos + offset * i, Vector2.Zero, angle);
                }
            }
        }

        private void emitImpactParticles(HitboxColliderGeometry laserHitbox)
        {
            var level = ((OriginalLevelSceneRoot)Scene);
            var particle = CassetteIndex == 0 ? particles.blueImpactParticle : particles.pinkImpactParticle;
            var offset = Orientation.Vertical() ? Vector2.UnitX : Vector2.UnitY;
            float angle = Orientation.Angle() + (float)Math.PI / 2f;

            int thickness = (int)Orientation.ThicknessOfHitbox(laserHitbox);
            var startPos = new Vector2(Orientation == Orientations.Right ? laserHitbox.Right + X : laserHitbox.Left + X,
                Orientation == Orientations.Down ? laserHitbox.Bottom + Y : laserHitbox.Top + Y);

            const int particleCount = 3;
            level.ParticlesFG.Emit(particle, particleCount, startPos, Vector2.Zero, angle);
            level.ParticlesFG.Emit(particle, particleCount, startPos + offset * thickness / 2, Vector2.Zero, angle);
            level.ParticlesFG.Emit(particle, particleCount, startPos + offset * thickness, Vector2.Zero, angle);
        }

        private IEnumerator impactParticlesSequence()
        {
            var laserColliders = OwnedComponents.GetAll<LaserColliderComponent>().ToArray();

            while (Scene != null)
            {
                if (State != LaserState.Firing)
                {
                    yield return null;
                    continue;
                }

                object yieldValue = null;
                foreach (var laser in laserColliders)
                {
                    if (!laser.CollidedWithScreenBounds)
                    {
                        yieldValue = impactParticleInterval;
                        emitImpactParticles(laser.Collider);
                    }
                }

                yield return yieldValue;
            }
        }
    }
}
