// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Animation;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Audio.Surfaces;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Platforms.FloatySpaceBlocks;
using Celeste.Graphics.Content.Tiles;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.StrawberryJam.Entities.Platforms.FloatyBlocks
{
    [CustomEntity("SJ2021/FlagFloatySpaceBlock")]
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.FlagFloatySpaceBlock")]
    public partial class FlagFloatySpaceBlock : SourceSolidEntityState
    {
        private readonly ISourceEngineContext engine;
        private TileGrid tiles;
        private char tileType;
        private string flag;
        private bool activated;
        private Vector2 node, startPosition, tweenStartPosition;
        private float moveTime;
        private SourceTweenState floatTween;
        private float sineWave, sinkTimer, yLerp, dashEase;
        private Vector2 dashDirection;
        private bool awake;
        private FlagFloatySpaceBlock master;
        private List<FlagFloatySpaceBlock> group;
        private List<SourceJumpThruEntityState> jumpthrus;
        private Dictionary<SourcePlatformEntityState, Vector2> moves;
        private IntPoint groupBoundsMin, groupBoundsMax;
        public bool HasGroup { get; private set; }
        public bool MasterOfGroup { get; private set; }

        public FlagFloatySpaceBlock(ISourceEngineContext engine, EntityData data, Vector2 offset,
            ISourceSolidWorld world) : base(SourceVector2Math.Add(data.Position, offset), data.Width, data.Height, true,
            world)
        {
            this.engine = engine;
            node = SourceVector2Math.Add(data.Nodes[0], offset);
            startPosition = Position;
            moveTime = EntityAttributeReader.Float(data.Values, "moveTime", 1f);
            activated = false;
            tileType = EntityAttributeReader.Char(data.Values, "tiletype", '3');
            flag = EntityAttributeReader.Attr(data.Values, "flag");
            Depth = -9000;
            Add(new LightOccludeState());
            SurfaceSoundIndex = ((IOriginalTileSurfaceEnvironment)engine).SurfaceIndices.TileToIndex[tileType];
            sineWave = !EntityAttributeReader.Bool(data.Values, "disableSpawnOffset")
                ? SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random, (float)Math.PI * 2f)
                : 0f;
        }

        public static FlagFloatySpaceBlock Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, EntityData data) => new(level.EngineContext, data, offset,
            level.SolidWorldFactory.Create(SourceVector2Math.Add(data.Position, offset), true));

        public override void OnStaticMoverTrigger(SourceStaticMoverState mover) =>
            FloatySpringReaction.Apply(mover, ref sinkTimer, ref dashEase, ref dashDirection);
    }
}
