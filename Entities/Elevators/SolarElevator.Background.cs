using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Scheduling;
using Celeste.Core.Rendering;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Audio;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Components.Talking;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Rendering;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Holdables;

namespace Celeste.Mods.StrawberryJam.Entities.Elevators
{
    public partial class SolarElevator
    {
        [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.SolarElevator+Background")]
        private class Background : SceneEntityState
        {
            private readonly SolarElevator elevator;

            public Background(SolarElevator elevator, int depth) : base(
                ((ISourceEntitySourceDataEnvironment)elevator.Engine).EntitySourceDataRegistration)
            {
                Depth = depth;
                this.elevator = elevator;
            }

            public override void Render(ISourceRenderContext context)
            {
                for (int i = 0; i < elevator.Distance + 60; i += elevator.rails.Height)
                    SourceAtlasDrawing.DrawJustified(context, this, elevator.rails,
                        new Vector2(elevator.X, elevator.StartY - i), new Vector2(.5f, 1), PackedColor.White);
                SourceAtlasDrawing.DrawJustified(context, this, elevator.back, elevator.Position + Vector2.UnitY * 10,
                    new Vector2(.5f, 1), PackedColor.White);
            }
        }
    }
}
