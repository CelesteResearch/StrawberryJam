using Celeste.Mods.StrawberryJam.Runtime.Switches;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private ResizableDashSwitchRuntime resizableDashSwitches;

        private void LoadResizableDashSwitches() =>
            resizableDashSwitches = new ResizableDashSwitchRuntime(SourceEngine, this);

        private void LoadResizableDashSwitchParticles() => resizableDashSwitches.LoadParticles();

        private void UnloadResizableDashSwitches()
        {
            resizableDashSwitches?.Dispose();
            resizableDashSwitches = null;
        }
    }
}
