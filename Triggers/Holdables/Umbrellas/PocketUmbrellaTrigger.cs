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

namespace Celeste.Mods.StrawberryJam.Triggers.Holdables.Umbrellas
{
    [CustomEntity("SJ2021/PocketUmbrellaTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Triggers.PocketUmbrellaTrigger")]
    public class PocketUmbrellaTrigger : Celeste.Simulation.Gameplay.Triggers.Foundation.SourceTriggerEntityState
    {
        private bool Enable = true, revertOnLeave = false, prevVal;
        private float staminaCost, prevCost, cooldown, prevCooldown;

        private string musicParam;
        private readonly ISourceEngineContext engine;
        private readonly string controllerRegistryKey;

        public PocketUmbrellaTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
            data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            controllerRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog
                .Find(typeof(PocketUmbrellaController)).RegistryKey;
            Enable = data.Bool("enabled", true);
            revertOnLeave = data.Bool("revertOnLeave", false);
            staminaCost = data.Float("staminaCost", 100 / 2.2f);
            cooldown = data.Float("cooldown", 0.2f);
            musicParam = data.Attr("musicParam", "");
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            PocketUmbrellaController controller =
                ((ISourceScene)engine.Frame.Scenes.CurrentScene).SourceScene.Entities
                .GetTrackedEntity<PocketUmbrellaController>(controllerRegistryKey);
            if (controller == null)
            {
                ((ISourceScene)Scene).SourceScene.QueueSourceEntity(controller = new PocketUmbrellaController(engine));
            }

            prevVal = controller.Enabled;
            prevCost = controller.StaminaCost;
            prevCooldown = controller.Cooldown;
            if (Enable)
            {
                controller.Enabled = true;
                controller.StaminaCost = staminaCost;
                controller.Cooldown = cooldown;
                controller.MusicParam = musicParam;
            }
            else
            {
                ((ISourceScene)Scene).SourceScene.RemoveScheduledEntity(controller);
            }
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            base.OnLeave(player);
            PocketUmbrellaController controller =
                ((ISourceScene)engine.Frame.Scenes.CurrentScene).SourceScene.Entities
                .GetTrackedEntity<PocketUmbrellaController>(controllerRegistryKey);
            if (revertOnLeave && controller != null)
            {
                controller.StaminaCost = prevCost;
                controller.Cooldown = prevCooldown;
                if (prevVal)
                {
                    controller.Enabled = true;
                }
                else
                {
                    ((ISourceScene)Scene).SourceScene.RemoveScheduledEntity(controller);
                }
            }
        }
    }
}
