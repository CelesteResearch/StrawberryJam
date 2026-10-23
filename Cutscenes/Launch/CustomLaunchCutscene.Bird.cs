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
        private IEnumerator BirdRoutine(float delay)
        {
            yield return delay;
            bird = BirdNpcState.Create(Level, Vector2.Zero, BirdNpcMode.None);
            Level.SourceScene.QueueSourceEntity(bird);
            bird.Sprite.Play("flyupIdle");
            Vector2 vector = new Vector2(320f, 180f) / 2f;
            Vector2 topCenter = new Vector2(vector.X, 0f);
            Vector2 vector2 = new Vector2(vector.X, 180f);
            Vector2 from = vector2 + new Vector2(40f, 40f);
            Vector2 to = vector + new Vector2(-32f, -24f);
            for (float t = 0f; t < 1f; t += engine.DeltaTime / 4f)
            {
                birdScreenPosition =
                    from + (to - from) * ((ISourceEasingEnvironment)engine).EasingArithmetic.BackOut(t);
                yield return null;
            }

            bird.Sprite.Play("flyupRoll");
            for (float t = 0f; t < 1f; t += engine.DeltaTime / 2f)
            {
                birdScreenPosition =
                    to + new Vector2(64f, 0f) * ((ISourceEasingEnvironment)engine).EasingArithmetic.CubeInOut(t);
                yield return null;
            }

            to = birdScreenPosition;
            from = topCenter + new Vector2(-40f, -100f);
            bool playedAnim = false;
            for (float t = 0f; t < 1f; t += engine.DeltaTime / 4f)
            {
                if (t >= 0.35f && !playedAnim)
                {
                    bird.Sprite.Play("flyupRoll");
                    playedAnim = true;
                }

                birdScreenPosition =
                    to + (from - to) * ((ISourceEasingEnvironment)engine).EasingArithmetic.BigBackIn(t);
                birdScreenPosition.X += t * 32f;
                yield return null;
            }

            bird.RemoveSelf();
            bird = null;
        }
    }
}
