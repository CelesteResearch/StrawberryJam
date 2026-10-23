// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Game.Presentation.Platforms.ZipMovers;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Mods.StrawberryJam.Entities.Platforms.ZipMovers;
using Celeste.Scene.Entities;

namespace Celeste.Mods.StrawberryJam.Rendering.Platforms.ZipMovers
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.DashZipMover+DashZipMoverPathRenderer")]
    public class DashZipMoverPathRenderer : SceneEntityState
    {
        private static readonly PackedColor ropeColor = PackedColor.FromHex("046e19"),
            ropeLightColor = PackedColor.FromHex("329415"),
            ropeShadow = PackedColor.FromHex("003622");

        private readonly DashZipMover zipMover;
        private readonly AtlasEntry cog;
        private readonly Vector2 from, to, sparkAdd, direction, perpendicular;
        private readonly float sparkDirFromA, sparkDirFromB, sparkDirToA, sparkDirToB, length;
        private readonly ISourceVectorArithmetic vectors;

        public DashZipMoverPathRenderer(DashZipMover owner) : base(
            ((ISourceEntitySourceDataEnvironment)owner.EngineContext).EntitySourceDataRegistration)
        {
            Depth = 5000;
            zipMover = owner;
            vectors = ((ISourceVectorEnvironment)owner.EngineContext).VectorArithmetic;
            from = SourceVector2Math.Add(owner.Start, new Vector2(owner.Width / 2f, owner.Height / 2f));
            to = SourceVector2Math.Add(owner.Target, new Vector2(owner.Width / 2f, owner.Height / 2f));
            var difference = SourceVector2Math.Subtract(from, to);
            sparkAdd = Vector2Geometry.Perpendicular(vectors.SafeNormalize(difference, 5f));
            float angle = Vector2Geometry.Angle(difference);
            length = vectors.Length(SourceVector2Math.Subtract(to, from));
            sparkDirFromA = angle + (float)Math.PI / 8f;
            sparkDirFromB = angle - (float)Math.PI / 8f;
            sparkDirToA = angle + (float)Math.PI - (float)Math.PI / 8f;
            sparkDirToB = angle + (float)Math.PI + (float)Math.PI / 8f;
            cog = ((ISourceGameGraphicsEnvironment)owner.EngineContext).GameGraphics.GameAtlas[
                "objects/StrawberryJam2021/dashZipMover/cog"];
            direction = vectors.SafeNormalize(SourceVector2Math.Subtract(to, from));
            perpendicular = Vector2Geometry.Perpendicular(direction);
        }

        public void CreateSparks() => ZipMoverDrawing.Sparks(this, zipMover.EngineContext, from, to, sparkAdd,
            sparkDirFromA, sparkDirFromB, sparkDirToA, sparkDirToB, null);

        public override void Render(ISourceRenderContext context)
        {
            if (length != 0f)
            {
                DrawCogs(context, Vector2.UnitY, ropeShadow);
                DrawCogs(context, Vector2.Zero);
            }
        }

        private void DrawCogs(ISourceRenderContext context, Vector2 offset, PackedColor? colorOverride = null)
        {
            Vector2 value = SourceVector2Math.Multiply(perpendicular, 3f),
                value2 = SourceVector2Math.Multiply(SourceVector2Math.Negate(perpendicular), 4f);
            float rotation = zipMover.Percent * (float)Math.PI * 2f;
            Vector2 p1from =
                SourceVector2Math.Add(SourceVector2Math.Add(SourceVector2Math.Add(from, value), perpendicular), offset);
            Vector2 p2from = SourceVector2Math.Add(SourceVector2Math.Add(to, value2), offset);
            for (float distance = 4f - zipMover.Percent * (float)Math.PI * 8f % 4f; distance < length; distance += 4f)
            {
                float progress = distance / length;
                float amount = progress * (1 - progress) * 8;
                Vector2 wave =
                    SourceVector2Math.Multiply(SourceVector2Math.Multiply(perpendicular, (float)Math.Sin(distance)),
                        amount);
                Vector2 p1to = SourceVector2Math.Add(
                    SourceVector2Math.Add(
                        SourceVector2Math.Add(SourceVector2Math.Add(SourceVector2Math.Add(from, value), perpendicular),
                            SourceVector2Math.Multiply(direction, distance)), wave), offset);
                Vector2 p2to = SourceVector2Math.Add(
                    SourceVector2Math.Add(
                        SourceVector2Math.Subtract(SourceVector2Math.Add(to, value2),
                            SourceVector2Math.Multiply(direction, distance)), wave), offset);
                if (colorOverride != null)
                {
                    SourcePrimitiveDrawing.Line(context, this, p1from, p1to, colorOverride.Value, 3f, vectors);
                    SourcePrimitiveDrawing.Line(context, this, p2from, p2to, colorOverride.Value, 3f, vectors);
                }

                SourcePrimitiveDrawing.Line(context, this, p1from, p1to, colorOverride ?? ropeColor, vectors);
                SourcePrimitiveDrawing.Line(context, this, p2from, p2to, colorOverride ?? ropeColor, vectors);
                SourcePrimitiveDrawing.Line(context, this, p1to,
                    SourceVector2Math.Add(p1to, SourceVector2Math.Multiply(direction, 4f)),
                    colorOverride ?? ropeLightColor, vectors);
                SourcePrimitiveDrawing.Line(context, this, p2to,
                    SourceVector2Math.Subtract(p2to, SourceVector2Math.Multiply(direction, 4f)),
                    colorOverride ?? ropeLightColor, vectors);
                p1from = p1to;
                p2from = p2to;
            }

            SourceAtlasDrawing.DrawCentered(context, this, cog, SourceVector2Math.Add(from, offset),
                colorOverride ?? PackedColor.White, 1f, rotation);
            SourceAtlasDrawing.DrawCentered(context, this, cog, SourceVector2Math.Add(to, offset),
                colorOverride ?? PackedColor.White, 1f, rotation);
        }
    }
}
