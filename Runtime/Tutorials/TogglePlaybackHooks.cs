// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Entities.Playback;
using Celeste.Graphics.Animation.Replay;
using Celeste.Mods.StrawberryJam.Configuration;
using Celeste.Scene.Entities;
using Celeste.Simulation.Gameplay.Tutorial.Replay;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Tutorials
{
    public sealed class TogglePlaybackHooks : IDisposable
    {
        private static readonly Action<PlayerPlaybackState> Restart =
            (Action<PlayerPlaybackState>)typeof(PlayerPlaybackState)
                .GetMethod("Restart", BindingFlags.Instance | BindingFlags.NonPublic)
                .CreateDelegate(typeof(Action<PlayerPlaybackState>));

        private readonly ISourceEngineContext engine;
        private readonly Func<StrawberryJamSettings> settings;
        private readonly Hook hook;
        private readonly Action<SceneEntityState> preUpdate;

        private delegate void CtorOrig(OriginalPlayerPlaybackState self, ISourceEngineContext engine, Vector2 start,
            List<CharacterReplayFrame> timeline, EntityData data, Vector2 offset, IPlayerPlaybackAudio audio);

        private delegate void CtorHook(CtorOrig orig, OriginalPlayerPlaybackState self, ISourceEngineContext engine,
            Vector2 start, List<CharacterReplayFrame> timeline, EntityData data, Vector2 offset,
            IPlayerPlaybackAudio audio);

        public TogglePlaybackHooks(ISourceEngineContext engine, Func<StrawberryJamSettings> settings)
        {
            this.engine = engine;
            this.settings = settings;
            preUpdate = PreUpdate;
            hook = new Hook(
                typeof(OriginalPlayerPlaybackState).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[]
                    {
                        typeof(ISourceEngineContext), typeof(Vector2), typeof(List<CharacterReplayFrame>),
                        typeof(EntityData), typeof(Vector2), typeof(IPlayerPlaybackAudio)
                    }, null), (CtorHook)Constructed);
        }

        private void Constructed(CtorOrig orig, OriginalPlayerPlaybackState self, ISourceEngineContext source,
            Vector2 start, List<CharacterReplayFrame> timeline, EntityData data, Vector2 offset,
            IPlayerPlaybackAudio audio)
        {
            orig(self, source, start, timeline, data, offset, audio);
            if (ReferenceEquals(source, engine)) self.PreUpdate += preUpdate;
        }

        private void PreUpdate(SceneEntityState obj)
        {
            if (settings().TogglePlaybacks.Released)
            {
                if (obj.Active)
                {
                    obj.Active = false;
                    obj.Visible = false;
                }
                else
                {
                    obj.Active = true;
                    Restart((PlayerPlaybackState)obj);
                }
            }
        }

        public void Dispose() => hook.Dispose();
    }
}
