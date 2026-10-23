using System;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Mathematics;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation;

namespace Celeste.Mods.StrawberryJam.Entities.Hazards.Projectiles
{
    public sealed partial class PelletShot
    {
        public override void Update()
        {
            base.Update();
            if (Dead) return;
            impactSprite.Visible = impactSprite.Animating;
            if (!Collidable)
            {
                if (!impactSprite.Animating) Destroy();
                return;
            }

            Vector2 newCentre = vectors.SafeNormalize(new Vector2(-Speed.Y, Speed.X)) * travelSineWave.Value *
                                wiggleAmount;
            newCentre += hitDir * hitWiggler.Value * 8f;
            killHitbox.Center = newCentre;
            projectileSprite.Position = newCentre;
            if (Scene is not OriginalLevelSceneRoot level) return;
            if (delayTimeRemaining > 0)
            {
                delayTimeRemaining -= engine.DeltaTime;
                if (delayTimeRemaining > 0) return;
                projectileSprite.Visible = true;
                projectileSprite.Play(projectileAnimationKey);
                parentEmitter?.PlayFireSound(firedCassetteIndex);
            }

            Move();
            if (level.OnInterval(.05f))
                level.ParticlesBG.Emit(particleType, 1, Center, Vector2.One * 2f, Vector2Geometry.Angle(-Speed));
            if (!level.IsInBounds(this)) Destroy();
        }

        private void Impact(bool air)
        {
            projectileSprite.Stop();
            projectileSprite.Visible = false;
            impactSprite.Play(air ? impactAnimationKey + "_air" : impactAnimationKey);
            impactSprite.Visible = true;
            Collidable = false;
            killHitbox.Center = projectileSprite.Position = Vector2.Zero;
            if (Scene is OriginalLevelSceneRoot level && Right >= level.Camera.Left - 32f &&
                Left <= level.Camera.Right + 32f && Bottom >= level.Camera.Top - 32f &&
                Top <= level.Camera.Bottom + 32f)
                ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/sj21_mosscairn_sfx/emitter_impact",
                    Center);
        }

        private void OnPlayerCollide(RoomPlayerSession player)
        {
            Vector2 direction = vectors.SafeNormalize(player.Center - Position);
            if (player.Die(direction) == null) return;
            Speed = Vector2.Zero;
            travelSineWave.Active = false;
            hitDir = direction;
            hitWiggler.Start();
        }
    }
}
