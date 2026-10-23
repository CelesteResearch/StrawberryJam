using System;
using Celeste.Core.Rendering;
using Celeste.Core.Rendering.States;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Rendering;
using Celeste.Mods.StrawberryJam.Entities.Decals;
using Celeste.Scene.Tracking;
using Celeste.Scene.Rendering;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Rendering.Decals
{
    public sealed class MaskedDecalRenderProcess : IDisposable
    {
        private static readonly SourceBlendState DestinationAlphaBlend = new SourceBlendState
        {
            ColorSourceBlend = SourceBlendFactor.DestinationAlpha,
            ColorDestinationBlend = SourceBlendFactor.InverseSourceAlpha,
            AlphaSourceBlend = SourceBlendFactor.Zero,
            AlphaDestinationBlend = SourceBlendFactor.One,
            AlphaBlendFunction = SourceBlendFunction.Add
        };

        private readonly ISourceEngineContext engine;
        private readonly string registryKey;
        private readonly ILHook renderHook;

        public MaskedDecalRenderProcess(ISourceEngineContext engine)
        {
            this.engine = engine;
            registryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(MaskedDecal)).RegistryKey;
            renderHook = new ILHook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.Render)),
                ModifyRender);
        }

        private void ModifyRender(ILContext il)
        {
            var cursor = new ILCursor(il);
            bool found = false;
            while (cursor.TryGotoNext(MoveType.After,
                       i => i.MatchCallOrCallvirt<OriginalLevelSceneRoot>("get_GameplayRenderer"),
                       i => i.MatchLdarg(0),
                       i => i.MatchCallOrCallvirt<SceneRenderer>(nameof(SceneRenderer.Render))))
            {
                found = true;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<OriginalLevelSceneRoot>>(RenderDecals);
            }

            if (!found)
                throw new InvalidOperationException("Missing native GameplayRenderer.Render masked-decal stage.");
        }

        private void RenderDecals(OriginalLevelSceneRoot level)
        {
            if (!ReferenceEquals(level.EngineContext, engine)) return;
            var entities = level.Scheduler.Entities.GetTrackedEntities(registryKey);
            if (entities.Count == 0) return;
            var context = ((ISourceRenderContextScene)level).SourceRenderContext;
            var batch = new SourceRenderBatchState(SourceSpriteSortMode.Deferred, DestinationAlphaBlend,
                SourceSamplerState.PointWrap, SourceDepthStencilState.None, SourceRasterizerState.CullNone,
                null, level.Camera.Matrix);
            context.BeginBatch(in batch);
            foreach (MaskedDecal masked in entities)
            {
                var decal = masked.Decal;
                if (decal != null) decal.Render(context);
            }

            context.EndBatch();
        }

        public void Dispose() => renderHook.Dispose();
    }
}
