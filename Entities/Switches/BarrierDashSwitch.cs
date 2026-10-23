using System;
using System.Numerics;
using Celeste.Core.Maps;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Mechanisms.DashSwitches;
using Celeste.Graphics.Content;
using Celeste.Scene.Entities;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.StrawberryJam.Entities.Switches
{
    [CustomEntity("SJ2021/BarrierDashSwitch")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.BarrierDashSwitch")]
    public class BarrierDashSwitch : OriginalDashSwitchState
    {
        public BarrierDashSwitch(ISourceEngineContext engine, Vector2 position, OriginalDashSwitchSide side,
            bool persistent, MapEntityId id, string spritePath, ISourceSolidWorld world)
            : base(position, side, persistent, false, id, "default", engine, world)
        {
            if (!string.IsNullOrEmpty(spritePath))
            {
                var sprite = SourceSwitchSprite;
                sprite.Reset(((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas,
                    $"{spritePath}/dashButton");
                sprite.CenterOrigin();
                sprite.Justify = new Vector2(.5f, .5f);
                int[] idleFrames = new int[21];
                for (int i = 0; i <= 20; i++) idleFrames[i] = i;
                sprite.AddLoop("idle", "", .08f, idleFrames);
                sprite.AddLoop("pushed", "", .08f, 27);
                sprite.Add("push", "", .07f, "pushed", 21, 22, 23, 24, 25, 26, 27);
                switch (side)
                {
                    case OriginalDashSwitchSide.Right:
                        sprite.Position = new Vector2(8f, 8f);
                        sprite.Rotation = 0f;
                        break;
                    case OriginalDashSwitchSide.Left:
                        sprite.Position = new Vector2(0f, 8f);
                        sprite.Rotation = (float)Math.PI;
                        break;
                }

                sprite.Play("idle");
            }
        }

        public BarrierDashSwitch(ISourceEngineContext engine, EntityData data, Vector2 offset, MapEntityId id,
            ISourceSolidWorld world)
            : this(engine, data.Position + offset, SwitchSide(data.Enum("orientation", OriginalDashSwitchSide.Left)),
                data.Bool("persistent"), id, data.Attr("spritePath", ""), world)
        {
        }

        private static OriginalDashSwitchSide SwitchSide(OriginalDashSwitchSide side) => side switch
        {
            OriginalDashSwitchSide.Left => OriginalDashSwitchSide.Right,
            OriginalDashSwitchSide.Right => OriginalDashSwitchSide.Left,
            _ => OriginalDashSwitchSide.Right
        };

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new BarrierDashSwitch(level.EngineContext, data, offset,
            level.CreateEntityId(room, data), level.SolidWorldFactory.Create(data.Position + offset, true));
    }
}
