// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Mods.StrawberryJam.Rendering.Platforms.ZipMovers;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.StrawberryJam.Entities.Platforms.ZipMovers
{
    [CustomEntity("SJ2021/DashZipMover")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.DashZipMover")]
    public partial class DashZipMover : SourceSolidEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly AtlasEntry[,] edges = new AtlasEntry[3, 3];
        private readonly Sprite streetlight;
        private readonly BloomPointState bloom;
        private DashZipMoverPathRenderer pathRenderer;
        private readonly List<AtlasEntry> innerCogs;
        private readonly AtlasEntry temp = new();
        internal Vector2 Start;
        internal Vector2 Target;
        internal float Percent;
        private bool triggered;
        private Vector2 scale = Vector2.One;
        private readonly SoundSourceComponentState sfx;
        internal ISourceEngineContext EngineContext => engine;

        public DashZipMover(ISourceEngineContext engine, Vector2 position, int width, int height, Vector2 target,
            ISourceSolidWorld world)
            : base(position, width, height, false, world)
        {
            this.engine = engine;
            sfx = new SoundSourceComponentState(engine);
            Depth = -9999;
            Start = Position;
            Target = target;
            Add(new SimulationCoroutine(engine, Sequence()));
            Add(new LightOccludeState());
            string path = "objects/zipmover/moon/light",
                id = "objects/zipmover/moon/block",
                key = "objects/StrawberryJam2021/dashZipMover/innercog";
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            innerCogs = atlas.GetAtlasSubtextures(key);
            Add(streetlight = new Sprite(engine, atlas, path));
            streetlight.Add("frames", "", 1f);
            streetlight.Play("frames");
            streetlight.Active = false;
            streetlight.SetAnimationFrame(1);
            streetlight.Position = new Vector2(Width / 2f - streetlight.Width / 2f, 0f);
            Add(bloom = new BloomPointState(1f, 6f));
            bloom.Position = new Vector2(Width / 2f, 10f);
            for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
                edges[x, y] = atlas[id].GetSubtexture(x * 8, y * 8, 8, 8);
            SurfaceSoundIndex = 7;
            OnDashCollide = OnDashed;
            sfx.Position = ((ISourceVectorEnvironment)engine).VectorArithmetic.Divide(new Vector2(Width, Height), 2f);
            Add(sfx);
        }

        public DashZipMover(ISourceEngineContext engine, EntityData data, Vector2 offset, ISourceSolidWorld world)
            : this(engine, SourceVector2Math.Add(data.Position, offset), data.Width, data.Height,
                SourceVector2Math.Add(data.Nodes[0], offset), world)
        {
        }

        public static DashZipMover Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            EntityData data)
            => new(level.EngineContext, data, offset,
                level.SolidWorldFactory.Create(SourceVector2Math.Add(data.Position, offset), false));

        public DashCollisionResult OnDashed(RoomPlayerSession player, Vector2 direction)
        {
            if (!triggered)
            {
                triggered = true;
                scale = new Vector2(1f + Math.Abs(direction.Y) * .4f - Math.Abs(direction.X) * .4f,
                    1f + Math.Abs(direction.X) * .4f - Math.Abs(direction.Y) * .4f);
                return DashCollisionResult.Rebound;
            }

            return DashCollisionResult.NormalCollision;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            ((OriginalLevelSceneRoot)scene).Scheduler.QueueSourceEntity(pathRenderer =
                new DashZipMoverPathRenderer(this));
        }

        public override void Removed(object scene)
        {
            ((OriginalLevelSceneRoot)scene).Scheduler.RemoveEntity(pathRenderer);
            pathRenderer = null;
            base.Removed(scene);
        }

        public override void Update()
        {
            base.Update();
            scale = ((ISourceVectorEnvironment)engine).VectorArithmetic.Approach(scale, Vector2.One,
                3f * engine.DeltaTime);
            streetlight.Scale = scale;
            Vector2 zeroCenter =
                ((ISourceVectorEnvironment)engine).VectorArithmetic.Divide(new Vector2(Width, Height), 2f);
            streetlight.Position = SourceVector2Math.Add(zeroCenter,
                SourceVector2Math.Multiply(
                    SourceVector2Math.Subtract(new Vector2(zeroCenter.X - streetlight.Width / 2f, 0), zeroCenter),
                    scale));
        }
    }
}
