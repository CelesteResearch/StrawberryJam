using Celeste.Mods.StrawberryJam.Runtime.Bosses;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private CrystalBombBossHooks crystalBombBossHooks;
        private void LoadCrystalBombBosses() => crystalBombBossHooks = new CrystalBombBossHooks(this, SourceEngine);

        private void UnloadCrystalBombBosses()
        {
            crystalBombBossHooks?.Dispose();
            crystalBombBossHooks = null;
        }
    }
}
