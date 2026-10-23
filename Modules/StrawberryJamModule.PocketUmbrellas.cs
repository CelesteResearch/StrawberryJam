using Celeste.Mods.StrawberryJam.Runtime.Holdables.Umbrellas;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private PocketUmbrellaHooks pocketUmbrellas;
        private void LoadPocketUmbrellas() => pocketUmbrellas = new PocketUmbrellaHooks(SourceEngine);
        private void LoadPocketUmbrellaParticles() => PocketUmbrellaParticles.Load(SourceEngine);

        private void UnloadPocketUmbrellas()
        {
            pocketUmbrellas?.Dispose();
            pocketUmbrellas = null;
        }
    }
}
