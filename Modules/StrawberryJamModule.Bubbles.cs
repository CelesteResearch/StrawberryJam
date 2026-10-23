using Celeste.Mods.StrawberryJam.Runtime.Bubbles;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private FloatingBubbleProcess floatingBubbleProcess;
        private void LoadFloatingBubbles() => floatingBubbleProcess = new FloatingBubbleProcess(SourceEngine, this);

        private void UnloadFloatingBubbles()
        {
            floatingBubbleProcess?.Dispose();
            floatingBubbleProcess = null;
        }
    }
}
