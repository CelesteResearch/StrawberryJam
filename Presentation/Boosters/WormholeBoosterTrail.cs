using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Animation;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Scheduling.ExecutionTags;
using Celeste.Mods.StrawberryJam.Runtime.Boosters;

namespace Celeste.Mods.StrawberryJam.Presentation.Boosters
{
    internal sealed class WormholeBoosterTrail : SceneEntityState
    {
        private readonly SourceTweenState tween;

        public WormholeBoosterTrail(Vector2 from, Vector2 to, WormholeBoosterProcess owner)
            : base(((ISourceEntitySourceDataEnvironment)owner.Engine).EntitySourceDataRegistration, from)
        {
            Tags = ((ISourceGlobalFrozenExecutionTagsEnvironment)owner.Engine).GlobalFrozenExecutionTags.FrozenUpdate
                .Value;
            Add(tween = SourceTweenState.Create(owner.Engine.TweenPool, SourceTweenState.TweenMode.Oneshot,
                ((ISourceEasingEnvironment)owner.Engine).EasingArithmetic.Linear, .2f, false));
            tween.OnUpdate = t =>
            {
                Vector2 where = Vector2Interpolation.Lerp(Position, to,
                    ((Celeste.Core.Mathematics.Extrema.ISourceExtremaEnvironment)owner.Engine).ExtremaArithmetic.Max(
                        t.Eased - .07f, 0f), ((ISourceScalarEnvironment)owner.Engine).ScalarArithmetic);
                ((OriginalLevelSceneRoot)Scene).Particles.Emit(owner.PTeleporting, 8, where, Vector2.Zero,
                    Vector2Geometry.Angle(to - Position));
            };
            tween.OnComplete = _ => RemoveSelf();
        }

        public override void Awake(object scene) => tween.Start();
    }
}
