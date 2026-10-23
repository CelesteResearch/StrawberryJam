using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Levels.Cutscenes;
using Celeste.Game.Runtime;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Presentation.Scene.Transitions;
using System.Threading;
using Celeste.Audio.Fmod.Events;
using Celeste.Mods.StrawberryJam.Entities.Arcades;

namespace Celeste.Mods.StrawberryJam.Cutscenes.Pinball
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.PinballIntroCutscene")]
    internal partial class PinballIntroCutscene : OriginalCutsceneEntityState
    {
        private readonly GameApplication engine;
        private RoomPlayerSession player;
        private Fader fader;
        private FmodEventReference sfx;
        private PinballMachine pinball;

        public PinballIntroCutscene(GameApplication engine, RoomPlayerSession player, PinballMachine pinball) : base(
            engine, true, false)
        {
            this.engine = engine;
            this.player = player;
            this.pinball = pinball;
        }

        public override void OnBegin(OriginalLevelSceneRoot level)
        {
            Add(new SimulationCoroutine(engine, Cutscene(level), true));
            level.Scheduler.QueueSourceEntity(fader = new Fader(engine));
        }

        private IEnumerator Cutscene(OriginalLevelSceneRoot level)
        {
            player.Machine.State = 11;
            player.Machine.Locked = true;
            engine.AudioSession.SetMusic(null, true, true);
            yield return player.DummyWalkToExact((int)pinball.X, false, 1f, false);
            yield return .25f;
            yield return player.DummyWalkToExact((int)pinball.X - 16, false, 1f, false);
            yield return .5f;
            yield return player.DummyWalkToExact((int)pinball.X + 16, false, 1f, false);
            yield return .25f;
            player.Data.Facing = -1;
            yield return .1f;
            yield return 1f;
            pinball.Activate();
            yield return 4f;
            player.Dummy.AutoAnimate = false;
            player.Sprite.Play("lookUp", false, false);
            yield return 1.1f;
            player.Dummy.AutoAnimate = true;
            Add(new SimulationCoroutine(engine, level.ZoomTo(new Vector2(160f, 95f), 2.3f, 10f), true));
            yield return .25f;
            player.Appearance.ForceStrongWindHair.X = -1f;
            yield return player.DummyWalkToExact((int)player.X + 12, false, 1f, false);
            player.Data.Facing = 1;
            player.Dummy.AutoAnimate = false;
            player.Dummy.Gravity = false;
            sfx = engine.AudioSession.Play("event:/strawberry_jam_2021/game/maya/pumber", pinball.Center,
                engine.SaveContext);
            player.Sprite.Play("runWind", false, false);
            while (player.Sprite.Rate > 0f)
            {
                player.MoveH(player.Sprite.Rate * 10f * engine.DeltaTime);
                player.MoveV((0f - (1f - player.Sprite.Rate)) * 6f * engine.DeltaTime);
                player.Sprite.Rate -= engine.DeltaTime * .15f;
                yield return null;
            }

            yield return .5f;
            player.Sprite.Play("fallFast", false, false);
            player.Sprite.Rate = 1f;
            Vector2 target = pinball.Center - new Vector2(0f, 7f), from = player.Position;
            for (float p = 0f; p < 1f; p += engine.DeltaTime * 2f)
            {
                player.Position = from + (target - from) * engine.EasingArithmetic.SineInOut(p);
                if (p > .1f)
                {
                    engine.Effects.Glitch = .3f;
                    ((OriginalLevelSceneRoot)Scene).Session.SetFlag("introSilhouette", true);
                }

                if (p > .3f)
                {
                    engine.Effects.Glitch = 0f;
                    ((OriginalLevelSceneRoot)Scene).Session.SetFlag("introSilhouette", false);
                }

                yield return null;
            }

            engine.Effects.Glitch = .3f;
            yield return .1f;
            ((OriginalLevelSceneRoot)Scene).Session.SetFlag("introSilhouette", true);
            yield return .1f;
            engine.Effects.Glitch = 0f;
            player.Appearance.ForceStrongWindHair.X = 0f;
            ((ISourceScreenWipeEnvironment)engine).ProcessWipe.SpotlightFocusPoint = player.Position -
                ((OriginalLevelSceneRoot)Scene).Camera.Position + new Vector2(0f, -8f);
            var wipe = new SpotlightWipePresentation((OriginalLevelSceneRoot)Scene, false, () =>
            {
                Thread.Sleep(100);
                EndCutscene(level, true);
            });
            level.Renderers.Add(wipe);
        }

        public override void OnEnd(OriginalLevelSceneRoot level)
        {
            level.OnEndOfFrame += () =>
            {
                if (fader != null && !WasSkipped)
                {
                    fader.Tags = engine.Tags.Global.Value;
                    fader.Target = 0f;
                    fader.Ended = true;
                }

                if (WasSkipped)
                {
                    ((ISceneEntityRemovalScene)level).RemoveEntity(player);
                    level.UnloadLevel();
                    engine.AudioSession.SetMusic(null, true, true);
                    if (sfx != null) engine.AudioSession.Stop(sfx, true);
                    level.Session.Level = "Tutorial";
                    level.Session.FirstLevel = false;
                    level.Session.RespawnPoint =
                        level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Bottom));
                    level.LoadLevel((PlayerIntroType)8, false);
                    level.Wipe.Cancel();
                }
                else
                {
                    ((ISceneEntityRemovalScene)level).RemoveEntity(player);
                    level.EndCutscene();
                    level.UnloadLevel();
                    level.Session.Level = "Tutorial";
                    level.Session.FirstLevel = false;
                    level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top));
                    level.LoadLevel((PlayerIntroType)6, false);
                    engine.AudioSession.SetMusic(null, true, true);
                    level.Scheduler.QueueSourceEntity(new PinballFallCutscene(engine));
                    level.Camera.Position -= new Vector2(0f, 8f);
                    if (!WasSkipped && level.Wipe != null) level.Wipe.Cancel();
                    if (fader != null) fader.Tags &= ~engine.Tags.Global.Value;
                }
            };
        }
    }
}
