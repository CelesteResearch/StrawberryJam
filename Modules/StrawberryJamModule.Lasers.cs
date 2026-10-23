using System.Linq;
using Celeste.Game.Execution.Levels;
using Celeste.Everest.Modules.Runtime;
using Celeste.Mods.AdventureHelper.Modules;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Game.Execution.Processes;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        private AdventureHelperModule laserAdventureModule;

        private void LoadLaserHooks()
        {
            laserAdventureModule = ((ISourceEverestModulesEnvironment)SourceEngine).Modules
                .OfType<AdventureHelperModule>().Single();
            laserAdventureModule.AdditionalRiderCheck += LaserHasRider;
            laserAdventureModule.BeforeStopSound += LaserBeforeStopSound;
        }

        private void UnloadLaserHooks()
        {
            if (laserAdventureModule == null) return;
            laserAdventureModule.AdditionalRiderCheck -= LaserHasRider;
            laserAdventureModule.BeforeStopSound -= LaserBeforeStopSound;
            laserAdventureModule = null;
        }

        private static bool LaserHasRider(SourceSolidEntityState mover, string colorCode) =>
            (mover.Scene as OriginalLevelSceneRoot)?.Session.GetFlag($"ZipMoverSyncLaser:{colorCode.ToLower()}") ??
            false;

        private static void LaserBeforeStopSound(SourceSolidEntityState mover, string colorCode) =>
            (mover.Scene as OriginalLevelSceneRoot)?.Session.SetFlag($"ZipMoverSyncLaser:{colorCode.ToLower()}", false);

        public ParticleTypePresentation LaserSparkParticle { get; private set; }

        private void LoadLaserContent()
        {
            LaserSparkParticle =
                new ParticleTypePresentation(((IOriginalGameplayProcess)SourceEngine).ParticleTypes.ZipMover.P_Sparks)
                {
                    SpeedMultiplier = 3f, LifeMin = .3f, LifeMax = .5f,
                    FadeMode = ParticleTypePresentation.FadeModes.Late
                };
        }
    }
}
