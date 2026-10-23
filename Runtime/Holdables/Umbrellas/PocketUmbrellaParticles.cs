using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Startup;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Display.Colors;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Runtime.Holdables.Umbrellas
{
    public sealed class PocketUmbrellaParticles
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, PocketUmbrellaParticles> owners = new();
        public ParticleTypePresentation P_Glow, P_Glide, P_GlideUp, P_Expand;
        public static PocketUmbrellaParticles For(ISourceEngineContext engine) => owners.GetValue(engine, Create);
        private static PocketUmbrellaParticles Create(ISourceEngineContext engine) => new();

        public static void Load(ISourceEngineContext engine) => For(engine).Initialize(engine);

        private void Initialize(ISourceEngineContext engine)
        {
            var original = ((IOriginalGameplayProcess)engine).ParticleTypes.Glider;
            P_Glow = new(original.P_Glow)
                { Color = SourceHexColor.HexToColor("d34949"), Color2 = SourceHexColor.HexToColor("615a5a") };
            P_Glide = new(original.P_Glide)
                { Color = SourceHexColor.HexToColor("7a2222"), Color2 = SourceHexColor.HexToColor("c75353") };
            P_GlideUp = new(original.P_GlideUp);
            P_Expand = new(original.P_Expand);
        }
    }
}
