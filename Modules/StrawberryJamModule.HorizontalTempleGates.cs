using Celeste.Mods.StrawberryJam.Runtime.Doors;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private HorizontalTempleGateHooks horizontalTempleGateHooks;

        private void LoadHorizontalTempleGates() =>
            horizontalTempleGateHooks = new HorizontalTempleGateHooks(SourceEngine);

        private void UnloadHorizontalTempleGates()
        {
            horizontalTempleGateHooks?.Dispose();
            horizontalTempleGateHooks = null;
        }
    }
}
