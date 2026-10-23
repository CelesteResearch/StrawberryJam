using System;
using Celeste.Everest.Runtime.Events.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.StrawberryJam.Entities.Doors;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private string grabTempleGateRegistryKey;

        private void LoadGrabTempleGate()
        {
            grabTempleGateRegistryKey = ((Celeste.Scene.Tracking.ISourceSceneCatalogEnvironment)SourceEngine)
                .SceneCatalog.Find(typeof(GrabTempleGate)).RegistryKey;
            EverestLevelFreezeEvents.OnAfterFreezeFrameUpdate += UpdateGrabTempleGates;
        }

        private void UnloadGrabTempleGate() =>
            EverestLevelFreezeEvents.OnAfterFreezeFrameUpdate -= UpdateGrabTempleGates;

        private void UpdateGrabTempleGates(EverestLevelFreezeContext context)
        {
            if (context.NativeLevel is not OriginalLevelSceneRoot level ||
                !ReferenceEquals(level.EngineContext, SourceEngine))
                return;
            foreach (GrabTempleGate gate in level.SourceScene.Entities.GetTrackedEntities(grabTempleGateRegistryKey))
                gate.CheckToggle();
        }
    }
}
