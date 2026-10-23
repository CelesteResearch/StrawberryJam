using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Content;
using Celeste.Graphics.Display.Colors;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Runtime.Holdables
{
    public static class TripleBoostFlowerParticles
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, ParticleTypePresentation> owners = new();
        public static ParticleTypePresentation For(ISourceEngineContext engine) => owners.GetValue(engine, Create);

        private static ParticleTypePresentation Create(ISourceEngineContext engine) => new()
        {
            Source = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas["particles/petal"],
            Color = SourceHexColor.HexToColor("E63244"),
            DirectionRange = 1 / 4 * (float)Math.PI,
            FadeMode = ParticleTypePresentation.FadeModes.Late,
            LifeMin = 1f,
            LifeMax = 1.5f,
            SpeedMin = 15f,
            SpeedMax = 25f,
            RotationMode = ParticleTypePresentation.RotationModes.Random,
            SpinMin = 0,
            SpinMax = (float)Math.PI,
            Size = 1f
        };
    }
}
