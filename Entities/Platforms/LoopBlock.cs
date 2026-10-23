using System;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Display;
using Celeste.Core.Collections;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Audio.Surfaces;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Hazards.Spikes;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Entities.Platforms
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.LoopBlock")]
    [CustomEntity("SJ2021/LoopBlock")]
    public sealed class LoopBlock : SourceSolidEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly LoopBlockTextures textures;

        private Vector2 start;
        private Vector2 speed;

        private AtlasEntry[,] tiles;
        private Vector2 scale = Vector2.One;
        private PackedColor color;

        private ParticleTypePresentation particleType;

        private OriginalLevelSceneRoot level;

        private bool waiting = true;
        private bool canRumble;
        private bool returning, returningDash;
        private bool dashed, scaledSpikes;

        private float respawnTimer;
        private float targetSpeedX;
        private float dashedDirX;

        private int edgeThickness;

        private const float sideBoostSpeed = 310f; // decided by mapper

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new LoopBlock(level.EngineContext, data.Position + offset, data.Width, data.Height,
                data.Int("edgeThickness", 1), SourceHexColor.HexToColor(data.Attr("color", "FFFFFF")),
                ((ISourcePlatformCollisionScene)level).SolidWorldFactory.Create(data.Position + offset, false));

        public LoopBlock(ISourceEngineContext engine, Vector2 position, int width, int height,
            int edgeThickness, PackedColor color, ISourceSolidWorld world)
            : base(position, width, height, false, world)
        {
            this.engine = engine;
            textures = LoopBlockTextures.Get(((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas);
            Depth = -9999;
            SurfaceSoundIndex = OriginalSurfaceIndex.Snow;

            start = position;

            int minEdgeSize = Math.Min(width, height) / 8;
            this.edgeThickness = ScalarMath.Clamp(edgeThickness, 1, (int)((minEdgeSize - 1) / 2f));
            this.color = color;

            particleType = new ParticleTypePresentation(((IOriginalGameplayProcess)engine).ParticleTypes.Cloud.P_Cloud)
            {
                Color = color
            };

            OnDashCollide = OnDashed;

            SetupTiles();
        }

        private void SetupTiles()
        {
            int w = (int)(Width / 8f);
            int h = (int)(Height / 8f);

            tiles = new AtlasEntry[w, h];
            VirtualMap<bool> tileMap = new VirtualMap<bool>(w, h);

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    tileMap[i, j] = (i < edgeThickness || i >= w - edgeThickness || j < edgeThickness ||
                                     j >= h - edgeThickness);
                }
            }

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    if (tileMap[i, j])
                    {
                        int index = ((ISourceRandomEnvironment)engine).Random.Next(3);

                        bool left = tileMap.SafeCheck(i - 1, j);
                        bool right = tileMap.SafeCheck(i + 1, j);
                        bool up = tileMap.SafeCheck(i, j - 1);
                        bool down = tileMap.SafeCheck(i, j + 1);
                        bool upleft = tileMap.SafeCheck(i - 1, j - 1);
                        bool upright = tileMap.SafeCheck(i + 1, j - 1);
                        bool downleft = tileMap.SafeCheck(i - 1, j + 1);
                        bool downright = tileMap.SafeCheck(i + 1, j + 1);

                        bool innerEdge = left && right && up && down;
                        bool filler = innerEdge && upleft && upright && downleft && downright;

                        AtlasEntry texture = null;
                        if (filler)
                        {
                            texture = textures.CenterTiles[((ISourceRandomEnvironment)engine).Random.Next(8)];
                        }
                        else if (innerEdge)
                        {
                            if (!downright)
                                texture = textures.InnerCorners[0, 0];
                            else if (!downleft)
                                texture = textures.InnerCorners[1, 0];
                            else if (!upright)
                                texture = textures.InnerCorners[0, 1];
                            else if (!upleft)
                                texture = textures.InnerCorners[1, 1];
                        }
                        else
                        {
                            if (!up && down && left && right)
                                texture = textures.OuterEdges[1, 0, index];
                            else if (up && !down && left && right)
                                texture = textures.OuterEdges[1, 2, index];
                            else if (up && down && !left && right)
                                texture = textures.OuterEdges[0, 1, index];
                            else if (up && down && left && !right)
                                texture = textures.OuterEdges[2, 1, index];
                            else if (right && down)
                                texture = (downright
                                    ? textures.OuterEdges[0, 0, index]
                                    : textures.WallEdges[0, 0, index]);
                            else if (left && down)
                                texture =
                                    (downleft ? textures.OuterEdges[2, 0, index] : textures.WallEdges[1, 0, index]);
                            else if (right && up)
                                texture = (upright
                                    ? textures.OuterEdges[0, 2, index]
                                    : textures.WallEdges[0, 1, index]);
                            else if (left && up)
                                texture = (upleft ? textures.OuterEdges[2, 2, index] : textures.WallEdges[1, 1, index]);
                            else if (left && right && !up && !down)
                                texture = textures.WallEdges[2, 0, index];
                            else if (!left && !right && up && down)
                                texture = textures.WallEdges[2, 1, index];
                        }

                        if (texture != null)
                            tiles[i, j] = texture;
                    }
                }
            }
        }

        private DashCollisionResult OnDashed(RoomPlayerSession player, Vector2 dir)
        {
            if (dir.Y == 0)
            {
                dashedDirX = dir.X;

                // Math.Abs(dashedDirX) is always 1.
                scale = new Vector2(0.6f, 1.4f);

                int dashes = player.Data.Dashes;
                float stamina = player.Data.Stamina;

                // We'll rescale spikes here instead, and it won't be done in Update.
                // Because of the Celeste.Freeze call in Player.ExplodeLaunch,
                // they would remain unscaled during the freeze time, which looked a bit weird.
                RescaleSpikes();
                player.ExplodeLaunch(new Vector2(Center.X, player.Center.Y), false, true);

                player.Speed = new Vector2(-sideBoostSpeed * dir.X, player.Speed.Y);

                player.Data.Dashes = dashes;
                player.Data.Stamina = stamina;

                level.DirectionalShake(Vector2.UnitX, 0.2f);
                ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble((int)RumbleStrength.Medium,
                    (int)RumbleLength.Short);
                ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                    "event:/strawberry_jam_2021/game/loop_block/sideboost", player.Center);

                speed.X = dir.X * 180f;
                targetSpeedX = -dir.X * 90f;

                dashed = true;
            }
            else if (dir.Y == -1)
            {
                // Only has a visual purpose, not an used mechanic.
                scale = new Vector2(1.4f, 0.8f);

                waiting = false;
                canRumble = true;
                speed.Y = -90f;

                return DashCollisionResult.Bounce;
            }

            return DashCollisionResult.NormalCollision;
        }

        private void RescaleSpikes()
        {
            if (scaledSpikes)
                return;
            foreach (var staticMover in staticMovers)
            {
                if (staticMover.Entity is SpikesState spikes)
                {
                    spikes.SetOrigins(Center);
                    foreach (var component in spikes.Components)
                    {
                        if (component is SpriteImage image)
                            image.Scale = scale;
                    }
                }
            }

            scaledSpikes = true;
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            level = (OriginalLevelSceneRoot)Scene;
        }

        public override void Update()
        {
            base.Update();

            scale = ((ISourceVectorEnvironment)engine).VectorArithmetic.Approach(scale, Vector2.One,
                3f * engine.DeltaTime);
            if (!scaledSpikes)
                RescaleSpikes();

            scaledSpikes = false;

            if (respawnTimer > 0f)
            {
                respawnTimer -= engine.DeltaTime;
                if (respawnTimer <= 0f)
                {
                    waiting = true;
                    Y = start.Y;
                    speed.Y = 0f;
                    Collidable = true;
                }

                return;
            }

            if (dashed)
            {
                if (returningDash)
                {
                    speed.X = ScalarMath.Approach(speed.X, -targetSpeedX * 0.75f, 600f * engine.DeltaTime,
                        ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                    MoveH(speed.X * engine.DeltaTime);
                    if ((dashedDirX < 0 && X <= start.X) || (dashedDirX > 0 && X >= start.X))
                    {
                        returningDash = dashed = false;
                        MoveToX(start.X);
                        speed.X = 0f;
                    }
                }
                else
                {
                    speed.X = ScalarMath.Approach(speed.X, targetSpeedX, 1200f * engine.DeltaTime,
                        ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                    MoveH(speed.X * engine.DeltaTime);
                    if (speed.X == targetSpeedX && ((dashedDirX < 0 && X > start.X) || (dashedDirX > 0 && X < start.X)))
                        returningDash = true;
                }
            }

            if (waiting)
            {
                RoomPlayerSession playerRider = GetPlayerRider();
                if (playerRider != null && playerRider.Machine.State != 21)
                {
                    canRumble = true;
                    speed.Y = 180f;
                    waiting = false;
                    ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                        "event:/game/04_cliffside/cloud_blue_boost", Center);
                }

                return;
            }

            if (returning)
            {
                speed.Y = ScalarMath.Approach(speed.Y, 180f, 600f * engine.DeltaTime,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);

                // Acts like Platform.MoveTowardsY, but with custom liftspeed.
                // Essentially makes it possible to get the Y boost in a larger window of time.
                MoveToY(
                    ScalarMath.Approach(ExactPosition.Y, start.Y, speed.Y * engine.DeltaTime,
                        ((ISourceExtremaEnvironment)engine).ExtremaArithmetic), speed.Y < 0f ? -220f : speed.Y);

                if (ExactPosition.Y == start.Y)
                {
                    returning = false;
                    waiting = true;
                    speed.Y = 0f;
                }

                return;
            }

            if (speed.Y < 0f && canRumble)
            {
                canRumble = false;
                if (HasPlayerRider())
                {
                    ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble((int)RumbleStrength.Medium,
                        (int)RumbleLength.Medium);
                }
            }

            if (speed.Y < 0f && level.OnInterval(0.02f))
            {
                level.ParticlesBG.Emit(particleType, 1, Position + new Vector2(Width / 2, Height),
                    new Vector2(Width / 2f, 1f), (float)Math.PI / 2f);
            }

            if (Y >= start.Y)
            {
                speed.Y -= 1200f * engine.DeltaTime;
            }
            else
            {
                speed.Y += 1200f * engine.DeltaTime;
                if (speed.Y >= -100f)
                {
                    RoomPlayerSession playerRider = GetPlayerRider();
                    if (playerRider != null && playerRider.Speed.Y >= 0f && !HasPlayerClimbing())
                    {
                        playerRider.Speed = new Vector2(playerRider.Speed.X, -200f);
                    }

                    returning = true;
                }
            }

            MoveV(speed.Y * engine.DeltaTime, speed.Y < 0f ? -220f : speed.Y);
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);

            int w = (int)(Width / 8f);
            int h = (int)(Height / 8f);

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    AtlasEntry tile = tiles[i, j];
                    if (tile != null)
                    {
                        Vector2 pos = Center + (new Vector2(X + i * 8 + 4, Y + j * 8 + 4) - Center) * scale;
                        SourceAtlasDrawing.DrawCentered(context, this, tile, pos, color, scale);
                    }
                }
            }
        }
    }
}
