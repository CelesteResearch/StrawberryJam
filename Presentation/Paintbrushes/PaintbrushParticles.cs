using Celeste.Graphics.Content;
using Celeste.Simulation;
using System.Numerics;
using System.Runtime.CompilerServices;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Animation;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Presentation.Paintbrushes
{
    public sealed class PaintbrushParticles
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, PaintbrushParticles> Owners = new();

        private PaintbrushParticles()
        {
        }

        public static PaintbrushParticles For(ISourceEngineContext engine) =>
            Owners.GetValue(engine, _ => new PaintbrushParticles());

        public static void Load(ISourceEngineContext engine) => For(engine).Initialize(engine);
        public ParticleTypePresentation blueCooldownParticle;
        public ParticleTypePresentation pinkCooldownParticle;
        public ParticleTypePresentation blueImpactParticle;
        public ParticleTypePresentation pinkImpactParticle;

        private void Initialize(ISourceEngineContext engine)
        {
            blueCooldownParticle =
                new ParticleTypePresentation(((IOriginalGameplayProcess)engine).ParticleTypes.Booster.P_Burst)
                {
                    Source = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas["particles/blob"],
                    Color = PackedColor.FromHex("42bfe8"),
                    Color2 = PackedColor.FromHex("7550e8"),
                    ColorMode = ParticleTypePresentation.ColorModes.Fade,
                    LifeMin = 0.5f,
                    LifeMax = 0.8f,
                    Size = 0.7f,
                    SizeRange = 0.25f,
                    ScaleOut = true,
                    Direction = 5.712389f,
                    DirectionRange = 1.17453292f,
                    SpeedMin = 40f,
                    SpeedMax = 100f,
                    SpeedMultiplier = 0.005f,
                    Acceleration = Vector2.Zero,
                };

            pinkCooldownParticle = new ParticleTypePresentation(blueCooldownParticle)
            {
                Color = PackedColor.FromHex("e84292"), Color2 = PackedColor.FromHex("9c2a70"),
            };

            blueImpactParticle =
                new ParticleTypePresentation(((IOriginalGameplayProcess)engine).ParticleTypes.Booster.P_Burst)
                {
                    Source = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas["particles/fire"],
                    Color = PackedColor.FromHex("ffffff"),
                    Color2 = PackedColor.FromHex("73efe8"),
                    ColorMode = ParticleTypePresentation.ColorModes.Fade,
                    LifeMin = 0.3f,
                    LifeMax = 0.5f,
                    Size = 0.7f,
                    SizeRange = 0.25f,
                    ScaleOut = true,
                    Direction = 4.712389f,
                    DirectionRange = 3.14159f,
                    SpeedMin = 10f,
                    SpeedMax = 80f,
                    SpeedMultiplier = 0.005f,
                    Acceleration = Vector2.Zero,
                };

            pinkImpactParticle = new ParticleTypePresentation(blueImpactParticle)
                { Color2 = PackedColor.FromHex("ef73bf"), };
        }
    }
}
