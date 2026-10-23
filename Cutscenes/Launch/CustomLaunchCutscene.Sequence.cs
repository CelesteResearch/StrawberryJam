using Celeste.Core.Display;
using Celeste.Game.Presentation.Backdrops.Blackhole;
using Celeste.Graphics.Display;
using System;
using Celeste.Core.Randomness;
using Celeste.Game.UI.Dialogue.Textboxes;
using System.Collections;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Rendering;
using Celeste.Core.Mathematics.Easing;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Levels.Cutscenes;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Profiles;
using Celeste.Game.Entities.BadelineBoosts;
using Celeste.Game.Entities.Birds;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collectibles.Strawberry;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Presentation.Backdrops.Blackhole;
using Celeste.Simulation.Presentation.Backdrops.Parallax;
using Celeste.Game.Entities.Ascension;
using Celeste.Simulation.Presentation.Scene.Transitions;
using Celeste.Scene.Rendering;
using Celeste.Graphics.Content;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Display.Colors;

namespace Celeste.Mods.StrawberryJam.Cutscenes.Launch
{
    public sealed partial class CustomLaunchCutscene
    {
        private IEnumerator Cutscene()
        {
            engine.Effects.TimeRate = 1f;
            boost.Active = false;
            yield return null;
            yield return 0.152f;

            cameraOffset = new Vector2(0f, -20f);
            boost.Active = true;
            player.EnforceLevelBounds = false;
            yield return null;
            BlackholeBackdropPresentation blackholeBG = Level.Background.Get<BlackholeBackdropPresentation>();
            blackholeBG.Direction = -2.5f;
            blackholeBG.SnapStrength(Level, BlackholeBackdropPresentation.Strengths.High);
            blackholeBG.CenterOffset.Y = 100f;
            blackholeBG.OffsetOffset.Y = -50f;

            OwnedComponents.Add(wave = new SimulationCoroutine(engine, WaveCamera()));
            OwnedComponents.Add(new SimulationCoroutine(engine, BirdRoutine(0.8f)));
            var manager = GetAscendManager(player);
            streaks = new Celeste.Mods.StrawberryJam.Entities.Ascension.CustomAscendManager.Streaks(engine, manager);
            Level.SourceScene.QueueSourceEntity(streaks);
            float p;
            for (p = 0f; p < 1f; p += engine.DeltaTime / 12f)
            {
                fadeToColor = p;
                streaks.Alpha = p;
                manager?.SetFade(p);
                foreach (ParallaxBackdropPresentation item in Level.Foreground.GetEach<ParallaxBackdropPresentation>(
                             "blackhole"))
                {
                    item.FadeAlphaMultiplier = 1f - p;
                }

                yield return null;
            }

            while (bird != null)
            {
                yield return null;
            }

            FadeWipePresentation wipe = new FadeWipePresentation(Level, wipeIn: false)
            {
                Duration = 4f
            };
            ((ISourceScreenWipeEnvironment)engine).ProcessWipe.WipeColor = wipeColor;
            p = cameraOffset.Y;
            int to = 180;
            for (float p2 = 0f; p2 < 1f; p2 += engine.DeltaTime / 2f)
            {
                cameraOffset.Y =
                    p + ((float)to - p) * ((ISourceEasingEnvironment)engine).EasingArithmetic.BigBackIn(p2);
                yield return null;
            }

            while (wipe.Percent < 1f)
            {
                yield return null;
            }

            EndCutscene(Level);
        }
    }
}
