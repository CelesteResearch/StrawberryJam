using Celeste.Mods.StrawberryJam.Runtime.Holdables.Flowers;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private TripleBoostFlowerFallSpeedHooks tripleBoostFlowers;
        private void LoadTripleBoostFlowers() => tripleBoostFlowers = new TripleBoostFlowerFallSpeedHooks(SourceEngine);

        private void UnloadTripleBoostFlowers()
        {
            tripleBoostFlowers?.Dispose();
            tripleBoostFlowers = null;
        }
    }
}
