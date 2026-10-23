using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Holdables.Gliders;
using Celeste.Graphics.Animation;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.StrawberryJam.Entities.Holdables
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.PinkCloudyFlower")]
    [CustomEntity("SJ2021/PinkCloudyFlower")]
    public sealed class PinkCloudyFlower : OriginalGliderState
    {
        private readonly ISourceEngineContext engine;
        private readonly SourceJumpThruEntityState platform;
        private readonly Vector2 carryOffset = new(-16, -20);
        private readonly Action originalPickup;
        private readonly Action<Vector2> originalRelease;
        private bool waiting;
        private bool returning;
        private float speedY;

        public PinkCloudyFlower(ISourceEngineContext engine, EntityData data, Vector2 offset,
            ISourceSolidWorldFactory worlds)
            : base(data, offset, engine)
        {
            this.engine = engine;
            Components.Get<Sprite>().Color = PackedColor.MediumOrchid;
            originalPickup = Hold.OnPickup;
            originalRelease = Hold.OnRelease;
            Hold.OnPickup = OnPickup;
            Hold.OnRelease = OnRelease;
            var position = Position + carryOffset;
            platform = new SourceJumpThruEntityState(position, 32, false, worlds.CreateJumpThru(position, false));
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, OriginalLevelEntityData data)
            => new PinkCloudyFlower(level.EngineContext, data, offset,
                ((ISourcePlatformCollisionScene)level).SolidWorldFactory);

        public override void Added(object scene)
        {
            base.Added(scene);
            ((ISourceScene)scene).SourceScene.AddEntity(platform);
        }

        public override void Update()
        {
            base.Update();
            if (platform.GetPlayerRider() == null) Collidable = true;
            UpdatePlatform();
        }

        private void UpdatePlatform()
        {
            if (waiting)
            {
                var rider = platform.GetPlayerRider();
                if (rider != null && rider.Speed.Y >= 0f)
                {
                    Collidable = false;
                    speedY = 180f;
                    waiting = false;
                    var process = (IOriginalGameplayProcess)engine;
                    process.AudioSession.Play("event:/game/04_cliffside/cloud_blue_boost", Position,
                        process.SaveContext);
                    platform.MoveV(speedY * engine.DeltaTime);
                    platform.MoveTowardsX(Position.X + carryOffset.X, Math.Abs(Speed.X) * engine.DeltaTime);
                    return;
                }

                platform.MoveTo(Position + carryOffset);
            }
            else if (returning)
            {
                speedY = ScalarMath.Approach(speedY, 180f, 600f * engine.DeltaTime,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                platform.MoveTowardsY(Position.Y + carryOffset.Y, speedY * engine.DeltaTime);
                platform.MoveTowardsX(Position.X + carryOffset.X, Math.Abs(Speed.X) * engine.DeltaTime);
                if (platform.ExactPosition.Y != Position.Y + carryOffset.Y) return;
                returning = false;
                waiting = true;
                speedY = 0f;
            }
            else
            {
                if (platform.Y >= Position.Y + carryOffset.Y) speedY -= 1200f * engine.DeltaTime;
                else
                {
                    speedY += 1200f * engine.DeltaTime;
                    if (speedY >= -100f)
                    {
                        var rider = platform.GetPlayerRider();
                        if (rider != null && rider.Speed.Y >= 0f)
                        {
                            rider.Speed = new Vector2(rider.Speed.X, -200f);
                            Collidable = false;
                        }

                        returning = true;
                    }
                }

                float liftSpeed = speedY;
                if (liftSpeed < 0f) liftSpeed = -220f;
                platform.MoveV(speedY * engine.DeltaTime, liftSpeed);
                platform.MoveTowardsX(Position.X + carryOffset.X, Math.Abs(Speed.X) * engine.DeltaTime);
            }
        }

        private void OnPickup()
        {
            platform.AddTag(OriginalSceneTags.Persistent);
            originalPickup();
        }

        private void OnRelease(Vector2 force)
        {
            platform.RemoveTag(OriginalSceneTags.Persistent);
            originalRelease(force);
        }
    }
}
