using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Core.Scheduling;
using Celeste.Everest;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Everest.Runtime.Events;
using Celeste.Formats.Graphics.Sprites;
using Celeste.Game.Chapters.Flow;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Environment;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Runtime.Session;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Presentation
{
    public sealed class SpriteSwapHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly Dictionary<string, string> spriteSwaps = new(StringComparer.OrdinalIgnoreCase);
        private static readonly char[] Separators = { ',' };
        private readonly EverestGameEventsProcess events;
        private readonly Hook load, create, createOn;

        public SpriteSwapHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            events = ((IEverestGameEventsEnvironment)engine).GameEvents;
            load = new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.LoadLevel)),
                (Action<Action<OriginalLevelSceneRoot, PlayerIntroType, bool>, OriginalLevelSceneRoot, PlayerIntroType,
                    bool>)LoadLevel);
            create = new Hook(typeof(SpriteTemplateCollection).GetMethod(nameof(SpriteTemplateCollection.Create)),
                (Func<Func<SpriteTemplateCollection, string, Sprite>, SpriteTemplateCollection, string, Sprite>)Create);
            createOn = new Hook(typeof(SpriteTemplateCollection).GetMethod(nameof(SpriteTemplateCollection.CreateOn)),
                (Func<Func<SpriteTemplateCollection, Sprite, string, Sprite>, SpriteTemplateCollection, Sprite, string,
                    Sprite>)CreateOn);
            events.OnExit += Exit;
        }

        private SpriteBank Bank => (SpriteBank)((ISourceGameGraphicsEnvironment)engine).GameGraphics.SpriteFactory;

        private void LoadLevel(Action<OriginalLevelSceneRoot, PlayerIntroType, bool> orig, OriginalLevelSceneRoot level,
            PlayerIntroType intro, bool fromLoader)
        {
            if (ReferenceEquals(level.EngineContext, engine))
            {
                spriteSwaps.Clear();
                foreach (var data in ((OriginalLevelRoomData)level.Session.LevelData).Triggers.FindAll(data =>
                             data.Name == "SJ2021/SpriteSwapTrigger"))
                {
                    string[] fromIds = data.Attr("fromIds", "").Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                        .Select(str => str.Trim()).ToArray();
                    string[] toIds = data.Attr("toIds", "").Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                        .Select(str => str.Trim()).ToArray();
                    if (fromIds.Length == toIds.Length)
                    {
                        for (int i = 0; i < fromIds.Length; i++)
                        {
                            if (!Bank.Has(fromIds[i]) || !Bank.Has(toIds[i]))
                            {
                                ((IEverestLoggerProcessEnvironment)engine).Logger.Log(EverestLogLevel.Warn,
                                    "SJ2021/SpriteSwapTrigger", $"Couldn't find ID in swap {fromIds[i]} -> {toIds[i]}");
                                continue;
                            }

                            spriteSwaps[fromIds[i]] = toIds[i];
                        }
                    }
                    else
                        ((IEverestLoggerProcessEnvironment)engine).Logger.Log(EverestLogLevel.Warn,
                            "SJ2021/SpriteSwapTrigger", "ID lists are not the same length, cancelling swap");
                }
            }

            orig(level, intro, fromLoader);
        }

        private Sprite Create(Func<SpriteTemplateCollection, string, Sprite> orig, SpriteTemplateCollection bank,
            string id) => orig(bank,
            ReferenceEquals(bank, Bank) && spriteSwaps.TryGetValue(id, out var newId) ? newId : id);

        private Sprite CreateOn(Func<SpriteTemplateCollection, Sprite, string, Sprite> orig,
            SpriteTemplateCollection bank, Sprite sprite, string id) => orig(bank, sprite,
            ReferenceEquals(bank, Bank) && spriteSwaps.TryGetValue(id, out var newId) ? newId : id);

        private void Exit(OriginalLevelSceneRoot level, LevelExitSequence exit, LevelExitMode mode,
            ChapterCompletionSession session, HiresSnowState snow)
        {
            spriteSwaps.Clear();
        }

        public void Dispose()
        {
            events.OnExit -= Exit;
            createOn.Dispose();
            create.Dispose();
            load.Dispose();
        }
    }
}
