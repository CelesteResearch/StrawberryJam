using System.Runtime.CompilerServices;
using Celeste.Core.Display;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Entities
{
    internal sealed class NodedCloudParticles
    {
        private static readonly ConditionalWeakTable<ParticleTypePresentation, NodedCloudParticles> Resources = new();
        public readonly ParticleTypePresentation Spawn;
        public readonly ParticleTypePresentation Ghost;

        private NodedCloudParticles(ParticleTypePresentation fragileCloud)
        {
            Spawn = new ParticleTypePresentation(fragileCloud) { Color = new PackedColor(255, 154, 224) };
            Ghost = new ParticleTypePresentation(fragileCloud) { Color = new PackedColor(0, 0, 0, 0x62) };
        }

        public static NodedCloudParticles For(ParticleTypePresentation fragileCloud)
            => Resources.GetValue(fragileCloud, Create);

        private static NodedCloudParticles Create(ParticleTypePresentation fragileCloud) => new(fragileCloud);
    }
}
