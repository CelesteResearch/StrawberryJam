using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Animation;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Reflection;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Animation;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Player.Dash;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.StrawberryJam.Triggers
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Triggers.RequireDashlessTrigger")]
    [CustomEntity("SJ2021/RequireDashlessTrigger")]
    public sealed class RequireDashlessTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly List<SceneEntityState> trackedEntities = new();
        private bool removed;
        public readonly string[] EntityNames;

        public RequireDashlessTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            EntityNames = data.Attr("entityNames", "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(name => name.Trim()).ToArray();
            Add(new DashListenerState { OnDash = OnDash });
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            var aggregate = ((ISourceEverestReflectionEnvironment)engine).Reflection.GetFakeEntryAssembly();
            foreach (SceneEntityState entity in ((ISourceScene)scene).SourceScene.Entities)
                if (SourceEntityCollision.Check(this, entity) &&
                    EntityNames.Any(name => aggregate.NameMatches(entity.GetType(), name, allowShortName: true)))
                    trackedEntities.Add(entity);
            var session = ((OriginalLevelSceneRoot)Scene).Session;
            if (session.Dashes == 0 && (session.StartedFromBeginning || session.RestartedFromGolden)) return;
            foreach (var entity in trackedEntities) entity.RemoveSelf();
            removed = true;
        }

        private void OnDash(Vector2 direction)
        {
            if (removed) return;
            foreach (var entity in trackedEntities)
                entity.Add(new SimulationCoroutine(engine, RemoveRoutine(entity)));
            removed = true;
        }

        private IEnumerator RemoveRoutine(SceneEntityState entity)
        {
            entity.Collidable = false;
            foreach (var component in entity.Components)
            {
                if (component is not Sprite sprite) continue;
                var tween = SourceTweenState.Create(engine.TweenPool, SourceTweenState.TweenMode.Oneshot, null, 0.15f,
                    true);
                tween.OnUpdate = current =>
                {
                    sprite.Scale.X = 1f + (0f - 1f) * current.Eased;
                    sprite.Scale.Y = 1f + (0f - 1f) * current.Eased;
                };
                Add(tween);
                break;
            }

            yield return 0.15f;
            var process = (IOriginalGameplayProcess)engine;
            process.AudioSession.Play("event:/game/general/seed_poof", entity.Position, process.SaveContext);
            ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble((int)RumbleStrength.Medium,
                (int)RumbleLength.Medium);
            for (int i = 0; i < 6; i++)
            {
                var angle = SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random, 6.2831855f);
                var displacement = new Vector2((float)Math.Cos(angle) * 4f, (float)Math.Sin(angle) * 4f);
                ((OriginalLevelSceneRoot)Scene).ParticlesFG.Emit(process.ParticleTypes.StrawberrySeed.P_Burst, 1,
                    entity.Position + displacement, Vector2.Zero, angle);
            }

            entity.RemoveSelf();
        }
    }
}
