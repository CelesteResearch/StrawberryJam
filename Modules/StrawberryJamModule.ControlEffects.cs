// SPDX-License-Identifier: MIT

using Celeste.Mods.StrawberryJam.Runtime.Audio;
using Celeste.Mods.StrawberryJam.Runtime.Enemies;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private OshiroAttackTimeHooks oshiroAttackTimes;
        private OshiroAttackTimeBinding oshiroAttackBinding;
        private TimeFreezeMusicHooks timeFreezeMusic;

        private void LoadOshiroAttackTimes()
        {
            oshiroAttackBinding = OshiroAttackTimeBinding.For(SourceEngine);
            oshiroAttackTimes =
                new OshiroAttackTimeHooks(SourceEngine, () => oshiroAttackBinding.Module.Session.OshiroBSideMode);
        }

        private void UnloadOshiroAttackTimes()
        {
            oshiroAttackTimes?.Dispose();
            oshiroAttackTimes = null;
        }

        private void LoadTimeFreezeMusic() => timeFreezeMusic = new TimeFreezeMusicHooks(SourceEngine);

        private void UnloadTimeFreezeMusic()
        {
            timeFreezeMusic?.Dispose();
            timeFreezeMusic = null;
        }
    }
}
