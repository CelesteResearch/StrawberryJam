using Celeste.Mods.StrawberryJam.Runtime.Cassette;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private CassetteListenerProcess cassetteListenerProcess;
        private void LoadCassetteListeners() => cassetteListenerProcess = new CassetteListenerProcess(SourceEngine);

        private void UnloadCassetteListeners()
        {
            cassetteListenerProcess?.Dispose();
            cassetteListenerProcess = null;
        }
    }
}
