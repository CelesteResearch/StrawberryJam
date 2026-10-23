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
        private IEnumerator DoFakeRoutine(RoomPlayerSession player)
        {
            OriginalLevelSceneRoot level = Scene as OriginalLevelSceneRoot;
            yield return 1f;
            engine.Effects.Glitch = 0.75f;
            message.Shake();
            while (engine.Effects.Glitch > 0f)
            {
                engine.Effects.Glitch = ScalarMath.Approach(engine.Effects.Glitch, 0f, engine.RawDeltaTime * 4f);
                level.Shake();

                yield return null;
            }

            yield return 1.1f;
            engine.Effects.Glitch = 0.75f;
            while (engine.Effects.Glitch > 0f)
            {
                engine.Effects.Glitch = ScalarMath.Approach(engine.Effects.Glitch, 0f, engine.RawDeltaTime * 4f);
                level.Shake();
                yield return null;
            }

            yield return 0.4f;


            engine.Effects.TimeRate = 0f;
            level.Frozen = false;
            player.Active = false;
            player.Machine.State = 11;
            while (engine.Effects.TimeRate != 1f)
            {
                engine.Effects.TimeRate = ScalarMath.Approach(engine.Effects.TimeRate, 1f, 0.5f * engine.RawDeltaTime);
                yield return null;
            }

            message.StopShake();
            message.sprite.Rate = 1f;
            while (message.sprite.Animating)
            {
                if (message.sprite.CurrentAnimationFrame == 12)
                {
                    level.Session.SetFlag(flagOnCollect);
                }

                yield return null;
            }

            message.RemoveSelf();
            level.FormationBackdrop.Alpha = 1f;
            level.FormationBackdrop.Display = false;
            engine.Effects.TimeRate = 1f;
            level.Shake();
            engine.Effects.Glitch = 0.8f;
            while (engine.Effects.Glitch > 0f)
            {
                engine.Effects.Glitch -= engine.DeltaTime * 4f;
                yield return null;
            }

            yield return 0.25f;
            player.Depth = 0;
        }
    }
}
