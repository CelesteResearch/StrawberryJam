using Celeste.Graphics.Rendering.Devices;
using Celeste.Graphics.Rendering.Textures;

namespace Celeste.Mods.StrawberryJam.Rendering.Masks
{
    internal sealed class MaskRenderTargets
    {
        private readonly SourceRenderTargetBinding[][] buffers =
        {
            new SourceRenderTargetBinding[0], new SourceRenderTargetBinding[1],
            new SourceRenderTargetBinding[2], new SourceRenderTargetBinding[3], new SourceRenderTargetBinding[4]
        };

        private SourceRenderTargetBinding[] captured;

        public void Capture(SourceGraphicsDevice device)
        {
            captured = buffers[device.GetRenderTargetsNoAllocEXT(null)];
            device.GetRenderTargetsNoAllocEXT(captured);
        }

        public void Restore(SourceGraphicsDevice device) => device.SetRenderTargets(captured);
    }
}
