using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components.Animation;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Rendering.States;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Gameplay.Platforms.Cassette;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Audio.Surfaces;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Gameplay.Platforms.Cassette;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Graphics.Animation;
using Celeste.Mods.StrawberryJam.Components.Cassette;
using Celeste.Mods.StrawberryJam.Entities.Audio;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Tiles;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.StrawberryJam.Entities.Platforms.Cassette
{
    [CustomEntity("SJ2021/CassetteBadelineBlock")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.CassetteBadelineBlock")]
    public class CassetteBadelineBlock : SourceSolidEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly ISourceSolidWorldFactory worlds;
        public bool HideFinalTransition { get; }
        public Vector2[] Nodes { get; }
        public int[] IgnoredNodes { get; }
        public bool OffBeat { get; }
        public char TileType { get; }
        public bool EmitImpactParticles { get; }

        public string CenterSpriteName { get; }
        public SourceSpriteEffects CenterSpriteEffects { get; private set; }
        public int CenterSpriteRotation { get; }

        private SpriteImage centerImage;
        private int offsetNodeIndex;
        private int sourceNodeIndex;
        private int targetNodeIndex;
        private readonly int initialNodeIndex;
        private bool initialized;

        private SingletonAudioController sfx;

        public CassetteBadelineBlock(CassetteBadelineBlock parent, int initialNodeIndex)
            : base(parent.Nodes[initialNodeIndex], parent.Width, parent.Height, false,
                parent.worlds.Create(parent.Nodes[initialNodeIndex], false))
        {
            engine = parent.engine;
            worlds = parent.worlds;
            Nodes = parent.Nodes;
            TileType = parent.TileType;
            IgnoredNodes = parent.IgnoredNodes;
            HideFinalTransition = parent.HideFinalTransition;
            OffBeat = parent.OffBeat;
            EmitImpactParticles = parent.EmitImpactParticles;
            CenterSpriteName = parent.CenterSpriteName;
            CenterSpriteRotation = parent.CenterSpriteRotation;
            CenterSpriteEffects = parent.CenterSpriteEffects;
            SurfaceSoundIndex = parent.SurfaceSoundIndex;

            sourceNodeIndex = targetNodeIndex = this.initialNodeIndex = initialNodeIndex;

            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.FrozenUpdate.Value;

            AddComponents();
        }

        public CassetteBadelineBlock(ISourceEngineContext engine, EntityData data, Vector2 offset,
            ISourceSolidWorldFactory worlds)
            : base(data.Position + offset, data.Width, data.Height, false, worlds.Create(data.Position + offset, false))
        {
            this.engine = engine;
            this.worlds = worlds;
            Nodes = data.NodesWithPosition(offset);
            TileType = data.Char("tiletype", 'g');
            OffBeat = data.Bool("offBeat");
            HideFinalTransition = data.Bool("hideFinalTransition");
            EmitImpactParticles = data.Bool("emitImpactParticles", true);
            SurfaceSoundIndex =
                ((IOriginalTileSurfaceEnvironment)engine).SurfaceIndices.TileToIndex.TryGetValue(TileType,
                    out int value)
                    ? value
                    : 0;

            CenterSpriteName = data.Attr("centerSpriteName");
            CenterSpriteRotation = data.Int("centerSpriteRotation");
            CenterSpriteEffects = SourceSpriteEffects.None;
            if (data.Bool("centerSpriteFlipX")) CenterSpriteEffects |= SourceSpriteEffects.FlipHorizontally;
            if (data.Bool("centerSpriteFlipY")) CenterSpriteEffects |= SourceSpriteEffects.FlipVertically;

            string ignoredNodesString = data.Attr("ignoredNodes") ?? string.Empty;
            IgnoredNodes = ignoredNodesString
                .Trim()
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out int value) ? value : int.MaxValue)
                .Where(i => Math.Abs(i) < Nodes.Length)
                .ToArray();

            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.FrozenUpdate.Value;

            AddComponents();
        }

        private void AddComponents()
        {
            TileGrid sprite = ((ISourceAutotilingResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics)
                .ForegroundAutotiler.GenerateBox(TileType, (int)Width / 8, (int)Height / 8).TileGrid;
            Add(sprite, new TileInterceptorState(sprite, false));

            if (!string.IsNullOrWhiteSpace(CenterSpriteName))
            {
                centerImage =
                    new SpriteImage(((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[CenterSpriteName]);
                centerImage.CenterOrigin();
                centerImage.Rotation = CenterSpriteRotation * ((float)Math.PI / 180f);
                centerImage.FlipX = (CenterSpriteEffects & SourceSpriteEffects.FlipHorizontally) != 0;
                centerImage.FlipY = (CenterSpriteEffects & SourceSpriteEffects.FlipVertically) != 0;
                centerImage.Position = Vector2Geometry.Round(new Vector2(Width / 2, Height / 2));
                Add(centerImage);
            }

            Add(new LightOccludeState(),
                new CassetteListener(initialNodeIndex)
                {
                    OnTick = (_, isSwap) =>
                    {
                        if (isSwap != OffBeat || ((OriginalLevelSceneRoot)Scene).Transitioning) return;
                        offsetNodeIndex++;
                        targetNodeIndex = (initialNodeIndex + offsetNodeIndex) % Nodes.Length;
                    },
                    OnSilentUpdate = activated =>
                    {
                        if (initialized) return;
                        initialized = true;
                        offsetNodeIndex = 0;
                        if (initialNodeIndex < Nodes.Length)
                            Position = Nodes[initialNodeIndex];
                    },
                },
                new SimulationCoroutine(engine, MoveSequence())
            );
        }

        public override void Added(object scene)
        {
            base.Added(scene);

            sfx = SingletonAudioController.Ensure(scene);

            offsetNodeIndex = -1;
            if (initialNodeIndex == 0)
            {
                for (int i = 1; i < Nodes.Length; i++)
                {
                    if (!IgnoredNodes.Contains(i) && !IgnoredNodes.Contains(i - Nodes.Length))
                        ((ISourceScene)scene).SourceScene.QueueSourceEntity(new CassetteBadelineBlock(this, i));
                }

                if (IgnoredNodes.Contains(0))
                    RemoveSelf();
            }
        }

        private void TeleportTo(Vector2 to)
        {
            MoveStaticMovers(to - Position);
            Position = to;
        }

        private IEnumerator MoveSequence()
        {
            var block = this;

            if (Scene == null) yield break;

            var time = -1f;
            while (Scene != null && time < 0)
            {
                var cbm =
                    ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<OriginalCassetteBlockManagerState>(
                        "Celeste.CassetteBlockManager");
                if (cbm == null)
                {
                    yield return null;
                }
                else
                {
                    var beatLength = (10 / 60f) / cbm.TempoMultiplier;
                    time = beatLength * (cbm is InstalledCassetteBlockManagerState installed
                        ? installed.BeatsPerTick
                        : 4);
                }
            }

            while (Scene != null)
            {
                while (sourceNodeIndex == targetNodeIndex)
                {
                    yield return null;
                }

                var to = block.Nodes[targetNodeIndex];
                var from = block.Nodes[sourceNodeIndex];

                if (targetNodeIndex == 0 && HideFinalTransition)
                {
                    block.TeleportTo(to);
                    Visible = Collidable = false;
                }
                else
                {
                    var tween = SourceTweenState.Create(engine.TweenPool, SourceTweenState.TweenMode.Oneshot,
                        ((ISourceEasingEnvironment)engine).EasingArithmetic.CubeIn, time, true);
                    tween.OnUpdate = t =>
                        MoveTo(Vector2Interpolation.Lerp(from, to, t.Eased,
                            ((ISourceScalarEnvironment)engine).ScalarArithmetic));
                    tween.OnComplete = _ =>
                    {
                        if (SourceSceneCollisionQueries.CheckAt(block,
                                ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities("Celeste.SolidTiles"),
                                block.Position +
                                ((ISourceVectorEnvironment)engine).VectorArithmetic.SafeNormalize(to - from) * 2f))
                        {
                            if (block.EmitImpactParticles)
                            {
                                block.ImpactParticles(to - from);
                            }
                        }
                        else
                        {
                            block.StopParticles(to - from);
                        }

                        sfx?.Play("event:/sj21_mosscairn_sfx/cassette_crusher_snap", block);
                    };

                    block.Add(tween);
                }

                yield return time;
                sourceNodeIndex = targetNodeIndex;
                Visible = Collidable = true;
            }
        }

        protected void StopParticles(Vector2 moved) =>
            Celeste.Game.Presentation.Platforms.Effects.MovingSolidParticleEffects.Stop(this, engine, moved);

        protected void ImpactParticles(Vector2 moved) =>
            Celeste.Game.Presentation.Platforms.Effects.MovingSolidParticleEffects.Impact(this, engine, moved);

        public static CassetteBadelineBlock Load(OriginalLevelSceneRoot level,
            Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room, Vector2 offset,
            Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData data) => new(level.EngineContext, data, offset,
            ((ISourcePlatformCollisionScene)level).SolidWorldFactory);
    }
}
