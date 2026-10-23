using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Game.Content.Maps;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.LevelLoader;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.VirtualAssets;
using Celeste.Input.Gameplay;
using Celeste.Mods.StrawberryJam.Rendering.DarkMatter;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Player.Configuration;
using Celeste.Simulation.Gameplay.Player.Presentation.Sprites;
using Celeste.Simulation.Runtime.Session;
using Celeste.Simulation.Runtime.Session.Maps;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.DarkMatter
{
    public sealed class DarkMatterRuntime : IDisposable
    {
        private readonly List<Hook> hooks = new();
        private readonly ConditionalWeakTable<RoomPlayerSession, DarkMatterPlayerState> states = new();
        private OriginalGameplayBuffers bufferOwner;
        public ISourceEngineContext Engine { get; }
        public Sprite Sprite { get; private set; }
        public VirtualRenderTarget LightningBuffer { get; private set; }
        public VirtualRenderTarget BlurTempBuffer { get; private set; }

        public DarkMatterRuntime(ISourceEngineContext engine)
        {
            Engine = engine;
        }

        private delegate void PlayerConstructor(RoomPlayerSession player, Vector2 spawn, IPlayerAssistSettings settings,
            ISourceGameInput input, ISourceEngineContext engine, PlatformCollisionWorld world,
            IReadOnlyDictionary<string, string> chaseSoundMapping, PlayerSpriteMode spriteMode);

        public void Load()
        {
            var constructor = typeof(RoomPlayerSession).GetConstructor(new[]
            {
                typeof(Vector2), typeof(IPlayerAssistSettings),
                typeof(ISourceGameInput), typeof(ISourceEngineContext), typeof(PlatformCollisionWorld),
                typeof(IReadOnlyDictionary<string, string>), typeof(PlayerSpriteMode)
            });
            hooks.Add(new Hook(constructor,
                (Action<PlayerConstructor, RoomPlayerSession, Vector2, IPlayerAssistSettings,
                    ISourceGameInput, ISourceEngineContext, PlatformCollisionWorld, IReadOnlyDictionary<string, string>,
                    PlayerSpriteMode>)OnPlayerConstructor));
            hooks.Add(new Hook(
                typeof(OriginalLevelLoadingResources).GetMethod("Load", BindingFlags.Instance | BindingFlags.NonPublic),
                (Action<Action<OriginalLevelLoadingResources, OriginalLevelSceneRoot, ChapterCompletionSession,
                        IChapterSessionMapData, IOriginalAreaData, Vector2?>,
                    OriginalLevelLoadingResources, OriginalLevelSceneRoot, ChapterCompletionSession,
                    IChapterSessionMapData, IOriginalAreaData, Vector2?>)
                ((orig, self, level, session, map, area, start) =>
                {
                    orig(self, level, session, map, area, start);
                    if (ReferenceEquals(level.EngineContext, Engine) && map is OriginalChapterMapData original &&
                        original.Levels.Any(room =>
                            room.Entities?.Any(entity => entity.Name == "SJ2021/DarkMatter") ?? false))
                        level.SourceScene.QueueSourceEntity(new DarkMatterRenderer(Engine));
                })));
            hooks.Add(new Hook(
                typeof(OriginalGameplayBuffers).GetMethod(nameof(OriginalGameplayBuffers.Create),
                    new[] { typeof(VirtualContent) }),
                (Action<Action<OriginalGameplayBuffers, VirtualContent>, OriginalGameplayBuffers, VirtualContent>)((
                    orig, buffers, content) =>
                {
                    orig(buffers, content);
                    if (!ReferenceEquals(content, ((ISourceVirtualContentEnvironment)Engine).VirtualContent)) return;
                    bufferOwner = buffers;
                    LightningBuffer = content.CreateRenderTarget("SJ2021-buffer-dark-matter", 160, 160);
                    BlurTempBuffer = content.CreateRenderTarget("SJ2021-temp-blur-buffer", 320, 180);
                })));
            hooks.Add(new Hook(typeof(OriginalGameplayBuffers).GetMethod(nameof(OriginalGameplayBuffers.Unload)),
                (Action<Action<OriginalGameplayBuffers>, OriginalGameplayBuffers>)((orig, buffers) =>
                {
                    orig(buffers);
                    if (ReferenceEquals(bufferOwner, buffers)) UnloadBuffers();
                })));
        }

        private void OnPlayerConstructor(PlayerConstructor orig, RoomPlayerSession player, Vector2 spawn,
            IPlayerAssistSettings settings,
            ISourceGameInput input, ISourceEngineContext engine, PlatformCollisionWorld world,
            IReadOnlyDictionary<string, string> chaseSoundMapping, PlayerSpriteMode spriteMode)
        {
            orig(player, spawn, settings, input, engine, world, chaseSoundMapping, spriteMode);
            if (ReferenceEquals(engine, Engine)) states.Add(player, new DarkMatterPlayerState(this, player));
        }

        public DarkMatterPlayerState GetPlayerState(RoomPlayerSession player) =>
            states.GetValue(player, source => new DarkMatterPlayerState(this, source));

        public void LoadContent()
        {
            Sprite = new Sprite(Engine, ((ISourceGameGraphicsEnvironment)Engine).GameGraphics.GameAtlas,
                "objects/StrawberryJam2021/darkMatterSprite/");
            Sprite.AddLoop("boost", "darkMatter", .06f);
            Sprite.Justify = new Vector2(.5f, .75f);
        }

        private void UnloadBuffers()
        {
            LightningBuffer?.Dispose();
            LightningBuffer = null;
            BlurTempBuffer?.Dispose();
            BlurTempBuffer = null;
            bufferOwner = null;
        }

        public void Dispose()
        {
            for (int index = hooks.Count - 1; index >= 0; index--) hooks[index].Dispose();
            hooks.Clear();
            UnloadBuffers();
            Sprite = null;
        }
    }
}
