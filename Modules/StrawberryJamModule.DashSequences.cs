// SPDX-License-Identifier: MIT

using System;
using Celeste.Mods.StrawberryJam.Configuration;
using Celeste.Mods.StrawberryJam.Runtime.DashSequences;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        public override Type SettingsType => typeof(StrawberryJamSettings);
        public StrawberryJamSettings Settings => (StrawberryJamSettings)_Settings;
        private DashSequenceDisplayHooks dashSequences;
        private void LoadDashSequences() => dashSequences = new DashSequenceDisplayHooks(SourceEngine, () => Settings);

        private void UnloadDashSequences()
        {
            dashSequences?.Dispose();
            dashSequences = null;
        }
    }
}
