using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using Celeste.Core.Maps;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Mods.StrawberryJam.Rendering.DarkMatter;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Hazards.Lightning;

namespace Celeste.Mods.StrawberryJam.Entities.Hazards.DarkMatter
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.DarkMatter")]
    [SceneTracked]
    [CustomEntity("SJ2021/DarkMatter")]
    public sealed class DarkMatter : LightningHazardState
    {
        public const string Flag = "disable_lightning";
        public DarkMatterRenderer Renderer;
        public readonly MapEntityId Id;

        public DarkMatter(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, engine)
        {
            Id = new MapEntityId((data as OriginalLevelEntityData)?.Level.Name, data.ID);
            ToggleOffset = (float)((ISourceRandomEnvironment)engine).Random.NextDouble();
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            ((DarkMatterRenderer)((ISourceScene)scene).SourceScene.Entities.GetTrackedEntities(
                typeof(DarkMatterRenderer))[0]).Track(this);
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            ((DarkMatterRenderer)((ISourceScene)scene).SourceScene.Entities.GetTrackedEntities(
                typeof(DarkMatterRenderer))[0]).Untrack(this);
        }

        protected override void OnPlayer(RoomPlayerSession player)
        {
            if (((ISourceLightningScene)Scene).Invincible) return;
            int direction = Math.Sign(player.X - X);
            if (direction == 0) direction = -1;
            player.Die(Vector2.UnitX * direction);
        }

        public static IEnumerator PulseRoutine(OriginalLevelSceneRoot level)
        {
            for (float value = 0f; value < 1f; value += level.EngineContext.DeltaTime * 8f)
            {
                SetPulseValue(level, value);
                yield return null;
            }

            for (float value = 1f; value > 0f; value -= level.EngineContext.DeltaTime * 8f)
            {
                SetPulseValue(level, value);
                yield return null;
            }

            SetPulseValue(level, 0f);
        }

        private static void SetPulseValue(OriginalLevelSceneRoot level, float amount)
        {
            var arithmetic = ((ISourceScalarEnvironment)level.EngineContext).ScalarArithmetic;
            level.Glitch.Value = arithmetic.Lerp(0f, .075f, amount);
            level.Bloom.Strength = arithmetic.Lerp(1f, .8f, amount);
            ((DarkMatterRenderer)level.SourceScene.Entities.GetTrackedEntities(typeof(DarkMatterRenderer))[0]).Fade =
                amount * .2f;
        }
    }
}
