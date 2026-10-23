using System;
using System.Linq;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using Celeste.Core.Audio;
using Celeste.Core.Maps;
using Celeste.Core.Components.Timing;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Identifiers;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Audio.Surfaces;
using Celeste.Game.Components.Switches;
using Celeste.Game.Gameplay.Mechanisms.DashSwitches;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Content;
using Celeste.Graphics.Rendering;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Mods.StrawberryJam.Runtime.Switches;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Emitters;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;

namespace Celeste.Mods.StrawberryJam.Entities.Switches
{
    [CustomEntity("SJ2021/ResizableDashSwitch")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.ResizableDashSwitch")]
    public class ResizableDashSwitch : OriginalDashSwitchState
    {
        #region Fields/Properties

        public OriginalDashSwitchSide Side;
        public OriginalSwitchComponent Switch;

        private readonly ISourceEngineContext engine;
        private readonly ResizableDashSwitchRuntime runtime;
        internal ISourceEngineContext EngineContext => engine;
        internal bool CanPress(Vector2 direction) => !SourcePressed && direction == SourcePressDirection;

        private Vector2 spriteOffset;
        private int width;
        private List<SpriteImage> switchImages = new();

        private int widthTiles => width / 8;

        private float SwitchRotation => Side switch
        {
            OriginalDashSwitchSide.Down => 0f,
            OriginalDashSwitchSide.Up => (float)Math.PI,
            OriginalDashSwitchSide.Right => -((float)Math.PI / 2f),
            OriginalDashSwitchSide.Left => ((float)Math.PI / 2f),
            _ => throw new InvalidOperationException("Dash switch direction is invalid.")
        };

        #endregion

        #region Constructor stuff

        public ResizableDashSwitch(Vector2 position, OriginalDashSwitchSide side, bool persistent, MapEntityId id,
            int width, bool actLikeTouchSwitch, bool attachToSolid, ResizableDashSwitchRuntime runtime,
            ISourceSolidWorld world)
            : base(position, side, persistent, false, id, "default", runtime.Engine, world)
        {
            this.runtime = runtime;
            engine = runtime.Engine;
            Side = side;
            this.width = width;
            if (side == OriginalDashSwitchSide.Up || side == OriginalDashSwitchSide.Down)
            {
                Collider.Width = width;
            }
            else
            {
                Collider.Height = width;
            }

            if (attachToSolid)
            {
                Add(new SourceStaticMoverState
                {
                    SolidChecker = IsRiding,
                    OnMove = OnMove,
                    OnAttach = OnAttach,
                    OnShake = OnShake,
                });
            }

            if (actLikeTouchSwitch)
            {
                Add(Switch = new OriginalSwitchComponent(groundReset: false));
                Switch.OnStartFinished = () =>
                {
                    // if these are set, in Awake() it'll start out already pressed
                    SourcePersistent = true;
                    ((OriginalLevelSceneRoot)Scene).Session.SetFlag(SourceSwitchFlagName);
                };
            }

            SurfaceSoundIndex = OriginalSurfaceIndex.DreamBlockInactive;

            Remove(SourceSwitchSprite);
            CreateSprite(0);
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
        {
            return new ResizableDashSwitch(data, offset, new MapEntityId(room.Name, data.ID),
                ResizableDashSwitchRuntime.For(level.EngineContext),
                ((ISourcePlatformCollisionScene)level).SolidWorldFactory.Create(data.Position + offset, true));
        }

        public ResizableDashSwitch(EntityData data, Vector2 offset, MapEntityId id,
            ResizableDashSwitchRuntime runtime, ISourceSolidWorld world)
            : this(data.Position + offset,
                SwitchSide(EntityAttributeReader.Enum(data.Values, "orientation", OriginalDashSwitchSide.Up)),
                EntityAttributeReader.Bool(data.Values, "persistent"), id, GetWidth(data),
                EntityAttributeReader.Bool(data.Values, "actLikeTouchSwitch", true),
                EntityAttributeReader.Bool(data.Values, "attachToSolid", true), runtime, world)
        {
        }

        // again copied from FlagDashSwitch :)
        private static OriginalDashSwitchSide SwitchSide(OriginalDashSwitchSide side) => side switch
        {
            OriginalDashSwitchSide.Up => OriginalDashSwitchSide.Down,
            OriginalDashSwitchSide.Down => OriginalDashSwitchSide.Up,
            OriginalDashSwitchSide.Left => OriginalDashSwitchSide.Right,
            OriginalDashSwitchSide.Right => OriginalDashSwitchSide.Left,
            _ => throw new InvalidOperationException("Dash switch direction is invalid.")
        };

        private static int GetWidth(EntityData data)
        {
            OriginalDashSwitchSide side =
                SwitchSide(EntityAttributeReader.Enum(data.Values, "orientation", OriginalDashSwitchSide.Left));
            return side switch
            {
                OriginalDashSwitchSide.Up => data.Width,
                OriginalDashSwitchSide.Down => data.Width,
                OriginalDashSwitchSide.Left => data.Height,
                OriginalDashSwitchSide.Right => data.Height,
                _ => throw new InvalidOperationException("Dash switch direction is invalid.")
            };
        }

        #endregion

        #region Overrides

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (SourcePressed)
                Switch?.Activate();
        }

        public override void Render(ISourceRenderContext context)
        {
            Vector2 oldPos = Position;
            Position += spriteOffset;
            foreach (SpriteImage image in switchImages)
                DrawOnlyOutlineCentered(context, image.Texture, Position + image.Position, image.Rotation);
            base.Render(context);
            Position = oldPos;
        }

        #endregion

        #region Static mover stuff

        private bool IsRiding(SourceSolidEntityState solid)
        {
            return SourceEntityCollision.CheckAt(this, solid, Position + SourcePressDirection);
        }

        private void OnMove(Vector2 amount)
        {
            // if currently solid, move player and stuff along
            if (Collidable)
            {
                MoveH(amount.X);
                MoveV(amount.Y);
            }
            else
            {
                // otherwise, just move without doing that
                Position += amount;
            }

            SourcePressedTarget += amount;
            SourceStartY += amount.Y;
        }

        private void OnAttach(ISourcePlatformEntity platform)
        {
            Depth = ((SourcePlatformEntityState)platform).Depth + 1;
        }

        private new void OnShake(Vector2 amount)
        {
            spriteOffset += amount;
        }

        #endregion

        #region Sprites

        private void CreateSprite(int index)
        {
            foreach (SpriteImage image in switchImages)
                Remove(image);
            switchImages.Clear();

            Vector2 startPos, posIncrement;
            switch (Side)
            {
                case OriginalDashSwitchSide.Down:
                    startPos = new Vector2(0, 0);
                    posIncrement = new Vector2(8, 0);
                    break;
                case OriginalDashSwitchSide.Up:
                    startPos = new Vector2(width - 8, 0);
                    posIncrement = new Vector2(-8, 0);
                    break;
                case OriginalDashSwitchSide.Right:
                    startPos = new Vector2(0, width - 8);
                    posIncrement = new Vector2(0, -8);
                    break;
                case OriginalDashSwitchSide.Left:
                    startPos = new Vector2(0, 0);
                    posIncrement = new Vector2(0, 8);
                    break;
                default:
                    throw new InvalidOperationException("Dash switch direction is invalid.");
            }

            AtlasEntry switchTexture =
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.Entries[
                    $"objects/StrawberryJam2021/bigDashSwitch/bigSwitch{index:D2}"];
            Vector2 currentPos = startPos;
            for (int i = 0; i < widthTiles; i++)
            {
                int subtextureOffset;
                if (i == 0)
                {
                    subtextureOffset = 0;
                }
                else if (i == widthTiles - 1)
                {
                    subtextureOffset = 2;
                }
                else
                {
                    subtextureOffset = 1;
                }

                SpriteImage switchImage = new SpriteImage(switchTexture.GetSubtexture(subtextureOffset * 8, 0, 8, 8));
                switchImage.JustifyOrigin(0.5f, 0.5f);
                switchImage.Position = currentPos + new Vector2(4, 4);
                switchImage.Rotation = SwitchRotation;
                Add(switchImage);
                switchImages.Add(switchImage);

                currentPos += posIncrement;
            }

            SpriteImage midImage =
                new SpriteImage(
                    ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.Entries[
                        $"objects/StrawberryJam2021/bigDashSwitch/bigSwitchMid{index:D2}"]);
            midImage.JustifyOrigin(0.5f, 0.5f);
            midImage.Position = Center - Position;
            midImage.Rotation = SwitchRotation;
            Add(midImage);
            switchImages.Add(midImage);
        }

        private void AddLightningSprite(Vector2 playerPosition)
        {
            Sprite lightningSprite = runtime.Module.SpriteBank.Create("bigSwitch_lightning");
            Vector2 playerOffset = playerPosition - Position;
            Vector2 spritePosition = Side switch
            {
                OriginalDashSwitchSide.Down => new Vector2(ScalarMath.Clamp(playerOffset.X, 3f, width - 4f), -12f),
                OriginalDashSwitchSide.Up => new Vector2(ScalarMath.Clamp(playerOffset.X, 4f, width - 3f), 20f),
                OriginalDashSwitchSide.Left => new Vector2(20f, ScalarMath.Clamp(playerOffset.Y, 3f, width - 4f)),
                OriginalDashSwitchSide.Right => new Vector2(-12f, ScalarMath.Clamp(playerOffset.Y, 4f, width - 3f)),
                _ => throw new InvalidOperationException("Dash switch direction is invalid."),
            };
            lightningSprite.Rotation = SwitchRotation;
            lightningSprite.Position = spritePosition;
            lightningSprite.OnFinish = _ => lightningSprite.RemoveSelf();
            Add(lightningSprite);
        }

        #endregion

        #region OnPressed stuff

        internal void OnPressed(RoomPlayerSession player, Vector2 direction)
        {
            player.UseRefill(twoDashes: false);
            if (Switch?.Activate() == true)
                SoundEmitterState.Play("event:/game/general/touchswitch_last_oneshot", engine);
            Add(new SimulationCoroutine(engine, PlayPushedAnimation()));
            AddLightningSprite(player.Center);

            // add particles
            OriginalLevelSceneRoot level = ((OriginalLevelSceneRoot)Scene);
            float rotation = SwitchRotation - ((float)Math.PI / 2f);
            Vector2 shatterPos = player.Center + direction * 8f;
            level.ParticlesFG.Emit(runtime.P_Shatter,
                SourceRandomMath.Range(((ISourceRandomEnvironment)engine).Random, 3, 4), shatterPos, Vector2.Zero,
                rotation);
            Vector2 fizzlePos = Center + direction * 4f;
            int fizzleCount = Math.Max(3 * widthTiles, 16);
            level.ParticlesFG.Emit(runtime.P_Fizzle, fizzleCount, fizzlePos,
                Celeste.Core.Mathematics.Vector2Geometry.Perpendicular(direction) * width / 2, rotation);

            // play sounds
            float volume = 0.5f;
            ((ISourceAudioEnvironment)engine).SourceAudio.Play("event:/game/general/diamond_touch").SetVolume(volume);
            ISourceAudioEventReference pressSound =
                ((ISourceAudioEnvironment)engine).SourceAudio.Play("event:/game/03_resort/clutterswitch_books");
            float quarterTone = 1.0293f;
            float clampedWidthTiles = ScalarMath.Clamp(widthTiles, 2, 20);
            float pitch = 1.16f * (float)Math.Pow(quarterTone, -clampedWidthTiles);
            pressSound.SetPitch(pitch);
            pressSound.SetVolume(0.7f * volume);
            SourceAlarmState.Set(engine.AlarmPool, OwnedComponents,
                0.05f / ((ISourceExtremaEnvironment)engine).ExtremaArithmetic.Min(pitch, 1f),
                () => ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Stop(pressSound));
        }

        private IEnumerator PlayPushedAnimation()
        {
            CreateSprite(1);
            yield return 0.15f;
            CreateSprite(2);
        }

        #endregion

        private static void DrawOnlyOutlineCentered(ISourceRenderContext context, AtlasEntry texture,
            Vector2 position, float rotation)
        {
            SourceAtlasDrawing.DrawCentered(context, null, texture, position - Vector2.UnitX, PackedColor.Black, 1f,
                rotation);
            SourceAtlasDrawing.DrawCentered(context, null, texture, position + Vector2.UnitX, PackedColor.Black, 1f,
                rotation);
            SourceAtlasDrawing.DrawCentered(context, null, texture, position - Vector2.UnitY, PackedColor.Black, 1f,
                rotation);
            SourceAtlasDrawing.DrawCentered(context, null, texture, position + Vector2.UnitY, PackedColor.Black, 1f,
                rotation);
        }
    }
}
