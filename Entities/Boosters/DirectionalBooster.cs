using System.Numerics;
using Celeste.Core.Display;
using Celeste.Graphics.Display.Colors;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Entities.Interaction.Boosters;
using Celeste.Game.Execution.Processes;
using Celeste.Mods.StrawberryJam.Runtime.Boosters;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Entities.Boosters
{
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.DirectionalBooster")]
    [CustomEntity("SJ2021/DirectionalBooster")]
    public sealed class DirectionalBooster : OriginalBoosterState
    {
        internal ISourceEngineContext Engine { get; }
        internal ParticleTypePresentation AppearParticleType { get; }
        internal Celeste.Graphics.Animation.Sprite DirectionalSprite => BoosterSprite;

        public DirectionalBooster(EntityData data, Vector2 offset, ISourceEngineContext engine)
            : base(engine, data.Position + offset, true)
        {
            Engine = engine;
            BoosterSprite = DirectionalBoosterProcess.For(engine).Module.SpriteBank
                .CreateOn(BoosterSprite, "directionalBooster");
            PackedColor color = SourceHexColor.HexToColor("e6a434");
            if (data.Values.TryGetValue("boosterColor", out var value))
            {
                string text = value.ToString();
                if (text.Length == 6) color = SourceHexColor.HexToColor(text);
            }

            var particles = ((IOriginalGameplayProcess)engine).ParticleTypes.Booster;
            BoosterParticleType = new ParticleTypePresentation(particles.P_BurstRed) { Color = color, Color2 = color };
            AppearParticleType = new ParticleTypePresentation(particles.P_RedAppear) { Color = color, Color2 = color };
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            EntityData data)
            => new DirectionalBooster(data, offset, level.EngineContext);
    }
}
