using Celeste.Mods.StrawberryJam.Runtime.Elevators;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public partial class StrawberryJamModule
    {
        private SolarElevatorTalkHooks solarElevatorTalkHooks;
        private void LoadSolarElevators() => solarElevatorTalkHooks = new SolarElevatorTalkHooks(SourceEngine);

        private void UnloadSolarElevators()
        {
            solarElevatorTalkHooks?.Dispose();
            solarElevatorTalkHooks = null;
        }
    }
}
