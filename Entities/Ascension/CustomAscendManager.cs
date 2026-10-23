using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;
using Celeste.Scene.Rendering;
using Celeste.Scene.Scheduling;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Presentation.Backdrops.Weather.Snow;
using Celeste.Simulation.Presentation.Backdrops.Weather.Rain;
using Celeste.Simulation.Presentation.Scene.Transitions;

namespace Celeste.Mods.StrawberryJam.Entities.Ascension
{
    [SceneTracked(false)]
    [CustomEntity("SJ2021/CustomAscendManager")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.CustomAscendManager")]
    public partial class CustomAscendManager : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly string playerRegistryKey;
        public PackedColor[] StreakColors;
        public bool Borders;
        public bool Dark;
        private readonly PackedColor backgroundColor;
        private readonly bool introLaunch;
        private float fade;
        private bool launchStarted;
        private bool launchCompleted;
        private OriginalLevelSceneRoot level;
        private readonly bool finalLaunch;

        public CustomAscendManager(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            playerRegistryKey = ((Celeste.Scene.Tracking.ISourceSceneCatalogEnvironment)engine).SceneCatalog
                .Find(typeof(Celeste.Simulation.RoomPlayerSession)).RegistryKey;
            Tags = new SceneTagSet(((IOriginalGameTagsEnvironment)engine).Tags.TransitionUpdate.Value);
            Depth = 8900;
            finalLaunch = data.Bool("finalLaunch", false);
            backgroundColor = SourceHexColor.HexToColor(data.Attr("backgroundColor", "75a0ab"));
            introLaunch = data.Bool("introLaunch", false);
            Borders = data.Bool("borders", true);
            Dark = data.Bool("dark", false);
            string[] colors = data.Attr("streakColors", "ffffff,e69ecb")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            StreakColors = new PackedColor[colors.Length];
            for (int i = 0; i < colors.Length; i++) StreakColors[i] = SourceHexColor.HexToColor(colors[i].Trim());
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            level = (OriginalLevelSceneRoot)Scene;
            if (!finalLaunch) OwnedComponents.Add(new SimulationCoroutine(engine, Routine()));
        }

        public override void Render(ISourceRenderContext context) =>
            SourcePrimitiveDrawing.Rect(context, this, level.Camera.Position.X - 10f, level.Camera.Position.Y - 10f,
                340f, 200f, backgroundColor * fade);

        public override void Removed(object scene)
        {
            if (launchStarted) FadeSnapTo(0f);
            if (launchCompleted)
            {
                ((ISourceScreenWipeEnvironment)engine).ProcessWipe.WipeColor =
                    Dark ? PackedColor.Black : PackedColor.White;
                new MountainWipePresentation((ISourceRendererScene)Scene, true);
                ((ISourceScreenWipeEnvironment)engine).ProcessWipe.WipeColor = PackedColor.Black;
            }

            base.Removed(scene);
        }

        public void SetFade(float target) => fade = target;

        private IEnumerator FadeTo(float target, float duration = .8f)
        {
            while ((fade = ScalarMath.Approach(fade, target, engine.DeltaTime / duration)) != target)
            {
                FadeSnapTo(fade);
                yield return null;
            }

            FadeSnapTo(target);
        }

        private void FadeSnapTo(float target)
        {
            fade = target;
            SetSnowAlpha(1f - fade);
            level.Bloom.Base = level.AreaData.BloomBase + fade * .1f;
        }

        private void SetSnowAlpha(float value)
        {
            var snow = level.Foreground.Get<SnowBackdropPresentation>();
            if (snow != null) snow.Alpha = value;
            var rain = level.Foreground.Get<RainBackdropPresentation>();
            if (rain != null) rain.Alpha = value;
            var wind = level.Foreground.Get<WindSnowBackdropPresentation>();
            if (wind != null) wind.Alpha = value;
        }
    }
}
