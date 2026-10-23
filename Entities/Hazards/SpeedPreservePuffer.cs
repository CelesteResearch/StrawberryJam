using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Hazards.Puffer;
using Celeste.Physics.Collision;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.StrawberryJam.Entities.Hazards
{
    [CustomEntity("SJ2021/SpeedPreservePuffer")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.SpeedPreservePuffer")]
    public class SpeedPreservePuffer : OriginalPufferState
    {
        public bool Static;

        public SpeedPreservePuffer(OriginalLevelSceneRoot level, EntityData data, Vector2 offset)
            : base(new PixelActor(((ISourcePlatformCollisionScene)level).PlatformWorld, data.Position + offset),
                data.Position + offset, data.Bool("right"), level.EngineContext,
                ((ISourcePlatformTargetMotionEnvironment)level.EngineContext).TargetMotionPolicy)
        {
            Static = data.Bool("static", true);
            if (Static)
            {
                PufferIdleSine.RemoveSelf();
                var position = Position;
                position.X += .0001f;
                Position = position;
            }

            Depth = -1;
        }

        internal void ResetIdleSine() => PufferIdleSine.Reset();

        public static SpeedPreservePuffer Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new SpeedPreservePuffer(level, data, offset);
    }
}
