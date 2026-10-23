using Celeste.Everest.Modules.Persistence;

namespace Celeste.Mods.StrawberryJam.Persistence
{
    public sealed partial class StrawberryJamSession : EverestModuleSession
    {
        public int MusicWonkyBeatIndex;
        public int CassetteWonkyBeatIndex;
        public float MusicBeatTimer;
        public float CassetteBeatTimer;
        public bool CassetteBlocksDisabled = true;
        public string CassetteBlocksLastParameter = "";
    }
}
