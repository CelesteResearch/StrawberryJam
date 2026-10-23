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
        private void WindHandler(Vector2 windDirection)
        {
            if (!hold.IsHeld)
            {
                if (windDirection.X != 0)
                    MoveH(windDirection.X * 0.5f);
                if (windDirection.Y != 0)
                    MoveV(windDirection.Y);
            }
        }

        private bool SpringHandler(ISourceSpringEntity spring)
        {
            if (!hold.IsHeld)
            {
                if (spring is Celeste.Mods.StrawberryJam.Entities.Springs.UpsidedownSpring udspring)
                {
                    speed.X = speed.X * udspring.xAxisFriction;
                    speed.Y = 160f * udspring.strength;
                    noGravityTimer = 0.15f;
                    wiggler.Start();
                    return true;
                }

                if (spring.Orientation == SpringOrientation.Floor && speed.Y >= 0f)
                {
                    speed.X = speed.X * 0.5f;
                    speed.Y = -160f;
                    noGravityTimer = 0.15f;
                    wiggler.Start();
                    return true;
                }

                if (spring.Orientation == SpringOrientation.WallLeft && speed.X <= 0f)
                {
                    MoveTowardsY(spring.Center.Y + 5f, 4f, null);
                    speed.X = 160f;
                    speed.Y = 80f;
                    noGravityTimer = 0.1f;
                    wiggler.Start();
                    return true;
                }

                if (spring.Orientation == SpringOrientation.WallRight && speed.X >= 0f)
                {
                    MoveTowardsY(spring.Center.Y + 5f, 4f, null);
                    speed.X = -160f;
                    speed.Y = 80f;
                    noGravityTimer = 0.1f;
                    wiggler.Start();
                    return true;
                }
            }

            return false;
        }

        protected override void OnSquish(PixelCollision data)
        {
            if (!TrySquishWiggle(data, 3, 3))
            {
                RemoveSelf();
            }
        }

        private void ReleaseHandler(Vector2 force)
        {
            if (force.X == 0f)
            {
                PlaySound("event:/new_content/char/madeline/glider_drop", Position);
            }

            AllowPushing = true;
            RemoveTag(((IOriginalGameTagsEnvironment)engine).Tags.Persistent.Value);
            bool dropped = false;
            if (force == Vector2.Zero)
            {
                // speed will be set to Vector2.Zero later
                dropped = true;
            }

            if (((ISourceGameInputEnvironment)engine).Input.MoveY.Value == -1 && force.X != 0)
            {
                force.X = 0;
                force.Y = downThrowMultiplier;
                dropped = true;
            }

            if (dropped)
            {
                lastDroppedTime = ((ISourceScene)Scene).SourceScene.Clock.TimeActive;
                PlaySound("event:/new_content/char/madeline/glider_drop", Position);
            }
            else if (force.Y == 0)
            {
                force.Y = diagThrowYMultiplier;
                force.X = diagThrowXMultiplier * Math.Sign(force.X);
            }

            speed = force * 100;
            wiggler.Start();
        }

        private void PickupHandler()
        {
            if (bubble)
            {
                for (int i = 0; i < 24; i++)
                {
                    level.Particles.Emit(particlePlatform, Position + PlatformAdd(i), PlatformColor(i));
                }
            }

            AllowPushing = false;
            speed = Vector2.Zero;
            AddTag(((IOriginalGameTagsEnvironment)engine).Tags.Persistent.Value);
            highFrictionTimer = 0.5f;
            bubble = false;
            wiggler.Start();
        }

        private void CollideHandlerH(PixelCollision data)
        {
            if (data.Hit is OriginalDashSwitchState dashswitch)
                dashswitch.OnDashCollide(null, Vector2.UnitX * (float)Math.Sign(speed.X));
            string sfx = "event:/new_content/game/10_farewell/glider_wallbounce_" + ((speed.X < 0) ? "left" : "right");
            PlaySound(sfx, Position);
            speed.X *= -1;
            sprite.Scale = new Vector2(0.8f, 1.2f);
        }

        private void CollideHandlerV(PixelCollision data)
        {
            if (Math.Abs(speed.Y) > 8)
            {
                sprite.Scale = new Vector2(1.2f, 0.8f);
                PlaySound("event:/new_content/game/10_farewell/glider_land", Position);
            }

            if (speed.Y > 0)
            {
                speed.Y *= -0.5f;
                return;
            }

            speed.Y = 0;
        }
    }
}
