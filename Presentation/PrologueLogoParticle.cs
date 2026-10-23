using System;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Content.Atlas;

namespace Celeste.Mods.StrawberryJam.Presentation
{
    internal sealed class PrologueLogoParticle
    {
        public const float DefaultParticleLifetime = 1f;
        private const float StartingSize = 0.3f;
        private readonly ISourceEngineContext engine;
        private readonly Random random;
        public readonly AtlasEntry Texture;
        public Vector2 Offset;
        public float Opacity;
        public float Size;
        private float livedTime;
        private float lifetime;
        private float relativeSize;
        private float preLivedTime;
        private float appearTime;

        public PrologueLogoParticle(ISourceEngineContext engine, Random random, AtlasEntry texture, float timeOffset)
        {
            this.engine = engine;
            this.random = random;
            Texture = texture;
            Reset(timeOffset);
        }

        public void Update()
        {
            if (preLivedTime < appearTime)
            {
                preLivedTime += engine.DeltaTime;
                Opacity = 0f;
                return;
            }

            if (livedTime > lifetime) Reset(0f, NextFloat(DefaultParticleLifetime) + 1f);
            livedTime += engine.DeltaTime;
            float percentLived = livedTime / lifetime;
            float adjustedEaser = -4 * (float)Math.Pow(percentLived - 0.5f, 2) + 1;
            Opacity = adjustedEaser;
            Size = (adjustedEaser / (1 / StartingSize) + StartingSize) * relativeSize;
        }

        private float NextFloat(float maximum) => (float)random.NextDouble() * maximum;

        private void Reset(float time = 0f, float life = DefaultParticleLifetime)
        {
            lifetime = life;
            preLivedTime = 0f;
            appearTime = NextFloat(4f);
            livedTime = time % life;
            relativeSize = NextFloat(1f) + 0.25f;
            Offset = new Vector2(random.Next(-700, 700), random.Next(-200, 200));
            Opacity = 0f;
            Size = 0.5f * relativeSize;
        }
    }
}
