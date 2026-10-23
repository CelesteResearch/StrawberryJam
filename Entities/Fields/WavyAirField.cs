using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;
using Celeste.Simulation.Presentation.PostProcessing.Displacement;

namespace Celeste.Mods.StrawberryJam.Entities.Fields
{
    [CustomEntity("SJ2021/WavyAirField")]
    [global::Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.WavyAirField")]
    public class WavyAirField : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly float fieldWidth, fieldHeight;

        public WavyAirField(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            fieldWidth = data.Width;
            fieldHeight = data.Height;
            OwnedComponents.Add(new DisplacementRenderHookState(RenderDisplacement));
        }

        public void RenderDisplacement()
            => SourcePrimitiveDrawing.Rect(((ISourceRenderEnvironment)engine).CurrentContext, this, X, Y, fieldWidth,
                fieldHeight, new PackedColor(.5f, .5f, .1f, 1f));

        public override void DebugRender(ISourceRenderContext context, object camera)
        {
            base.DebugRender(context, camera);
            SourcePrimitiveDrawing.HollowRect(context, this, X, Y, fieldWidth, fieldHeight, PackedColor.Red);
        }

        public static WavyAirField Load(OriginalLevelSceneRoot level,
            Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room, Vector2 offset,
            Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData data) => new(level.EngineContext, data, offset);
    }
}
