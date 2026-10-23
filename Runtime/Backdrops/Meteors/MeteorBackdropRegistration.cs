using System;
using Celeste.Core.Scheduling;
using Celeste.Everest.Runtime.Levels.Update;
using Celeste.Everest.Content.Maps.Binary;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Game.Content.Maps;
using Celeste.Mods.StrawberryJam.Backdrops.Meteors;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.StrawberryJam.Runtime.Backdrops.Meteors
{
    public sealed class MeteorBackdropRegistration : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly EverestLevelUpdateEvents events;

        public MeteorBackdropRegistration(ISourceEngineContext engine)
        {
            this.engine = engine;
            events = ((IEverestLevelEntityLoadEnvironment)engine).LevelUpdateEvents;
            events.OnLoadBackdrop += Load;
        }

        private BackdropPresentation Load(OriginalChapterMapData map, BinaryPackerElement child,
            BinaryPackerElement above)
        {
            if (child.Name.Equals("SJ2021/HeartComet", StringComparison.OrdinalIgnoreCase))
                return new HeartComet(engine, child.AttrInt("cometX", 0), child.AttrInt("cometY", 0));
            if (child.Name.Equals("SJ2021/MeteorShower", StringComparison.OrdinalIgnoreCase))
                return new MeteorShower(engine, child.AttrInt("numberOfMeteors", 0));
            return null;
        }

        public void Dispose() => events.OnLoadBackdrop -= Load;
    }
}
