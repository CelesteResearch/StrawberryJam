using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Maps;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Game.Audio.Runtime;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Platforms.Cassette;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Core.Rendering;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Mods.StrawberryJam.Persistence;
using Celeste.Mods.StrawberryJam.Runtime.Cassette;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Scene.Tagging;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Audio.Fmod.Events;

namespace Celeste.Mods.StrawberryJam.Entities.Platforms.Cassette
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.WonkyCassetteBlockController")]
    [CustomEntity("SJ2021/WonkyCassetteBlockController")]
    [SceneTracked]
    public class WonkyCassetteBlockController : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly StrawberryJamModule module;

        // Music stuff
        public readonly int bpm;
        public readonly int bars;
        public readonly int barLength; // The top number in the time signature
        public readonly int beatLength; // The bottom number in the time signature
        public readonly float cassetteOffset;
        private readonly string param;

        public readonly int ExtraBoostFrames;

        private readonly string DisableFlag;

        public float beatIncrement;
        public int maxBeats;

        private bool isLevelMusic;
        private FmodEventReference sfx;
        private FmodEventReference snapshot;

        private bool transitioningIn = false;

        public WonkyCassetteBlockController(ISourceEngineContext engine,
            Celeste.Formats.Map.Entities.Structure.EntityData data, Vector2 offset)
            : this(engine, data.Position + offset, data.Int("bpm"), data.Int("bars"), data.Attr("timeSignature"),
                data.Attr("sixteenthNoteParam", "sixteenth_note"), data.Float("cassetteOffset"),
                data.Int("boostFrames", 1), data.Attr("disableFlag"))
        {
        }

        public WonkyCassetteBlockController(ISourceEngineContext engine, Vector2 position, int bpm, int bars,
            string timeSignature, string param, float cassetteOffset, int boostFrames, string disableFlag)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
            module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<StrawberryJamModule>().Single();
            Tags = ((ISourceBitTagEnvironment)engine).BitTags.Get("frozenUpdate") |
                   ((ISourceBitTagEnvironment)engine).BitTags.Get("transitionUpdate");

            Add(new RoomTransitionListener()
            {
                OnInBegin = () => transitioningIn = true,
                OnInEnd = () => transitioningIn = false,
            });

            this.bpm = bpm;
            this.bars = bars;
            this.param = param;
            this.cassetteOffset = cassetteOffset;

            GroupCollection timeSignatureParsed = new Regex(@"^(\d+)/(\d+)$").Match(timeSignature).Groups;
            if (timeSignatureParsed.Count == 0)
                throw new ArgumentException($"\"{timeSignature}\" is not a valid time signature.");

            barLength = int.Parse(timeSignatureParsed[1].Value);
            beatLength = int.Parse(timeSignatureParsed[2].Value);

            if (boostFrames < 1)
                throw new ArgumentException($"Boost Frames must be 1 or greater, but is set to {boostFrames}.");

            ExtraBoostFrames = boostFrames - 1;

            this.DisableFlag = disableFlag;
        }

        public void DisableAndReset(object scene, StrawberryJamSession session)
        {
            session.MusicBeatTimer = 0;
            session.MusicWonkyBeatIndex = 0;

            session.CassetteBeatTimer = session.MusicBeatTimer - cassetteOffset;
            session.CassetteWonkyBeatIndex = 0;

            var wonkyBlocks = ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntities(typeof(WonkyCassetteBlock))
                .Cast<WonkyCassetteBlock>();

            foreach (WonkyCassetteBlock wonkyBlock in wonkyBlocks)
            {
                wonkyBlock.Activated = false;
            }

            var minorControllers =
                ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntities(
                    typeof(WonkyMinorCassetteBlockController));

            foreach (WonkyMinorCassetteBlockController minorController in minorControllers)
            {
                minorController.Reset(session);
            }

            session.CassetteBlocksDisabled = true;
        }

        public void PrepareEnable(object scene, StrawberryJamSession session)
        {
            var wonkyBlocks = ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntities(typeof(WonkyCassetteBlock))
                .Cast<WonkyCassetteBlock>();

            foreach (WonkyCassetteBlock wonkyBlock in wonkyBlocks)
            {
                if (wonkyBlock.ControllerIndex == 0)
                {
                    if (wonkyBlock.OnAtBeats.Contains(session.CassetteWonkyBeatIndex / (16 / beatLength) % barLength) !=
                        wonkyBlock.Activated)
                    {
                        wonkyBlock.WillToggle();
                    }
                }
                else
                {
                    foreach (WonkyMinorCassetteBlockController minorController in ((ISourceScene)Scene).SourceScene
                             .Entities.GetTrackedEntities(typeof(WonkyMinorCassetteBlockController)))
                    {
                        if (wonkyBlock.ControllerIndex == minorController.ControllerIndex &&
                            wonkyBlock.OnAtBeats.Contains(minorController.CassetteWonkyBeatIndex /
                                (16 / minorController.beatLength) % minorController.barLength) != wonkyBlock.Activated)
                        {
                            wonkyBlock.WillToggle();
                        }
                    }
                }
            }
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);

            if (((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities("Celeste.CassetteBlockManager").Count > 0)
                throw new Exception("WonkyCassetteBlockController detected in same room as ManualCassetteController!");

            isLevelMusic = ((IOriginalAreaKeyEnvironment)engine).AreaKeys
                .GetAreaById(((OriginalLevelSceneRoot)Scene).Session.AreaID).CassetteSong == "-";

            if (isLevelMusic)
                sfx = ((IOriginalGameplayProcess)engine).AudioSession.CurrentMusicEventInstance;
            else
                snapshot = ((IOriginalGameplayProcess)engine).AudioSession.CreateSnapshot("snapshot:/music_mains_mute");

            StrawberryJamSession session = module.Session;

            // We always want sixteenth notes here, regardless of time signature
            beatIncrement = (float)(60.0 / bpm * beatLength / 16.0);
            maxBeats = 16 * bars * barLength / beatLength;

            session.MusicWonkyBeatIndex = session.MusicWonkyBeatIndex % maxBeats;

            // Synchronize the beat indices.
            // This may leave cassette blocks activated or deactivated for up to
            // the duration of an offset longer than normal at the start, but
            // that will fix itself within one beatIncrement duration
            session.CassetteWonkyBeatIndex = session.MusicWonkyBeatIndex;

            // Re-synchronize the beat timers
            // Positive offsets will make the cassette blocks lag behind the music progress
            session.CassetteBeatTimer = session.MusicBeatTimer - cassetteOffset;

            // Reset timers on song change
            if (session.CassetteBlocksLastParameter != param)
            {
                DisableAndReset(scene, session);
                session.CassetteBlocksLastParameter = param;
            }

            // Make sure minor controllers are set up after the main one
            foreach (WonkyMinorCassetteBlockController minorController in ((ISourceScene)Scene).SourceScene.Entities
                     .GetTrackedEntities(typeof(WonkyMinorCassetteBlockController)))
            {
                minorController.MinorAwake(scene, session, this);
            }
        }

        public void CheckDisableAndReset()
        {
            StrawberryJamSession session = module.Session;

            if (DisableFlag.Length == 0)
            {
                if (session.CassetteBlocksDisabled)
                    session.CassetteBlocksDisabled = false;
                return;
            }

            OriginalLevelSceneRoot level = (OriginalLevelSceneRoot)Scene;
            bool shouldDisable = level.Session.GetFlag(DisableFlag);

            if (!session.CassetteBlocksDisabled && shouldDisable)
            {
                DisableAndReset(level, session);
            }
            else if (session.CassetteBlocksDisabled && !shouldDisable)
            {
                session.CassetteBlocksDisabled = false;

                PrepareEnable(level, session);
            }
        }

        internal void AdvanceMusic(float time, object scene, StrawberryJamSession session)
        {
            CheckDisableAndReset();

            if (session.CassetteBlocksDisabled)
                return;

            session.CassetteBeatTimer += time;

            bool synchronizeMinorControllers = false;

            if (session.CassetteBeatTimer >= beatIncrement)
            {
                session.CassetteBeatTimer -= beatIncrement;

                // beatIndex is always in sixteenth notes
                var wonkyBlocks = ((ISourceScene)scene).SourceScene.Entities
                    .GetTrackedEntities(typeof(WonkyCassetteBlock)).Cast<WonkyCassetteBlock>();
                int nextBeatIndex = (session.CassetteWonkyBeatIndex + 1) % maxBeats;
                int beatInBar = session.CassetteWonkyBeatIndex / (16 / beatLength) % barLength; // current beat

                int nextBeatInBar = nextBeatIndex / (16 / beatLength) % barLength; // next beat
                bool beatIncrementsNext =
                    (nextBeatIndex / (float)(16 / beatLength)) % 1 ==
                    0; // will the next beatIndex be the start of a new beat

                foreach (WonkyCassetteBlock wonkyBlock in wonkyBlocks)
                {
                    if (wonkyBlock.ControllerIndex != 0)
                        continue;

                    wonkyBlock.Activated = wonkyBlock.OnAtBeats.Contains(beatInBar);

                    if (wonkyBlock.OnAtBeats.Contains(nextBeatInBar) != wonkyBlock.Activated && beatIncrementsNext)
                    {
                        wonkyBlock.WillToggle();
                    }
                }

                // Doing this here because it would go to the next beat with a sixteenth note offset at start
                session.CassetteWonkyBeatIndex = (session.CassetteWonkyBeatIndex + 1) % maxBeats;

                // Synchronize minor controllers right before the start of a bar
                if (nextBeatInBar == 0 && beatInBar != 0)
                {
                    synchronizeMinorControllers = true;
                }
            }

            session.MusicBeatTimer += time;

            if (session.MusicBeatTimer >= beatIncrement)
            {
                session.MusicBeatTimer -= beatIncrement;

                sfx?.SetParameter(param, (session.MusicWonkyBeatIndex * beatLength / 16) + 1);

                // Doing this here because it would go to the next beat with a sixteenth note offset at start
                session.MusicWonkyBeatIndex = (session.MusicWonkyBeatIndex + 1) % maxBeats;
            }

            // Make sure minor controllers are set up after the main one
            foreach (WonkyMinorCassetteBlockController minorController in ((ISourceScene)Scene).SourceScene.Entities
                     .GetTrackedEntities(typeof(WonkyMinorCassetteBlockController)))
            {
                if (synchronizeMinorControllers)
                {
                    minorController.Synchronize(time, session);
                }

                minorController.AdvanceMusic(time, scene);
            }
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            if (!isLevelMusic)
            {
                ((IOriginalGameplayProcess)engine).AudioSession.Stop(snapshot);
                ((IOriginalGameplayProcess)engine).AudioSession.Stop(sfx);
            }
        }

        public override void Update()
        {
            base.Update();

            if (transitioningIn)
                return;

            if (isLevelMusic)
                sfx = ((IOriginalGameplayProcess)engine).AudioSession.CurrentMusicEventInstance;

            if (!isLevelMusic && sfx == null)
            {
                sfx = ((IOriginalGameplayProcess)engine).AudioSession.CreateInstance(
                    ((IOriginalAreaKeyEnvironment)engine).AreaKeys
                    .GetAreaById(((OriginalLevelSceneRoot)Scene).Session.AreaID).CassetteSong, null, null);
                ((IOriginalGameplayProcess)engine).AudioSession.Play("event:/game/general/cassette_block_switch_2");
                sfx.Start();
            }
            else
            {
                AdvanceMusic(engine.DeltaTime, Scene, module.Session);
            }
        }
    }
}
