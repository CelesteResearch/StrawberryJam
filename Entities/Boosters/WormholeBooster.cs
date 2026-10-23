using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Entities.Interaction.Boosters;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display.Colors;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Scene.Entities;
using Celeste.Mods.StrawberryJam.Runtime.Boosters;
using Celeste.Simulation.Presentation.PostProcessing.Displacement;

namespace Celeste.Mods.StrawberryJam.Entities.Boosters
{
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.WormholeBooster")]
    [CustomEntity("SJ2021/WormholeBooster")]
    public partial class WormholeBooster : OriginalBoosterState
    {
        public string deathColor;
        public bool instantCamera;
        private PackedColor color;
        private static readonly PackedColor DisplaceColor = SourceHexColor.HexToColor("827E00");
        public DisplacementRenderHookState displaceHook;
        private readonly AtlasEntry displace;
        private readonly Sprite displacementMask;
        private float displaceEase = 1f;
        internal readonly WormholeBoosterProcess Process;
        internal ISourceEngineContext Engine => Process.Engine;

        public WormholeBooster(EntityData data, Vector2 offset, ISourceEngineContext engine)
            : base(engine, data.Position + offset, false)
        {
            Process = WormholeBoosterProcess.For(engine);
            deathColor = EntityAttributeReader.Attr(data.Values, "deathColor", "61010c");
            instantCamera = EntityAttributeReader.Bool(data.Values, "instantCamera", false);
            color = SourceHexColor.HexToColor("7800bd");
            displace =
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.Entries[
                    "util/StrawberryJam2021/wormhole_disp"];
            Add(displaceHook = new DisplacementRenderHookState(BlackHoleDisplacement));
            Add(displacementMask = Process.Module.SpriteBank.Create("wormholeMask"));
            displacementMask.Stop();
            displacementMask.Visible = false;
            BoosterSprite = Process.Module.SpriteBank.CreateOn(BoosterSprite, "WormholeBooster");
            BoosterSprite.Color = color;
            BoosterParticleType = Process.PBurst;
            Process.TeleDeath = false;
            Process.CanTeleport = true;
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new WormholeBooster(data, offset, level.EngineContext);

        public override void Awake(object scene)
        {
            base.Awake(scene);
            BoosterOutline.RemoveSelf();
        }

        public override void Update()
        {
            base.Update();
            BoosterSprite.Color = color;
            if (BoosterRespawnTimer > .1f) BoosterRespawnTimer = .1f;
            Process.TeleDeath = ((ISourceScene)Scene).SourceScene.Entities
                .GetTrackedEntities(((ISourceSceneCatalogEnvironment)Engine).SceneCatalog.Find(typeof(WormholeBooster))
                    .RegistryKey).Count == 1;
            if (Process.TeleDeath)
                color = PackedColor.Lerp(color, SourceHexColor.HexToColor(deathColor), 3f * Engine.DeltaTime);
        }

        internal bool CanEnter => Process.CanTeleport && BoosterRespawnTimer <= 0 && BoosterCannotUseTimer <= 0 &&
                                  !BoostingPlayer;

        private WormholeBooster FindNearestBooster()
        {
            WormholeBooster closest = null;
            float shortestDistance = float.MaxValue;
            foreach (WormholeBooster booster in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                         ((ISourceSceneCatalogEnvironment)Engine).SceneCatalog.Find(typeof(WormholeBooster))
                         .RegistryKey))
            {
                if (this == booster) continue;
                float distance =
                    ((Celeste.Core.Mathematics.ISourceVectorEnvironment)Engine).VectorArithmetic.LengthSquared(
                        booster.Position - Position);
                if (distance < shortestDistance)
                {
                    closest = booster;
                    shortestDistance = distance;
                }
            }

            return closest;
        }
    }
}
