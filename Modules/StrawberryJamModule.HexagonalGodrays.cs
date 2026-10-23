using System;
using Celeste.Everest.Runtime.Levels.Update;
using Celeste.Everest.Content.Maps.Binary;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Game.Content.Maps;
using Celeste.Mods.StrawberryJam.Backdrops.Light;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private void LoadHexagonalGodrays() => ((IEverestLevelEntityLoadEnvironment)SourceEngine)
            .LevelUpdateEvents.OnLoadBackdrop += LoadHexagonalGodrayBackdrop;

        private void UnloadHexagonalGodrays() => ((IEverestLevelEntityLoadEnvironment)SourceEngine)
            .LevelUpdateEvents.OnLoadBackdrop -= LoadHexagonalGodrayBackdrop;

        private BackdropPresentation LoadHexagonalGodrayBackdrop(OriginalChapterMapData map,
            BinaryPackerElement child, BinaryPackerElement above)
        {
            if (child.Name.Equals("SJ2021/HexagonalGodray", StringComparison.OrdinalIgnoreCase))
                return new HexagonalGodray(SourceEngine, child.Attr("color"), child.Attr("fadeColor"),
                    child.AttrInt("numberOfRays"), child.AttrFloat("speedX"), child.AttrFloat("speedY"),
                    child.AttrFloat("rotation"), child.AttrFloat("rotationRandomness"));
            return null;
        }
    }
}
