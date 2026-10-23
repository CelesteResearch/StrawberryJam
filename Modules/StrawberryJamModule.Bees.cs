using Celeste.Core.Display;
using Celeste.Game.Execution.Processes;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule
    {
        public ParticleTypePresentation BeeTrailParticle { get; private set; }

        private void LoadBeeContent()
        {
            BeeTrailParticle =
                new ParticleTypePresentation(
                    ((IOriginalGameplayProcess)SourceEngine).ParticleTypes.FireBall.P_FireTrail)
                {
                    Color = PackedColor.White * 0f,
                    Color2 = PackedColor.White * 0f
                };
        }
    }
}
