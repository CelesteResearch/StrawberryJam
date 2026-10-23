// SPDX-License-Identifier: MIT

using Celeste.Everest.Modules.Settings;
using Celeste.Everest.Input.Bindings;
using Celeste.Input.Devices.Keyboard;
using Celeste.Input.Devices.GamePads;
using Celeste.Everest.Settings.Metadata;

namespace Celeste.Mods.StrawberryJam.Configuration
{
    public sealed class StrawberryJamSettings : EverestModuleSettings
    {
        [SettingName("StrawberryJam2021_DisplayDashSequence")]
        public bool DisplayDashSequence { get; set; }

        [SettingName("StrawberryJam2021_TogglePlaybacks")]
        [DefaultButtonBinding(SourceGamePadButtonFlags.Back, SourceKeys.Tab)]
        public EverestButtonBinding TogglePlaybacks { get; set; }
    }
}
