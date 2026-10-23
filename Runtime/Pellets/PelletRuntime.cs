using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Formats.Graphics.Sprites;
using Celeste.Graphics.Content;
using Celeste.Mods.StrawberryJam.Components.Cassette;
using Celeste.Mods.StrawberryJam.Entities.Hazards.Projectiles;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Runtime.Pellets
{
    public sealed class PelletRuntime : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, PelletRuntime> Owners = new();
        private readonly StrawberryJamModule module;
        private readonly IDisposable poolLease;
        public ISourceEngineContext Engine { get; }
        public SpriteBank SpriteBank => module.SpriteBank;
        public ParticleTypePresentation BlueTrail { get; private set; }
        public ParticleTypePresentation PinkTrail { get; private set; }

        public PelletRuntime(ISourceEngineContext engine, StrawberryJamModule module)
        {
            Engine = engine;
            this.module = module;
            poolLease = engine.EntityPools.RegisterScoped(() => new PelletShot(engine, this));
            try
            {
                Owners.Add(engine, this);
            }
            catch
            {
                poolLease.Dispose();
                throw;
            }
        }

        public static PelletRuntime For(ISourceEngineContext engine) => Owners.TryGetValue(engine, out var runtime)
            ? runtime
            : throw new InvalidOperationException("Strawberry Jam pellet pool is not loaded in this engine.");

        public void Dispose()
        {
            poolLease.Dispose();
            if (Owners.TryGetValue(Engine, out var current) && ReferenceEquals(current, this)) Owners.Remove(Engine);
        }

        public void LoadContent()
        {
            BlueTrail = new ParticleTypePresentation
            {
                Source = ((ISourceGameGraphicsEnvironment)Engine).GameGraphics.GameAtlas["particles/blob"],
                Color = CassetteListener.ColorFromCassetteIndex(0),
                Color2 = PackedColor.FromHex("7550e8"),
                ColorMode = ParticleTypePresentation.ColorModes.Fade,
                FadeMode = ParticleTypePresentation.FadeModes.Late,
                Size = .7f, SizeRange = .25f, ScaleOut = true,
                LifeMin = .2f, LifeMax = .4f, SpeedMin = 5f, SpeedMax = 25f, DirectionRange = .5f
            };
            PinkTrail = new ParticleTypePresentation(BlueTrail) { Color = CassetteListener.ColorFromCassetteIndex(1) };
        }
    }
}
