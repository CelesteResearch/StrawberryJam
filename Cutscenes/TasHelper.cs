using System;
using System.Collections;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Mods.Tas;
using Celeste.Game.Runtime;
using Celeste.Mods.Tas.Playback;

namespace Celeste.Mods.StrawberryJam.Cutscenes
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Cutscenes.TasHelper")]
    public sealed class TasHelper : IDisposable
    {
        private readonly GameApplication engine;
        private readonly TutorialPlaybackService playback;

        public TasHelper(GameApplication engine)
        {
            this.engine = engine;
            if (((ISourceRuntimeExtensionEnvironment)engine).GetRuntimeExtension<TasRuntimeEnvironment>() != null)
                playback = new TutorialPlaybackService(engine);
        }

        public bool Active => playback?.Active ?? false;
        public bool Paused => playback?.Paused ?? false;
        public float TotalTime => playback?.TotalTime ?? 0f;
        public void Clear() => playback?.Clear();
        public void Play(string path) => playback?.Play(path);
        public IEnumerator Wait(float buffer = 0f) => playback?.Wait(buffer);
        public void Stop() => playback?.Stop();
        public void Dispose() => playback?.Dispose();

        public bool Preload(string path)
        {
            if (playback == null || !engine.SourceContentRegistry.TryGet(path, out var asset)) return false;
            using var stream = asset.Stream;
            return playback.Preload(path, stream);
        }
    }
}
