using Celeste.Mods.StrawberryJam.Runtime.Gravity;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private ZeroGPlayerHooks zeroGPlayerHooks;

        private void LoadZeroG()
        {
            ZeroGRuntime runtime = ZeroGRuntime.For(SourceEngine);
            runtime.Bind(() => Session);
            zeroGPlayerHooks = new ZeroGPlayerHooks(runtime);
        }

        private void UnloadZeroG()
        {
            zeroGPlayerHooks?.Dispose();
            zeroGPlayerHooks = null;
        }
    }
}
