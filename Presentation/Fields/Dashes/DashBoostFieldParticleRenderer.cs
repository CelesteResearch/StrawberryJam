using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Core.Reflection.Runtime;
using Celeste.Graphics.Rendering;
using Celeste.Mods.StrawberryJam.Entities.Fields.Dashes;

namespace Celeste.Mods.StrawberryJam.Presentation.Fields.Dashes
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.DashBoostFieldParticleRenderer")]
    public sealed class DashBoostFieldParticleRenderer : SourceComponentState
    {
        private const float density = .02f;
        private readonly List<Particle> particles = new List<Particle>();
        private DashBoostField BoostField => Entity as DashBoostField;
        private float ParticlePositionRadius => (float)Math.Sqrt(BoostField.Radius * (BoostField.Radius + 75f));

        public DashBoostFieldParticleRenderer() : base(true, true)
        {
        }

        public override void Added(object entity)
        {
            base.Added(entity);
            int particleCount = (int)(Math.PI * Math.Pow(ParticlePositionRadius, 2) * density);
            for (int i = 0; i < particleCount; i++) particles.Add(new Particle(this));
        }

        public override void Update()
        {
            base.Update();
            foreach (var particle in particles)
            {
                if (particle.Percent >= 1f) particle.Reset();
                particle.Percent += BoostField.EngineContext.DeltaTime / particle.Duration;
                particle.Position += particle.Velocity * BoostField.EngineContext.DeltaTime;
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            foreach (var particle in particles)
            {
                particle.Alpha = particle.Percent >= .7f
                    ? ScalarMath.ClampedMap(particle.Percent, .7f, 1f, 1f, 0f)
                    : ScalarMath.ClampedMap(particle.Percent, 0f, .3f);
                float maximumDistance = BoostField.Radius - 1f;
                if (((ISourceVectorEnvironment)BoostField.EngineContext).VectorArithmetic.Distance(
                        BoostField.Center, particle.Position) <= maximumDistance)
                {
                    particle.RenderPosition = particle.Position;
                }
                else
                {
                    float angle = Vector2Geometry.Angle(BoostField.Center, particle.Position);
                    particle.RenderPosition = BoostField.Center + Vector2Geometry.AngleToVector(angle, maximumDistance);
                }

                SourcePrimitiveDrawing.Point(context, Entity, particle.RenderPosition, particle.Color * particle.Alpha);
            }
        }

        private sealed class Particle
        {
            public Vector2 Position;
            public Vector2 RenderPosition;
            public PackedColor Color;
            public Vector2 Velocity;
            public float Duration;
            public float Percent;
            public float Alpha;
            private readonly DashBoostFieldParticleRenderer parent;
            private readonly PackedColor[] colors;
            private ISourceRandom Random => ((ISourceRandomEnvironment)parent.BoostField.EngineContext).Random;

            public Particle(DashBoostFieldParticleRenderer parent)
            {
                this.parent = parent;
                colors = new[]
                {
                    PackedColor.Lerp(parent.BoostField.Color, PackedColor.White, .25f),
                    PackedColor.Lerp(parent.BoostField.Color, PackedColor.White, .5f),
                    PackedColor.Lerp(parent.BoostField.Color, PackedColor.White, .75f)
                };
                Reset(SourceRandomMath.NextFloat(Random));
            }

            public void Reset(float percent = 0f)
            {
                Percent = percent;
                Vector2 offset;
                do
                {
                    float offsetX = SourceRandomMath.Range(Random, -parent.ParticlePositionRadius,
                        parent.ParticlePositionRadius);
                    float offsetY = SourceRandomMath.Range(Random, -parent.ParticlePositionRadius,
                        parent.ParticlePositionRadius);
                    offset = new Vector2(offsetX, offsetY);
                } while (((ISourceVectorEnvironment)parent.BoostField.EngineContext).VectorArithmetic.Distance(
                             Vector2.Zero, offset) > parent.ParticlePositionRadius);

                Position = parent.BoostField.Center + offset;
                Color = Random.Choose(colors);
                float speed = SourceRandomMath.Range(Random, 4f, 8f);
                Velocity = Vector2Geometry.AngleToVector(SourceRandomMath.NextAngle(Random), speed);
                Duration = SourceRandomMath.Range(Random, .6f, 1.5f);
            }
        }
    }
}
