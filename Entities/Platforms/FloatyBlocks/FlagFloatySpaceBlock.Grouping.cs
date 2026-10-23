// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Components.Animation;
using Celeste.Core.Mathematics.Easing;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Platforms.FloatySpaceBlocks;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.StrawberryJam.Entities.Platforms.FloatyBlocks
{
    public partial class FlagFloatySpaceBlock
    {
        public override void Awake(object scene)
        {
            base.Awake(scene);
            awake = true;
            activated = ((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag);
            if (!HasGroup)
            {
                MasterOfGroup = true;
                moves = new();
                group = new();
                jumpthrus = new();
                groupBoundsMin = new((int)X, (int)Y);
                groupBoundsMax = new((int)Right, (int)Bottom);
                AddToGroupAndFindChildren(this);
                tiles = OriginalFloatyBlockGroupTiles.Generate(group, groupBoundsMin, groupBoundsMax, ref tileType,
                    engine);
                tiles.Position = new Vector2(groupBoundsMin.X - X, groupBoundsMin.Y - Y);
                Add(tiles);
                floatTween = SourceTweenState.Create(engine.TweenPool, SourceTweenState.TweenMode.Persist,
                    ((ISourceEasingEnvironment)engine).EasingArithmetic.QuadOut, moveTime);
                floatTween.OnUpdate = OnTweenUpdate;
                floatTween.OnComplete = OnTweenComplete;
                Add(floatTween);
            }

            TryToInitPosition();
        }

        private void TryToInitPosition()
        {
            if (MasterOfGroup)
            {
                foreach (var item in group)
                    if (!item.awake)
                        return;
                if (activated)
                {
                    Vector2 diff = SourceVector2Math.Subtract(node, startPosition);
                    foreach (var jp in jumpthrus)
                    {
                        jp.Position = SourceVector2Math.Add(jp.Position, diff);
                        moves[jp] = jp.Position;
                    }

                    foreach (var block in group)
                    {
                        block.Position = SourceVector2Math.Add(block.Position, diff);
                        moves[block] = block.Position;
                    }

                    MoveStaticMovers(diff);
                    MoveToTarget();
                }
            }
            else master.TryToInitPosition();
        }

        private void AddToGroupAndFindChildren(FlagFloatySpaceBlock from)
        {
            if (from.X < (float)groupBoundsMin.X)
            {
                groupBoundsMin.X = (int)from.X;
            }

            if (from.Y < (float)groupBoundsMin.Y)
            {
                groupBoundsMin.Y = (int)from.Y;
            }

            if (from.Right > (float)groupBoundsMax.X)
            {
                groupBoundsMax.X = (int)from.Right;
            }

            if (from.Bottom > (float)groupBoundsMax.Y)
            {
                groupBoundsMax.Y = (int)from.Bottom;
            }

            from.HasGroup = true;
            from.OnDashCollide = OnDash;
            group.Add(from);
            moves.Add(from, from.Position);
            if (from != this)
            {
                from.master = this;
            }

            foreach (SourceJumpThruEntityState item in SourceSceneCollision.AllRect<SourceJumpThruEntityState>(
                         ((ISourceScene)Scene).SourceScene.Entities, "Celeste.JumpThru",
                         new IntRect((int)from.X - 1, (int)from.Y, (int)from.Width + 2, (int)from.Height)))
            {
                if (!jumpthrus.Contains(item))
                {
                    AddJumpThru(item);
                }
            }

            foreach (SourceJumpThruEntityState item2 in SourceSceneCollision.AllRect<SourceJumpThruEntityState>(
                         ((ISourceScene)Scene).SourceScene.Entities, "Celeste.JumpThru",
                         new IntRect((int)from.X, (int)from.Y - 1, (int)from.Width, (int)from.Height + 2)))
            {
                if (!jumpthrus.Contains(item2))
                {
                    AddJumpThru(item2);
                }
            }

            foreach (FlagFloatySpaceBlock entity in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(FlagFloatySpaceBlock)))
            {
                if (!entity.HasGroup && entity.tileType == tileType &&
                    (SourceSceneCollision.CheckRect(
                         new IntRect((int)from.X - 1, (int)from.Y, (int)from.Width + 2, (int)from.Height), entity) ||
                     SourceSceneCollision.CheckRect(
                         new IntRect((int)from.X, (int)from.Y - 1, (int)from.Width, (int)from.Height + 2), entity)))
                {
                    AddToGroupAndFindChildren(entity);
                }
            }
        }

        private void AddJumpThru(SourceJumpThruEntityState jp)
        {
            jp.OnDashCollide = OnDash;
            jumpthrus.Add(jp);
            moves.Add(jp, jp.Position);
            foreach (FlagFloatySpaceBlock entity in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(FlagFloatySpaceBlock)))
            {
                if (!entity.HasGroup && entity.tileType == tileType &&
                    SourceSceneCollision.CheckRect(
                        new IntRect((int)jp.X - 1, (int)jp.Y, (int)jp.Width + 2, (int)jp.Height),
                        entity))
                {
                    AddToGroupAndFindChildren(entity);
                }
            }
        }
    }
}
