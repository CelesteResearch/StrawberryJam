using System;
using System.Xml;
using Celeste.Mods.StrawberryJam.Runtime.Enemies;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Registry;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Graphics.Sprites;
using Celeste.Graphics.Content;
using Celeste.Mods.StrawberryJam.Persistence;
using Celeste.Mods.StrawberryJam.Cutscenes;
using Celeste.Mods.StrawberryJam.Rendering.Masks;

namespace Celeste.Mods.StrawberryJam.Modules
{
    public sealed partial class StrawberryJamModule : SourceEverestModule
    {
        public override Type SessionType => typeof(StrawberryJamSession);
        public StrawberryJamSession Session => (StrawberryJamSession)_Session;
        private Runtime.Cassette.WonkyCassetteProcess wonkyCassetteProcess;
        private Runtime.Boosters.WormholeBoosterProcess wormholeBoosters;
        private Runtime.Dashes.DashCountColorHooks dashCountColors;

        public Runtime.DarkMatter.DarkMatterRuntime DarkMatter { get; private set; }
        public override Type SaveDataType => typeof(StrawberryJamSaveData);
        public StrawberryJamSaveData SaveData => (StrawberryJamSaveData)_SaveData;
        public SpriteBank SpriteBank { get; private set; }
        private IDisposable fallingBlockControllerHook;
        private IDisposable glowControllerHook;
        private MaskRenderProcess maskRenderProcess;
        private CreditsRuntime creditsRuntime;
        private Runtime.HintRuntime hintRuntime;
        private Runtime.SwapBlocks.ToggleSwapBlockRuntime toggleSwapBlockRuntime;

        public StrawberryJamModule(ISourceEngineContext engine) : base(engine)
        {
            OshiroAttackTimeBinding.Bind(engine, this);
        }

        public override void Load()
        {
            LoadSkyLanterns();
            LoadPellets();
            LoadExplodingStrawberries();
            LoadCrystalBombBosses();
            LoadMaskedDecals();
            LoadFloatingBubbles();
            LoadHoldableDashes();
            LoadRainDensity();
            wormholeBoosters = Runtime.Boosters.WormholeBoosterProcess.Install(SourceEngine);
            dashCountColors = new Runtime.Dashes.DashCountColorHooks(SourceEngine);
            LoadDashBoostFields();
            LoadFlagDashSwitches();
            LoadLaserHooks();
            LoadOshiroAttackTimes();
            LoadZeroG();
            DarkMatter = new Runtime.DarkMatter.DarkMatterRuntime(SourceEngine);
            DarkMatter.Load();
            LoadPocketUmbrellas();
            LoadBarrierDashSwitches();
            LoadTripleBoostFlowers();
            LoadResettingRefills();
            LoadHorizontalTempleGates();
            wonkyCassetteProcess = new Runtime.Cassette.WonkyCassetteProcess(this, SourceEngine);
            LoadSpeedPreservePuffers();
            fallingBlockControllerHook = Entities.ConstantDelayFallingBlockController.InstallHook();
            glowControllerHook = Entities.GlowController.InstallHook(SourceEngine);
            maskRenderProcess = new MaskRenderProcess(SourceEngine);
            Rendering.Outlines.MaskedOutlineResources.Load(SourceEngine);
            LoadDashSequences();
            creditsRuntime = new CreditsRuntime(SourceEngine);
            hintRuntime = new Runtime.HintRuntime(SourceEngine);
            LoadExpiringDashRefills();
            toggleSwapBlockRuntime = new Runtime.SwapBlocks.ToggleSwapBlockRuntime(SourceEngine);
            LoadShowHitboxes();
            LoadTogglePlaybacks();
            LoadCassetteListeners();
            LoadTimeFreezeMusic();
            LoadSolarElevators();
            LoadSpriteSwaps();
            LoadGrabTempleGate();
            LoadLiftBoosts();
            LoadDirectionalBoosters();
            LoadResizableDashSwitches();
            LoadSkateboards();
            LoadHexagonalGodrays();
            LoadMeteorBackdrops();
            LoadLightSourceLimits();
        }

        public override void LoadContent(bool firstLoad)
        {
            var registry = ((IEverestContentRegistryEnvironment)SourceEngine).SourceContentRegistry;
            var xml = new XmlDocument();
            using (var stream = registry.Get("Graphics/StrawberryJam2021/CustomEntitySprites").Stream)
            {
                xml.Load(stream);
            }

            SpriteBank = new SpriteBank(SourceEngine,
                ((ISourceGameGraphicsEnvironment)SourceEngine).GameGraphics.GameAtlas, xml);
            LoadSkyLanternParticles();
            wormholeBoosters.LoadParticles();
            Entities.Switches.SwitchCrateHolder.SetupParticles(SourceEngine);
            LoadDashBoostFieldParticles();
            LoadPelletContent();
            LoadLaserContent();
            DarkMatter.LoadContent();
            Rendering.Outlines.MaskedOutlineResources.LoadTexture(SourceEngine);
            LoadBeeContent();
            LoadResizableDashSwitchParticles();
            LoadSkateboardTextures();
            LoadPocketUmbrellaParticles();
            Celeste.Mods.StrawberryJam.Presentation.Paintbrushes.PaintbrushParticles.Load(SourceEngine);
        }

        public override void Unload()
        {
            UnloadDashSequences();
            UnloadSkyLanterns();
            UnloadPellets();
            UnloadResizableDashSwitches();
            UnloadSkateboards();
            UnloadHexagonalGodrays();
            UnloadMeteorBackdrops();
            UnloadLightSourceLimits();
            UnloadExplodingStrawberries();
            UnloadCrystalBombBosses();
            UnloadMaskedDecals();
            UnloadFloatingBubbles();
            UnloadHoldableDashes();
            UnloadRainDensity();
            wormholeBoosters?.Dispose();
            wormholeBoosters = null;
            dashCountColors?.Dispose();
            dashCountColors = null;
            UnloadFlagDashSwitches();
            UnloadDashBoostFields();
            UnloadLaserHooks();
            UnloadOshiroAttackTimes();
            UnloadGrabTempleGate();
            UnloadLiftBoosts();
            UnloadDirectionalBoosters();
            UnloadZeroG();
            DarkMatter?.Dispose();
            DarkMatter = null;
            UnloadPocketUmbrellas();
            UnloadBarrierDashSwitches();
            UnloadTripleBoostFlowers();
            UnloadResettingRefills();
            UnloadHorizontalTempleGates();
            wonkyCassetteProcess?.Dispose();
            wonkyCassetteProcess = null;
            UnloadSpeedPreservePuffers();
            UnloadExpiringDashRefills();
            UnloadShowHitboxes();
            UnloadTogglePlaybacks();
            toggleSwapBlockRuntime?.Dispose();
            toggleSwapBlockRuntime = null;
            UnloadCassetteListeners();
            UnloadTimeFreezeMusic();
            UnloadSolarElevators();
            UnloadSpriteSwaps();
            hintRuntime?.Dispose();
            hintRuntime = null;
            creditsRuntime?.Dispose();
            creditsRuntime = null;
            maskRenderProcess?.Dispose();
            maskRenderProcess = null;
            glowControllerHook?.Dispose();
            glowControllerHook = null;
            fallingBlockControllerHook?.Dispose();
            fallingBlockControllerHook = null;
            SpriteBank = null;
        }
    }
}
