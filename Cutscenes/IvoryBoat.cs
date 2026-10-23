using System.Collections;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.StrawberryJam.Cutscenes
{
    [CustomEntity("SJ2021/IvoryBoat")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Cutscenes.IvoryBoat")]
    public class IvoryBoat : SourceJumpThruEntityState
    {
        private const float StartSpeed = 60f, EndSpeed = 16f;
        private readonly SineWaveState sine;
        private readonly SpriteImage image;
        private readonly Vector2 start, end;
        private readonly string flag;
        private readonly string jumpThruKey;
        private readonly ISourceEngineContext engine;
        private SimulationCoroutine boatRoutine, playerRoutine;
        private bool activated;

        public IvoryBoat(EntityData data, Vector2 offset, ISourceSolidWorld world) : base(data.Position + offset, 48,
            true, world)
        {
            engine = world.EngineContext;
            start = Position;
            end = data.NodesWithPosition(offset)[1];
            SurfaceSoundIndex = 5;
            flag = data.Attr("flag", "");
            jumpThruKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(SourceJumpThruEntityState))
                .RegistryKey;
            Add(sine = new SineWaveState(engine, .16f));
            Add(image = new SpriteImage(
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[
                    "objects/StrawberryJam2021/ivoryBoat/boat"]));
            image.Position = new Vector2(-8f, -9f);
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new IvoryBoat(data, offset,
            level.SolidWorldFactory.CreateJumpThru(data.Position + offset, true));

        public override void Added(object scene)
        {
            base.Added(scene);
            if (activated = ((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag)) X = end.X;
        }

        public override void Update()
        {
            base.Update();
            MoveToY(start.Y + sine.Value);
            if (activated) return;
            var player = GetPlayerRider();
            if (player != null)
            {
                activated = true;
                ((OriginalLevelSceneRoot)Scene).StartCutscene(level => SkipCutscene(level, player), false, false, true);
                Add(boatRoutine = new SimulationCoroutine(engine, BoatRoutine(), true));
                Add(playerRoutine = new SimulationCoroutine(engine, PlayerRoutine(player), true));
            }
        }

        private IEnumerator BoatRoutine()
        {
            float distance = end.X - start.X;
            while (end.X - X > .5f * distance)
            {
                MoveH(60f * engine.DeltaTime);
                yield return null;
            }

            float speed = 60f;
            while (X != end.X)
            {
                speed = ScalarMath.Approach(speed, 16f, 7f * engine.DeltaTime);
                MoveH(speed * engine.DeltaTime);
                yield return null;
            }
        }

        private IEnumerator PlayerRoutine(RoomPlayerSession player)
        {
            var level = (OriginalLevelSceneRoot)player.Scene;
            player.Machine.State = 11;
            player.ForceCameraUpdate = true;
            while (boatRoutine != null && !boatRoutine.Finished)
            {
                if (player.CollideCheck(jumpThruKey, player.Position + Vector2.UnitX * 8f))
                {
                    player.Jump(true, true);
                    player.Data.Motion.AutoJump = true;
                    player.Data.AutoJumpTimer = 1.8f;
                    yield return player.Dummy.RunTo(end.X + Width / 2f, false);
                    break;
                }

                yield return null;
            }

            player.Machine.State = 0;
            player.ForceCameraUpdate = false;
            level.Session.SetFlag(flag, true);
            level.EndCutscene();
        }

        public void SkipCutscene(OriginalLevelSceneRoot level, RoomPlayerSession player)
        {
            boatRoutine?.Cancel();
            playerRoutine?.Cancel();
            player.Machine.State = 0;
            player.ForceCameraUpdate = false;
            player.Data.Motion.AutoJump = false;
            level.Session.SetFlag(flag, true);
            X = end.X;
            player.Position = new Vector2(CenterX, Top);
            level.Camera.Position = level.GetFullCameraTargetAt(player, player.Position);
        }
    }
}
