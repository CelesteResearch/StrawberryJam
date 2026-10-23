using System;
using Celeste.Core.Scheduling;
using Celeste.Core.Rendering;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Simulation.Presentation.Backdrops.Runtime;
using Celeste.Simulation.Presentation.Backdrops.Weather.Rain;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Weather
{
    public sealed class RainDensityProcess : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly IRainDensityEnvironment environment;
        private Hook update;
        private ILHook render;

        public RainDensityProcess(ISourceEngineContext engine, IRainDensityEnvironment environment)
        {
            this.engine = engine;
            this.environment = environment;
            try
            {
                update = new Hook(typeof(RainBackdropPresentation).GetMethod(nameof(RainBackdropPresentation.Update)),
                    (UpdateHook)Update);
                render = new ILHook(typeof(RainBackdropPresentation).GetMethod(nameof(RainBackdropPresentation.Render)),
                    ModifyRender);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void Update(UpdateOriginal original, RainBackdropPresentation rain, ISourceBackdropScene scene)
        {
            original(rain, scene);
            if (!ReferenceEquals(scene.EngineContext, engine)) return;
            var data = environment.RainDensityData;
            if (data.Density != data.EndDensity)
            {
                float rate = Math.Abs(data.EndDensity - data.StartDensity) / data.Duration;
                data.Density = ScalarMath.Approach(data.Density, data.EndDensity, rate * engine.Frame.DeltaTime,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            }
        }

        private void ModifyRender(ILContext context)
        {
            var cursor = new ILCursor(context);
            int points = 0;
            while (cursor.TryGotoNext(MoveType.After, instruction => instruction.MatchConvI4()))
            {
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.EmitDelegate<Func<int, ISourceBackdropScene, int>>(RenderCount);
                points++;
            }

            if (points != 1) throw new InvalidOperationException("Expected RainFG particle loop length conversion.");
        }

        private int RenderCount(int length, ISourceBackdropScene scene) =>
            ReferenceEquals(scene.EngineContext, engine) ? (int)(length * environment.RainDensityData.Density) : length;

        public void Dispose()
        {
            render?.Dispose();
            render = null;
            update?.Dispose();
            update = null;
        }

        private delegate void UpdateOriginal(RainBackdropPresentation rain, ISourceBackdropScene scene);

        private delegate void UpdateHook(UpdateOriginal original, RainBackdropPresentation rain,
            ISourceBackdropScene scene);
    }
}
