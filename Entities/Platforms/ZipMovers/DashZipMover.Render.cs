// SPDX-License-Identifier: MIT

using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Rendering;
using Celeste.Game.Presentation.Platforms.ZipMovers;

namespace Celeste.Mods.StrawberryJam.Entities.Platforms.ZipMovers
{
    public partial class DashZipMover
    {
        public override void Render(ISourceRenderContext context)
        {
            Vector2 position = Position;
            Position = SourceVector2Math.Add(Position, Shake);
            ZipMoverBodyDrawing.DrawScaled(context, this, Percent, innerCogs, edges, temp, 2f, PackedColor.White,
                ref scale);
            base.Render(context);
            Position = position;
        }
    }
}
