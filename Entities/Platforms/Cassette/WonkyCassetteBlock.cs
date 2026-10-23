using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Maps;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Game.Audio.Runtime;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Platforms.Cassette;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Core.Rendering;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Mods.StrawberryJam.Persistence;
using Celeste.Mods.StrawberryJam.Runtime.Cassette;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Scene.Tagging;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Audio.Fmod.Events;

namespace Celeste.Mods.StrawberryJam.Entities.Platforms.Cassette
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.WonkyCassetteBlock")]
    [CustomEntity("SJ2021/WonkyCassetteBlock")]
    [SceneTracked]
    public class WonkyCassetteBlock : CassetteBlockState
    {
        private static readonly Regex OnAtBeatsSplitRegex = new(@",\s*", RegexOptions.Compiled);

        public readonly int[] OnAtBeats;
        public readonly int ControllerIndex;

        private readonly int OverrideBoostFrames;
        public int boostFrames = 0;
        public bool boostActive = false;

        private string textureDir;

        private readonly string Key;

        private readonly Dictionary<string, WonkyCassetteConnectionGrid> connections;

        private List<SpriteImage>
            _pressed,
            _solid; // we'll use these instead of pressed and solid, to make `UpdateVisualState` not enumerate through them for no reason.

        public WonkyCassetteBlock(ISourceEngineContext engine, Vector2 position, MapEntityId id, float width,
            float height, int index, string moveSpec, PackedColor color, string textureDir, int overrideBoostFrames,
            int controllerIndex, ISourceSolidWorld world)
            : base(position, id, width, height, index, 1.0f, world)
        {
            Tags = ((ISourceBitTagEnvironment)engine).BitTags.Get("frozenUpdate") |
                   ((ISourceBitTagEnvironment)engine).BitTags.Get("transitionUpdate");
            connections = WonkyCassetteConnectionRegistry.For(engine).Connections;

            OnAtBeats = OnAtBeatsSplitRegex.Split(moveSpec).Select(s => int.Parse(s) - 1).ToArray();
            Array.Sort(OnAtBeats);

            base.color = color;

            this.textureDir = textureDir;

            OverrideBoostFrames = overrideBoostFrames;

            if (controllerIndex < 0)
                throw new ArgumentException($"Controller Index must be 0 or greater, but is set to {controllerIndex}.");

            ControllerIndex = controllerIndex;

            Key = $"{Index}|{ControllerIndex}|{string.Join(",", OnAtBeats)}";

            _pressed = new();
            _solid = new();
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new WonkyCassetteBlock(level.EngineContext, data.Position + offset, new MapEntityId(room.Name, data.ID),
                data.Width, data.Height, data.Int("index"), data.Attr("onAtBeats"),
                data.Has("color") ? SourceHexColor.HexToColor(data.Attr("color")) : default(PackedColor),
                data.Attr("textureDirectory", "objects/cassetteblock").TrimEnd('/'), data.Int("boostFrames", -1),
                data.Int("controllerIndex", 0),
                ((ISourcePlatformCollisionScene)level).SolidWorldFactory.Create(data.Position + offset, false));

        protected override bool ShouldQueueBoxSide => false;

        protected override void FindInGroup(CassetteBlockState block)
        {
            foreach (WonkyCassetteBlock entity in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(WonkyCassetteBlock)))
            {
                if (entity != this && entity != block && entity.Index == Index &&
                    entity.ControllerIndex == ControllerIndex &&
                    (SourceEntityGeometryCollision.Rectangle(entity,
                         new IntRect((int)block.X - 1, (int)block.Y, (int)block.Width + 2, (int)block.Height)) ||
                     SourceEntityGeometryCollision.Rectangle(entity,
                         new IntRect((int)block.X, (int)block.Y - 1, (int)block.Width, (int)block.Height + 2))) &&
                    !group.Contains(entity) && entity.OnAtBeats.SequenceEqual(OnAtBeats))
                {
                    group.Add(entity);
                    FindInGroup(entity);
                }
            }
        }

        public override void Awake(object scene)
        {
            if (connections.Count == 0)
            {
                IndexConnections((OriginalLevelSceneRoot)Scene);
            }

            base.Awake(scene);
        }

        public override void Update()
        {
            bool activating = groupLeader && Activated && !Collidable;

            base.Update();

            if (Activated && Collidable)
            {
                if (activating)
                {
                    // Block has activated, Cassette boost is possible this frame
                    if (OverrideBoostFrames > 0)
                    {
                        boostFrames = OverrideBoostFrames - 1;
                        boostActive = true;
                    }
                    else if (OverrideBoostFrames < 0)
                    {
                        WonkyCassetteBlockController controller = ((ISourceScene)Scene).SourceScene.Entities
                            .GetTrackedEntity<WonkyCassetteBlockController>();
                        if (controller != null)
                        {
                            boostFrames = controller.ExtraBoostFrames;
                            boostActive = true;
                        }
                    }

                    foreach (CassetteBlockState cassetteBlock in group)
                    {
                        WonkyCassetteBlock wonkyBlock = (WonkyCassetteBlock)cassetteBlock;
                        wonkyBlock.boostFrames = boostFrames;
                        wonkyBlock.boostActive = boostActive;
                    }
                }

                if (boostActive)
                {
                    // Vanilla lift boost is active this frame, do nothing
                    boostActive = false;
                }
                else if (boostFrames > 0)
                {
                    // Provide an extra boost for the duration of the extra boost frames
                    var lift = LiftSpeed;
                    lift.Y = -1 / SourceWorld.EngineContext.DeltaTime;
                    LiftSpeed = lift;

                    // Update lift of riders
                    MoveVExact(0);

                    boostFrames -= 1;
                }
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            if (IsRectangleVisible())
            {
                List<SpriteImage> images = Collidable ? _solid : _pressed;

                foreach (SpriteImage item in images)
                {
                    SourceAtlasDrawing.Draw(context, this, item.Texture, item.Position + Position, item.Origin,
                        item.Color, item.Scale, item.Rotation, item.FlipX, item.FlipY);
                }
            }
        }

        private void IndexConnections(OriginalLevelSceneRoot level)
        {
            OriginalLevelRoomData levelData = (OriginalLevelRoomData)level.Session.LevelData;
            IntRect bounds = levelData.Bounds;
            IntRect tileBounds = levelData.TileBounds;

            foreach (WonkyCassetteBlock entity in level.SourceScene.Entities.GetTrackedEntities(
                         typeof(WonkyCassetteBlock)))
            {
                WonkyCassetteConnectionGrid connection;

                if (!connections.TryGetValue(entity.Key, out connection))
                {
                    connections.Add(entity.Key,
                        connection = new WonkyCassetteConnectionGrid(tileBounds.Width + 2, tileBounds.Height + 2));
                }

                for (float x = entity.Left; x < entity.Right; x += 8f)
                {
                    for (float y = entity.Top; y < entity.Bottom; y += 8f)
                    {
                        int ix = ((int)x - bounds.Left) / 8 + 1;
                        int iy = ((int)y - bounds.Top) / 8 + 1;

                        if (ix < 0) ix = 0;
                        else if (ix > tileBounds.Width) ix = tileBounds.Width + 1;
                        if (iy < 0) iy = 0;
                        else if (iy > tileBounds.Height) iy = tileBounds.Height + 1;

                        connection[ix, iy] = true;
                    }
                }
            }
        }

        protected override bool CheckForSame(float x, float y)
        {
            WonkyCassetteConnectionGrid connection;

            if (!connections.TryGetValue(Key, out connection))
            {
                // Fallback just in case
                foreach (WonkyCassetteBlock entity in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                             typeof(WonkyCassetteBlock)))
                {
                    if (entity.Index == Index && entity.ControllerIndex == ControllerIndex &&
                        SourceEntityGeometryCollision.Rectangle(entity.Collider, new IntRect((int)x, (int)y, 8, 8)) &&
                        entity.OnAtBeats.SequenceEqual(OnAtBeats))
                    {
                        return true;
                    }
                }

                return false;
            }

            OriginalLevelSceneRoot level = (OriginalLevelSceneRoot)Scene;
            OriginalLevelRoomData levelData = (OriginalLevelRoomData)level.Session.LevelData;
            IntRect bounds = levelData.Bounds;
            IntRect tileBounds = levelData.TileBounds;

            int ix = ((int)x - bounds.Left) / 8 + 1;
            int iy = ((int)y - bounds.Top) / 8 + 1;

            if (ix < 0) ix = 0;
            else if (ix > tileBounds.Width) ix = tileBounds.Width + 1;
            if (iy < 0) iy = 0;
            else if (iy > tileBounds.Height) iy = tileBounds.Height + 1;

            return connection[ix, iy];
        }

        protected override void SetImage(float x, float y, int tx, int ty)
        {
            var atlas = ((ISourceGameGraphicsEnvironment)SourceWorld.EngineContext).GameGraphics.GameAtlas;
            atlas.PushFallback(atlas["objects/cassetteblock/pressed00"]);
            SpriteImage image = CreateImage(x, y, tx, ty, atlas[textureDir + "/pressed"]);
            image.RemoveSelf();
            _pressed.Add(image);
            atlas.PopFallback();

            atlas.PushFallback(atlas["objects/cassetteblock/solid"]);
            image = CreateImage(x, y, tx, ty, atlas[textureDir + "/solid"]);
            image.RemoveSelf();
            _solid.Add(image);
            atlas.PopFallback();
        }

        protected override void ShiftSize(int amount)
        {
            if (OverrideBoostFrames == 0) MoveV(amount, 0);
            else MoveV(amount);
            blockHeight -= amount;
        }

        private bool IsRectangleVisible()
        {
            var camera = (SourceWorld.EngineContext.Frame.Scenes.CurrentScene as OriginalLevelSceneRoot)?.Camera;
            if (camera == null) return true;
            return X + Width >= camera.Left - 4 && X <= camera.Right + 4 &&
                   Y + Height >= camera.Top - 4 && Y <= camera.Bottom + 180 + 4;
        }
    }
}
