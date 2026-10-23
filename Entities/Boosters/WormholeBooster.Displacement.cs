using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Rendering;
using Celeste.Graphics.Rendering;

namespace Celeste.Mods.StrawberryJam.Entities.Boosters
{
    public partial class WormholeBooster
    {
        private void BlackHoleDisplacement()
        {
            if (!BoosterSprite.Visible) displaceEase -= 8f * Engine.RawDeltaTime;
            displaceEase = ScalarMath.Clamp(displaceEase, 0f, 1f);
            var context = ((ISourceRenderEnvironment)Engine).CurrentContext;
            SourceAtlasDrawing.Draw(context, this, displace, Position + BoosterSprite.Position, displace.Center,
                PackedColor.White * displaceEase, new Vector2(.2f));
            SourceAtlasDrawing.Draw(context, this,
                displacementMask.GetFrame(BoosterSprite.CurrentAnimationID, BoosterSprite.CurrentAnimationFrame),
                Position + BoosterSprite.Position - new Vector2(16), Vector2.Zero, DisplaceColor * displaceEase,
                Vector2.One);
        }
    }
}
