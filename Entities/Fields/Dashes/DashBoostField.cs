using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Display.Colors;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Mods.StrawberryJam.Presentation.Fields.Dashes;

namespace Celeste.Mods.StrawberryJam.Entities.Fields.Dashes
{
    [SceneTracked]
    [CustomEntity("SJ2021/DashBoostField = Create")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.DashBoostField")]
    public class DashBoostField : SceneEntityState
    {
        public PackedColor Color;
        public bool PreserveDash;
        public float DashSpeedMult;
        public float TargetTimeRateMult;
        public float Radius;
        private SpriteImage boostFieldTexture;
        public const string DefaultColor = "ffffff";
        public const float DefaultDashSpeedMult = 1.7f;
        public const float DefaultTimeRateMult = .65f;
        public const float DefaultRadius = 1.5f;
        public const bool DefaultPreserveDash = true;
        public ISourceEngineContext EngineContext { get; }
        public new Vector2 Center => Position - new Vector2(.5f, .5f);

        public static DashBoostField Create(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public DashBoostField(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            EngineContext = engine;
            Color = SourceHexColor.HexToColor(data.Attr("color", DefaultColor));
            DashSpeedMult = data.Float("dashSpeedMultiplier", DefaultDashSpeedMult);
            TargetTimeRateMult = data.Float("timeRateMultiplier", DefaultTimeRateMult);
            Radius = data.Float("radius", DefaultRadius) * 8f;
            PreserveDash = data.Bool("preserve", DefaultPreserveDash);
            Depth = -8500;
            Collider = new CircleColliderGeometry(((ISourceVectorEnvironment)engine).VectorArithmetic, Radius);
            Add(boostFieldTexture =
                new SpriteImage(
                    ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[
                        "objects/StrawberryJam2021/dashBoostField/white"]));
            boostFieldTexture.Color = Color;
            boostFieldTexture.CenterOrigin();
            var lightColor = PackedColor.Lerp(Color, PackedColor.White, .9f);
            int startFade = (int)(Radius + 5f), endFade = (int)(Radius + 5f);
            Add(new VertexLightState(engine, new Vector2(-.5f, -.5f), lightColor, 1f, startFade, endFade));
            var particleRenderer = new DashBoostFieldParticleRenderer();
            Add(particleRenderer);
            Tags = OriginalSceneTags.TransitionUpdate;
            Add(new RoomTransitionListener
            {
                OnInBegin = () => Active = particleRenderer.Active = false,
                OnInEnd = () => Active = particleRenderer.Active = true,
                OnOutBegin = () => Active = particleRenderer.Active = false
            });
        }
    }
}
