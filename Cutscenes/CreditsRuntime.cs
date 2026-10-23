using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Maps;
using Celeste.Everest.Runtime.Levels.Update;
using Celeste.Game.Chapters.Flow;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.LevelLoader;
using Celeste.Game.Execution.Session;
using Celeste.Game.Runtime;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Presentation.Scene.Transitions;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Cutscenes
{
    public sealed class CreditsRuntime : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, CreditsRuntime> runtimes = new();
        private readonly GameApplication engine;
        private readonly ILHook areaCompleteHook;
        private bool disposed;
        public TasHelper Playback { get; }

        public CreditsRuntime(ISourceEngineContext context)
        {
            engine = (GameApplication)context;
            runtimes.Add(context, this);
            try
            {
                areaCompleteHook =
                    new ILHook(typeof(AreaCompleteSequence).GetMethod(nameof(AreaCompleteSequence.Update)),
                        AreaCompleteUpdateHook);
                Playback = new TasHelper(engine);
                engine.LevelUpdateEvents.OnLoadEntity += LevelOnLoadEntity;
            }
            catch
            {
                runtimes.Remove(context);
                try
                {
                    areaCompleteHook?.Dispose();
                }
                finally
                {
                    Playback?.Dispose();
                }

                throw;
            }
        }

        internal static CreditsRuntime Get(ISourceEngineContext engine)
            => runtimes.TryGetValue(engine, out var runtime)
                ? runtime
                : throw new InvalidOperationException("Strawberry Jam credits runtime is not loaded for this engine.");

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            engine.LevelUpdateEvents.OnLoadEntity -= LevelOnLoadEntity;
            runtimes.Remove(engine);
            try
            {
                areaCompleteHook.Dispose();
            }
            finally
            {
                Playback.Dispose();
            }
        }

        private bool LevelOnLoadEntity(Celeste.Game.Execution.Levels.OriginalLevelSceneRoot level,
            OriginalLevelRoomData levelData, Vector2 offset, OriginalLevelEntityData entityData)
        {
            if (!ReferenceEquals(level.EngineContext, engine) ||
                !CreditsCutscene.HeartsidesToLobbies.Values.Contains(
                    engine.AreaKeys.GetSID(engine.AreaKeys.GetSessionArea(level.Session)))) return false;
            var credits = level.SourceScene.GetToAdd().OfType<CreditsCutscene>().FirstOrDefault();
            if (credits != null && entityData.Name == "CollabUtils2/RainbowBerry") return true;
            if (credits != null && entityData.Name == "CollabUtils2/MiniHeartDoor")
            {
                level.Session.SetFlag("opened_mini_heart_door_" + new MapEntityId(level.Session.Level, entityData.ID));
            }
            else if (entityData.Name == "playbackTutorial")
            {
                if (credits != null)
                {
                    var path = $"Tutorials/{entityData.Attr("tutorial")}.tas";
                    if (engine.SourceContentRegistry.TryGet(path, out _))
                    {
                        if (Playback.Preload(path)) credits.Playbacks[path] = entityData.Position + offset;
                        else
                            engine.LevelEnterStatics.ErrorMessage =
                                "[SJ] Could not parse inputs in {#ff1144}" + path + "{#}";
                    }
                    else engine.LevelEnterStatics.ErrorMessage = "[SJ] Could not find {#ff1144}" + path + "{#}";
                }

                return true;
            }

            return false;
        }

        private void AreaCompleteUpdateHook(ILContext il)
        {
            var cursor = new ILCursor(il);
            ILLabel breakLabel = null;
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction => instruction.MatchBrfalse(out breakLabel),
                    instruction => instruction.MatchLdarg(0),
                    instruction => instruction.MatchLdcI4(0),
                    instruction => instruction.MatchStfld<AreaCompleteSequence>("canConfirm")))
                throw new InvalidOperationException(
                    "Strawberry Jam credits could not bind the AreaComplete confirmation branch.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<AreaCompleteSequence, bool>>(SwitchToCredits);
            cursor.Emit(OpCodes.Brtrue, breakLabel);
        }

        private bool SwitchToCredits(AreaCompleteSequence areaComplete)
        {
            if (!ReferenceEquals(areaComplete.EngineContext, engine) ||
                !CreditsCutscene.HeartsidesToLobbies.TryGetValue(
                    engine.AreaKeys.GetSID(engine.AreaKeys.GetSessionArea(areaComplete.Session)), out var lobbySid))
                return false;
            var target = engine.AreaKeys.GetAreaBySID(lobbySid);
            var targetArea = target != null ? engine.AreaKeys.ToKey(target) : engine.AreaKeys.Default;
            new FadeWipePresentation(areaComplete, false, () =>
            {
                engine.AudioSession.SetMusic(null);
                var session = ((IOriginalSessionEnvironment)engine).Sessions.CreateSession(targetArea);
                var loader = ((IOriginalLevelLoaderEnvironment)engine).LevelLoaders.CreateLevelLoader(session);
                loader.PlayerIntroTypeOverride = PlayerIntroType.None;
                loader.Level.SourceScene.QueueSourceEntity(new CreditsCutscene(engine));
                engine.Frame.Scenes.NextScene = loader;
            });
            return true;
        }
    }
}
