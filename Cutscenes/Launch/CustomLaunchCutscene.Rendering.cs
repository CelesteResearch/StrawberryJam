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
        public override void Update()
        {
            base.Update();
            timer += engine.DeltaTime;
            if (bird != null)
            {
                bird.Position = Level.Camera.Position + birdScreenPosition;
                bird.Position = new Vector2(bird.Position.X + (float)Math.Sin(timer) * 4f, bird.Position.Y);
                bird.Position = new Vector2(bird.Position.X,
                    bird.Position.Y + (float)Math.Sin(timer * 0.1f) * 4f + (float)Math.Sin(timer * 0.25f) * 4f);
            }

            Level.CameraOffset = cameraOffset + cameraWaveOffset;
        }

        public override void Render(ISourceRenderContext context)
        {
            var camera = Level.Camera;
            SourcePrimitiveDrawing.Rect(context, this, camera.Position.X - 1f, camera.Position.Y - 1f, 322f, 322f,
                wipeColor * fadeToColor);
        }
    }
}
