using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Geometry;
using Celeste.Core.Rendering.States;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Lighting;
using Celeste.Graphics.Content.VirtualAssets;
using Celeste.Graphics.Rendering;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Rendering.Masks
{
    public sealed partial class MaskRenderProcess
    {
        private readonly List<VirtualRenderTarget> lightingFadeBuffers = new();
        private readonly List<IntRect> lightingRectangles = new(320);
        private readonly MaskRenderTargets lightingTargets = new();

        private static readonly SourceBlendState SubtractAlpha = new()
        {
            ColorSourceBlend = SourceBlendFactor.Zero, ColorDestinationBlend = SourceBlendFactor.InverseSourceAlpha,
            ColorBlendFunction = SourceBlendFunction.Add, AlphaSourceBlend = SourceBlendFactor.One,
            AlphaDestinationBlend = SourceBlendFactor.One, AlphaBlendFunction = SourceBlendFunction.ReverseSubtract
        };

        private static readonly SourceBlendState InvertAlpha = new()
        {
            ColorSourceBlend = SourceBlendFactor.InverseDestinationAlpha,
            ColorDestinationBlend = SourceBlendFactor.Zero,
            ColorBlendFunction = SourceBlendFunction.Add, AlphaSourceBlend = SourceBlendFactor.One,
            AlphaDestinationBlend = SourceBlendFactor.One, AlphaBlendFunction = SourceBlendFunction.Subtract
        };

        private void LoadLightingHook() => hooks.Add(new Hook(
            typeof(LightingRendererPresentation).GetMethod(nameof(LightingRendererPresentation.Render)),
            (Action<Action<LightingRendererPresentation, object>, LightingRendererPresentation, object>)
            RenderLighting));

        private void RenderLighting(Action<LightingRendererPresentation, object> orig,
            LightingRendererPresentation renderer, object scene)
        {
            if (scene is not OriginalLevelSceneRoot level || !Owns(level))
            {
                orig(renderer, scene);
                return;
            }

            var masks = Tracked<LightingMask>(level);
            if (masks.Count == 0)
            {
                orig(renderer, scene);
                return;
            }

            lightingTargets.Capture(Device);
            lightingRectangles.Clear();
            int customCount = 0;
            foreach (LightingMask mask in masks)
                if (mask.Fade == MaskFadeType.Custom)
                    customCount++;
            ResizeBuffers(lightingFadeBuffers, customCount, "lightingmaskfade");
            if (customCount > 0)
            {
                Begin(level, SourceSpriteSortMode.Immediate);
                int index = 0;
                foreach (LightingMask mask in masks)
                {
                    if (mask.Fade != MaskFadeType.Custom) continue;
                    mask.BufferIndex = index++;
                    float to = mask.LightingTo >= 0f ? mask.LightingTo : level.Session.LightingAlphaAdd;
                    float from = mask.LightingFrom >= 0f ? mask.LightingFrom : level.Session.LightingAlphaAdd;
                    to = Clamp((mask.AddBase ? level.BaseLightingAlpha : 0f) + to);
                    from = Clamp((mask.AddBase ? level.BaseLightingAlpha : 0f) + from);
                    bool inverted = to > from;
                    float baseAlpha = inverted ? 1f - from : from;
                    float subAlpha = from - to;
                    Device.SetRenderTarget(lightingFadeBuffers[mask.BufferIndex].Target);
                    Device.Clear(PackedColor.Transparent);
                    Device.BlendState = SourceBlendState.AlphaBlend;
                    foreach (var slice in mask.GetMaskSlices())
                        DrawBuffer(level.Buffers.Light, slice.Position, slice.Source, PackedColor.White * baseAlpha);
                    Device.BlendState = SubtractAlpha;
                    mask.DrawFadeMask(PackedColor.White * subAlpha);
                    if (inverted)
                    {
                        Device.BlendState = InvertAlpha;
                        foreach (var slice in mask.GetMaskSlices())
                            DrawBuffer(level.Buffers.Light, slice.Position, slice.Source, PackedColor.White);
                    }
                }

                Batch.End();
                lightingTargets.Restore(Device);
            }

            Graphics.FxDither.CurrentTechnique = Graphics.FxDither.Techniques["InvertDither"];
            Graphics.FxDither.Parameters["size"]
                .SetValue(new Vector2(level.Buffers.Light.Width, level.Buffers.Light.Height));
            Begin(level, blend: Graphics.DestinationTransparencySubtract, effect: Graphics.FxDither);
            foreach (LightingMask mask in masks)
            {
                float to = mask.LightingTo >= 0f
                    ? (mask.AddBase ? level.BaseLightingAlpha : 0f) + mask.LightingTo
                    : level.BaseLightingAlpha + level.Session.LightingAlphaAdd;
                float from = mask.LightingFrom >= 0f
                    ? (mask.AddBase ? level.BaseLightingAlpha : 0f) + mask.LightingFrom
                    : level.BaseLightingAlpha + level.Session.LightingAlphaAdd;
                foreach (var slice in mask.GetMaskSlices())
                {
                    if (mask.Fade != MaskFadeType.Custom)
                        DrawBuffer(level.Buffers.Light, slice.Position, slice.Source,
                            PackedColor.White * Clamp(slice.GetValue(from, to)));
                    else
                        DrawBuffer(lightingFadeBuffers[mask.BufferIndex], slice.Position, slice.Source,
                            PackedColor.White);
                    lightingRectangles.Add(new IntRect((int)slice.Position.X, (int)slice.Position.Y, slice.Source.Width,
                        slice.Source.Height));
                }
            }

            Batch.End();
            Device.SetRenderTarget(level.Buffers.Light.Target);
            Begin(level, blend: SourceBlendState.Opaque);
            foreach (var rectangle in lightingRectangles)
                SourcePrimitiveDrawing.Rect((Celeste.Graphics.Rendering.Drawing.ISourceDrawScope)engine, rectangle,
                    PackedColor.White);
            Batch.End();
            lightingTargets.Restore(Device);
            orig(renderer, scene);
        }

        private static float Clamp(float value) => Math.Max(0f, Math.Min(1f, value));

        private void ResizeBuffers(List<VirtualRenderTarget> buffers, int count, string prefix)
        {
            if (buffers.Count > count)
            {
                for (int i = count; i < buffers.Count; i++) buffers[i].Dispose();
                buffers.RemoveRange(count, buffers.Count - count);
            }
            else
                for (int i = buffers.Count; i < count; i++)
                    buffers.Add(CreateBuffer(prefix + i));
        }

        private void DisposeLightingBuffers()
        {
            foreach (var buffer in lightingFadeBuffers) buffer.Dispose();
            lightingFadeBuffers.Clear();
            lightingRectangles.Clear();
        }
    }
}
