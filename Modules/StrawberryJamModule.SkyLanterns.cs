using Celeste.Mods.StrawberryJam.Runtime.Holdables.Lanterns;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private SkyLanternRuntime skyLanterns;
        private void LoadSkyLanterns() => skyLanterns = new SkyLanternRuntime(SourceEngine, this);
        private void LoadSkyLanternParticles() => skyLanterns.LoadParticles();

        private void UnloadSkyLanterns()
        {
            skyLanterns?.Dispose();
            skyLanterns = null;
        }
    }
}
