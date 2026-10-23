// SPDX-License-Identifier: MIT

using System.Linq;
using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Hazards.FireBalls;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Environment.Core;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Entities.Hazards.FireBalls
{
    [CustomEntity("SJ2021/BeeFireball")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.BeeFireball")]
    public class BeeFireball : OriginalFireBallState
    {
        private readonly StrawberryJamModule module;
        protected override ParticleTypePresentation FireTrailParticle => module.BeeTrailParticle;

        private bool isFacingLeft;
        private bool isFacingLeftAtStartOfTrack;

        public BeeFireball(ISourceEngineContext engine, EntityData data, Vector2 offset) : this(engine,
            data.NodesWithPosition(offset), EntityAttributeReader.Int(data.Values, "amount", 1), 0,
            EntityAttributeReader.Float(data.Values, "offset", 0),
            EntityAttributeReader.Float(data.Values, "speed", 1f))
        {
        }

        public BeeFireball(ISourceEngineContext engine, Vector2[] nodes, int amount, int index, float offset,
            float speedMult) : base(nodes, amount, index, offset, speedMult, notCoreMode: true,
            (IOriginalFireBallProcess)engine)
        {
            module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<StrawberryJamModule>().Single();
            // replace fireball sprites with bee sprites
            module.SpriteBank.CreateOn(FireBallSprite, "beeFireball");
            FireBallSprite.Play("idle", restart: false, randomizeFrame: true);

            // this fireball doesn't support core mode switching, so just make sure.
            OwnedComponents.Remove(Get<CoreModeListenerState>());

            // lower the hitbox a bit to match the FireBallSprite.
            Collider.CenterY += 2;
        }

        // Added method similar to vanilla, except iceMode is forced to false, and amount > 1 instantiates more bee fireballs.
        public override void Added(object scene)
        {
            AddedAsEntity(scene);

            IceMode = false;
            CurrentSpeedMultiplier = 1f;

            if (InstanceIndex == 0)
            {
                for (int i = 1; i < InstanceAmount; i++)
                {
                    ((ISourceScene)Scene).SourceScene.QueueSourceEntity(new BeeFireball(
                        ((OriginalLevelSceneRoot)scene).EngineContext, PathNodes, InstanceAmount, i, PathOffset,
                        PathSpeedMultiplier));
                }
            }

            TrackSound?.RemoveSelf();
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);

            // what will be the initial facing? determine it by computing (position at PathPercent + 0.01) - (position at PathPercent).
            float firstMoveX = GetPercentPosition((PathPercent + 0.01f) % 1f).X - GetPercentPosition(PathPercent).X;
            if (firstMoveX < 0)
            {
                // facing left!
                FireBallSprite.Scale.X = -1;
                isFacingLeft = true;
            }

            // check if the bee is facing left at the start of the track (if it is moving left between 0% and 1% of the track).
            isFacingLeftAtStartOfTrack = GetPercentPosition(0f).X > GetPercentPosition(0.01f).X;
        }

        public override void Update()
        {
            Vector2 prev = Position;
            float prevPercent = PathPercent;

            // The inherited update reads this entity's transparent trail override.
            base.Update();

            // determine if we should be facing left by ... determining if we just moved left.
            float moveX = Position.X - prev.X;

            // if we didn't move horizontally, don't change the facing.
            if (moveX == 0)
            {
                return;
            }

            // if the percentage was reset, this means we teleported back to the start of the track...
            if (PathPercent < prevPercent)
            {
                // ... so we need to adjust the facing immediately.
                FireBallSprite.Play("idle");
                FireBallSprite.Scale.X = isFacingLeftAtStartOfTrack ? -1 : 1;
                isFacingLeft = isFacingLeftAtStartOfTrack;
                return;
            }

            bool shouldFaceLeft = (moveX < 0);

            if (shouldFaceLeft == isFacingLeft)
            {
                if (FireBallSprite.CurrentAnimationID == "idle")
                {
                    // ensure facing is enforced (in particular if turning animation just finished).
                    FireBallSprite.Scale.X = isFacingLeft ? -1 : 1;
                }
            }
            else
            {
                // we should turn around!
                // the FireBallSprite should be flipped if we should face **right**, because the animation is for turning **right to left**.
                FireBallSprite.Scale.X = shouldFaceLeft ? 1 : -1;

                if (FireBallSprite.CurrentAnimationID == "idle")
                {
                    // start rotating
                    FireBallSprite.Play("rotate");
                }
                else
                {
                    // rotating while already rotating: if there are 8 frames and we are at frame 6, restart playing the animation from frame 2 for example.
                    FireBallSprite.SetAnimationFrame(FireBallSprite.CurrentAnimationTotalFrames -
                                                     FireBallSprite.CurrentAnimationFrame);
                }

                isFacingLeft = shouldFaceLeft;
            }
        }
    }
}
