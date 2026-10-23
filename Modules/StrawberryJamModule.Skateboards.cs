using Celeste.Mods.StrawberryJam.Runtime.Skateboards;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private SkateboardHooks skateboards;

        private void LoadSkateboards() => skateboards = new SkateboardHooks(SourceEngine,
            () => Session.SkateboardEnabled, value => Session.SkateboardEnabled = value);

        private void LoadSkateboardTextures() => skateboards.InitializeTextures();

        private void UnloadSkateboards()
        {
            skateboards?.Dispose();
            skateboards = null;
        }
    }
}
