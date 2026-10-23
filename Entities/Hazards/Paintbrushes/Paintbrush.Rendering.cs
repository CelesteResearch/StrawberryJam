using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Rendering;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Mods.StrawberryJam.Entities.Hazards.Lasers;
using Celeste.Physics.Collision.Shapes;

namespace Celeste.Mods.StrawberryJam.Entities.Hazards.Paintbrushes
{
    public partial class Paintbrush
    {
        public override void Render(ISourceRenderContext context)
        {
            if (State is LaserState.Burst or LaserState.Firing)
            {
                for (int i = 0; i < beamHitboxes.Length; i++)
                    renderBeam(context, beamHitboxes[i], i);
            }
            else if (State == LaserState.Charging)
            {
                foreach (var hitbox in beamHitboxes)
                    renderTelegraph(context, hitbox);
            }

            var offset = Orientation.Vertical() ? new Vector2(tileSize, 0) : new Vector2(0, tileSize);

            if (smallBrushSprite.Animations.TryGetValue(idleAnimation, out var smallBrushAnimation))
            {
                int smallBrushFrameIndex = 0;
                for (int i = 2; i < Tiles; i += 2, smallBrushFrameIndex++)
                {
                    smallBrushFrameIndex %= smallBrushFrames.Length;
                    var smallBrushFrame = smallBrushAnimation.Frames[smallBrushFrames[smallBrushFrameIndex]];
                    SourceAtlasDrawing.Draw(context, this, smallBrushFrame, Position + offset * i,
                        smallBrushSprite.Origin, smallBrushSprite.Color, smallBrushSprite.Scale,
                        smallBrushSprite.Rotation);
                }
            }

            if (paintBackSprite.CurrentAnimationID != string.Empty && State is LaserState.Firing or LaserState.Cooldown)
            {
                var firstHalf = Orientation.Horizontal() ? offset : offset * (Tiles - 1);
                var secondHalf = Orientation.Vertical() ? offset : offset * (Tiles - 1);
                var paintBackFrame = paintBackSprite.GetFrame(paintBackSprite.CurrentAnimationID,
                    paintBackSprite.CurrentAnimationFrame);
                var paintBackTopHalf =
                    backTop.Reset(paintBackFrame, 0, 0, paintBackFrame.FrameWidth, paintBackFrame.FrameHeight / 2);
                var paintBackBottomHalf = backBottom.Reset(paintBackFrame, 0, paintBackFrame.FrameHeight / 2,
                    paintBackFrame.FrameWidth, paintBackFrame.FrameHeight / 2);
                SourceAtlasDrawing.Draw(context, this, paintBackTopHalf, Position + firstHalf,
                    new Vector2(0, paintBackTopHalf.FrameHeight), beamSprite.Color, beamSprite.Scale,
                    beamSprite.Rotation);
                SourceAtlasDrawing.Draw(context, this, paintBackBottomHalf, Position + secondHalf, Vector2.Zero,
                    beamSprite.Color, beamSprite.Scale, beamSprite.Rotation);
            }

            for (int i = 1; i < Tiles; i += 2)
            {
                largeBrushSprite.Position = offset * i;
                largeBrushSprite.Render(context);
            }

            if (State != LaserState.Idle)
            {
                for (int i = 1; i < Tiles; i += 2)
                {
                    paintParticlesSprite.Position = offset * i;
                    paintParticlesSprite.Render(context);
                }
            }
        }

        private void renderBeam(ISourceRenderContext context, HitboxColliderGeometry beamHitbox, int index)
        {
            if (beamSprite.CurrentAnimationID == string.Empty)
                return;

            var frame = beamSprite.GetFrame(beamSprite.CurrentAnimationID, beamSprite.CurrentAnimationFrame);
            float length = Math.Abs(Orientation switch
            {
                Orientations.Up => beamSprite.Y - beamHitbox.Top,
                Orientations.Down => beamSprite.Y - beamHitbox.Bottom,
                Orientations.Left => beamSprite.X - beamHitbox.Left,
                Orientations.Right => beamSprite.X - beamHitbox.Right,
                _ => 0,
            });

            var startPosition = Position + beamSprite.Position + (Orientation.Vertical()
                ? new Vector2(beamHitbox.CenterX, 0)
                : new Vector2(0, beamHitbox.CenterY));

            var frameOffset = Orientation.Normal() * frame.FrameWidth;
            var origin = beamSprite.Origin;

            if (beamHitboxes.Length > 1)
            {
                if (index == 0 && Orientation.Horizontal() ||
                    index == beamHitboxes.Length - 1 && Orientation.Vertical())
                    frame = beamSlice.Reset(frame, 0, 0, frame.FrameWidth, frame.FrameHeight / 2);
                else if (index == 0 && Orientation.Vertical() ||
                         index == beamHitboxes.Length - 1 && Orientation.Horizontal())
                    frame = beamSlice.Reset(frame, 0, frame.FrameHeight / 2, frame.FrameWidth, frame.FrameHeight / 2);
                else
                {
                    var rectTopLeft = Position + beamHitbox.TopLeft;
                    var color = State == LaserState.Burst && beamSprite.CurrentAnimationFrame == 0
                        ? PackedColor.White
                        : beamFillColor;
                    SourcePrimitiveDrawing.Rect((ISourceDrawScope)context, rectTopLeft.X, rectTopLeft.Y,
                        beamHitbox.Width, beamHitbox.Height, color);
                    return;
                }

                origin = new Vector2(0, tileSize / 2f);
            }

            int count = (int)Math.Ceiling(length / frame.FrameWidth);
            int remainder = (int)length % frame.FrameWidth;

            for (int i = 0; i < count; i++)
            {
                var position = startPosition + i * frameOffset;
                int width = i == count - 1 && remainder != 0 ? remainder : frame.FrameWidth;
                SourceAtlasDrawing.Draw(context, this, frame, position, origin, beamSprite.Color, beamSprite.Scale,
                    beamSprite.Rotation, new IntRect(0, 0, width, frame.FrameHeight));
            }
        }

        private void renderTelegraph(ISourceRenderContext context, HitboxColliderGeometry beamHitbox)
        {
            float animationProgress = (float)largeBrushSprite.CurrentAnimationFrame /
                                      largeBrushSprite.CurrentAnimationTotalFrames;
            int hitboxThickness = (int)Orientation.ThicknessOfHitbox(beamHitbox);
            int lerped = (int)ScalarMath.LerpClamp(0, hitboxThickness,
                ((ISourceEasingEnvironment)engine).EasingArithmetic.QuintOut(animationProgress),
                ((ISourceScalarEnvironment)engine).ScalarArithmetic);
            int thickness = Math.Min(lerped + 2, hitboxThickness);
            thickness -= thickness % 2;

            var rect = Orientation == Orientations.Up || Orientation == Orientations.Down
                ? new IntRect((int)(X + beamHitbox.CenterX) - thickness / 2, (int)(Y + beamHitbox.Top), thickness,
                    (int)beamHitbox.Height)
                : new IntRect((int)(X + beamHitbox.Left), (int)(Y + beamHitbox.CenterY) - thickness / 2,
                    (int)beamHitbox.Width, thickness);

            SourcePrimitiveDrawing.Rect((ISourceDrawScope)context, rect, telegraphColor * 0.3f);
        }
    }
}
