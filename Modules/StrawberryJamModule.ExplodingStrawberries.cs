using Celeste.Mods.StrawberryJam.Runtime.Strawberries;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private ExplodingStrawberryHooks explodingStrawberries;
        private void LoadExplodingStrawberries() => explodingStrawberries = new ExplodingStrawberryHooks(SourceEngine);

        private void UnloadExplodingStrawberries()
        {
            explodingStrawberries?.Dispose();
            explodingStrawberries = null;
        }
    }
}
