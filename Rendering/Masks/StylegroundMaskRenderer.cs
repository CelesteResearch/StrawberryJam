using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Rendering.States;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content.VirtualAssets;
using Celeste.Scene.Rendering;
using Celeste.Simulation.Presentation.Backdrops.Runtime;
using Celeste.Simulation.Presentation.Backdrops.Rendering;

namespace Celeste.Mods.StrawberryJam.Rendering.Masks
{
    public sealed class StylegroundMaskRenderer : IDisposable
    {
        public const string TagPrefix = "sjstylemask_";

        public readonly List<Celeste.Simulation.Presentation.Backdrops.HeatWave.HeatWaveBackdropPresentation>
            ForegroundHeatWaves = new();

        private readonly MaskRenderProcess process;
        private readonly BackdropRendererPresentation dummy = new();
        private readonly Dictionary<string, VirtualRenderTarget> backgroundBuffers = new();
        private readonly Dictionary<string, VirtualRenderTarget> foregroundBuffers = new();
        private readonly Dictionary<string, List<BackdropPresentation>> backgroundBackdrops = new();
        private readonly Dictionary<string, List<BackdropPresentation>> foregroundBackdrops = new();
        private readonly Dictionary<string, List<StylegroundMask>> cachedMasks = new();
        private readonly MaskRenderTargets targets = new();
        private readonly MaskRenderTargets customTargets = new();

        public StylegroundMaskRenderer(MaskRenderProcess process)
        {
            this.process = process;
        }

        public Dictionary<string, VirtualRenderTarget> GetBuffers(bool foreground) =>
            foreground ? foregroundBuffers : backgroundBuffers;

        private Dictionary<string, List<BackdropPresentation>> GetBackdrops(bool foreground) =>
            foreground ? foregroundBackdrops : backgroundBackdrops;

        public void ClearMaskCache() => cachedMasks.Clear();

        private List<StylegroundMask> GetMasksWithTag(OriginalLevelSceneRoot level, string tag)
        {
            if (cachedMasks.TryGetValue(tag, out var cached)) return cached;
            var result = new List<StylegroundMask>();
            foreach (StylegroundMask mask in process.Tracked<StylegroundMask>(level))
                if (Array.IndexOf(mask.RenderTags, tag) >= 0)
                    result.Add(mask);
            cachedMasks.Add(tag, result);
            return result;
        }

        public void ConsumeStylegrounds(OriginalLevelSceneRoot level)
        {
            Consume(level.Background.Backdrops, backgroundBackdrops);
            Consume(level.Foreground.Backdrops, foregroundBackdrops);
            foreach (var group in foregroundBackdrops.Values)
            foreach (var backdrop in group)
                if (backdrop is Celeste.Simulation.Presentation.Backdrops.HeatWave.HeatWaveBackdropPresentation wave
                    && !ForegroundHeatWaves.Contains(wave))
                    ForegroundHeatWaves.Add(wave);
        }

        private void Consume(List<BackdropPresentation> source,
            Dictionary<string, List<BackdropPresentation>> destination)
        {
            for (int i = source.Count - 1; i >= 0; i--)
            {
                var backdrop = source[i];
                bool consumed = false;
                foreach (var tag in backdrop.Tags)
                {
                    if (!tag.StartsWith(TagPrefix)) continue;
                    string name = tag.Substring(TagPrefix.Length);
                    if (!destination.TryGetValue(name, out var list))
                        destination.Add(name, list = new List<BackdropPresentation>());
                    list.Insert(0, backdrop);
                    backdrop.Renderer = dummy;
                    consumed = true;
                }

                if (consumed) source.RemoveAt(i);
            }
        }

        private bool AnyMaskIsInView(OriginalLevelSceneRoot level, string tag)
        {
            var view = new IntRect((int)level.Camera.X, (int)level.Camera.Y, 320, 180);
            foreach (var mask in GetMasksWithTag(level, tag))
                if (new IntRect((int)mask.X, (int)mask.Y, (int)mask.Width, (int)mask.Height).Intersects(view))
                    return true;
            return false;
        }

        private VirtualRenderTarget GetBuffer(string tag, bool foreground)
        {
            var buffers = GetBuffers(foreground);
            if (!buffers.TryGetValue(tag, out var buffer)) buffers.Add(tag, buffer = process.CreateBuffer(tag));
            return buffer;
        }

        private void RenderStylegroundsIntoBuffers(OriginalLevelSceneRoot level, bool foreground)
        {
            foreach (var pair in GetBackdrops(foreground))
            {
                if (!AnyMaskIsInView(level, pair.Key)) continue;
                dummy.Backdrops = pair.Value;
                if (!level.Paused) dummy.Update(level);
                dummy.BeforeRender(level);
                process.Device.SetRenderTarget(GetBuffer(pair.Key, foreground).Target);
                process.Device.Clear(PackedColor.Transparent);
                dummy.Render(level);
            }
        }

        public void RenderWith(OriginalLevelSceneRoot level, bool foreground, bool behind = false)
        {
            targets.Capture(process.Device);
            RenderStylegroundsIntoBuffers(level, foreground);
            targets.Restore(process.Device);
            var buffers = GetBuffers(foreground);
            if (buffers.Count == 0) return;
            var masks = process.Tracked<StylegroundMask>(level);
            bool anyCustom = false, anyBatch = false;
            foreach (StylegroundMask mask in masks)
            {
                if (!Eligible(mask, foreground, behind)) continue;
                if (mask.Fade == MaskFadeType.Custom) anyCustom = true;
                else anyBatch = true;
            }

            if (anyCustom)
            {
                customTargets.Capture(process.Device);
                process.Begin(level, SourceSpriteSortMode.Immediate);
                foreach (StylegroundMask mask in masks)
                {
                    if (mask.Fade != MaskFadeType.Custom || !Eligible(mask, foreground, behind)) continue;
                    process.Device.SetRenderTarget(level.Buffers.TempA.Target);
                    process.Device.Clear(PackedColor.Transparent);
                    mask.DrawFadeMask();
                    process.Device.BlendState = Mask.DestinationAlphaBlend;
                    foreach (var tag in mask.RenderTags)
                        if (buffers.TryGetValue(tag, out var buffer))
                            foreach (var slice in mask.GetMaskSlices())
                                process.DrawBuffer(buffer, slice.Position, slice.Source, PackedColor.White);
                    customTargets.Restore(process.Device);
                    process.Device.BlendState = SourceBlendState.AlphaBlend;
                    process.DrawBuffer(level.Buffers.TempA, level.Camera.Position, PackedColor.White);
                }

                process.Batch.End();
            }

            if (anyBatch)
            {
                process.Begin(level);
                foreach (StylegroundMask mask in masks)
                {
                    if (mask.Fade == MaskFadeType.Custom || !Eligible(mask, foreground, behind)) continue;
                    foreach (var tag in mask.RenderTags)
                        if (buffers.TryGetValue(tag, out var buffer))
                            foreach (var slice in mask.GetMaskSlices())
                                process.DrawBuffer(buffer, slice.Position, slice.Source,
                                    PackedColor.White * slice.GetValue(mask.AlphaFrom, mask.AlphaTo));
                }

                process.Batch.End();
            }
        }

        private static bool Eligible(StylegroundMask mask, bool foreground, bool behind)
            => !mask.EntityRenderer && (!foreground || mask.BehindForeground == behind) && mask.IsVisible();

        public void Dispose()
        {
            foreach (var buffer in backgroundBuffers.Values) buffer.Dispose();
            foreach (var buffer in foregroundBuffers.Values) buffer.Dispose();
            backgroundBuffers.Clear();
            foregroundBuffers.Clear();
            cachedMasks.Clear();
            backgroundBackdrops.Clear();
            foregroundBackdrops.Clear();
            dummy.Backdrops.Clear();
            ForegroundHeatWaves.Clear();
        }
    }
}
