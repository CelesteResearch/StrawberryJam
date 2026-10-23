using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Rendering.States;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Effects.Bloom;
using Celeste.Graphics.Content.VirtualAssets;
using Celeste.Graphics.Effects.Blur;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Batches;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Graphics.Rendering.Textures;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Rendering.Masks
{
    public sealed partial class MaskRenderProcess
    {
        private VirtualRenderTarget bloomMaskBuffer;
        private readonly MaskRenderTargets bloomMaskTargets = new();
        private readonly ConditionalWeakTable<BloomRendererPresentation, BloomMaskPassState> bloomMaskStates = new();

        private void LoadBloomHooks()
        {
            var apply = typeof(BloomRendererPresentation).GetMethod(nameof(BloomRendererPresentation.Apply));
            hooks.Add(new ILHook(apply, PatchBloomRendering));
            hooks.Add(new Hook(apply,
                (Action<Action<BloomRendererPresentation, VirtualRenderTarget, object>, BloomRendererPresentation,
                    VirtualRenderTarget, object>)ApplyBloomMasks));
        }

        private void ApplyBloomMasks(Action<BloomRendererPresentation, VirtualRenderTarget, object> orig,
            BloomRendererPresentation renderer, VirtualRenderTarget target, object scene)
        {
            if (scene is not OriginalLevelSceneRoot level || !Owns(level))
            {
                orig(renderer, target, scene);
                return;
            }

            var state = bloomMaskStates.GetOrCreateValue(renderer);
            float savedStrength = renderer.Strength;
            state.Strength = savedStrength;
            state.Rectangles.Clear();
            if (Tracked<BloomMask>(level).Count > 0) renderer.Strength = 1f;
            try
            {
                orig(renderer, target, scene);
            }
            finally
            {
                renderer.Strength = savedStrength;
                state.Rectangles.Clear();
            }
        }

        private void PatchBloomRendering(ILContext il)
        {
            var cursor = new ILCursor(il);
            int blurredTextureLocal = -1;
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction => CallsBloomMethod(instruction, typeof(GaussianBlurPresentation),
                        nameof(GaussianBlurPresentation.Blur)),
                    instruction => instruction.MatchStloc(out blurredTextureLocal)))
                throw new InvalidOperationException(
                    "Strawberry Jam BloomMask requires the actual bloom GaussianBlur result.");

            if (!cursor.TryGotoNext(instruction => instruction.MatchLdcR4(-10f),
                    instruction => instruction.MatchLdcR4(-10f)) ||
                !cursor.TryGotoPrev(MoveType.Before,
                    instruction =>
                        CallsBloomMethod(instruction, typeof(SourceSpriteBatch), nameof(SourceSpriteBatch.Begin))))
                throw new InvalidOperationException(
                    "Strawberry Jam BloomMask requires the actual bloom base-mask batch.");

            // Begin's receiver and arguments may already be on the evaluation stack.
            // The delegate consumes only the four values added here and preserves those original arguments.
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.Emit(OpCodes.Ldloc, il.Body.Variables[blurredTextureLocal]);
            cursor
                .EmitDelegate<Action<BloomRendererPresentation, VirtualRenderTarget, object, SourceTexture2DResource>>(
                    RenderBloomMasks);

            if (!cursor.TryGotoNext(MoveType.After,
                    instruction => CallsBloomMethod(instruction, typeof(IOriginalBloomBlendStates),
                        "get_BlurredScreenToMask")) ||
                !cursor.TryGotoNext(MoveType.After,
                    instruction =>
                        CallsBloomMethod(instruction, typeof(SourceSpriteBatch), nameof(SourceSpriteBatch.End))))
                throw new InvalidOperationException(
                    "Strawberry Jam BloomMask requires the actual blurred bloom-mask completion stage.");

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.EmitDelegate<Action<BloomRendererPresentation, object>>(ClearOriginalBloomMaskSlices);
        }

        private static bool CallsBloomMethod(Instruction instruction, Type type, string name)
            => (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
               instruction.Operand is MethodReference method && method.DeclaringType.FullName == type.FullName &&
               method.Name == name;

        private void RenderBloomMasks(BloomRendererPresentation renderer, VirtualRenderTarget target,
            object scene, SourceTexture2DResource blurredTexture)
        {
            if (scene is not OriginalLevelSceneRoot level || !Owns(level)) return;
            var masks = Tracked<BloomMask>(level);
            if (masks.Count == 0) return;
            var state = bloomMaskStates.GetOrCreateValue(renderer);
            var blends = ((IOriginalBloomProcess)engine).BloomBlendStates;
            if (bloomMaskBuffer == null || bloomMaskBuffer.IsDisposed)
            {
                bloomMaskBuffer?.Dispose();
                bloomMaskBuffer = CreateBuffer("bloomMask-buffer");
            }

            bloomMaskTargets.Capture(Device);
            try
            {
                foreach (BloomMask mask in masks)
                {
                    float baseFrom = mask.BaseFrom >= 0f ? mask.BaseFrom : renderer.Base;
                    float baseTo = mask.BaseTo >= 0f ? mask.BaseTo : renderer.Base;
                    float strengthFrom = mask.StrengthFrom >= 0f ? mask.StrengthFrom : state.Strength;
                    float strengthTo = mask.StrengthTo >= 0f ? mask.StrengthTo : state.Strength;
                    var slices = mask.GetMaskSlices();

                    Device.SetRenderTarget(bloomMaskBuffer.Target);
                    Device.Clear(PackedColor.Transparent);
                    Begin(level, sampler: SourceSamplerState.LinearClamp);
                    DrawBuffer(Graphics.Buffers.TempA,
                        Vector2.Transform(Vector2.Zero, Matrix4x4.Negate(level.Camera.Matrix)), PackedColor.White);
                    foreach (var slice in slices)
                        SourcePrimitiveDrawing.Rect((ISourceDrawScope)engine, slice.Position.X, slice.Position.Y,
                            slice.Source.Width, slice.Source.Height,
                            PackedColor.White * slice.GetValue(baseFrom, baseTo));
                    Batch.End();

                    Begin(level, blend: blends.BlurredScreenToMask, sampler: SourceSamplerState.LinearClamp,
                        matrix: Matrix4x4.Identity);
                    Batch.Draw(blurredTexture, Vector2.Zero, PackedColor.White.PackedValue);
                    Batch.End();

                    Device.SetRenderTarget(target.Target);
                    Begin(level, blend: blends.AdditiveMaskToScreen, sampler: SourceSamplerState.LinearClamp);
                    foreach (var slice in slices)
                    {
                        float strength = slice.GetValue(strengthFrom, strengthTo);
                        for (int iteration = 0; iteration < strength; iteration++)
                        {
                            float scale = iteration < strength - 1f ? 1f : strength - iteration;
                            DrawBuffer(bloomMaskBuffer, slice.Position, slice.Source, PackedColor.White * scale);
                        }

                        state.Rectangles.Add(new IntRect((int)slice.Position.X, (int)slice.Position.Y,
                            slice.Source.Width, slice.Source.Height));
                    }

                    Batch.End();
                }
            }
            finally
            {
                bloomMaskTargets.Restore(Device);
            }
        }

        private void ClearOriginalBloomMaskSlices(BloomRendererPresentation renderer, object scene)
        {
            if (scene is not OriginalLevelSceneRoot level || !Owns(level)) return;
            var state = bloomMaskStates.GetOrCreateValue(renderer);
            if (state.Rectangles.Count > 0)
            {
                Begin(level, blend: SourceBlendState.Opaque, sampler: SourceSamplerState.LinearClamp);
                foreach (var rectangle in state.Rectangles)
                    SourcePrimitiveDrawing.Rect((ISourceDrawScope)engine, rectangle.X, rectangle.Y,
                        rectangle.Width, rectangle.Height, PackedColor.Transparent);
                Batch.End();
            }

            renderer.Strength = state.Strength;
        }

        private void DisposeBloomBuffers()
        {
            bloomMaskBuffer?.Dispose();
            bloomMaskBuffer = null;
        }
    }
}
