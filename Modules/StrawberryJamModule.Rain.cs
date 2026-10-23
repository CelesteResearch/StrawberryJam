using Celeste.Mods.StrawberryJam.Runtime.Weather;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule : IRainDensityEnvironment
    {
        private RainDensityProcess rainDensityProcess;
        public RainDensityData RainDensityData => Session.RainDensityData;
        private void LoadRainDensity() => rainDensityProcess = new RainDensityProcess(SourceEngine, this);

        private void UnloadRainDensity()
        {
            rainDensityProcess?.Dispose();
            rainDensityProcess = null;
        }
    }
}
