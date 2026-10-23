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
        // Also sets flag as a fallback / for effect on skip cutscene
        private IEnumerator GroundPound(RoomPlayerSession player)
        {
            OriginalLevelSceneRoot level = ((OriginalLevelSceneRoot)player.Scene);
            level.PauseLock = true;

            player.Machine.State = 11;
            while (!player.Dead && !player.OnGround())
            {
                player.X = ScalarMath.Clamp(player.X, Left - Width, Right + Width);
                yield return null;
            }

            player.Machine.ForceState(20);
            level.Session.SetFlag(flagOnCollect, true);
            level.PauseLock = false;
        }

        public void SkipCutscene(OriginalLevelSceneRoot level, RoomPlayerSession player)
        {
            level.Frozen = false;
            level.Paused = false;
            engine.Effects.Glitch = 0f;
            level.FormationBackdrop.Alpha = 1f;
            level.FormationBackdrop.Display = false;
            ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Stop(collectAudioEvent);

            player.Add(new SimulationCoroutine(engine, GroundPound(player)));

            message?.RemoveSelf();
            level.Camera.Zoom = 1f;
            RemoveSelf();
        }
    }
}
