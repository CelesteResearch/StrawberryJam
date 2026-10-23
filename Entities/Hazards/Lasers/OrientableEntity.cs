using System;
using Celeste.Core.Reflection.Runtime;
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
    /// <summary>
    /// Base class for an entity that that has an orientation.
    /// </summary>
    /// <remarks>
    /// Automatically applies a <see cref="SourceStaticMoverState"/> to the backside of the entity.
    /// </remarks>
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.OrientableEntity")]
    public abstract class OrientableEntity : SceneEntityState
    {
        #region Properties

        /// <summary>
        /// The orientation of the entity. The opposite direction will be used for static mover checks.
        /// </summary>
        public Orientations Orientation { get; }

        #endregion

        protected OrientableEntity(ISourceEngineContext engine, EntityData data, Vector2 offset,
            Orientations orientation)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            Orientation = orientation;

            // same depth as springs
            Depth = -8501;

            Add(new SourceStaticMoverState
            {
                OnAttach = p => Depth = p.Depth + 1,
                SolidChecker = s => SourceEntityGeometryCollision.Point(s, Position - Orientation.Direction()),
                JumpThruChecker = jt => SourceEntityGeometryCollision.Point(jt, Position - Orientation.Direction()),
                OnEnable = () => Collidable = true,
                OnDisable = () => Collidable = false,
            });
        }

        /// <summary>
        /// The available orientations of an <see cref="OrientableEntity"/>, where the direction indicates the "front" of the entity.
        /// The opposite direction is used for checking <see cref="SourceStaticMoverState"/>s.
        /// </summary>
        public enum Orientations
        {
            /// <summary>
            /// Indicates that the entity points toward the top of the screen.
            /// </summary>
            Up,

            /// <summary>
            /// Indicates that the entity points toward the bottom of the screen.
            /// </summary>
            Down,

            /// <summary>
            /// Indicates that the entity points toward the left of the screen.
            /// </summary>
            Left,

            /// <summary>
            /// Indicates that the entity points toward the right of the screen.
            /// </summary>
            Right,
        }
    }
}
