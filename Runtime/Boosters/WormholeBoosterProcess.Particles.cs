using System;
using System.Numerics;
using Celeste.Graphics.Content;
using Celeste.Graphics.Display.Colors;
using Celeste.Game.Execution.Processes;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Runtime.Boosters
{
    public sealed partial class WormholeBoosterProcess
    {
        public void LoadParticles()
        {
            PTeleporting = new ParticleTypePresentation
            {
                Source = ((ISourceGameGraphicsEnvironment)Engine).GameGraphics.GameAtlas.Entries["particles/blob"],
                Color = SourceHexColor.HexToColor("8100C1") * .2f,
                Color2 = SourceHexColor.HexToColor("7800bd") * .2f,
                ColorMode = ParticleTypePresentation.ColorModes.Choose,
                RotationMode = ParticleTypePresentation.RotationModes.SameAsDirection,
                Size = .7f, SizeRange = .2f,
                DirectionRange = (float)Math.PI / 12f,
                FadeMode = ParticleTypePresentation.FadeModes.Late,
                LifeMax = .2f,
                SpeedMin = 70f, SpeedMax = 100f, SpeedMultiplier = 1f,
                Acceleration = new Vector2(0f, 10f)
            };
            var defaults = ((IOriginalGameplayProcess)Engine).ParticleTypes.Booster;
            PBurst = new ParticleTypePresentation(defaults.P_Burst) { Color = SourceHexColor.HexToColor("7800bd") };
            PAppear = new ParticleTypePresentation(defaults.P_Appear) { Color = SourceHexColor.HexToColor("8100C1") };
        }
    }
}
