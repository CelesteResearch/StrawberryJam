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
using Celeste.Core.Mathematics.Extrema;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Drawing;
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
    /// Entity that emits a flickering laser beam.
    /// </summary>
    /// <remarks>
    /// Opacity of the beam edges is calculated as <see cref="Alpha"/> times a dynamic multiplier, where
    /// the multiplier represents an optional flickering based on a <see cref="SineWave"/>.
    /// The centre third of the beam is twice that opacity.<br/>
    ///
    /// Configurable values from Ahorn:
    /// <list type="bullet">
    /// <item><term>alpha</term><description>
    /// The base alpha value for the beam.
    /// Defaults to 0.4 (40%).
    /// </description></item>
    /// <item><term>collideWithSolids</term><description>
    /// Whether or not the beam will be blocked by <see cref="Solid"/>s.
    /// Defaults to true.
    /// </description></item>
    /// <item><term>color</term><description>
    /// The base <see cref="Microsoft.Xna.Framework.Color"/> used to render the beam.
    /// Defaults to <see cref="Microsoft.Xna.Framework.Color.Red"/>.
    /// </description></item>
    /// <item><term>colorChannel</term><description>
    /// The color hex code used to match lasers with LinkedZipMovers.
    /// </description></item>
    /// <item><term>disableLasers</term><description>
    /// Whether or not colliding with this beam will disable all beams of the same color.
    /// Defaults to false.
    /// </description></item>
    /// <item><term>flicker</term><description>
    /// Whether or not the beam should flicker.
    /// Defaults to true, flickering 4 times per second.
    /// </description></item>
    /// <item><term>killPlayer</term><description>
    /// Whether or not colliding with the beam will kill the player.
    /// Defaults to true.
    /// </description></item>
    /// <item><term>thickness</term><description>
    /// The thickness of the beam (and corresponding <see cref="Hitbox"/> in pixels).
    /// Defaults to 6 pixels.
    /// </description></item>
    /// <item><term>triggerZipMovers</term><description>
    /// Whether or not colliding with this beam will trigger AdventureHelper LinkedZipMovers of the same color.
    /// Defaults to false.
    /// </description></item>
    /// </list>
    /// </remarks>
    [CustomEntity("SJ2021/LaserEmitterUp = LoadUp",
        "SJ2021/LaserEmitterDown = LoadDown",
        "SJ2021/LaserEmitterLeft = LoadLeft",
        "SJ2021/LaserEmitterRight = LoadRight")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.LaserEmitter")]
    public class LaserEmitter : OrientableEntity
    {
        #region Static Loader Methods

        public static SceneEntityState LoadUp(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, EntityData data) =>
            new LaserEmitter(level.EngineContext, data, offset, Orientations.Up);

        public static SceneEntityState LoadDown(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, EntityData data) =>
            new LaserEmitter(level.EngineContext, data, offset, Orientations.Down);

        public static SceneEntityState LoadLeft(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, EntityData data) =>
            new LaserEmitter(level.EngineContext, data, offset, Orientations.Left);

        public static SceneEntityState LoadRight(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, EntityData data) =>
            new LaserEmitter(level.EngineContext, data, offset, Orientations.Right);

        #endregion

        #region Properties

        /// <summary>
        /// The base alpha value for the beam.
        /// </summary>
        /// <remarks>
        /// Defaults to 0.4 (40%).
        /// </remarks>
        public float Alpha { get; }

        /// <summary>
        /// Whether or not the beam will be blocked by <see cref="Solid"/>s.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool CollideWithSolids { get; }

        /// <summary>
        /// The base <see cref="Microsoft.Xna.Framework.Color"/> used to render the beam.
        /// </summary>
        /// <remarks>
        /// If not set, will attempt to use the value of <see cref="ColorChannel"/>,
        /// otherwise defaults to <see cref="Microsoft.Xna.Framework.Color.Red"/>.
        /// </remarks>
        public PackedColor Color { get; protected set; }

        /// <summary>
        /// The color hex code used to match lasers with LinkedZipMovers.
        /// </summary>
        /// <remarks>
        /// If not set, will use the hex representation of the current <see cref="Color"/>.
        /// </remarks>
        public string ColorChannel { get; }

        /// <summary>
        /// Whether or not colliding with this beam will disable all beams of the same color.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool DisableLasers { get; }

        /// <summary>
        /// Whether or not the beam should flicker.
        /// </summary>
        /// <remarks>
        /// Defaults to true, flickering 4 times per second.
        /// </remarks>
        public bool Flicker { get; }

        /// <summary>
        /// Whether or not colliding with the beam will kill the player.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool KillPlayer { get; }

        /// <summary>
        /// The rendering style to use.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="EmitterStyle.Rounded"/>.
        /// </remarks>
        public EmitterStyle Style { get; }

        /// <summary>
        /// The thickness of the beam (and corresponding <see cref="Hitbox"/> in pixels).
        /// </summary>
        /// <remarks>
        /// Defaults to 6 pixels.
        /// </remarks>
        public float Thickness { get; }

        /// <summary>
        /// Whether or not colliding with this beam will trigger AdventureHelper LinkedZipMovers of the same color.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool TriggerZipMovers { get; }

        public int Leniency { get; }
        public bool BeamAboveEmitter { get; }
        public float EmitterColliderWidth { get; }
        public float EmitterColliderHeight { get; }
        public bool EmitSparks { get; }

        #endregion

        #region Private Fields

        private const float flickerFrequency = 4f;
        private const float beamFlickerRange = 0.25f;
        private const float emitterFlickerRange = 0.15f;

        private float sineValue;

        private readonly Sprite emitterSprite;
        private readonly Sprite tintSprite;
        private readonly LaserColliderComponent laserCollider;

        private readonly ISourceEngineContext engine;
        private readonly StrawberryJamModule module;

        #endregion

        private void setLaserSyncFlag(string channel, bool value) =>
            (Scene as OriginalLevelSceneRoot)?.Session.SetFlag($"ZipMoverSyncLaser:{channel.ToLower()}", value);

        public LaserEmitter(ISourceEngineContext engine, EntityData data, Vector2 offset, Orientations orientation)
            : base(engine, data, offset, orientation)
        {
            this.engine = engine;
            module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<StrawberryJamModule>().Single();
            string colorString = data.Attr("color", null);
            string colorChannelString = data.Attr("colorChannel", null);
            colorString ??= colorChannelString ?? "ff0000";
            colorChannelString ??= colorString;

            Alpha = ScalarMath.Clamp(data.Float("alpha", 0.4f), 0f, 1f,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            CollideWithSolids = data.Bool("collideWithSolids", true);
            Color = PackedColor.FromHex(colorString.ToLower());
            ColorChannel = colorChannelString.ToLower();
            DisableLasers = data.Bool("disableLasers");
            Flicker = data.Bool("flicker", true);
            KillPlayer = data.Bool("killPlayer", true);
            Style = data.Enum("style", EmitterStyle.Rounded);
            Thickness = ((ISourceExtremaEnvironment)engine).ExtremaArithmetic.Max(data.Float("thickness", 6f), 0f);
            TriggerZipMovers = data.Bool("triggerZipMovers");

            // these properties have separate defaults based on laser style, to provide backward compatibility
            Leniency = data.Int("leniency", Style == EmitterStyle.Large ? 1 : 0);
            BeamAboveEmitter = data.Bool("beamAboveEmitter", Style == EmitterStyle.Large);
            EmitterColliderWidth = data.Float("emitterColliderWidth", Style == EmitterStyle.Large ? 14 : 0);
            EmitterColliderHeight = data.Float("emitterColliderHeight", Style == EmitterStyle.Large ? 6 : 0);
            EmitSparks = data.Bool("emitSparks", Style == EmitterStyle.Large);

            Add(new PlayerColliderComponentState(onPlayerCollide));
            Add(
                new LaserColliderComponent
                {
                    CollideWithSolids = CollideWithSolids,
                    Thickness = Thickness - Leniency * 2,
                    Offset = Orientation.Normal() * EmitterColliderHeight,
                });
            Add(new SineWaveState(engine, flickerFrequency) { OnUpdate = v => sineValue = v });
            Add(new LedgeBlockerState(_ => KillPlayer)
            );

            laserCollider = Get<LaserColliderComponent>();

            if (Style == EmitterStyle.Simple)
            {
                emitterSprite = module.SpriteBank.Create("laserEmitter");
                emitterSprite.Play("simple");
                emitterSprite.Rotation = Orientation.Angle();
                Add(emitterSprite);
            }
            else if (Style == EmitterStyle.Large)
            {
                emitterSprite = module.SpriteBank.Create("laserEmitter");
                emitterSprite.Play("large_base");
                emitterSprite.Rotation = Orientation.Angle();
                Add(emitterSprite);

                tintSprite = module.SpriteBank.Create("laserEmitter");
                tintSprite.Play("large_tint");
                tintSprite.Color = Color;
                tintSprite.Rotation = Orientation.Angle();
                Add(tintSprite);
            }
            else if (Style == EmitterStyle.Rounded)
            {
                emitterSprite = module.SpriteBank.Create("laserEmitter");
                emitterSprite.Play("rounded_base");
                emitterSprite.Rotation = Orientation.Angle();
                Add(emitterSprite);

                tintSprite = module.SpriteBank.Create("laserEmitter");
                tintSprite.Play("rounded_tint");
                tintSprite.Color = Color;
                tintSprite.Rotation = Orientation.Angle();
                Add(tintSprite);
            }

            if (EmitterColliderWidth > 0 && EmitterColliderHeight > 0)
            {
                var hitbox = new HitboxColliderGeometry(
                    Orientation.Vertical() ? EmitterColliderWidth : EmitterColliderHeight,
                    Orientation.Vertical() ? EmitterColliderHeight : EmitterColliderWidth);
                if (Orientation == Orientations.Up)
                {
                    hitbox.BottomCenter = Vector2.Zero;
                }
                else if (Orientation == Orientations.Down)
                {
                    hitbox.TopCenter = Vector2.Zero;
                }
                else if (Orientation == Orientations.Left)
                {
                    hitbox.CenterRight = Vector2.Zero;
                }
                else
                {
                    hitbox.CenterLeft = Vector2.Zero;
                }

                Collider = new ColliderListGeometry(laserCollider.Collider, hitbox);
            }
            else
            {
                Collider = laserCollider.Collider;
            }

            Get<SourceStaticMoverState>().OnMove = v =>
            {
                Position += v;
                laserCollider.UpdateBeam();
            };
        }

        public override void Render(ISourceRenderContext context)
        {
            if (BeamAboveEmitter)
            {
                base.Render(context);
            }

            // only render beam if we're collidable
            if (Collidable)
            {
                float alphaMultiplier = 1f - (sineValue + 1f) * 0.5f * beamFlickerRange;
                var color = Color * Alpha * (Flicker ? alphaMultiplier : 1f);
                var collider = laserCollider.Collider;
                var bounds = collider.Bounds;

                if (Leniency != 0)
                {
                    if (Orientation.Horizontal())
                    {
                        bounds.Height += Leniency * 2;
                        bounds.Y -= Leniency;
                    }
                    else
                    {
                        bounds.Width += Leniency * 2;
                        bounds.X -= Leniency;
                    }
                }

                SourcePrimitiveDrawing.Rect((ISourceDrawScope)context, bounds, color);

                Vector2 source = laserCollider.Offset;
                Vector2 target = Orientation switch
                {
                    Orientations.Up => Collider.TopCenter,
                    Orientations.Down => Collider.BottomCenter,
                    Orientations.Left => Collider.CenterLeft,
                    Orientations.Right => Collider.CenterRight,
                    _ => Vector2.Zero,
                };

                float lineThickness = Orientation == Orientations.Left || Orientation == Orientations.Right
                    ? bounds.Height / 3f
                    : bounds.Width / 3f;

                SourcePrimitiveDrawing.Line((ISourceDrawScope)context, new Vector2(X + source.X, Y + source.Y),
                    new Vector2(X + target.X, Y + target.Y), color, lineThickness,
                    ((ISourceVectorEnvironment)engine).VectorArithmetic);
            }

            // update tint layer based on multiplier and collision
            if (tintSprite != null)
            {
                PackedColor color;
                if (!Collidable)
                    color = PackedColor.Gray;
                else
                {
                    float alphaMultiplier = 1f - (sineValue + 1f) * 0.5f * emitterFlickerRange;
                    color = Color * (Flicker ? alphaMultiplier : 1f);
                }

                color.A = 255;
                tintSprite.Color = color;
            }

            if (!BeamAboveEmitter)
            {
                base.Render(context);
            }
        }

        public override void Update()
        {
            base.Update();

            if (EmitSparks && Collidable && ((ISourceScene)Scene).SourceScene.Clock.OnInterval(0.1f, engine) &&
                !laserCollider.CollidedWithScreenBounds)
            {
                var laserHitbox = laserCollider.Collider;
                float angle = Orientation.Angle() + (float)Math.PI / 2f;
                var startX = Orientation switch
                {
                    Orientations.Right => laserHitbox.Right,
                    Orientations.Left => laserHitbox.Left + 1,
                    _ => 0,
                };
                var startY = Orientation switch
                {
                    Orientations.Up => laserHitbox.Top + 1,
                    Orientations.Down => laserHitbox.Bottom,
                    _ => 0,
                };
                var startPos = new Vector2(startX + X, startY + Y);
                var perp = Vector2.Abs(new Vector2(-Orientation.Normal().Y, Orientation.Normal().X));
                (Scene as OriginalLevelSceneRoot).ParticlesBG.Emit(module.LaserSparkParticle, startPos + perp * 3,
                    angle);
                (Scene as OriginalLevelSceneRoot).ParticlesBG.Emit(module.LaserSparkParticle, startPos - perp * 2,
                    angle);
            }
        }

        public override void Removed(object scene)
        {
            setLaserSyncFlag(ColorChannel, false);
            base.Removed(scene);
        }

        private void onPlayerCollide(RoomPlayerSession player)
        {
            var level = player.Scene as OriginalLevelSceneRoot;

            if (DisableLasers)
            {
                foreach (var emitter in ((ISourceScene)level).SourceScene.Entities.Entities.OfType<LaserEmitter>())
                {
                    if (emitter.ColorChannel == ColorChannel)
                        emitter.Collidable = false;
                }
            }

            if (TriggerZipMovers)
            {
                setLaserSyncFlag(ColorChannel, true);
            }

            if (KillPlayer)
            {
                Vector2 direction;
                if (Orientation == Orientations.Left || Orientation == Orientations.Right)
                    direction = player.Center.Y <= Position.Y ? -Vector2.UnitY : Vector2.UnitY;
                else
                    direction = player.Center.X <= Position.X ? -Vector2.UnitX : Vector2.UnitX;

                player.Die(direction, false, true);
            }
        }

        public enum EmitterStyle
        {
            Simple,
            Rounded,
            Large,
        }
    }
}
