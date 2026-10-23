using Celeste.Mods.StrawberryJam.Runtime.Presentation;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private SpriteSwapHooks spriteSwapHooks;
        private void LoadSpriteSwaps() => spriteSwapHooks = new SpriteSwapHooks(SourceEngine);

        private void UnloadSpriteSwaps()
        {
            spriteSwapHooks?.Dispose();
            spriteSwapHooks = null;
        }
    }
}
