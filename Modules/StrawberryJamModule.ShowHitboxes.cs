using Celeste.Mods.StrawberryJam.Runtime.Debugging;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private void LoadShowHitboxes() => ShowHitboxRuntime.For(SourceEngine).Load();
        private void UnloadShowHitboxes() => ShowHitboxRuntime.For(SourceEngine).Dispose();
    }
}
