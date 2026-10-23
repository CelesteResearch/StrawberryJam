using Celeste.Mods.StrawberryJam.Runtime.Switches;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private BarrierDashSwitchHooks barrierDashSwitchHooks;
        private void LoadBarrierDashSwitches() => barrierDashSwitchHooks = new BarrierDashSwitchHooks(SourceEngine);

        private void UnloadBarrierDashSwitches()
        {
            barrierDashSwitchHooks?.Dispose();
            barrierDashSwitchHooks = null;
        }
    }
}
