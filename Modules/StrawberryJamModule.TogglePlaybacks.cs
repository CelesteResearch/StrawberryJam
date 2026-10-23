// SPDX-License-Identifier: MIT

using Celeste.Mods.StrawberryJam.Runtime.Tutorials;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private TogglePlaybackHooks togglePlaybacks;
        private void LoadTogglePlaybacks() => togglePlaybacks = new TogglePlaybackHooks(SourceEngine, () => Settings);

        private void UnloadTogglePlaybacks()
        {
            togglePlaybacks?.Dispose();
            togglePlaybacks = null;
        }
    }
}
