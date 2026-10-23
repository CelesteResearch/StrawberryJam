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
    [CustomEntity("SJ2021/SolarElevator")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.SolarElevator")]
    public partial class SolarElevator : SourceSolidEntityState
    {
        [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.SolarElevator+StartPosition")]
        public enum StartPosition
        {
            Closest,
            Top,
            Bottom
        }

        private readonly ColliderListGeometry OpenCollider = new(new HitboxColliderGeometry(3, 16, -24, -54),
            new HitboxColliderGeometry(3, 16, 21, -54), new HitboxColliderGeometry(48, 8, -24, -62),
            new HitboxColliderGeometry(48, 5, -24, 0));

        private readonly ColliderListGeometry ClosedCollider = new(new HitboxColliderGeometry(3, 54, -24, -54),
            new HitboxColliderGeometry(3, 54, 21, -54), new HitboxColliderGeometry(48, 8, -24, -62),
            new HitboxColliderGeometry(48, 5, -24, 0));

        internal readonly ISourceEngineContext Engine;
        private readonly OriginalTalkComponent interaction;
        private readonly Celeste.Simulation.Audio.Sources.SoundSourceComponentState sfx;
        public readonly float StartY, Distance;
        private readonly int bgDepth;
        private readonly float time, delay;
        private readonly bool oneWay;
        private readonly StartPosition startPosition;
        private readonly string moveSfx, haltSfx;
        private readonly AtlasEntry back, rails;
        private readonly string playerKey, holdableKey;
        private Background bg;
        public const string DefaultHintDialog = "StrawberryJam2021_Entities_SolarElevator_DefaultHint";
        public readonly string HoldableHintDialog;
        public readonly bool RequiresHoldable;
        public bool Moving;
        public bool AtGroundFloor = true;
        public bool IsCarryingHoldable;

        public SolarElevator(ISourceEngineContext engine, EntityData data, Vector2 offset, ISourceSolidWorld world) :
            base(data.Position + offset, 56, 80, true, world)
        {
            Engine = engine;
            Depth = -10500;
            SurfaceSoundIndex = 42;
            StartY = Y;
            Distance = data.Int("distance", 128);
            bgDepth = data.Int("bgDepth", 9000);
            time = data.Float("time", 3);
            delay = data.Float("delay", 1);
            oneWay = data.Bool("oneWay", false);
            startPosition = EntityAttributeReader.Enum(data.Values, "startPosition", StartPosition.Closest);
            moveSfx = data.Attr("moveSfx", "event:/strawberry_jam_2021/game/solar_elevator/elevate");
            haltSfx = data.Attr("haltSfx", "event:/strawberry_jam_2021/game/solar_elevator/halt");
            RequiresHoldable = data.Bool("requiresHoldable", false);
            HoldableHintDialog = data.Attr("holdableHintDialog", DefaultHintDialog);
            var catalog = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog;
            playerKey = catalog.Find(typeof(RoomPlayerSession)).RegistryKey;
            holdableKey = catalog.Find(typeof(HoldableState)).RegistryKey;
            UpdateCollider(true);
            OwnedComponents.Add(sfx = ((Celeste.Simulation.Audio.Sources.ISourceSoundSourceEnvironment)engine)
                .SoundSources.Create());
            OwnedComponents.Add(interaction = new OriginalTalkComponent((IOriginalTalkProcess)engine,
                new IntRect(-12, -8, 24, 8), Vector2.UnitY * -24, Activate));
            string skin = data.Attr("reskinDirectory", "").Trim().TrimEnd('/');
            string path = string.IsNullOrEmpty(skin) ? "objects/StrawberryJam2021/solarElevator/" : skin + "/";
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            atlas.PushFallback(atlas["objects/StrawberryJam2021/solarElevator/front"]);
            var front = atlas[path + "front"];
            atlas.PopFallback();
            atlas.PushFallback(atlas["objects/StrawberryJam2021/solarElevator/back"]);
            back = atlas[path + "back"];
            atlas.PopFallback();
            atlas.PushFallback(atlas["objects/StrawberryJam2021/solarElevator/rails"]);
            rails = atlas[path + "rails"];
            atlas.PopFallback();
            var image = new SpriteImage(front);
            image.JustifyOrigin(.5f, 1);
            image.Position.Y = 10;
            OwnedComponents.Add(image);
        }

        public static SolarElevator Load(OriginalLevelSceneRoot level,
            Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room, Vector2 offset,
            Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData data) => new(level.EngineContext, data, offset,
            ((ISourcePlatformCollisionScene)level).SolidWorldFactory.Create(data.Position + offset, true));

        public override void Awake(object scene)
        {
            base.Awake(scene);
            switch (startPosition)
            {
                case StartPosition.Top:
                    Y -= Distance;
                    AtGroundFloor = false;
                    return;
                case StartPosition.Bottom: return;
            }

            var player = ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(playerKey);
            if (player != null && Vector2.DistanceSquared(player.Center, Position) >
                Vector2.DistanceSquared(player.Center, Position - Vector2.UnitY * Distance))
            {
                Y -= Distance;
                AtGroundFloor = false;
            }
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            ((ISourceScene)scene).SourceScene.QueueSourceEntity(bg = new Background(this, bgDepth));
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            ((ISourceScene)scene).SourceScene.RemoveEntities(bg);
        }

        private void UpdateCollider(bool open) => Collider = open ? OpenCollider : ClosedCollider;

        private void Activate(RoomPlayerSession player)
        {
            ((ISourceSpatialAudio)Engine).Play("event:/new_content/game/10_farewell/ppt_mouseclick", Position);
            if (!Moving && (!RequiresHoldable || IsCarryingHoldable))
                OwnedComponents.Add(new SimulationCoroutine(Engine, Sequence(player)));
        }

        private bool CanCarry(HoldableState holdable)
        {
            if (holdable.Entity is not SourceActorEntityState actor) return false;
            var holder = holdable.Holder;
            if (holder != null && holder.IsRiding(this)) return true;
            return actor.Left >= Left + 3 && actor.Right <= Right - 3 && actor.Bottom <= Bottom && actor.Top >= Top + 8;
        }

        private bool HoldableCheck()
        {
            var components = ((ISourceScene)Scene).SourceScene.Components.GetTrackedComponents(holdableKey);
            foreach (var component in components)
                if (CanCarry((HoldableState)component))
                    return true;
            return false;
        }

        public override void Update()
        {
            base.Update();
            IsCarryingHoldable = HoldableCheck();
        }
    }
}
