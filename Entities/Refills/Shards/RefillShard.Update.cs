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
        public override void Update()
        {
            base.Update();

            if (!Finished && resetOnGround)
            {
                if (canLoseTimer > 0f)
                {
                    canLoseTimer -= engine.DeltaTime;
                }
                else if (Follower.HasLeader && (Follower.Leader.Entity as RoomPlayerSession).LoseShards)
                {
                    losing = true;
                }

                if (losing)
                {
                    var player = Follower.Leader.Entity as RoomPlayerSession;
                    if (loseTimer <= 0f || player.Data.Motion.Normal.Speed.Y < 0f)
                    {
                        player.Leader.LoseFollower(Follower);
                        losing = false;
                    }
                    else if (player.LoseShards)
                    {
                        loseTimer -= engine.DeltaTime;
                    }
                    else
                    {
                        loseTimer = LoseTime;
                        losing = false;
                    }
                }
            }

            if (!Finished && Visible && ((ISourceScene)Scene).SourceScene.Clock.OnInterval(0.1f, engine))
                ((IOriginalRefillScene)Scene).ParticlesFG.Emit(p_glow, 1, Position, Vector2.One * 4f);

            if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(2f, engine) && Visible)
            {
                flash.Play("flash", true);
                flash.Visible = true;
            }

            light.Alpha = ScalarMath.Approach(light.Alpha, Visible ? 1f : 0f, 4f * engine.DeltaTime,
                engine.ExtremaArithmetic);
            bloom.Alpha = light.Alpha * 0.8f;
            UpdateY();
        }
    }
}
