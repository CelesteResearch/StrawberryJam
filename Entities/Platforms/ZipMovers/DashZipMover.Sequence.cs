// SPDX-License-Identifier: MIT

using System.Collections;
using System.Numerics;
using Celeste.Game.Controls.Source;
using Celeste.Game.Entities.Platforms.ZipMovers.Motion;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Platforms.ZipMovers;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.StrawberryJam.Entities.Platforms.ZipMovers
{
    public partial class DashZipMover
    {
        private IEnumerator Sequence()
        {
            Vector2 start = Position;
            while (true)
            {
                if (!triggered)
                {
                    yield return null;
                    continue;
                }

                sfx.Play("event:/strawberry_jam_2021/game/dash_zip_mover/zip_mover");
                ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Medium,
                    RumbleLength.Short);
                StartShaking(.1f);
                yield return .1f;
                streetlight.SetAnimationFrame(3);
                StopPlayerRunIntoAnimation = false;
                float at = 0f;
                while (at < 1f)
                {
                    yield return null;
                    Vector2 position = ZipMoverMotion.Forward(engine, start, Target, ref at, ref Percent);
                    ZipMoverDrawing.ScrapeParticles(this, engine, position);
                    if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(.1f, engine)) pathRenderer.CreateSparks();
                    MoveTo(position);
                }

                StartShaking(.2f);
                ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Strong,
                    RumbleLength.Medium);
                streetlight.SetAnimationFrame(2);
                ((OriginalLevelSceneRoot)Scene).Shake();
                StopPlayerRunIntoAnimation = true;
                yield return .5f;
                StopPlayerRunIntoAnimation = false;
                streetlight.SetAnimationFrame(1);
                triggered = false;
                Target = start;
                start = Position;
                sfx.Stop();
            }
        }
    }
}
