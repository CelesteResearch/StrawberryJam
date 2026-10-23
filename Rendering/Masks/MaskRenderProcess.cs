using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Rendering.States;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.VirtualAssets;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Batches;
using Celeste.Graphics.Rendering.Devices;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Graphics.Rendering.Effects;
using Celeste.Simulation.Gameplay.Player.Intro;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;

namespace Celeste.Mods.StrawberryJam.Rendering.Masks
{
    public sealed partial class MaskRenderProcess : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, MaskRenderProcess> processes = new();
        private readonly ISourceEngineContext engine;
        private readonly List<IDisposable> hooks = new();
        private readonly Dictionary<OriginalLevelSceneRoot, StylegroundMaskRenderer> renderers = new();

        public OriginalGameGraphicsResources Graphics =>
            (OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics;

        public SourceGraphicsDevice Device => ((ISourceGraphicsDeviceEnvironment)engine).GraphicsDevice;
        public SourceSpriteBatch Batch => ((ISourceDrawScope)engine).Draw.SpriteBatch;

        public MaskRenderProcess(ISourceEngineContext engine)
        {
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
            processes.Add(engine, this);
            try
            {
                hooks.Add(new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.LoadLevel)),
                    (Action<Action<OriginalLevelSceneRoot, PlayerIntroType, bool>, OriginalLevelSceneRoot,
                        PlayerIntroType, bool>)LoadLevel));
                hooks.Add(new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.End)),
                    (Action<Action<OriginalLevelSceneRoot>, OriginalLevelSceneRoot>)EndLevel));
                hooks.Add(new ILHook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.Render)),
                    PatchLevelRendering));
                LoadHeatWaveHooks();
                LoadLightingHook();
                LoadBloomHooks();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public static MaskRenderProcess Get(ISourceEngineContext engine) =>
            processes.TryGetValue(engine, out var process)
                ? process
                : throw new InvalidOperationException("Strawberry Jam mask renderer is not loaded for this engine.");

        public StylegroundMaskRenderer GetStylegrounds(OriginalLevelSceneRoot level) => renderers[level];

        public VirtualRenderTarget CreateBuffer(string name) =>
            ((ISourceVirtualContentEnvironment)engine).VirtualContent.CreateRenderTarget(name, 320, 180);

        public List<object> Tracked<T>(OriginalLevelSceneRoot level) =>
            level.Scheduler.Entities.GetTrackedEntities(MaskTrackedTypeKey<T>.QualifiedName);

        public void Begin(OriginalLevelSceneRoot level, SourceSpriteSortMode sort = SourceSpriteSortMode.Deferred,
            SourceBlendState blend = null, SourceSamplerState sampler = null, SourceEffect effect = null,
            Matrix4x4? matrix = null, SourceDepthStencilState depth = null)
            => Batch.Begin(sort, blend ?? SourceBlendState.AlphaBlend, sampler ?? SourceSamplerState.PointClamp,
                depth ?? SourceDepthStencilState.None, SourceRasterizerState.CullNone, effect,
                matrix ?? level.Camera.Matrix);

        public void DrawBuffer(VirtualRenderTarget buffer, Vector2 position, IntRect rectangle, PackedColor color)
            => Batch.Draw(buffer.Target, position, rectangle, color.PackedValue);

        public void DrawBuffer(VirtualRenderTarget buffer, Vector2 position, PackedColor color)
            => Batch.Draw(buffer.Target, position, color.PackedValue);

        private bool Owns(OriginalLevelSceneRoot level) => ReferenceEquals(level.EngineContext, engine);

        private void LoadLevel(Action<OriginalLevelSceneRoot, PlayerIntroType, bool> orig,
            OriginalLevelSceneRoot level, PlayerIntroType intro, bool fromLoader)
        {
            orig(level, intro, fromLoader);
            if (!Owns(level)) return;
            if (fromLoader || !renderers.TryGetValue(level, out var renderer))
            {
                if (renderers.TryGetValue(level, out var previous)) previous.Dispose();
                renderers[level] = renderer = new StylegroundMaskRenderer(this);
                renderer.ConsumeStylegrounds(level);
            }

            renderer.ClearMaskCache();
        }

        private void EndLevel(Action<OriginalLevelSceneRoot> orig, OriginalLevelSceneRoot level)
        {
            orig(level);
            if (!Owns(level) || !renderers.TryGetValue(level, out var renderer)) return;
            renderer.Dispose();
            renderers.Remove(level);
        }

        private void PatchLevelRendering(ILContext il)
        {
            PatchStylegroundRenderStages(il);
            PatchColorGradeRenderStages(il);
        }

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
            foreach (var renderer in renderers.Values) renderer.Dispose();
            renderers.Clear();
            DisposeLightingBuffers();
            DisposeColorGradeBuffers();
            DisposeBloomBuffers();
            processes.Remove(engine);
        }
    }
}
