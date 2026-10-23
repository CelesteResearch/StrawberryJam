// SPDX-License-Identifier: MIT

using System;
using System.Linq;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Components.Timing;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Rendering;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Scene.Components.Shaking;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Collision;

namespace Celeste.Mods.StrawberryJam.Entities.Doors
{
    [CustomEntity("SJ2021/GrabTempleGate")]
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.GrabTempleGate")]
    public class GrabTempleGate : SourceSolidEntityState
    {
        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            EntityData data)
            => new GrabTempleGate(data, offset, ((ISourcePlatformCollisionScene)level).SolidWorldFactory);

        private const float switchTimeDelay = 0.2f;
        private readonly int closedHeight;

        private readonly Sprite sprite;
        private readonly ShakerComponentState shaker;
        private readonly ISourceEngineContext engine;
        private readonly Action<int> closingMove;

        private readonly bool startClosed;
        private bool open;

        private float drawHeight, drawHeightMoveSpeed;
        private float canSwitchTimer;

        private readonly SoundSourceComponentState sfx;

        public GrabTempleGate(Vector2 position, bool startClosed, ISourceSolidWorld world)
            : base(position, 8f, 48, true, world)
        {
            engine = world.EngineContext;
            closingMove = MoveWhileClosing;
            closedHeight = 48;
            this.startClosed = startClosed;

            Add(sprite = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<StrawberryJamModule>().Single()
                .SpriteBank.Create("grabTempleGate"));
            sprite.Position.X = Collider.Width / 2f;
            sprite.Play("idle");

            Add(sfx = ((ISourceSoundSourceEnvironment)engine).SoundSources.Create());
            Add(shaker = new ShakerComponentState(engine, on: false));
            Depth = -9000;
        }

        public GrabTempleGate(EntityData data, Vector2 offset, ISourceSolidWorldFactory factory)
            : this(data.Position + offset, EntityAttributeReader.Bool(data.Values, "closed", false),
                factory.Create(data.Position + offset, true))
        {
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);

            if (!startClosed)
                StartOpen();

            drawHeight = Math.Max(4f, Height);
        }

        public bool CloseBehindPlayerCheck()
        {
            RoomPlayerSession player =
                ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(typeof(RoomPlayerSession)
                    .FullName);
            return player is not null && player.X < X;
        }

        public void SwitchOpen()
        {
            sprite.Play("open");
            SourceAlarmState.Set(engine.AlarmPool, OwnedComponents, 0.2f, () =>
            {
                shaker.ShakeFor(0.2f, removeOnFinish: false);
                SourceAlarmState.Set(engine.AlarmPool, OwnedComponents, 0.2f, Open);
            });
        }

        public void Open()
        {
            sfx.Play("event:/game/05_mirror_temple/gate_main_open");
            drawHeightMoveSpeed = 200f;
            drawHeight = Height;
            shaker.ShakeFor(0.2f, removeOnFinish: false);
            SetHeight(0);
            sprite.Play("open");
            open = true;
        }

        public void StartOpen()
        {
            SetHeight(0);
            drawHeight = 4f;
            open = true;
        }

        public void Close()
        {
            sfx.Play("event:/game/05_mirror_temple/gate_main_close");
            drawHeightMoveSpeed = 300f;
            drawHeight = Math.Max(4f, Height);
            shaker.ShakeFor(0.2f, removeOnFinish: false);
            SetHeight(closedHeight);
            sprite.Play("hit");
            open = false;
        }

        private void SetHeight(int height) => ClosingSolidResize.SetHeight(this, height, closingMove);

        private void MoveWhileClosing(int amount)
        {
            RoomPlayerSession player =
                ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(typeof(RoomPlayerSession)
                    .FullName);
            if (player != null && player.Bottom <= Bottom)
                MoveVNaive(amount);
            else
                MoveV(amount);
        }

        internal void CheckToggle()
        {
            canSwitchTimer = ScalarMath.Approach(canSwitchTimer, 0f, engine.DeltaTime,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            if (((IOriginalGameInputProcessEnvironment)engine).InputSource.Grab.Pressed && canSwitchTimer == 0f)
            {
                sfx.Stop();
                if (open)
                    Close();
                else
                    Open();
                canSwitchTimer = switchTimeDelay;
            }
        }

        public override void Update()
        {
            base.Update();

            CheckToggle();

            float num = Math.Max(4f, Height);
            if (drawHeight != num)
                drawHeight = ScalarMath.Approach(drawHeight, num, drawHeightMoveSpeed * engine.DeltaTime,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
        }

        public override void Render(ISourceRenderContext context)
        {
            SourcePrimitiveDrawing.Rect(context, this, X - 2f, Y - 8f, 13f, 10f, PackedColor.Black);
            sprite.DrawSubrect(context, Vector2.UnitX * Math.Sign(shaker.Value.X),
                new IntRect(0, (int)(sprite.Height - drawHeight), (int)sprite.Width, (int)drawHeight),
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
        }
    }
}
