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
        public override void OnEnd(OriginalLevelSceneRoot level)
        {
            if (WasSkipped && boost != null && boost.FinalBoostSfx != null)
            {
                boost.FinalBoostSfx.Stop(allowFadeOut: true);
                boost.FinalBoostSfx.Release();
            }

            player.Active = true;
            player.Speed = Vector2.Zero;
            player.EnforceLevelBounds = true;
            player.Machine.State = 0;
            player.Dummy.Friction = true;
            player.Dummy.Gravity = true;
            player.Dummy.AutoAnimate = true;
            player.ForceCameraUpdate = false;
            engine.Effects.TimeRate = 1f;
            Level.SourceScene.OnEndOfFrame += delegate
            {
                Level.TeleportTo(player, room, transitionType, spawnPoint);
                if (Level.Wipe != null) Level.Wipe.Cancel();
                if (colorGrade != "")
                {
                    if (colorGrade == "none") colorGrade = null;
                    Level.SnapColorGrade(colorGrade);
                }

                new FadeWipePresentation(level, wipeIn: true).Duration = 2f;
                ((ISourceScreenWipeEnvironment)engine).ProcessWipe.WipeColor = wipeColor;
            };
        }
    }
}
