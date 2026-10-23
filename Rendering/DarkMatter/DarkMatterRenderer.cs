using System.Linq;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Settings;
using Celeste.Graphics.Content.VirtualAssets;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Hazards.Lightning;
using DarkMatterBlock = Celeste.Mods.StrawberryJam.Entities.Hazards.DarkMatter.DarkMatter;

namespace Celeste.Mods.StrawberryJam.Rendering.DarkMatter
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.DarkMatterRenderer")]
    [SceneTracked]
    public sealed class DarkMatterRenderer : TiledLightningRendererState<DarkMatterBlock>
    {
        private static readonly PackedColor[][] Palettes =
        {
            new[] { new PackedColor(120, 0, 181, 255), new PackedColor(102, 63, 160, 255) },
            new[] { new PackedColor(252, 245, 121, 255), new PackedColor(140, 247, 226, 255) },
            new[] { new PackedColor(201, 44, 208, 255), new PackedColor(152, 87, 170, 255) }
        };

        private readonly StrawberryJamModule module;
        public DarkMatterRenderMode Mode;

        public DarkMatterRenderer(ISourceEngineContext engine)
            : base(engine, (ISourceRenderEnvironment)engine, ((ISourceSoundSourceEnvironment)engine).SoundSources,
                renderBloom: false)
        {
            module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<StrawberryJamModule>().Single();
        }

        protected override bool AllowBoltFlash => !((ISettingsContext)engine).Settings.DisableFlashes;
        protected override PackedColor BoltFlashColor => PackedColor.Black;
        protected override float BoltFlashOpacity => .65f;
        protected override float EdgeFadeAmount => 4f * Fade;

        protected override PackedColor BufferClearColor => Mode == DarkMatterRenderMode.HeartsideTransitionAbuse
            ? PackedColor.White * (.1f + .9f * Fade)
            : PackedColor.Lerp(new PackedColor(71, 0, 118, 255) * .125f, PackedColor.Black, Fade);

        protected override VirtualRenderTarget LightningBuffer => module.DarkMatter.LightningBuffer;

        public override void Awake(object scene)
        {
            base.Awake(scene);
            UpdateMode((OriginalLevelSceneRoot)scene);
        }

        public override void Update()
        {
            UpdateMode();
            base.Update();
        }

        public new void Track(DarkMatterBlock block)
        {
            base.Track(block);
            block.Renderer = this;
        }

        public new void Untrack(DarkMatterBlock block)
        {
            base.Untrack(block);
            block.Renderer = null;
        }

        public void UpdateMode(OriginalLevelSceneRoot level = null)
        {
            level ??= (OriginalLevelSceneRoot)Scene;
            string sid =
                ((IOriginalAreaKeyEnvironment)engine).AreaKeys.GetSID(
                    ((IOriginalAreaKeyEnvironment)engine).AreaKeys.GetSessionArea(level.Session));
            Mode = sid == "StrawberryJam2021/3-Advanced/ZZ-HeartSide"
                ? level.Session.Level == "heartside_Viv"
                    ? DarkMatterRenderMode.HeartsideViv
                    : DarkMatterRenderMode.HeartsideTransitionAbuse
                : DarkMatterRenderMode.Normal;
            electricityColors[0] = Palettes[(int)Mode][0];
            electricityColors[1] = Palettes[(int)Mode][1];
            for (int index = 0; index < Bolts.Count; index++) Bolts[index].Color = Palettes[(int)Mode][index % 2];
        }
    }
}
