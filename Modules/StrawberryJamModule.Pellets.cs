using Celeste.Mods.StrawberryJam.Runtime.Pellets;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private PelletRuntime pelletRuntime;
        private void LoadPellets() => pelletRuntime = new PelletRuntime(SourceEngine, this);
        private void LoadPelletContent() => pelletRuntime.LoadContent();

        private void UnloadPellets()
        {
            pelletRuntime?.Dispose();
            pelletRuntime = null;
        }
    }
}
