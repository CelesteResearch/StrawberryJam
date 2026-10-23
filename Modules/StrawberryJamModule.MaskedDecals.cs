using Celeste.Mods.StrawberryJam.Rendering.Decals;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private MaskedDecalRenderProcess maskedDecals;
        private void LoadMaskedDecals() => maskedDecals = new MaskedDecalRenderProcess(SourceEngine);

        private void UnloadMaskedDecals()
        {
            maskedDecals?.Dispose();
            maskedDecals = null;
        }
    }
}
