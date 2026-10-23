using System.Collections.Generic;
using Celeste.Everest.Modules.Persistence;

namespace Celeste.Mods.StrawberryJam.Persistence
{
    public sealed class StrawberryJamSaveData : EverestModuleSaveData
    {
        public HashSet<string> FilledJamJarSIDs { get; set; } = new HashSet<string>();
        public HashSet<string> ModifiedThemeMaps = new HashSet<string>();
    }
}
