using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Startup;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Audio;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Display.Colors;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Enemies.Seeker;
using Celeste.Simulation.Audio.Sources;
using Celeste.Input.Gameplay;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Game.Controls.Source;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Mods.StrawberryJam.Runtime.Holdables.Umbrellas;
using Celeste.Mods.StrawberryJam.Entities.Holdables.Umbrellas;

namespace Celeste.Mods.StrawberryJam.Entities.Holdables.Umbrellas
{
    [CustomEntity("SJ2021/PocketUmbrellaController")]
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.PocketUmbrellaController")]
    public class PocketUmbrellaController : SceneEntityState
    {
        private Vector2 spawnOffset;
        private float holdDelay;
        private bool prevGrab;
        public bool Enabled { get; set; }
        public float StaminaCost { get; set; }
        public float Cooldown { get; set; }

        public string MusicParam { get; set; }

        public PocketUmbrellaController(ISourceEngineContext engine) : this(engine, 0, false)
        {
        }

        public PocketUmbrellaController(ISourceEngineContext engine, float cost, bool enabled, float cooldown = 0.2f) :
            base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = engine;
            var catalog = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog;
            playerRegistryKey = catalog.Find(typeof(RoomPlayerSession)).RegistryKey;
            barrierRegistryKey = catalog.Find(typeof(SeekerBarrierState)).RegistryKey;
            holdableRegistryKey = catalog.Find(typeof(HoldableState)).RegistryKey;
            AddTag(((IOriginalGameTagsEnvironment)engine).Tags.Global);
            StaminaCost = cost;
            Enabled = enabled;
            Cooldown = cooldown;
            spawnOffset = new Vector2(0f, -12f);
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level,
            Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room, Vector2 offset,
            Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData data) =>
            new PocketUmbrellaController(level.EngineContext);

        private readonly ISourceEngineContext engine;
        private readonly string playerRegistryKey, barrierRegistryKey, holdableRegistryKey;
        private ISourceGameInput Input => ((ISourceGameInputEnvironment)engine).Input;
        private Celeste.Scene.Scheduling.SceneUpdateScheduler Scheduler => ((ISourceScene)Scene).SourceScene;

        internal void CheckDrop(RoomPlayerSession player)
        {
            if (Enabled) dropUmbrella((PocketUmbrella)player.Holding.Holding.Entity);
        }

        public override void Update()
        {
            base.Update();
            if (holdDelay > 0)
            {
                holdDelay -= engine.DeltaTime;
                return;
            }

            RoomPlayerSession player =
                ((ISourceScene)engine.Frame.Scenes.CurrentScene).SourceScene.Entities
                .GetTrackedEntity<RoomPlayerSession>(playerRegistryKey);
            if (player is null)
            {
                return;
            }

            if (Enabled && player.Dead == false)
            {
                if (shouldGrabWall(player))
                {
                    player.Machine.State = 1;
                }
                else if (grab_check())
                {
                    if (player.Holding.Holding == null && exclusiveGrabCollide(player))
                    {
                        if (trySpawnJelly(out PocketUmbrella umbrella, player))
                        {
                            Scheduler.QueueSourceEntity(umbrella);
                        }
                    }
                }
            }
        }

        private bool shouldGrabWall(RoomPlayerSession player)
        {
            // this essentially emulates the checks that Player.NormalUpdate makes to figure out if the player should transition to grab state or not
            // except for the Holding == null part as well as all stamina and ducking checks because the player *must* be unducked and have stamina to hold the umbrella.
            return player.Holding.Holding?.Entity is PocketUmbrella && player.Machine.State == 0 &&
                   Input.GrabCheck && player.Speed.Y > 0f && !(Math.Sign(player.Speed.X) == -(int)player.Facing) &&
                   // all the above checks are common for the two individual situations where the game decides to switch to StGrab, the or'd checks below are the unique checks.
                   (
                       (player.World.ClimbCheck((int)player.Facing, 0) && !player.Settings.NoGrabbing) ||
                       !player.Settings.NoGrabbing && Input.MoveY.Value < 1f && weirdCheck(player)
                   );
        }

        //this is called that bc I honestly have no clue what it does or why it does it but hey its in the original code so its here too.
        private bool weirdCheck(RoomPlayerSession player)
        {
            for (int i = 1; i <= 2; i++)
            {
                if (!player.World.SolidAt(Vector2.UnitY * -i) && player.World.ClimbCheck((int)player.Facing, -i))
                {
                    player.World.MoveVExact(-i);
                    return true;
                }
            }

            return false;
        }

        internal static void FrozenUpdate(ISourceEngineContext engine, string controllerRegistryKey,
            string playerRegistryKey)
        {
            if (engine.Frame.Effects.FreezeTimer <= 0)
            {
                return;
            }

            RoomPlayerSession player =
                ((ISourceScene)engine.Frame.Scenes.CurrentScene).SourceScene.Entities
                .GetTrackedEntity<RoomPlayerSession>(playerRegistryKey);
            if (player is null)
            {
                return;
            }

            foreach (PocketUmbrellaController controller in ((ISourceScene)engine.Frame.Scenes.CurrentScene).SourceScene
                     .Entities.GetTrackedEntities(controllerRegistryKey))
            {
                if (controller.Enabled && player.Dead == false && player.Machine.State != 2 &&
                    !((ISourceGameInputEnvironment)engine).Input.Dash.Check)
                {
                    if (controller.grab_check())
                    {
                        if (player.Holding.Holding == null && controller.exclusiveGrabCollide(player))
                        {
                            if (controller.trySpawnJelly(out PocketUmbrella umbrella, player))
                            {
                                controller.Scheduler.QueueSourceEntity(umbrella);
                            }
                        }
                    }
                }
            }
        }

        private bool grab_check()
        {
            bool result = Input.GrabCheck && !prevGrab;
            prevGrab = Input.GrabCheck;
            return result;
        }

        private bool trySpawnJelly(out PocketUmbrella umbrella, RoomPlayerSession player)
        {
            umbrella = new PocketUmbrella(engine, player.Position + spawnOffset, StaminaCost, MusicParam);
            if (!checkSpawnCondition(player))
            {
                return false;
            }

            foreach (SeekerBarrierState entity in Scheduler.Entities.GetTrackedEntities(barrierRegistryKey))
            {
                if (umbrella.Collider.Collide(entity.Collider))
                {
                    return false;
                }
            }

            return true;
        }

        private bool checkSpawnCondition(RoomPlayerSession player)
        {
            return player.Data.Stamina > 20 && !player.Data.Motion.Ducking &&
                   !player.World.ClimbCheck((int)player.Facing, 0) && playerStateCheck(player);
        }

        private bool playerStateCheck(RoomPlayerSession player)
        {
            return player.Machine.State is 0 or 2 or 7;
        }

        private void dropUmbrella(PocketUmbrella umbrella)
        {
            holdDelay = Cooldown;
            umbrella.destroyed = true;
            umbrella.Collidable = false;
            umbrella.Hold.Active = false;
            ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble((int)RumbleStrength.Medium,
                (int)RumbleLength.Medium);
            umbrella.Add(new SimulationCoroutine(engine, umbrella.DestroyAnimationRoutine()));
        }

        private bool exclusiveGrabCollide(RoomPlayerSession player)
        {
            var components =
                ((ISourceScene)player.Scene).SourceScene.Components.GetTrackedComponents(holdableRegistryKey);
            if (components.Count == 0) return true;
            foreach (HoldableState holdable in components)
                if (holdable.Check(player))
                    return false;
            return true;
        }
    }
}
