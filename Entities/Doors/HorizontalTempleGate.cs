using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Core.Components.Timing;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Graphics.Sprites;
using Celeste.Game.Components.Switches;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Riding;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Shaking;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.StrawberryJam.Entities.Doors
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.HorizontalTempleGate")]
    [CustomEntity("SJ2021/HorizontalTempleGate")]
    public class HorizontalTempleGate : SourceSolidEntityState
    {
        public enum Types
        {
            NearestSwitch,
            TouchSwitches,
            FlagActive
        }

        //for context, this is implemented to allow the gate to open to the right, to the left or from the center of the gate's position. The center direction applies both the left and right door's hitboxes and sprites, modified to halve their extended distance from their respective side.
        public enum OpenDirections
        {
            Left,
            Right,
            Center
        }

        internal string LevelID;

        // 0 - Left; 1 - Right
        private HitboxColliderGeometry[] colliders;
        private Sprite[] sprites;

        private bool Inverted;
        private string Flag;
        public OpenDirections OpenDirection;
        public Types Type;

        // maximum distance between foot of door and edge
        private const float MinEdgeSpace = 4f;

        // how far the foot of the door is from the wall
        private readonly float OpenWidth;

        // distance between foot of door and wall
        private float drawWidth;

        // move drawWidth towards targetWidth;
        private float targetWidth;

        // how fast the door should be moving if it is moving
        private float widthMoveSpeed;

        // either 0.5 or 1. 0.5 to compensate for two doors moving in Center case
        private readonly float moveSpeedMultiplier;

        public bool ClaimedByASwitch;
        private bool open;
        private bool lockState;

        private ShakerComponentState shaker;


        public HorizontalTempleGate(ISourceEngineContext engine, OriginalLevelEntityData data, Vector2 offset,
            ISourceSolidWorld world, SpriteBank bank)
            : base(data.Position + offset, 48f, 8f, true, world)
        {
            this.engine = engine;
            playerKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(RoomPlayerSession))
                .RegistryKey;
            LevelID = data.Level.Name;

            Type = data.Enum<Types>("type", Types.FlagActive);
            OpenDirection = data.Enum<OpenDirections>("direction", OpenDirections.Left);
            OpenWidth = data.Int("openWidth", 0);
            Inverted = data.Bool("inverted", false);
            Flag = data.Attr("flag", "");

            Depth = -9000;
            Add(shaker = new ShakerComponentState(engine, on: false));
            Collider = new ColliderListGeometry();

            moveSpeedMultiplier = 1f;
            // foot of gate when closed
            float targetX = 48f;
            switch (OpenDirection)
            {
                case OpenDirections.Left:
                    targetX = 48f;
                    break;
                case OpenDirections.Right:
                    targetX = 0f;
                    break;
                case OpenDirections.Center:
                    targetX = 24f;
                    moveSpeedMultiplier = 0.5f;
                    break;
            }

            this.colliders = new HitboxColliderGeometry[]
            {
                new HitboxColliderGeometry(targetX, 8f, 0, 0),
                new HitboxColliderGeometry(48f - targetX, 8f, targetX, 0)
            };
            if (OpenDirection != OpenDirections.Right)
            {
                ((ColliderListGeometry)Collider).Add(colliders[0]);
            }

            if (OpenDirection != OpenDirections.Left)
            {
                ((ColliderListGeometry)Collider).Add(colliders[1]);
            }

            this.sprites = new Sprite[2];
            Add(sprites[0] = bank.Create("horizontalTempleGateLeft"));
            Add(sprites[1] = bank.Create("horizontalTempleGateRight"));
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);

            if (Inverted)
            {
                // Init gate as open
                this.drawWidth = Math.Max(OpenWidth, MinEdgeSpace);
                SetWidth((int)OpenWidth);
                this.open = true;
            }
            else
            {
                // init gate as closed
                this.drawWidth = 48f * this.moveSpeedMultiplier;
                SetWidth((int)this.drawWidth);
                this.open = false;
            }

            if (Type == Types.TouchSwitches)
            {
                Add(new SimulationCoroutine(engine, CheckTouchSwitches(), false));
            }
            else if (Type == Types.FlagActive)
            {
                Add(new SimulationCoroutine(engine, CheckFlag(this.Flag), false));
            }
        }

        //applies a delay to utilization of Open in the case the door is opened by a dash switch. I don't know why this was implemented, but for consistency with the vanilla temple gate this is being used.
        public void SwitchOpen()
        {
            foreach (Sprite s in sprites)
            {
                s.Play("open");
            }

            if (Inverted)
            {
                SourceAlarmState.Set(engine.AlarmPool, OwnedComponents, 0.2f, delegate
                {
                    shaker.ShakeFor(0.2f, removeOnFinish: false);
                    SourceAlarmState.Set(engine.AlarmPool, OwnedComponents, 0.2f, Close);
                });
            }
            else
            {
                SourceAlarmState.Set(engine.AlarmPool, OwnedComponents, 0.2f, delegate
                {
                    shaker.ShakeFor(0.2f, removeOnFinish: false);
                    SourceAlarmState.Set(engine.AlarmPool, OwnedComponents, 0.2f, Open);
                });
            }
        }

        public void Open()
        {
            ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/game/05_mirror_temple/gate_main_open",
                Position);
            widthMoveSpeed = 200f;
            shaker.ShakeFor(0.2f, removeOnFinish: false);
            SetWidth(0);
            foreach (Sprite s in sprites)
            {
                s.Play("open");
            }

            this.open = true;
        }

        public void Close()
        {
            ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/game/05_mirror_temple/gate_main_close",
                Position);
            widthMoveSpeed = 300f;
            shaker.ShakeFor(0.2f, removeOnFinish: false);
            SetWidth((int)(48f * moveSpeedMultiplier));

            foreach (Sprite s in sprites)
            {
                s.Play("hit");
            }

            this.open = false;
        }

        private IEnumerator PerformChange(bool shake = true)
        {
            if (shake)
            {
                yield return 0.5f;
                shaker.ShakeFor(0.2f, removeOnFinish: false);
                yield return 0.2f;
            }

            while (lockState)
            {
                yield return null;
            }

            if (Inverted)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private IEnumerator CheckTouchSwitches()
        {
            while (!OriginalSwitchComponent.Check(Scene))
            {
                yield return null;
            }

            yield return PerformChange();
        }

        private IEnumerator CheckFlag(string flag)
        {
            OriginalLevelSceneRoot level = Scene as OriginalLevelSceneRoot;
            while ((this.open == level.Session.GetFlag(flag)) != this.Inverted)
            {
                yield return null;
            }

            yield return PerformChange();
        }

        private void SetWidth(int width)
        {
            targetWidth = width;
            float x = X;
            float oldWidth = 0f;
            if (OpenDirection != OpenDirections.Right)
            {
                oldWidth = colliders[0].Width;
                colliders[0].Width = width;
                // if we're growing, try to push/kill the player
                // there's a separate check if we're in center mode
                if (oldWidth < width && OpenDirection != OpenDirections.Center)
                {
                    X -= width - oldWidth;
                    MoveHExact((int)(width - oldWidth));
                }

                X = x;
            }

            if (OpenDirection != OpenDirections.Left)
            {
                oldWidth = colliders[1].Width;
                colliders[1].Width = width;
                colliders[1].Position.X = 48 - width;
                // attempt to push/kill player
                if (oldWidth < width && OpenDirection != OpenDirections.Center)
                {
                    X -= oldWidth - width;
                    MoveHExact((int)(oldWidth - width));
                }

                X = x;
            }

            if (OpenDirection == OpenDirections.Center && oldWidth < width)
            {
                MoveRiders((int)(width - oldWidth));
            }

            Collidable = true;
        }

        public void MoveRiders(int width)
        {
            // this function is based on Solid.MoveHExact

            // Move/Kill riders when we're centered
            GetRiders();
            float right = Right;
            float left = Left;
            RoomPlayerSession player = null;
            player =
                ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(playerKey).FirstOrDefault() as
                RoomPlayerSession;

            // Drop the player down if they are at a particular height
            // the player gets stuck in the door without this
            if (player != null && SourceEntityCollision.Check(this, player) &&
                (player.Top - this.Bottom) <= -6 && (player.Top - this.Bottom) > -12)
            {
                player.NaiveMove((this.Bottom - player.Top) * Vector2.UnitY);
            }

            foreach (ISourcePlatformActor entity in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(SourceActorEntityState).FullName))
            {
                if (entity.AllowPushing)
                {
                    bool collidable = entity.Collidable;
                    entity.Collidable = true;
                    int move = (((SceneEntityState)entity).CenterX > (left + right) / 2) ? -width : width;
                    if (!entity.TreatNaive && SourceEntityCollision.Check(this, entity))
                    {
                        // yeah i kinda gave up here and just decided to hard center the entity
                        ((SceneEntityState)entity).CenterX = this.CenterX - 1f;
                        entity.MoveHExact(1, entity.SquishCallback, this);
                    }
                    else if (Riders().Contains(entity))
                    {
                        ((SceneEntityState)entity).CenterX = this.CenterX - 1f;
                        if (entity.TreatNaive)
                        {
                            entity.NaiveMove(Vector2.UnitX);
                        }
                        else
                        {
                            entity.MoveHExact(1, null, null);
                        }
                    }

                    entity.Collidable = collidable;
                }
            }

            Riders().Clear();
        }

        public override void Update()
        {
            base.Update();
            float num = Math.Max(targetWidth, MinEdgeSpace);
            if (drawWidth != num)
            {
                lockState = true;
                drawWidth = ScalarMath.Approach(drawWidth, num,
                    moveSpeedMultiplier * widthMoveSpeed * engine.DeltaTime,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            }
            else
            {
                lockState = false;
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            if (OpenDirection != OpenDirections.Right)
            {
                Vector2 shake = new Vector2(0f, Math.Sign(shaker.Value.X));
                sprites[0].DrawSubrect(context, new Vector2(0, -2) + shake,
                    new IntRect((int)(sprites[0].Width - drawWidth), 0,
                        (int)drawWidth, (int)sprites[0].Height), ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            }

            if (OpenDirection != OpenDirections.Left)
            {
                Vector2 shake = new Vector2(0f, Math.Sign(shaker.Value.Y));
                sprites[1].DrawSubrect(context, new Vector2(48f - drawWidth, -3) + shake,
                    new IntRect(0, 0, (int)drawWidth, (int)sprites[1].Height),
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            }
        }

        private readonly ISourceEngineContext engine;
        private readonly string playerKey;
        private static readonly Func<HashSet<ISourcePlatformActor>> Riders = CreateRidersGetter();

        private static Func<HashSet<ISourcePlatformActor>> CreateRidersGetter() => Expression
            .Lambda<Func<HashSet<ISourcePlatformActor>>>(Expression.Field(null,
                typeof(SourceSolidEntityState).GetField("riders", BindingFlags.NonPublic | BindingFlags.Static)))
            .Compile();

        public static SceneEntityState
            Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
                OriginalLevelEntityData data) => new HorizontalTempleGate(level.EngineContext, data, offset,
            level.SolidWorldFactory.Create(data.Position + offset, true),
            ((StrawberryJamModule)((ISourceEverestModulesEnvironment)level.EngineContext).Modules.First(m =>
                m is StrawberryJamModule)).SpriteBank);
    }
}
