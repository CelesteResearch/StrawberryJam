using Celeste.Mods.StrawberryJam.Runtime.Switches;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private FlagDashSwitchRuntime flagDashSwitches;
        private void LoadFlagDashSwitches() => flagDashSwitches = new FlagDashSwitchRuntime(SourceEngine);

        private void UnloadFlagDashSwitches()
        {
            flagDashSwitches?.Dispose();
            flagDashSwitches = null;
        }
    }
}
