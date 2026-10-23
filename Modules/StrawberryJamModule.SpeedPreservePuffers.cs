using Celeste.Mods.StrawberryJam.Runtime.Puffers;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private SpeedPreservePufferHooks speedPreservePuffers;
        private void LoadSpeedPreservePuffers() => speedPreservePuffers = new SpeedPreservePufferHooks(SourceEngine);

        private void UnloadSpeedPreservePuffers()
        {
            speedPreservePuffers?.Dispose();
            speedPreservePuffers = null;
        }
    }
}
