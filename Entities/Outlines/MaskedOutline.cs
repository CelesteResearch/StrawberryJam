using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Animation;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Entities.Interaction.Boosters;
using Celeste.Game.Gameplay.Collectibles.Refills;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Rendering;
using Celeste.Mods.StrawberryJam.Rendering.Outlines;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Components.Oscillation;
using Celeste.Simulation.Gameplay.Collision;

namespace Celeste.Mods.StrawberryJam.Entities.Outlines
{
    [CustomEntity("SJ2021/MaskedOutline")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.MaskedOutline")]
    public class MaskedOutline : SceneEntityState
    {
        private enum OutlineType
        {
            Booster,
            Refill,
            DoubleRefill
        }

        private readonly MaskedOutlineResources resources;
        private bool Controller;
        private EntityData data;
        private MaskedOutline[] children;
        private SceneEntityState parent;
        private SineWaveState RefillSine;
        private OutlineType type;
        private List<Vector2> dot_positions;
        private SourceTweenState tween;
        private PackedColor color;
        private List<object> solids = new();
        private readonly ISourceEngineContext engine;

        private MaskedOutline(ISourceEngineContext engine, EntityData data, Vector2 position, SceneEntityState entity,
            MaskedOutlineResources resources) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
            this.resources = resources;
            Depth = -9001;
            Visible = true;
            dot_positions = new();
            parent = entity;
            OwnedComponents.Add(tween =
                SourceTweenState.Create(engine.TweenPool, SourceTweenState.TweenMode.Looping, null, 3, true));
        }

        public MaskedOutline(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            resources = MaskedOutlineResources.For(engine);
            Controller = true;
            this.data = data;
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new MaskedOutline(level.EngineContext, data, offset);

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (Controller)
            {
                var candidates = new List<SceneEntityState>();
                var entities = ((ISourceScene)scene).SourceScene.Entities;
                foreach (var entity in entities.Entities)
                    if (entity is OriginalBoosterState b)
                        candidates.Add(b);
                foreach (var entity in entities.Entities)
                    if (entity is OriginalRefillState r)
                        candidates.Add(r);
                children = new MaskedOutline[candidates.Count];
                for (int i = 0; i < candidates.Count; i++)
                    ((ISourceScene)Scene).SourceScene.QueueSourceEntity(children[i] =
                        new MaskedOutline(engine, data, Position, candidates[i], resources));
                return;
            }

            if (parent is OriginalBoosterState) type = OutlineType.Booster;
            else if (parent is OriginalRefillState r)
                type = r.TwoDashes ? OutlineType.DoubleRefill : OutlineType.Refill;
            Setup();
        }

        private void Setup()
        {
            Position = parent.Position - Vector2.One;
            switch (type)
            {
                case OutlineType.Booster:
                    dot_positions.AddRange(resources.BoosterDots);
                    color = parent.OwnedComponents.Get<Sprite>().Texture.AtlasPath.Contains("Red")
                        ? PackedColor.Red
                        : PackedColor.White;
                    break;
                case OutlineType.Refill:
                    dot_positions.AddRange(resources.RefillDots);
                    RefillSine = parent.OwnedComponents.Get<SineWaveState>();
                    color = resources.SingleColor;
                    break;
                case OutlineType.DoubleRefill:
                    dot_positions.AddRange(resources.DoubleRefillDots);
                    RefillSine = parent.OwnedComponents.Get<SineWaveState>();
                    color = resources.DoubleColor;
                    break;
            }
        }

        private bool IsInBounds(SceneEntityState solid, Vector2 dot)
        {
            Vector2 coord = dot + Position;
            return solid.Bottom >= coord.Y && solid.Top <= coord.Y && solid.Left <= coord.X && solid.Right >= coord.X;
        }

        private float IndexToScale(int index)
        {
            float target = tween.Percent * dot_positions.Count;
            float result = Math.Min(Math.Abs(index - target), Math.Abs(target + dot_positions.Count - index));
            return Math.Max(0, 1.5f - result) + 1;
        }

        public override void Render(ISourceRenderContext context)
        {
            if (Controller || parent.Scene == null)
            {
                RemoveSelf();
                return;
            }

            base.Render(context);
            var scratch = solids ?? new List<object>();
            solids = null;
            scratch.Clear();
            SourceSceneCollisionQueries.All(parent,
                ((ISourceScene)parent.Scene).SourceScene.Entities.GetTrackedEntities("Celeste.Solid"), scratch);
            for (int j = 0; j < scratch.Count; j++)
            for (int i = 0; i < dot_positions.Count; i++)
                if (IsInBounds((SceneEntityState)scratch[j], dot_positions[i]))
                    SourceAtlasDrawing.Draw(context, this, resources.DotTexture,
                        dot_positions[i] + Position + (type == OutlineType.Booster ? Vector2.Zero : GetRefillOffset()),
                        Vector2.Zero, color, IndexToScale(i));
            solids = scratch;
        }

        private Vector2 GetRefillOffset() => parent.Collidable ? RefillSine.Value * 2 * Vector2.UnitY : Vector2.Zero;
    }
}
