using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Randomness;
using Celeste.Game.Presentation.Rendering;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Rendering.Primitives;
using Celeste.Graphics.Rendering.Vertices;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Rendering;
using Celeste.Mods.StrawberryJam.Rendering.Lightning;

namespace Celeste.Mods.StrawberryJam.Entities.Lightning
{
    [CustomEntity("SJ2021/CustomLightningEdge")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.CustomLightningEdge")]
    public class CustomLightningEdge : SceneEntityState
    {
        public enum Directions
        {
            Right,
            Up,
            Left,
            Down
        }

        private readonly ISourceEngineContext engine;
        private readonly ISourceVectorArithmetic vectors;
        private PackedColor[] _electricityColors;
        private float Fade = 0f;
        private Vector2 _start = Vector2.Zero;
        private Vector2 _end;
        private SourceVertexPositionColor[] _edgeVerts = new SourceVertexPositionColor[1024];
        private uint _edgeSeed;
        private int size;
        private float interval;

        public CustomLightningEdge(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            Depth = data.Int("Depth", 10);
            Vector2? node = data.FirstNodeNullable(offset);
            if (node.HasValue) _end = node.Value - Position;
            else
            {
                switch (data.Enum("direction", Directions.Up))
                {
                    default:
                        size = data.Width;
                        _end = Vector2.UnitX * size;
                        break;
                    case Directions.Right:
                    case Directions.Left:
                        size = data.Height;
                        _end = Vector2.UnitY * size;
                        break;
                }
            }

            _electricityColors = new[]
            {
                LightningEdgeColor.Parse(data.Attr("color1", "fcf579")),
                LightningEdgeColor.Parse(data.Attr("color2", "8cf7e2"))
            };
            interval = Math.Max(data.Float("interval", .05f), .016f);
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new CustomLightningEdge(level.EngineContext, data, offset);

        public override void Update()
        {
            base.Update();
            if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(interval, engine))
                _edgeSeed = (uint)((ISourceRandomEnvironment)engine).Random.Next();
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            var camera = (Scene as OriginalLevelSceneRoot).Camera;
            if (camera != null)
            {
                int index = 0;
                LightningEdgeDrawing.Draw(ref index, ref _edgeVerts, _edgeSeed, Position, _start, _end,
                    _electricityColors[0], vectors, 1f + Fade * 1f);
                LightningEdgeDrawing.Draw(ref index, ref _edgeVerts, _edgeSeed + 1, Position, _start, _end,
                    _electricityColors[1], vectors, 1f + Fade * 1f);
                if (index > 0)
                {
                    GameplayRendererState.End(context);
                    SourcePrimitiveGraphics.DrawVertices(
                        ((ISourcePrimitiveGraphicsEnvironment)engine).PrimitiveGraphics, camera.Matrix, _edgeVerts,
                        index);
                    GameplayRendererState.Begin(context);
                }
            }
        }
    }
}
