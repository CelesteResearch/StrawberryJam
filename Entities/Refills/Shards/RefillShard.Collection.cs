using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Timing;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Collectibles.Refills;
using Celeste.Game.Gameplay.Collectibles.Strawberries;
using Celeste.Graphics.Animation;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Scene.Following;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Entities.Refills.Shards
{
    public partial class RefillShard
    {
        public void OnCollectCutscene()
        {
            Finished = true;
            Follower.Leader.LoseFollower(Follower);
            Depth = -2000002;
            Tags = OriginalSceneTags.FrozenUpdate;
        }

        public void Collect(bool respawn)
        {
            Finished = true;
            Follower.Leader.LoseFollower(Follower);
            ((IOriginalRefillScene)Scene).ParticlesFG.Emit(p_shatter, 8, Position, Vector2.One * 3f,
                SourceRandomMath.NextFloat(engine.Random, (float)Math.PI * 2f));
            if (!respawn)
            {
                RemoveSelf();
            }
            else
            {
                Collidable = false;
                renderShard = false;
                OwnedComponents.Add(SourceAlarmState.Create(engine.AlarmPool, SourceAlarmState.AlarmMode.Oneshot,
                    Respawn, 3.6f + (index * 0.1f), true));
            }
        }

        private void UpdateY()
        {
            outlineSprite.Y = flash.Y = sprite.Y = bloom.Y = sine.Value * 2f;
        }

        private void OnGainLeader()
        {
            controller.CheckCollection();
            canLoseTimer = 0.25f;
            loseTimer = LoseTime;
        }

        private void OnLoseLeader()
        {
            if (!Finished)
            {
                OwnedComponents.Add(new SimulationCoroutine(engine, ReturnRoutine()));
            }
        }

        private void OnPlayer(RoomPlayerSession player)
        {
            engine.SpatialAudio.Play("event:/game/general/seed_touch", Position, "count", index % 5);
            Collidable = false;
            Depth = -1000000;
            player.Leader.GainFollower(Follower);
        }

        private IEnumerator ReturnRoutine()
        {
            engine.SpatialAudio.Play("event:/game/general/seed_poof", Position);
            Collidable = false;
            sprite.Scale = flash.Scale = Vector2.One * 2f;
            yield return 0.05f;

            engine.RumbleSession.Rumble(1, 1);
            for (int i = 0; i < 6; i++)
            {
                float dir = SourceRandomMath.NextFloat(engine.Random, (float)Math.PI * 2f);
                ((IOriginalRefillScene)Scene).ParticlesFG.Emit(
                    ((IOriginalStrawberryProcess)engine).ParticleTypes.StrawberrySeed.P_Burst, 1,
                    Position + Vector2Geometry.AngleToVector(dir, 4f), Vector2.Zero, dir);
            }

            renderShard = false;
            yield return 0.3f + (index * 0.1f);

            Respawn();
            yield break;
        }

        private void Respawn()
        {
            Position = start;
            if (attached != null)
                Position += attached.Position;
            var sound = engine.SpatialAudio.Play(
                twoDashes
                    ? "event:/new_content/game/10_farewell/pinkdiamond_return"
                    : "event:/game/general/diamond_return", Position);
            sound.SetVolume(0.75f);
            sound.SetPitch(2f);
            ((IOriginalRefillScene)Scene).ParticlesFG.Emit(p_regen, 8, Position, Vector2.One * 2f);
            sprite.Scale = flash.Scale = Vector2.One;
            wiggler.Start();
            renderShard = true;
            Collidable = true;
            Finished = false;
        }
    }
}
