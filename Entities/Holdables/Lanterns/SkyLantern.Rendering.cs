using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Scheduling;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Rendering;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Controls.Source;
using Celeste.Input.Gameplay;
using Celeste.Game.Gameplay.Mechanisms.DashSwitches;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Content;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Interaction.Springs;
using Celeste.Simulation.Gameplay.Environment.Wind;
using Celeste.Simulation.Gameplay.Enemies.Seeker;
using Celeste.Simulation.Gameplay.Player.Movement;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Mods.StrawberryJam.Runtime.Holdables.Lanterns;
using Celeste.Scene.Entities;

namespace Celeste.Mods.StrawberryJam.Entities.Holdables.Lanterns
{
    public partial class SkyLantern
    {
        private void PlayOpen()
        {
            if (!sprite.CurrentAnimationID.Equals("fall") && !sprite.CurrentAnimationID.Equals("fallLoop"))
            {
                sprite.Play("fall", false, false);
                sprite.Scale = new Vector2(1.5f, 0.6f);
                level.Particles.Emit(particleExpand, 16,
                    Center + ((ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(Vector2.UnitY * -12f,
                        sprite.Rotation), new Vector2(8f, 3f), -1 / 2 * (float)Math.PI + sprite.Rotation);
                if (hold.IsHeld)
                {
                    Rumble(RumbleStrength.Medium, RumbleLength.Short);
                }
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            if (!destroyed)
                sprite.DrawSimpleOutline(context);
            base.Render(context);
            if (bubble)
            {
                for (int i = 0; i < 24; i++)
                {
                    SourcePrimitiveDrawing.Point(context, this, Position + PlatformAdd(i), PlatformColor(i));
                }
            }
        }

        private PackedColor PlatformColor(int i)
        {
            if (i <= 1 || i >= 22)
            {
                return PackedColor.White * 0.4f;
            }

            return PackedColor.White * 0.8f;
        }

        private Vector2 PlatformAdd(int i)
        {
            return new Vector2(-12 + i,
                (-5 + (int)Math.Round(Math.Sin(((ISourceScene)Scene).SourceScene.Clock.TimeActive + i * 0.4f) * 1.8)));
        }
    }
}
