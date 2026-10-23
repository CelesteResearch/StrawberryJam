using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.StrawberryJam.Entities.Lighting
{
    [SceneTracked(false)]
    [CustomEntity("SJ2021/LightSourceLimitController")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.LightSourceLimitController")]
    public class LightSourceLimitController : SceneEntityState
    {
        public const int VanillaLightLimit = 64;
        public const int VanillaVertexCount = 11520;
        public const int VanillaResultVertexCount = 384;
        public const int VanillaRenderTargetBufferSize = 1024;
        public const int VanillaLightsPerChannel = 16;
        public const int VanillaTextureSplit = 4;
        public const float VanillaMatrixScale = 0.0009765625f;
        public const float Radius = 128f;

        public static LightSourceLimitController Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.EngineContext);

        public LightSourceLimitController(EntityData data, Vector2 offset, ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
        }
    }
}
