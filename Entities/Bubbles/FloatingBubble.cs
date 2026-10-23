using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Riding;
using Celeste.Physics.Collision.Shapes;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Interaction.Springs;
using Celeste.Game.Gameplay.Mechanisms.Switches;
using Celeste.Graphics.Animation;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collision;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Interaction.Springs;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Mods.MaxHelpingHand.Entities.Switches;
using Celeste.Mods.StrawberryJam.Components.Bubbles;
using Celeste.Mods.StrawberryJam.Runtime.Bubbles;

namespace Celeste.Mods.StrawberryJam.Entities.Bubbles
{
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.FloatingBubble")]
    public class FloatingBubble : SourceActorEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly ISourceExtremaArithmetic scalars;
        private readonly Sprite sprite;
        private Vector2 Speed;
        private float NoFloatTimer, springCooldownTimer;
        private bool broken;
        private string colliderKey;

        public FloatingBubble(FloatingBubbleProcess owner, Vector2 position) : base(new PixelActor(null, position),
            owner.Engine, ((ISourcePlatformTargetMotionEnvironment)owner.Engine).TargetMotionPolicy)
        {
            engine = owner.Engine;
            scalars = ((ISourceExtremaEnvironment)engine).ExtremaArithmetic;
            Collider = new HitboxColliderGeometry(14, 14, -7, -7);
            Add(new Celeste.Simulation.Gameplay.Player.Collision.Components.PlayerColliderComponentState(OnPlayer));
            Add(sprite = owner.Module.SpriteBank.Create("bubble"));
            sprite.OnFinish = OnAnimationFinished;
            sprite.CenterOrigin();
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            var tracker = ((ISourceScene)scene).SourceScene.Components;
            colliderKey = tracker.Catalog.Find(typeof(BubbleCollider)).RegistryKey;
        }

        public override bool IsRiding(ISourceJumpThruCollider jumpThru)
        {
            return false;
        }

        public override bool IsRiding(ISourceEntityCollider solid)
        {
            return false;
        }

        public override void Update()
        {
            base.Update();
            Vector2 ActualSpeed = Speed;
            if (springCooldownTimer > 0)
            {
                springCooldownTimer -= engine.DeltaTime;
            }

            if (NoFloatTimer > 0)
            {
                NoFloatTimer -= engine.DeltaTime;
            }
            else
            {
                ActualSpeed += new Vector2(0, -60f);
            }

            Position += ActualSpeed * engine.DeltaTime;
            Speed.X = ScalarMath.Approach(Speed.X, 0, 40f * engine.DeltaTime, scalars);
            Speed.Y = ScalarMath.Approach(Speed.Y, -60f, 20f * engine.DeltaTime, scalars);
            if (SourceSceneCollisionQueries.Check(this,
                    ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities("Celeste.Solid")))
            {
                Burst();
            }

            var levelBounds = ((OriginalLevelSceneRoot)Scene).Bounds;
            if ((Position.X > levelBounds.Right + 10 || Position.X < levelBounds.Left - 10) ||
                (Position.Y > levelBounds.Bottom + 10 || Position.Y < levelBounds.Top - 10))
            {
                Burst();
            }

            foreach (BubbleCollider collider in ((ISourceScene)Scene).SourceScene.Components.GetTrackedComponents(
                         colliderKey))
            {
                if (collider.Check(this))
                {
                    if (collider.Entity is OriginalSpringState spring)
                    {
                        if (springCooldownTimer <= 0)
                        {
                            HitSpring(spring);
                            spring.AnimateSpringBounce();
                        }
                    }
                    else if (collider.Entity is OriginalTouchSwitchState)
                    {
                        (collider.Entity as OriginalTouchSwitchState).TurnOn();
                    }
                    else if (collider.Entity is FlagTouchSwitch)
                    {
                        (collider.Entity as FlagTouchSwitch).TurnOn();
                    }
                }
            }

            if (sprite.CurrentAnimationID == "pop")
            {
                if (sprite.CurrentAnimationFrame == 1)
                {
                    if (broken == false)
                    {
                        Collidable = false;
                        Vector2 position = Position + new Vector2(0f, 1f) +
                                           Vector2Geometry.AngleToVector(
                                               SourceRandomMath.NextAngle(((ISourceRandomEnvironment)engine).Random),
                                               5f);
                        ((OriginalLevelSceneRoot)Scene).ParticlesFG.Emit(
                            ((IOriginalGameplayProcess)engine).ParticleTypes.Player.P_CassetteFly, 10, position,
                            new Vector2(8, 8), PackedColor.White, 0);
                        ((OriginalLevelSceneRoot)Scene).Displacement.AddBurst(Position, 0.6f, 4f, 28f, 0.2f);
                        ((IOriginalGameplayProcess)engine).AudioSession.Play(
                            "event:/strawberry_jam_2021/game/bubble_emitter/bubble_pop", Position,
                            ((IOriginalGameplayProcess)engine).SaveContext);
                        broken = true;
                    }
                }
            }
        }

        public bool HitSpring(OriginalSpringState spring)
        {
            springCooldownTimer = 0.05f;
            switch (spring.Orientation)
            {
                case SpringOrientation.WallLeft:
                    MoveTowardsY(spring.CenterY + 5f, 4f);
                    Speed.X = 160f;
                    Speed.Y = -80f;
                    NoFloatTimer = 0.1f;
                    break;
                case SpringOrientation.WallRight:
                    MoveTowardsY(spring.CenterY + 5f, 4f);
                    Speed.X = -160f;
                    Speed.Y = -80f;
                    NoFloatTimer = 0.1f;
                    break;
                case SpringOrientation.Floor:
                    Speed.X *= 0.5f;
                    Speed.Y = -160f;
                    NoFloatTimer = 0.15f;
                    springCooldownTimer += 0.2f;
                    break;
            }

            return true;
        }

        public void Burst()
        {
            sprite.Play("pop");
        }

        public void OnPlayer(RoomPlayerSession player)
        {
            player.SuperBounce(Top);
            Burst();
        }

        public void OnAnimationFinished(string id)
        {
            Remove(sprite);
            RemoveSelf();
        }
    }
}
