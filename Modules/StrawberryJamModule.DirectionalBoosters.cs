using Celeste.Mods.StrawberryJam.Runtime.Boosters;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private DirectionalBoosterProcess directionalBoosters;

        private void LoadDirectionalBoosters() =>
            directionalBoosters = new DirectionalBoosterProcess(SourceEngine, this);

        private void UnloadDirectionalBoosters()
        {
            directionalBoosters?.Dispose();
            directionalBoosters = null;
        }
    }
}
