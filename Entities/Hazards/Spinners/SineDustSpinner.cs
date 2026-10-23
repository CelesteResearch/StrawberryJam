// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Hazards.Spinners.Dust;
using Celeste.Physics.Collision.Shapes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Hazards.Spinners.Dust;

namespace Celeste.Mods.StrawberryJam.Entities.Hazards.Spinners
{
    [CustomEntity("SJ2021/SineDustSpinner")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.SineDustSpinner")]
    public sealed class SineDustSpinner : OriginalDustStaticSpinnerState
    {
        private readonly ISourceEngineContext engine;
        private readonly Vector2 originalPosition;
        private readonly float xAmplitude, yAmplitude;
        private readonly float xPeriod, xPhase, yPeriod, yPhase;
        private readonly bool xLinear, yLinear;
        private float timeSinceAwake;

        public SineDustSpinner(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(data, offset,
            engine)
        {
            this.engine = engine;
            var vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            Collider = new CircleColliderGeometry(vectors, 6f);
            OwnedComponents.Remove(Sprite);
            OwnedComponents.Remove(OwnedComponents.Get<DustEdgePresentation>());
            OwnedComponents.Remove(OwnedComponents.Get<RoomTransitionListener>());
            OwnedComponents.Add(Sprite = new DustGraphicState(engine, true, false, false));
            xPeriod = data.Float("xPeriod", 1f);
            xPhase = data.Float("xPhase");
            yPeriod = data.Float("yPeriod", 1f);
            yPhase = data.Float("yPhase");
            xLinear = data.Bool("xLinear");
            yLinear = data.Bool("yLinear");
            // Integer division is intentional: the source keeps half-pixel centres but truncates amplitudes.
            xAmplitude = data.Width / 2;
            yAmplitude = data.Height / 2;
            originalPosition = Position + Vector2.UnitX * data.Width / 2 + Vector2.UnitY * data.Height / 2;
            Vector2 previous = originalPosition
                               + Vector2.UnitY * GetOffset(yPeriod, yPhase, yAmplitude, yLinear, -.1f)
                               + Vector2.UnitX * GetOffset(xPeriod, xPhase, xAmplitude, xLinear, -.1f);
            Position = originalPosition
                       + Vector2.UnitY * GetOffset(yPeriod, yPhase, yAmplitude, yLinear, 0f)
                       + Vector2.UnitX * GetOffset(xPeriod, xPhase, xAmplitude, xLinear, 0f);
            Sprite.EyeDirection = vectors.Normalize(Position - previous);
        }

        public override void Update()
        {
            timeSinceAwake += engine.DeltaTime;
            base.Update();
            Vector2 previous = Position;
            Position = originalPosition
                       + Vector2.UnitX * GetOffset(xPeriod, xPhase, xAmplitude, xLinear)
                       + Vector2.UnitY * GetOffset(yPeriod, yPhase, yAmplitude, yLinear);
            Sprite.EyeDirection = ((ISourceVectorEnvironment)engine).VectorArithmetic.Normalize(Position - previous);
            var level = (OriginalLevelSceneRoot)Scene;
            if (level.Clock.OnInterval(.02f, engine))
                level.ParticlesBG.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.DustStaticSpinner.P_Move, 1,
                    Position, Vector2.One * 4f);
        }

        private float GetOffset(float period, float phase, float amplitude, bool linear, float? timeOverride = null)
        {
            if (period == 0) return 0;
            float time = timeOverride ?? timeSinceAwake;
            int adjust = linear ? 1 : 2;
            phase %= adjust;
            float completion = ((time + phase / adjust * period) % Math.Abs(period)) / period;
            if (completion > 1 || completion < 0)
                completion -= Math.Sign(completion) * (int)(Math.Abs(completion) + 1);
            return linear
                ? -amplitude + 2 * amplitude * completion
                : (float)Math.Sin(completion * Math.PI * 2) * amplitude;
        }
    }
}
