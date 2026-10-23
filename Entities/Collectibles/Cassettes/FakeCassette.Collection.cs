// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Runtime.Startup;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Runtime;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Rendering;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.Particles;
using Celeste.Graphics.Particles;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.StrawberryJam.Entities.Collectibles.Cassettes
{
    public partial class FakeCassette
    {
        private IEnumerator CollectRoutine(RoomPlayerSession player)
        {
            collecting = true;
            OriginalLevelSceneRoot level = Scene as OriginalLevelSceneRoot;
            var blocks = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(engine.SceneCatalog
                .Find(typeof(Celeste.Game.Gameplay.Platforms.Cassette.CassetteBlockState)).RegistryKey);
            level.Frozen = true;
            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.FrozenUpdate.Value;
            level.Session.RespawnPoint = level.GetSpawnPoint(Position);
            level.Session.UpdateLevelStartDashes();
            Depth = -1000000;
            level.Shake();
            level.Flash(PackedColor.White);
            level.Displacement.Clear();
            level.FormationBackdrop.Alpha = 0f;
            Vector2 camWas = level.Camera.Position;
            Vector2 camTo = Vector2Clamping.Clamp(Position - new Vector2(160f, 90f), level.Bounds.Left - 64,
                level.Bounds.Top - 32, level.Bounds.Right + 64 - 320, level.Bounds.Bottom + 32 - 180);
            level.Camera.Position = camTo;
            level.ZoomSnap(Vector2Clamping.Clamp(Position - level.Camera.Position, 60f, 60f, 260f, 120f), 2f);
            sprite.Play("spin", restart: true);
            sprite.Rate = 2f;
            for (float p3 = 0f; p3 < 1.5f; p3 += engine.DeltaTime)
            {
                sprite.Rate += engine.DeltaTime * 4f;
                yield return null;
            }

            sprite.Rate = 0f;
            sprite.SetAnimationFrame(0);
            scaleWiggler.Start();
            yield return 0.25f;
            Vector2 from = Position;
            Vector2 to = new Vector2(X, level.Camera.Top - 16f);
            float duration2 = 0.4f;
            for (float p3 = 0f; p3 < 1f; p3 += engine.DeltaTime / duration2)
            {
                sprite.Scale.X = ScalarMath.Lerp(1f, 0.1f, p3, engine.ScalarArithmetic);
                sprite.Scale.Y = ScalarMath.Lerp(1f, 3f, p3, engine.ScalarArithmetic);
                Position = Vector2Interpolation.Lerp(from, to, engine.EasingArithmetic.CubeIn(p3),
                    engine.ScalarArithmetic);
                yield return null;
            }

            Visible = false;
            message = new FakeCassetteShatterMessage(engine);
            ((ISourceScene)Scene).SourceScene.QueueSourceEntity(message);
            yield return message.EaseIn();
            level.PauseLock = true;
            yield return DoFakeRoutine(player);
            duration2 = 0.25f;
            Add(new SimulationCoroutine(engine, level.ZoomBack(duration2 - 0.05f)));
            for (float p3 = 0f; p3 < 1f; p3 += engine.DeltaTime / duration2)
            {
                level.Camera.Position = Vector2Interpolation.Lerp(camTo, camWas, engine.EasingArithmetic.SineInOut(p3),
                    engine.ScalarArithmetic);
                yield return null;
            }

            yield return 0.1f;
            player.Active = true;
            yield return GroundPound(player);

            level.EndCutscene();
            level.Frozen = false;
            level.PauseLock = false;
            yield return 0.25f;
            level.ResetZoom();
            RemoveSelf();
        }
    }
}
