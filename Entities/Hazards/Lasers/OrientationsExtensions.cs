using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Core.Rendering;
using Celeste.Core.Mathematics;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Rendering;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Components.Oscillation;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Gameplay.Collision.Blockers;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;

namespace Celeste.Mods.StrawberryJam.Entities.Hazards.Lasers
{
    public static class OrientationsExtensions
    {
        public static Vector2 Direction(this OrientableEntity.Orientations orientation) => orientation switch
        {
            OrientableEntity.Orientations.Up => -Vector2.UnitY,
            OrientableEntity.Orientations.Down => Vector2.UnitY,
            OrientableEntity.Orientations.Left => -Vector2.UnitX,
            OrientableEntity.Orientations.Right => Vector2.UnitX,
            _ => Vector2.Zero
        };

        public static float Angle(this OrientableEntity.Orientations orientation) => orientation switch
        {
            OrientableEntity.Orientations.Up => 0f,
            OrientableEntity.Orientations.Down => (float)Math.PI,
            OrientableEntity.Orientations.Left => (float)-Math.PI / 2f,
            OrientableEntity.Orientations.Right => (float)Math.PI / 2f,
            _ => 0f
        };

        public static Vector2 Normal(this OrientableEntity.Orientations orientation) => orientation switch
        {
            OrientableEntity.Orientations.Up => -Vector2.UnitY,
            OrientableEntity.Orientations.Down => Vector2.UnitY,
            OrientableEntity.Orientations.Left => -Vector2.UnitX,
            OrientableEntity.Orientations.Right => Vector2.UnitX,
            _ => Vector2.Zero
        };

        public static float LengthOfHitbox(this OrientableEntity.Orientations orientation,
            HitboxColliderGeometry hitbox) =>
            orientation switch
            {
                OrientableEntity.Orientations.Up => hitbox.Height,
                OrientableEntity.Orientations.Down => hitbox.Height,
                OrientableEntity.Orientations.Left => hitbox.Width,
                OrientableEntity.Orientations.Right => hitbox.Width,
                _ => 0f
            };

        public static float ThicknessOfHitbox(this OrientableEntity.Orientations orientation,
            HitboxColliderGeometry hitbox) =>
            orientation switch
            {
                OrientableEntity.Orientations.Up => hitbox.Width,
                OrientableEntity.Orientations.Down => hitbox.Width,
                OrientableEntity.Orientations.Left => hitbox.Height,
                OrientableEntity.Orientations.Right => hitbox.Height,
                _ => 0f
            };

        public static Vector2 OriginOfHitbox(this OrientableEntity.Orientations orientation,
            HitboxColliderGeometry hitbox) =>
            orientation switch
            {
                OrientableEntity.Orientations.Up => hitbox.BottomCenter,
                OrientableEntity.Orientations.Down => hitbox.TopCenter,
                OrientableEntity.Orientations.Left => hitbox.CenterRight,
                OrientableEntity.Orientations.Right => hitbox.CenterLeft,
                _ => Vector2.Zero
            };

        public static bool Horizontal(this OrientableEntity.Orientations orientation) =>
            orientation is OrientableEntity.Orientations.Left or OrientableEntity.Orientations.Right;

        public static bool Vertical(this OrientableEntity.Orientations orientation) =>
            orientation is OrientableEntity.Orientations.Up or OrientableEntity.Orientations.Down;
    }
}
