using Celeste.Mods.StrawberryJam.Runtime.Lighting;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private LightSourceLimitHooks lightSourceLimits;
        private void LoadLightSourceLimits() => lightSourceLimits = new LightSourceLimitHooks(SourceEngine);

        private void UnloadLightSourceLimits()
        {
            lightSourceLimits?.Dispose();
            lightSourceLimits = null;
        }
    }
}
