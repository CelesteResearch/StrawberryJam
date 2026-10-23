// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Celeste.Core.Components.Animation;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.LinearAlgebra;

namespace Celeste.Mods.StrawberryJam.Entities.Platforms.FloatyBlocks
{
    public partial class FlagFloatySpaceBlock
    {
        private void OnTweenUpdate(SourceTweenState tween)
        {
            Vector2 sineOffset =
                SourceVector2Math.Multiply(SourceVector2Math.Multiply(Vector2.UnitY, (float)Math.Sin(sineWave)), 4f);
            Vector2 end = activated ? SourceVector2Math.Add(node, sineOffset) : startPosition;
            Vector2 target = Vector2Interpolation.Lerp(tweenStartPosition, end, tween.Eased,
                ((ISourceScalarEnvironment)engine).ScalarArithmetic);
            Vector2 diff = SourceVector2Math.Subtract(target, Position);
            foreach (var jp in jumpthrus) jp.MoveTo(SourceVector2Math.Add(jp.Position, diff));
            foreach (var block in group) block.MoveTo(SourceVector2Math.Add(block.Position, diff));
        }

        private void OnTweenComplete(SourceTweenState tween)
        {
            if (moves is null) return;
            Vector2 sineOffset =
                SourceVector2Math.Multiply(SourceVector2Math.Multiply(Vector2.UnitY, (float)Math.Sin(sineWave)), 4f);
            foreach (var jp in jumpthrus) moves[jp] = SourceVector2Math.Subtract(jp.Position, sineOffset);
            foreach (var block in group) moves[block] = SourceVector2Math.Subtract(block.Position, sineOffset);
            yLerp = 0f;
        }
    }
}
