using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.StrawberryJam.Entities.Barriers;
using Celeste.Mods.StrawberryJam.Runtime.Gravity;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.StrawberryJam.Triggers.Gravity
{
    [CustomEntity("SJ2021/ZeroGTrigger = Load")]
    public sealed class ZeroGTrigger : SourceTriggerEntityState
    {
        private readonly ZeroGRuntime runtime;
        public ZeroGBarrierDirection Direction;
        public int OnlyOnJustRespawn;

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new ZeroGTrigger(level.EngineContext, data, offset);

        public ZeroGTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data, offset, ZeroGBarrierDirection.Up)
        {
            OnlyOnJustRespawn = data.Bool("state", true) ? 2 : 1;
        }

        public ZeroGTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset,
            ZeroGBarrierDirection direction)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            runtime = ZeroGRuntime.For(engine);
            Direction = direction;
        }

        public void SetZeroG(bool on) => runtime.SetZeroG(on);

        public override void OnEnter(RoomPlayerSession player)
        {
            if (OnlyOnJustRespawn > 0 && player.JustRespawned)
            {
                base.OnEnter(player);
                Triggered = true;
                runtime.SetZeroG(OnlyOnJustRespawn == 2);
                RemoveSelf();
            }
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            if (OnlyOnJustRespawn != 0) return;
            base.OnLeave(player);
            switch (Direction)
            {
                case ZeroGBarrierDirection.Right:
                    if (player.Right <= Left) runtime.SetZeroG(false);
                    else if (player.Left >= Right) runtime.SetZeroG(true);
                    break;
                case ZeroGBarrierDirection.Left:
                    if (player.Right <= Left) runtime.SetZeroG(true);
                    else if (player.Left >= Right) runtime.SetZeroG(false);
                    break;
                case ZeroGBarrierDirection.Up:
                    if (player.Bottom <= Top) runtime.SetZeroG(true);
                    else if (player.Top >= Bottom) runtime.SetZeroG(false);
                    break;
                case ZeroGBarrierDirection.Down:
                    if (player.Bottom <= Top) runtime.SetZeroG(false);
                    else if (player.Top >= Bottom) runtime.SetZeroG(true);
                    break;
            }
        }
    }
}
