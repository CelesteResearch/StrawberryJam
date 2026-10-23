using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Rendering;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Display;
using Celeste.Graphics.Rendering;
using Celeste.Mods.StrawberryJam.Triggers.Gravity;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Enemies.Seeker;

namespace Celeste.Mods.StrawberryJam.Entities.Barriers
{
    [CustomEntity("SJ2021/ZeroGBarrier")]
    [SceneTracked]
    public sealed class ZeroGBarrier : SeekerBarrierState
    {
        private static readonly Vector2[] Directions = { new(-1, 0), Vector2.UnitY, Vector2.UnitX, new(0, -1) };
        public EntityData Data;
        public Vector2 Offset;
        private readonly List<Vector2> zeroGParticles;
        private readonly Vector2 region;
        public ZeroGBarrierDirection Direction;

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new ZeroGBarrier(level, data, offset);

        public ZeroGBarrier(OriginalLevelSceneRoot level, EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height,
                level.SolidWorldFactory.Create(data.Position + offset, false))
        {
            Data = data;
            Offset = offset;
            Direction = data.Enum("direction", ZeroGBarrierDirection.Right);
            ClearParticlePositions();
            zeroGParticles = new List<Vector2>();
            region = new Vector2(Width - 1f, Height - 1f);
            Celeste.Simulation.Presentation.Barriers.BarrierParticleMotion.Fill(zeroGParticles, Width, Height,
                ((ISourceRandomEnvironment)BarrierEngineContext).Random);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            ((ISourceScene)scene).SourceScene.QueueSourceEntity(new ZeroGTrigger(BarrierEngineContext, Data, Offset,
                Direction));
        }

        public override void Update()
        {
            base.Update();
            int count = zeroGParticles.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2 value = zeroGParticles[i] +
                                Directions[(int)Direction] * ParticleSpeed(i % 3) * BarrierEngineContext.DeltaTime;
                zeroGParticles[i] = new Vector2(ScalarMath.Mod(value.X, region.X), ScalarMath.Mod(value.Y, region.Y));
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            PackedColor color = PackedColor.White * .5f;
            foreach (Vector2 particle in zeroGParticles)
            {
                var image = new AtlasTextureDraw(
                    ((Celeste.Graphics.Rendering.Drawing.ISourceDrawScope)context).Draw.Pixel,
                    Position + particle, Vector2.Zero, color, Vector2.One);
                ((ISourceRenderCommandSink<AtlasTextureDraw>)context).Draw(in image, this);
            }

            if (Flashing)
                SourcePrimitiveDrawing.Rect(context, this, Collider.AbsoluteLeft, Collider.AbsoluteTop, Collider.Width,
                    Collider.Height, PackedColor.White * Flash * .5f);
        }
    }
}
