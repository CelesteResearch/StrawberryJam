using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Rendering.States;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Presentation.PostProcessing.ColorGrading;
using Celeste.Game.Execution;
using Celeste.Graphics.Content.VirtualAssets;
using Celeste.Graphics.Rendering.Devices;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mods.StrawberryJam.Rendering.Masks
{
    public sealed partial class MaskRenderProcess
    {
        private readonly List<VirtualRenderTarget> colorGradeFadeBuffers = new();
        private readonly MaskRenderTargets colorGradeTargets = new();

        private static readonly SourceBlendState BetterAlphaBlend = new()
        {
            ColorSourceBlend = SourceBlendFactor.One, AlphaSourceBlend = SourceBlendFactor.One,
            ColorDestinationBlend = SourceBlendFactor.InverseSourceAlpha,
            AlphaDestinationBlend = SourceBlendFactor.InverseSourceAlpha
        };

        private void PatchColorGradeRenderStages(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.Before, instruction => instruction.MatchLdnull(),
                    instruction => instruction.MatchCallOrCallvirt<SourceGraphicsDevice>("SetRenderTarget")))
                throw new InvalidOperationException("Color grade mask preparation stage no longer matches.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<OriginalLevelSceneRoot>>(PrepareColorGradeMasks);
            if (!cursor.TryGotoNext(MoveType.Before, instruction => instruction.MatchLdarg(0),
                    instruction => instruction.MatchCallOrCallvirt<OriginalLevelSceneRoot>("get_Pathfinder")))
                throw new InvalidOperationException("Color grade mask screen stage no longer matches.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<OriginalLevelSceneRoot>>(RenderColorGradeMasks);
        }

        private void PrepareColorGradeMasks(OriginalLevelSceneRoot level)
        {
            if (!Owns(level)) return;
            var masks = Tracked<ColorGradeMask>(level);
            int count = 0;
            foreach (ColorGradeMask mask in masks)
                if (mask.Fade == MaskFadeType.Custom)
                    count++;
            ResizeBuffers(colorGradeFadeBuffers, count, "colorgrademaskfade");
            if (count == 0) return;
            colorGradeTargets.Capture(Device);
            Begin(level, SourceSpriteSortMode.Immediate, depth: SourceDepthStencilState.Default);
            int index = 0;
            foreach (ColorGradeMask mask in masks)
            {
                if (mask.Fade != MaskFadeType.Custom) continue;
                mask.BufferIndex = index++;
                Device.SetRenderTarget(colorGradeFadeBuffers[mask.BufferIndex].Target);
                Device.Clear(PackedColor.Transparent);
                mask.DrawFadeMask();
                Device.BlendState = Mask.DestinationAlphaBlend;
                foreach (var slice in mask.GetMaskSlices())
                    DrawBuffer(level.Buffers.Level, slice.Position, slice.Source, PackedColor.White);
                Device.BlendState = SourceBlendState.AlphaBlend;
            }

            Batch.End();
            colorGradeTargets.Restore(Device);
        }

        private void RenderColorGradeMasks(OriginalLevelSceneRoot level)
        {
            if (!Owns(level)) return;
            var masks = Tracked<ColorGradeMask>(level);
            if (masks.Count == 0) return;
            var atlas = Graphics.ColorGradesAtlas;
            var currentFrom = atlas.GetOrDefault(level.ColorGrade.LastGrade, atlas["none"]);
            var currentTo = atlas.GetOrDefault(level.Session.ColorGrade, atlas["none"]);
            var grades = ((IOriginalColorGradeEnvironment)engine).ColorGradeProcess;
            float currentValue = grades.Percent;
            var screenSize = new Vector2(320f, 180f);
            var scaledScreen = screenSize / level.ZoomTarget;
            var focusOffset = level.ZoomTarget != 1f
                ? (level.ZoomFocusPoint - scaledScreen / 2f) / (screenSize - scaledScreen) * screenSize
                : Vector2.Zero;
            var paddingOffset = new Vector2(level.ScreenPadding, level.ScreenPadding * .5625f);
            float scale = level.Zoom * ((320f - level.ScreenPadding * 2f) / 320f);
            var zoomMatrix = Matrix4x4.CreateTranslation(new Vector3(-focusOffset, 0f)) * Matrix4x4.CreateScale(scale)
                * Matrix4x4.CreateTranslation(new Vector3(focusOffset + paddingOffset, 0f));
            if (((IOriginalGameplayProcess)engine).SaveContext.CurrentSave.Assists.MirrorMode)
                zoomMatrix *= Matrix4x4.CreateScale(-1f, 1f, 1f) * Matrix4x4.CreateTranslation(320f, 0f, 0f);
            var matrix = Matrix4x4.CreateScale(6f) * EngineRuntime.ScreenMatrix;
            Begin(level, SourceSpriteSortMode.Immediate, effect: grades.Effect,
                matrix: level.Camera.Matrix * zoomMatrix * matrix, depth: SourceDepthStencilState.Default);
            foreach (ColorGradeMask mask in masks)
            {
                var from = mask.GetColorGrade(true);
                var to = mask.GetColorGrade(false);
                if (mask.Fade != MaskFadeType.Custom)
                {
                    foreach (var slice in mask.GetMaskSlices())
                    {
                        float value = Clamp(mask.FadeFrom + slice.Value * (mask.FadeTo - mask.FadeFrom));
                        if (value < 1f) grades.Set(from, to, value);
                        else grades.Set(to);
                        DrawBuffer(level.Buffers.Level, slice.Position, slice.Source, PackedColor.White);
                    }
                }
                else
                {
                    grades.Set(from, to, mask.FadeFrom);
                    foreach (var slice in mask.GetMaskSlices())
                        DrawBuffer(level.Buffers.Level, slice.Position, slice.Source, PackedColor.White);
                    grades.Set(from, to, mask.FadeTo);
                    Device.BlendState = BetterAlphaBlend;
                    DrawBuffer(colorGradeFadeBuffers[mask.BufferIndex], level.Camera.Position, PackedColor.White);
                    Device.BlendState = SourceBlendState.AlphaBlend;
                }
            }

            Batch.End();
            grades.Set(currentFrom, currentTo, currentValue);
        }

        private void DisposeColorGradeBuffers()
        {
            foreach (var buffer in colorGradeFadeBuffers) buffer.Dispose();
            colorGradeFadeBuffers.Clear();
        }
    }
}
