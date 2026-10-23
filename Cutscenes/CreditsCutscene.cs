using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Registry;
using Celeste.Everest.Content.Textures;
using Celeste.Everest.Entities.Birds.Tutorials;
using Celeste.Everest.Runtime.Startup;
using Celeste.Game.Audio;
using Celeste.Game.Components.Talking;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Levels.Cutscenes;
using Celeste.Game.Gameplay.Triggers.Audio;
using Celeste.Game.Presentation.Hud.Statistics;
using Celeste.Game.Presentation.Hud.Strawberries;
using Celeste.Game.Runtime;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Audio;
using Celeste.Simulation.Presentation.Scene.Transitions;

namespace Celeste.Mods.StrawberryJam.Cutscenes
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Cutscenes.CS_Credits")]
    public sealed class CreditsCutscene : OriginalCutsceneEntityState
    {
        private const float FadeTime = 2f;
        private const string CreditsSong = "event:/sj21_credits";
        private const string SRTFastRestartFlag = "StopFastRestartFlag";

        public static readonly Dictionary<string, string> HeartsidesToLobbies = new()
        {
            { "StrawberryJam2021/1-Beginner/ZZ-HeartSide", "StrawberryJam2021/0-Lobbies/1-Beginner" },
            { "StrawberryJam2021/2-Intermediate/ZZ-HeartSide", "StrawberryJam2021/0-Lobbies/2-Intermediate" },
            { "StrawberryJam2021/3-Advanced/ZZ-HeartSide", "StrawberryJam2021/0-Lobbies/3-Advanced" },
            { "StrawberryJam2021/4-Expert/ZZ-HeartSide", "StrawberryJam2021/0-Lobbies/4-Expert" },
            { "StrawberryJam2021/5-Grandmaster/ZZ-HeartSide", "StrawberryJam2021/0-Lobbies/5-Grandmaster" }
        };

        private readonly GameApplication engine;
        private readonly TasHelper playback;
        private OriginalGameInputProcess Input => engine.InputSource;
        private ISourceEasingArithmetic Ease => ((ISourceEasingEnvironment)engine).EasingArithmetic;
        public IDictionary<string, Vector2> Playbacks => playbacks;

        private readonly SortedDictionary<string, Vector2> playbacks = new(StringComparer.OrdinalIgnoreCase);
        private readonly AtlasEntry gradient;
        private AtlasEntry thanksImage;
        private RoomAudioState previousAudio;
        private Credits credits;
        private float fade;
        private float buttonEase;
        private bool fromHeartside;
        private bool finished;

        public CreditsCutscene(ISourceEngineContext context)
            : base(context, true, false)
        {
            engine = (GameApplication)context;
            playback = CreditsRuntime.Get(context).Playback;
            gradient = engine.GuiAtlas.Entries["creditsgradient"].GetSubtexture(0, 1, 1920, 1);
            Tags = ((IEverestGameTagsEnvironment)engine).EverestTags.SubHUD.Value;
            playback.Clear();
        }

        public override void OnBegin(OriginalLevelSceneRoot level)
        {
            // Speedrun Tool's fast restart skips scenes, so we need to disable it during credits
            level.Session.SetFlag(SRTFastRestartFlag);

            string sid = engine.AreaKeys.GetSID(engine.AreaKeys.GetSessionArea(Level.Session));
            string mapName = sid.Substring(sid.LastIndexOf('/') + 1);
            fromHeartside = mapName != "0-Prologue";
            if (engine.SourceContentRegistry.TryGet($"Graphics/Atlases/Credits/StrawberryJam2021/{mapName}",
                    out var thanksAsset))
            {
                thanksImage =
                    new EverestAtlasEntry(((EverestVirtualContent)engine.VirtualContent).CreateTexture(thanksAsset));
            }

            engine.AudioSession.BusMuted("bus:/gameplay_sfx", mute: true);
            engine.MInput.UpdateNull();
            engine.MInput.Disabled = true;
            Level.InCredits = true;
            Level.TimerHidden = true;

            if (fromHeartside)
            {
                Add(new SimulationCoroutine(engine, LobbyRoutine()));
            }
            else
            {
                Add(new SimulationCoroutine(engine, MovieRoutine()));
            }
        }

        public override void Update()
        {
            base.Update();
            engine.AudioSession.MusicUnderwater = false;

            if (!playback.Active && Level.InCredits)
            {
                engine.MInput.Disabled = false;
                if (Level.CanPause && (Input.Pause.Pressed || Input.ESC.Pressed))
                {
                    Input.Pause.ConsumeBuffer();
                    Input.ESC.ConsumeBuffer();
                    Level.Pause(minimal: true);
                }
                else if (credits != null && credits.BottomTimer > 2f)
                {
                    buttonEase = Approach(buttonEase, finished ? 0f : 1f, engine.DeltaTime * 2);
                    if (Input.MenuConfirm.Pressed)
                    {
                        finished = true;
                    }
                }

                engine.MInput.Disabled = true;
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            if (fromHeartside)
            {
                if (engine.SaveContext.CurrentSave.Assists.MirrorMode)
                {
                    SourceAtlasDrawing.Draw(context, this, gradient, new Vector2(1720f, -10f), Vector2.Zero,
                        PackedColor.White * 0.6f, new Vector2(-1f, 1100f));
                }
                else
                {
                    SourceAtlasDrawing.Draw(context, this, gradient, new Vector2(200f, -10f), Vector2.Zero,
                        PackedColor.White * 0.6f, new Vector2(1f, 1100f));
                }
            }

            if (fade > 0f)
            {
                SourcePrimitiveDrawing.Rect(context, this, -10f, -10f, 1940f, 1100f,
                    PackedColor.Black * Ease.CubeInOut(fade));
            }

            if (buttonEase > 0f)
            {
                SourceAtlasDrawing.DrawCentered(context, this,
                    Input.GuiButton(Input.MenuConfirm, OriginalGameInputProcess.PrefixMode.Latest,
                        "controls/keyboard/oemquestion"),
                    new Vector2(1920f - 120, 1080f - 120f * Ease.CubeOut(buttonEase)),
                    PackedColor.White * Ease.CubeOut(buttonEase));
            }
        }

        public override void OnEnd(OriginalLevelSceneRoot level)
        {
            engine.AudioSession.BusMuted("bus:/gameplay_sfx", mute: false);
            engine.MInput.Disabled = false;
            credits?.RemoveSelf();
            thanksImage?.Unload();
            thanksImage = null;

            if (fromHeartside)
            {
                Level.CompleteArea(skipScreenWipe: true, skipCompleteScreen: true);
            }
            else
            {
                Level.InCredits = false;
                Level.TimerHidden = false;
                Level.SourceScene.QueueSourceEntity(new TotalStrawberriesDisplayState(engine));
                Level.SourceScene.QueueSourceEntity(new GameplayStatsState(engine));
                Level.Session.Audio = previousAudio;
                OriginalAudioSession.Apply(Level.Session.Audio, engine.AudioSession.ChannelOutput,
                    engine.AudioApplyPolicy);
                if (Level.SourceScene.Entities.GetTrackedEntity<CreditsTalker>()?.Get<OriginalTalkComponent>() is
                    OriginalTalkComponent talker)
                {
                    talker.Enabled = true;
                }
            }
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            engine.AudioSession.BusMuted("bus:/gameplay_sfx", mute: false);
            engine.MInput.Disabled = false;
            thanksImage?.Unload();
            thanksImage = null;
        }

        private IEnumerator MovieRoutine()
        {
            previousAudio = Level.Session.Audio.Clone();
            engine.AudioSession.SetMusic(null);

            Level.SourceScene.Entities.FindFirst<TotalStrawberriesDisplayState>()?.RemoveSelf();
            Level.SourceScene.Entities.FindFirst<GameplayStatsState>()?.RemoveSelf();
            if (Level.SourceScene.Entities.GetTrackedEntity<CreditsTalker>()?.Get<OriginalTalkComponent>() is
                OriginalTalkComponent talker)
            {
                talker.Enabled = false;
            }

            yield return FadeTo(1f);

            Level.Session.Audio.Music.Event = CreditsSong;
            OriginalAudioSession.Apply(Level.Session.Audio, engine.AudioSession.ChannelOutput, engine.AudioApplyPolicy);

            yield return 0.5f;

            Level.SourceScene.QueueSourceEntity(credits = new Credits(engine, new Vector2(960f, 540f),
                thanksImage ?? engine.GuiAtlas.GetFallback()));

            while (!finished)
            {
                yield return null;
            }

            FadeWipePresentation wipe = new FadeWipePresentation(Level, wipeIn: false)
            {
                Duration = 3f,
                OnUpdate = (percent) => engine.AudioSession.SetMusicParam("fade", 1 - percent),
            };

            while (wipe.Percent < 1f)
            {
                // Runs check to see if wipe will finish next frame, so we can remove the credits appropriately so they do not appear for 1 frame
                if (Approach(wipe.Percent, 1f, engine.RawDeltaTime / wipe.Duration) >= 1f)
                {
                    credits.RemoveSelf();
                }

                yield return null;
            }

            yield return FadeTo(0f);

            EndCutscene(Level);
        }

        private IEnumerator LobbyRoutine()
        {
            yield return null;

            Level.SourceScene.Entities.FindFirst<TotalStrawberriesDisplayState>()?.RemoveSelf();
            Level.SourceScene.Entities.FindFirst<GameplayStatsState>()?.RemoveSelf();

            foreach (CustomBirdTutorial tutorial in Level.SourceScene.Entities
                         .GetTrackedEntities(typeof(CustomBirdTutorial)).Cast<CustomBirdTutorial>())
            {
                tutorial.TriggerHideTutorial();
            }

            foreach (OriginalAmbienceParamTriggerState trigger in Level.SourceScene.Entities.Where(e =>
                         e is OriginalAmbienceParamTriggerState))
            {
                trigger.RemoveSelf();
            }

            foreach (var entity in Level.SourceScene.Entities)
            {
                if (entity is ISourceOwnedComponentList componentOwner)
                    componentOwner.OwnedComponents.Get<OriginalTalkComponent>()?.RemoveSelf();
            }

            Level.Wipe.Cancel();

            fade = 1f;

            Level.Session.Audio.Music.Event = CreditsSong;
            OriginalAudioSession.Apply(Level.Session.Audio, engine.AudioSession.ChannelOutput, engine.AudioApplyPolicy);

            yield return 0.5f;

            float creditsX = engine.SaveContext.CurrentSave.Assists.MirrorMode ? 50f : 1870f;
            Level.SourceScene.QueueSourceEntity(credits = new Credits(engine, new Vector2(creditsX, 0f),
                thanksImage ?? engine.GuiAtlas.GetFallback(), alignment: 1f, scale: 0.6f, doubleColumns: false));

            yield return 1f;

            List<string> keys = playbacks.Keys.ToList();
            float deadTime = Credits.SongLength - playback.TotalTime - keys.Count - 2f;
            float gapTime = Math.Max(0f, deadTime / (keys.Count - 1));

            for (int i = 0; i < keys.Count; i++)
            {
                if (Level.SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>() is RoomPlayerSession player)
                {
                    player.Position = playbacks[keys[i]];
                    Level.Camera.Position = player.CameraTarget;
                    yield return 1f;
                }

                playback.Play(keys[i]);
                yield return FadeTo(0f);
                yield return playback.Wait(buffer: FadeTime);
                yield return FadeTo(1f);

                if (i < keys.Count - 1)
                {
                    yield return gapTime;
                }
            }

            while (!finished)
            {
                yield return null;
            }

            yield return new FadeWipePresentation(Level, wipeIn: false)
            {
                Duration = 3f,
                OnUpdate = (percent) => engine.AudioSession.SetMusicParam("fade", 1 - percent)
            }.Wait();

            engine.AudioSession.SetMusic(null);

            EndCutscene(Level);
        }

        private IEnumerator FadeTo(float value)
        {
            while (fade != value)
            {
                fade = Approach(fade, value, engine.DeltaTime / FadeTime);
                yield return null;
            }
        }

        private float Approach(float value, float target, float amount)
            => ScalarMath.Approach(value, target, amount, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
    }
}
