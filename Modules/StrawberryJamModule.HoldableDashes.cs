using Celeste.Mods.StrawberryJam.Runtime.Holdables.Dashes;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private HoldableDashHooks holdableDashes;
        private void LoadHoldableDashes() => holdableDashes = new HoldableDashHooks(SourceEngine);

        private void UnloadHoldableDashes()
        {
            holdableDashes?.Dispose();
            holdableDashes = null;
        }
    }
}
