// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.FloatySpaceBlock;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;

namespace Celeste.Mods.StrawberryJam.Entities.Platforms.FloatyBlocks
{
    public partial class FlagFloatySpaceBlock
    {
        private DashCollisionResult OnDash(RoomPlayerSession player, Vector2 direction)
        {
            if (MasterOfGroup && dashEase <= .2f && activated && !floatTween.Active)
            {
                dashEase = 1f;
                dashDirection = direction;
            }

            return DashCollisionResult.NormalOverride;
        }

        public override void Update()
        {
            base.Update();
            if (MasterOfGroup)
            {
                if (activated != ((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag))
                {
                    activated = !activated;
                    tweenStartPosition = Position;
                    if (floatTween.Active)
                    {
                        floatTween.Stop();
                        floatTween.Start(moveTime - floatTween.TimeLeft);
                    }
                    else floatTween.Start(moveTime);
                }

                if (!activated || floatTween.Active) return;
                bool hasPlayer = false;
                foreach (var block in group)
                    if (block.HasPlayerRider())
                    {
                        hasPlayer = true;
                        break;
                    }

                if (!hasPlayer)
                    foreach (var jp in jumpthrus)
                        if (jp.HasPlayerRider())
                        {
                            hasPlayer = true;
                            break;
                        }

                if (hasPlayer) sinkTimer = .3f;
                else if (sinkTimer > 0f) sinkTimer -= engine.DeltaTime;
                if (sinkTimer > 0f)
                    yLerp = ScalarMath.Approach(yLerp, 1f, 1f * engine.DeltaTime,
                        ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                else
                    yLerp = ScalarMath.Approach(yLerp, 0f, 1f * engine.DeltaTime,
                        ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                sineWave += engine.DeltaTime;
                dashEase = ScalarMath.Approach(dashEase, 0f, engine.DeltaTime * 1.5f,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                MoveToTarget();
            }

            LiftSpeed = Vector2.Zero;
        }

        private void MoveToTarget()
        {
            float sine = (float)Math.Sin(sineWave) * 4f;
            float amount = ScalarMath.YoYo(((ISourceEasingEnvironment)engine).EasingArithmetic.QuadIn(dashEase));
            Vector2 displacement = SourceVector2Math.Multiply(SourceVector2Math.Multiply(dashDirection, amount), 8f);
            FloatyPlatformGroupMotion.Move(ref moves, sine, displacement, new FlagTarget(this));
        }

        private readonly struct FlagTarget : IFloatyPlatformTarget
        {
            private readonly FlagFloatySpaceBlock owner;
            public FlagTarget(FlagFloatySpaceBlock owner) => this.owner = owner;

            public float GetTargetY(float origin, float sine, float dashY) =>
                ((ISourceScalarEnvironment)owner.engine).ScalarArithmetic.Lerp(origin, origin + 12f,
                    ((ISourceEasingEnvironment)owner.engine).EasingArithmetic.SineInOut(owner.yLerp)) + sine + dashY;
        }

        public override void OnShake(Vector2 amount)
        {
            if (!MasterOfGroup) return;
            base.OnShake(amount);
            tiles.Position = SourceVector2Math.Add(tiles.Position, amount);
            foreach (var jp in jumpthrus)
            foreach (SourceComponentState component in jp.Components)
                if (component is SpriteImage image)
                    image.Position = SourceVector2Math.Add(image.Position, amount);
        }
    }
}
