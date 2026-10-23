using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Rendering;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Rendering;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Simulation.Audio.Sources;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Debugging
{
    internal sealed partial class ShowHitboxRuntime : IDisposable
    {
        private static ShowHitboxRuntime[] owners = Array.Empty<ShowHitboxRuntime>();
        private static readonly List<IDisposable> hooks = new();

        internal void Load()
        {
            if (loaded) return;
            if (owners.Length == 0)
            {
                try
                {
                    GameplayRendererState.RenderAdditionalDebug += RenderSelectedHitboxes;
                    hooks.Add(new Hook(typeof(SoundSourceComponentState).GetMethod("DebugRender"),
                        (Action<Action<SoundSourceComponentState, ISourceRenderContext, object>,
                            SoundSourceComponentState, ISourceRenderContext, object>)DebugSound));
                    hooks.Add(new Hook(
                        typeof(SourcePrimitiveDrawing).GetMethod("HollowRect",
                            new[]
                            {
                                typeof(ISourceDrawScope), typeof(float), typeof(float), typeof(float), typeof(float),
                                typeof(PackedColor)
                            }),
                        (Action<Action<ISourceDrawScope, float, float, float, float, PackedColor>, ISourceDrawScope,
                            float, float, float, float, PackedColor>)HollowRect));
                    hooks.Add(new Hook(
                        typeof(SourcePrimitiveDrawing).GetMethod("Circle",
                            new[]
                            {
                                typeof(ISourceDrawScope), typeof(Vector2), typeof(float), typeof(PackedColor),
                                typeof(int), typeof(ISourceVectorArithmetic)
                            }),
                        (Action<Action<ISourceDrawScope, Vector2, float, PackedColor, int, ISourceVectorArithmetic>,
                            ISourceDrawScope, Vector2, float, PackedColor, int, ISourceVectorArithmetic>)Circle));
                }
                catch
                {
                    ReleaseHooks();
                    throw;
                }
            }

            owners = owners.Append(this).ToArray();
            loaded = true;
        }

        public void Dispose()
        {
            if (!loaded) return;
            owners = owners.Where(owner => !ReferenceEquals(owner, this)).ToArray();
            loaded = false;
            if (owners.Length == 0) ReleaseHooks();
        }

        private static void ReleaseHooks()
        {
            GameplayRendererState.RenderAdditionalDebug -= RenderSelectedHitboxes;
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
        }

        private static ShowHitboxRuntime ForDraw(ISourceDrawScope scope)
        {
            var current = owners;
            var draw = scope.Draw;
            for (int i = 0; i < current.Length; i++)
                if (ReferenceEquals(current[i].draw, draw))
                    return current[i];
            return null;
        }

        private static void RenderSelectedHitboxes(GameplayRendererState renderer, object scene)
        {
            var level = (OriginalLevelSceneRoot)scene;
            var current = owners;
            for (int i = 0; i < current.Length; i++)
                if (ReferenceEquals(current[i].engine, level.EngineContext))
                {
                    current[i].DrawEnabledHitboxes(renderer, level);
                    break;
                }
        }

        private static void DebugSound(Action<SoundSourceComponentState, ISourceRenderContext, object> original,
            SoundSourceComponentState self, ISourceRenderContext context, object camera)
        {
            var runtime = ForDraw((ISourceDrawScope)context);
            if (runtime == null || runtime.EnabledTypeNames.Count == 0) original(self, context, camera);
        }

        private static void HollowRect(Action<ISourceDrawScope, float, float, float, float, PackedColor> original,
            ISourceDrawScope scope, float x, float y, float width, float height, PackedColor color)
        {
            var runtime = ForDraw(scope);
            if (runtime == null || runtime.EnabledTypeNames.Count == 0)
            {
                original(scope, x, y, width, height, color);
                return;
            }

            float fx = (float)Math.Floor(x);
            float fy = (float)Math.Floor(y);
            float cw = (float)Math.Ceiling(width + x - fx);
            float cy = (float)Math.Ceiling(height + y - fy);
            original(scope, fx, fy, cw, cy, color);
        }

        private static void Circle(
            Action<ISourceDrawScope, Vector2, float, PackedColor, int, ISourceVectorArithmetic> original,
            ISourceDrawScope scope, Vector2 center, float radius, PackedColor color, int resolution,
            ISourceVectorArithmetic arithmetic)
        {
            var runtime = ForDraw(scope);
            if (runtime == null || runtime.EnabledTypeNames.Count == 0)
            {
                original(scope, center, radius, color, resolution, arithmetic);
                return;
            }

            CircleOctant(scope, center, radius, color, 1, 1, false);
            CircleOctant(scope, center, radius, color, 1, -1, false);
            CircleOctant(scope, center, radius, color, -1, 1, false);
            CircleOctant(scope, center, radius, color, -1, -1, false);
            CircleOctant(scope, center, radius, color, 1, 1, true);
            CircleOctant(scope, center, radius, color, 1, -1, true);
            CircleOctant(scope, center, radius, color, -1, 1, true);
            CircleOctant(scope, center, radius, color, -1, -1, true);
        }
    }
}
