using Celeste.Mods.StrawberryJam.Runtime.Refills;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private ResettingRefillProcess resettingRefillProcess;
        private void LoadResettingRefills() => resettingRefillProcess = new ResettingRefillProcess(SourceEngine);

        private void UnloadResettingRefills()
        {
            resettingRefillProcess?.Dispose();
            resettingRefillProcess = null;
        }
    }
}
