using Celeste.Mods.StrawberryJam.Runtime.Backdrops.Meteors;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private MeteorBackdropRegistration meteorBackdrops;
        private void LoadMeteorBackdrops() => meteorBackdrops = new MeteorBackdropRegistration(SourceEngine);

        private void UnloadMeteorBackdrops()
        {
            meteorBackdrops?.Dispose();
            meteorBackdrops = null;
        }
    }
}
