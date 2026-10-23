using Celeste.Mods.StrawberryJam.Runtime.Refills;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private ExpiringDashRefillRuntime expiringDashRefillRuntime;

        private void LoadExpiringDashRefills() =>
            expiringDashRefillRuntime = new ExpiringDashRefillRuntime(SourceEngine, () => Session);

        private void UnloadExpiringDashRefills()
        {
            expiringDashRefillRuntime?.Dispose();
            expiringDashRefillRuntime = null;
        }
    }
}
