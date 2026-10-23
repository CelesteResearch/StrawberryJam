using Celeste.Mods.StrawberryJam.Runtime.Movement;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private LiftBoostHooks liftBoostHooks;
        private void LoadLiftBoosts() => liftBoostHooks = new LiftBoostHooks(SourceEngine);

        private void UnloadLiftBoosts()
        {
            liftBoostHooks?.Dispose();
            liftBoostHooks = null;
        }
    }
}
