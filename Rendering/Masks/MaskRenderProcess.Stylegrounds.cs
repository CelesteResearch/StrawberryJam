using System;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Rendering.States;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Effects.Displacement;
using Celeste.Scene.Rendering;
using Celeste.Simulation.Presentation.Backdrops.Runtime;
using Celeste.Simulation.Presentation.Backdrops.HeatWave;
using Celeste.Simulation.Presentation.Backdrops.Rendering;
using Celeste.Graphics.Rendering;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Rendering.Masks
{
    public sealed partial class MaskRenderProcess
    {
        private static readonly Func<HeatWaveBackdropPresentation, float> ReadHeat = CreateHeatReader();

        private static Func<HeatWaveBackdropPresentation, float> CreateHeatReader()
        {
            var method = new DynamicMethod("ReadMaskHeat", typeof(float),
                new[] { typeof(HeatWaveBackdropPresentation) },
                typeof(MaskRenderProcess), true);
            var generator = method.GetILGenerator();
            generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            generator.Emit(System.Reflection.Emit.OpCodes.Ldfld, typeof(HeatWaveBackdropPresentation)
                .GetField("heat", BindingFlags.Instance | BindingFlags.NonPublic));
            generator.Emit(System.Reflection.Emit.OpCodes.Ret);
            return (Func<HeatWaveBackdropPresentation, float>)method.CreateDelegate(
                typeof(Func<HeatWaveBackdropPresentation, float>));
        }

        private void PatchStylegroundRenderStages(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instruction => instruction.MatchLdarg(0),
                    instruction => instruction.MatchLdfld<OriginalLevelSceneRoot>("Background"),
                    instruction => instruction.MatchLdarg(0), IsBackdropRender))
                throw new InvalidOperationException("Background mask render stage no longer matches.");
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<OriginalLevelSceneRoot>>(level => RenderStylegrounds(level, false, false));
            if (!cursor.TryGotoNext(MoveType.Before, instruction => instruction.MatchLdarg(0),
                    instruction => instruction.MatchLdfld<OriginalLevelSceneRoot>("Foreground"),
                    instruction => instruction.MatchLdarg(0), IsBackdropRender))
                throw new InvalidOperationException("Foreground mask render stage no longer matches.");
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<OriginalLevelSceneRoot>>(level => RenderStylegrounds(level, true, true));
            cursor.Index += 4;
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<OriginalLevelSceneRoot>>(level => RenderStylegrounds(level, true, false));
        }

        private static bool IsBackdropRender(Instruction instruction) =>
            instruction.Operand is MethodReference method && method.Name == "Render"
                                                          && (method.DeclaringType.FullName ==
                                                              typeof(BackdropRendererPresentation).FullName ||
                                                              method.DeclaringType.FullName ==
                                                              typeof(SceneRenderer).FullName);

        private void RenderStylegrounds(OriginalLevelSceneRoot level, bool foreground, bool behind)
        {
            if (Owns(level) && renderers.TryGetValue(level, out var renderer))
                renderer.RenderWith(level, foreground, behind);
        }

        private void LoadHeatWaveHooks()
        {
            hooks.Add(new Hook(
                typeof(HeatWaveBackdropPresentation).GetMethod(nameof(HeatWaveBackdropPresentation.Update)),
                (Action<Action<HeatWaveBackdropPresentation, ISourceBackdropScene>, HeatWaveBackdropPresentation,
                    ISourceBackdropScene>)UpdateHeatWave));
            hooks.Add(new Hook(
                typeof(DisplacementRendererPresentation).GetMethod(nameof(DisplacementRendererPresentation
                    .HasDisplacement)),
                (Func<Func<DisplacementRendererPresentation, object, bool>, DisplacementRendererPresentation, object,
                    bool>)HasMaskedDisplacement));
            hooks.Add(new Hook(
                typeof(DisplacementRendererPresentation).GetMethod(
                    nameof(DisplacementRendererPresentation.BeforeRender)),
                (Action<Action<DisplacementRendererPresentation, object>, DisplacementRendererPresentation, object>)
                RenderMaskedDisplacement));
        }

        private void UpdateHeatWave(Action<HeatWaveBackdropPresentation, ISourceBackdropScene> orig,
            HeatWaveBackdropPresentation wave, ISourceBackdropScene scene)
        {
            bool masked = false;
            foreach (var tag in wave.Tags)
                if (tag.StartsWith(StylegroundMaskRenderer.TagPrefix))
                {
                    masked = true;
                    break;
                }

            if (!masked || scene is not OriginalLevelSceneRoot level || !Owns(level))
            {
                orig(wave, scene);
                return;
            }

            var last = level.ColorGrade.LastGrade;
            var ease = level.ColorGrade.Ease;
            var speed = level.ColorGrade.EaseSpeed;
            var grade = level.Session.ColorGrade;
            orig(wave, scene);
            level.ColorGrade.LastGrade = last;
            level.ColorGrade.Ease = ease;
            level.ColorGrade.EaseSpeed = speed;
            level.Session.ColorGrade = grade;
        }

        private bool HasMaskedDisplacement(Func<DisplacementRendererPresentation, object, bool> orig,
            DisplacementRendererPresentation displacement, object scene)
            => orig(displacement, scene) || scene is OriginalLevelSceneRoot level && Owns(level)
                && renderers.TryGetValue(level, out var renderer) && renderer.ForegroundHeatWaves.Count > 0;

        private void RenderMaskedDisplacement(Action<DisplacementRendererPresentation, object> orig,
            DisplacementRendererPresentation displacement, object scene)
        {
            orig(displacement, scene);
            if (!displacement.Enabled || scene is not OriginalLevelSceneRoot level || !Owns(level) ||
                !renderers.TryGetValue(level, out var renderer) || renderer.ForegroundHeatWaves.Count == 0) return;
            Begin(level, sampler: SourceSamplerState.LinearClamp);
            foreach (var wave in renderer.ForegroundHeatWaves)
            {
                if (wave.Tags.Contains("nomaskhide")) wave.RenderDisplacement(level.SourceRenderContext, level);
                if (ReadHeat(wave) <= 0f) continue;
                foreach (StylegroundMask mask in Tracked<StylegroundMask>(level))
                {
                    bool matches = false;
                    foreach (var tag in mask.RenderTags)
                        if (wave.Tags.Contains(StylegroundMaskRenderer.TagPrefix + tag))
                        {
                            matches = true;
                            break;
                        }

                    if (!matches) continue;
                    foreach (var slice in mask.GetMaskSlices())
                        SourcePrimitiveDrawing.Rect(level.SourceRenderContext, level, slice.Position.X,
                            slice.Position.Y,
                            slice.Source.Width, slice.Source.Height, new PackedColor(.5f, .5f, .1f, 1f));
                }
            }

            Batch.End();
        }
    }
}
