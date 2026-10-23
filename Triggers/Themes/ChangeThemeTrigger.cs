using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.ExtendedVariants.Configuration;
using Celeste.Mods.ExtendedVariants.Runtime;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.StrawberryJam.Triggers.Themes
{
    [CustomEntity("SJ2021/ChangeThemeTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Triggers.ChangeThemeTrigger")]
    public class ChangeThemeTrigger : SourceTriggerEntityState
    {
        private readonly bool enableMode;
        private readonly bool toggleMode;
        private readonly StrawberryJamModule module;
        private readonly IOriginalAreaKeyPolicy areaKeys;
        private readonly ISourceEngineContext engine;

        private static readonly string[] GymSids =
        {
            "StrawberryJam2021/0-Gyms/1-Beginner", "StrawberryJam2021/0-Gyms/2-Intermediate",
            "StrawberryJam2021/0-Gyms/3-Advanced", "StrawberryJam2021/0-Gyms/4-Expert",
            "StrawberryJam2021/0-Gyms/5-Grandmaster", "StrawberryJam2021/0-Gyms/0-Prologue"
        };

        public ChangeThemeTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            enableMode = data.Bool("enable", false);
            toggleMode = data.Bool("toggle", false);
            areaKeys = ((IOriginalAreaKeyEnvironment)engine).AreaKeys;
            foreach (var candidate in ((ISourceEverestModulesEnvironment)engine).Modules)
                if (candidate is StrawberryJamModule owner)
                {
                    module = owner;
                    break;
                }
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            ApplyTheme();
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            string sid = areaKeys.GetSID(areaKeys.GetSessionArea(((OriginalLevelSceneRoot)Scene).Session));
            bool enabled = module.SaveData.ModifiedThemeMaps.Contains(sid);
            if (toggleMode)
            {
                if (enabled) RemoveMap(sid);
                else AddMap(sid);
            }
            else if (enableMode) AddMap(sid);
            else RemoveMap(sid);

            ApplyTheme();
        }

        private static bool IsGym(string sid)
        {
            for (int i = 0; i < GymSids.Length; i++)
                if (GymSids[i] == sid)
                    return true;
            return false;
        }

        private void AddMap(string sid)
        {
            if (IsGym(sid))
                for (int i = 0; i < GymSids.Length; i++)
                    module.SaveData.ModifiedThemeMaps.Add(GymSids[i]);
            else module.SaveData.ModifiedThemeMaps.Add(sid);
        }

        private void RemoveMap(string sid)
        {
            if (IsGym(sid))
                for (int i = 0; i < GymSids.Length; i++)
                    module.SaveData.ModifiedThemeMaps.Remove(GymSids[i]);
            else module.SaveData.ModifiedThemeMaps.Remove(sid);
        }

        private void ApplyTheme()
        {
            var level = (OriginalLevelSceneRoot)Scene;
            string sid = areaKeys.GetSID(areaKeys.GetSessionArea(level.Session));
            bool enabled = module.SaveData.ModifiedThemeMaps.Contains(sid);
            if (IsGym(sid)) level.Session.SetFlag("lightMode", enabled);
            else if (sid == "StrawberryJam2021/3-Advanced/Citrea")
                level.SnapColorGrade(enabled ? "SJ2021/LordCitrea/Citrea-Invert" : "max480/extendedvariants/greyscale");
            else if (sid == "StrawberryJam2021/4-Expert/Skunkynator")
            {
                level.SnapColorGrade(enabled ? "none" : "SJ2021/Skunkynator/thereisaoddglitch");
                level.Session.SetFlag("purplesunsetfade", enabled);
                level.Session.BloomBaseAdd = enabled ? 0f : 1.1f;
                level.Bloom.Base = level.AreaData.BloomBase + level.Session.BloomBaseAdd;
            }
            else if (sid == "StrawberryJam2021/4-Expert/Yoshachobi7")
            {
                level.Session.SetFlag("lightmode", !enabled);
                level.Session.LightingAlphaAdd = enabled ? .25f : 0f;
                level.Lighting.Alpha = level.BaseLightingAlpha + level.Session.LightingAlphaAdd;
                VariantRuntime.For(engine).Enter(Variant.BackgroundBrightness, enabled ? 8 : 10, false, false, true);
            }
        }
    }
}
