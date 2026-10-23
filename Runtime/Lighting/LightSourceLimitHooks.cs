using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Game.Content.Maps;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.LevelLoader;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Lighting;
using Celeste.Graphics.Content.VirtualAssets;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace Celeste.Mods.StrawberryJam.Runtime.Lighting
{
    public sealed class LightSourceLimitHooks : IDisposable
    {
        private static readonly Func<LightingRendererPresentation, ISourceEngineContext> RendererEngine =
            CreateRendererEngine();

        private static readonly Func<LightingRendererPresentation, int> RendererLightCount =
            CreateRendererLightCount();

        private readonly ISourceEngineContext engine;
        private static readonly ConditionalWeakTable<ISourceEngineContext, LightSourceLimitState> States = new();
        private readonly LightSourceLimitState state;
        private readonly List<IDisposable> hooks = new();

        public LightSourceLimitHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            state = States.GetValue(engine, _ => new LightSourceLimitState());
            try
            {
                hooks.Add(new Hook(
                    typeof(OriginalLevelLoaderScene).GetMethod("LoadingThread",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    (Action<Action<OriginalLevelLoaderScene>, OriginalLevelLoaderScene>)LoadingThread));
                hooks.Add(new ILHook(
                    typeof(OriginalGameplayBuffers).GetMethod(nameof(OriginalGameplayBuffers.Create),
                        new[] { typeof(VirtualContent) }), PatchBuffers));
                hooks.Add(new ILHook(
                    typeof(LightingRendererPresentation).GetConstructor(new[] { typeof(ISourceEngineContext) }),
                    PatchConstructor));
                hooks.Add(new ILHook(
                    typeof(LightingRendererPresentation).GetMethod(nameof(LightingRendererPresentation.BeforeRender)),
                    PatchBeforeRender));
                foreach (string name in new[] { "ClearDirtyLights", "DrawLightGradients", "DrawLightOccluders" })
                    hooks.Add(new ILHook(
                        typeof(LightingRendererPresentation).GetMethod(name,
                            BindingFlags.Instance | BindingFlags.NonPublic), PatchLightLimit));
                hooks.Add(new ILHook(
                    typeof(LightingRendererPresentation).GetMethod("DrawLight",
                        BindingFlags.Instance | BindingFlags.NonPublic), PatchDrawLight));
                hooks.Add(new Hook(
                    typeof(LightingRendererPresentation).GetMethod("GetCenter",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    (Func<Func<LightingRendererPresentation, int, Vector3>, LightingRendererPresentation, int, Vector3>)
                    GetCenter));
                hooks.Add(new Hook(
                    typeof(LightingRendererPresentation).GetMethod("GetMask",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    (Func<Func<LightingRendererPresentation, int, float, float, PackedColor>,
                        LightingRendererPresentation, int, float, float, PackedColor>)GetMask));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private static Func<LightingRendererPresentation, ISourceEngineContext> CreateRendererEngine()
        {
            var getter = new DynamicMethod("LightSourceLimitRendererEngine", typeof(ISourceEngineContext),
                new[] { typeof(LightingRendererPresentation) }, typeof(LightSourceLimitHooks), true);
            var il = getter.GetILGenerator();
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            il.Emit(System.Reflection.Emit.OpCodes.Ldfld,
                typeof(LightingRendererPresentation).GetField("engine",
                    BindingFlags.Instance | BindingFlags.NonPublic));
            il.Emit(System.Reflection.Emit.OpCodes.Ret);
            return (Func<LightingRendererPresentation, ISourceEngineContext>)getter.CreateDelegate(
                typeof(Func<LightingRendererPresentation, ISourceEngineContext>));
        }

        private static Func<LightingRendererPresentation, int> CreateRendererLightCount()
        {
            var getter = new DynamicMethod("LightSourceLimitRendererLightCount", typeof(int),
                new[] { typeof(LightingRendererPresentation) }, typeof(LightSourceLimitHooks), true);
            var il = getter.GetILGenerator();
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            il.Emit(System.Reflection.Emit.OpCodes.Ldfld,
                typeof(LightingRendererPresentation).GetField("lights",
                    BindingFlags.Instance | BindingFlags.NonPublic));
            il.Emit(System.Reflection.Emit.OpCodes.Ldlen);
            il.Emit(System.Reflection.Emit.OpCodes.Conv_I4);
            il.Emit(System.Reflection.Emit.OpCodes.Ret);
            return (Func<LightingRendererPresentation, int>)getter.CreateDelegate(
                typeof(Func<LightingRendererPresentation, int>));
        }

        private void LoadingThread(Action<OriginalLevelLoaderScene> orig, OriginalLevelLoaderScene self)
        {
            if (ReferenceEquals(self.EngineContext, engine))
            {
                state.VirtualContent = ((ISourceVirtualContentEnvironment)engine).VirtualContent;
                state.ControllerInMap = false;
                foreach (var room in ((OriginalChapterMapData)self.Session.MapData).Levels)
                {
                    foreach (var entity in room.Entities)
                    {
                        if (entity.Name != "SJ2021/LightSourceLimitController") continue;
                        state.ControllerInMap = true;
                        break;
                    }

                    if (state.ControllerInMap) break;
                }
            }

            orig(self);
        }

        private bool Applies(LightingRendererPresentation renderer) =>
            ReferenceEquals(RendererEngine(renderer), engine) && RendererLightCount(renderer) == 256;

        private int ConstructorSize(int value, ISourceEngineContext owner) =>
            state.ControllerInMap && ReferenceEquals(owner, engine) ? value * 4 : value;

        private int BufferSize(int value, VirtualContent owner) =>
            ReferenceEquals(owner, state.VirtualContent) &&
            engine.Frame.Scenes.CurrentScene is OriginalLevelSceneRoot level && Applies(level.Lighting)
                ? value * 2
                : value;

        private int LightCount(int value, LightingRendererPresentation renderer) =>
            Applies(renderer) ? value * 4 : value;

        private float MatrixScale(float value, LightingRendererPresentation renderer) =>
            Applies(renderer) ? value / 2f : value;

        private float TextureSize(float value, LightingRendererPresentation renderer) =>
            Applies(renderer) ? value * 2f : value;

        private void PatchBuffers(ILContext il)
        {
            var cursor = new ILCursor(il);
            int count = 0;
            while (cursor.TryGotoNext(MoveType.After, instruction => instruction.MatchLdcI4(1024)))
            {
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate<Func<int, VirtualContent, int>>(BufferSize);
                count++;
            }

            if (count != 2) throw new InvalidOperationException("GameplayBuffers light target size anchors: " + count);
        }

        private void PatchConstructor(ILContext il)
        {
            var cursor = new ILCursor(il);
            int count = 0;
            while (cursor.TryGotoNext(MoveType.After,
                       instruction => instruction.MatchLdcI4(11520) || instruction.MatchLdcI4(384) ||
                                      instruction.MatchLdcI4(64)))
            {
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate<Func<int, ISourceEngineContext, int>>(ConstructorSize);
                count++;
            }

            if (count != 4) throw new InvalidOperationException("LightingRenderer array size anchors: " + count);
        }

        private void PatchBeforeRender(ILContext il)
        {
            PatchLightLimit(il);
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instruction => instruction.MatchLdcR4(0.0009765625f)))
                throw new InvalidOperationException("LightingRenderer light matrix scale anchor");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<float, LightingRendererPresentation, float>>(MatrixScale);
        }

        private void PatchLightLimit(ILContext il)
        {
            var cursor = new ILCursor(il);
            int count = 0;
            while (cursor.TryGotoNext(MoveType.After, instruction => instruction.MatchLdcI4(64)))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<int, LightingRendererPresentation, int>>(LightCount);
                count++;
            }

            if (count == 0)
                throw new InvalidOperationException("LightingRenderer light count anchors: " + il.Method.Name);
        }

        private void PatchDrawLight(ILContext il)
        {
            var cursor = new ILCursor(il);
            int count = 0;
            while (cursor.TryGotoNext(MoveType.After, instruction => instruction.MatchLdcR4(1024f)))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<float, LightingRendererPresentation, float>>(TextureSize);
                count++;
            }

            if (count != 4)
                throw new InvalidOperationException("LightingRenderer light texture size anchors: " + count);
        }

        private Vector3 GetCenter(Func<LightingRendererPresentation, int, Vector3> orig,
            LightingRendererPresentation self, int index)
        {
            if (!Applies(self)) return orig(self, index);
            int cell = index % 64;
            return new Vector3(128f * (cell % 8 + 0.5f) * 2f, 128f * (cell / 8 + 0.5f) * 2f, 0f);
        }

        private PackedColor GetMask(Func<LightingRendererPresentation, int, float, float, PackedColor> orig,
            LightingRendererPresentation self, int index, float maskOn, float maskOff)
        {
            if (!Applies(self)) return orig(self, index, maskOn, maskOff);
            int channel = index / 64;
            return new PackedColor(channel == 0 ? maskOn : maskOff, channel == 1 ? maskOn : maskOff,
                channel == 2 ? maskOn : maskOff, channel == 3 ? maskOn : maskOff);
        }

        public void Dispose()
        {
            for (int index = hooks.Count - 1; index >= 0; index--) hooks[index].Dispose();
            hooks.Clear();
        }
    }
}
