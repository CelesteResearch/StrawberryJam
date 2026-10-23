using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Save.Runtime;
using Celeste.Mods.CollabUtils2.Triggers;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Audio.Sources;

namespace Celeste.Mods.StrawberryJam.Entities
{
    [CustomEntity("SJ2021/StrawberryJamJar")]
    public sealed class StrawberryJamJar : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly string map;
        private readonly string returnToLobbyMode;
        private readonly bool allowSaving;

        public StrawberryJamJar(ISourceEngineContext engine, Vector2 position, string spriteName, string map,
            string returnToLobbyMode, bool allowSaving)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
            this.map = map;
            this.returnToLobbyMode = returnToLobbyMode;
            this.allowSaving = allowSaving;

            var module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<StrawberryJamModule>().Single();
            var area = ((IOriginalAreaKeyEnvironment)engine).AreaKeys.GetAreaBySID(map);
            var complete = false;
            if (area != null)
            {
                var save = ((IOriginalGameplayProcess)engine).SaveContext.CurrentSave;
                var stats = ((IOriginalSaveCounterEnvironment)engine).SaveCounters.GetAreasSafe(save)[area.ID];
                complete = stats?.Modes[0].Completed ?? false;
            }

            string animation;
            if (complete)
            {
                if (!module.SaveData.FilledJamJarSIDs.Contains(map))
                {
                    animation = "before_fill";
                    module.SaveData.FilledJamJarSIDs.Add(map);
                }
                else
                {
                    animation = "full";
                }
            }
            else
            {
                animation = "empty";
            }

            var sprite = module.SpriteBank.Create("jamJar_" + spriteName);
            sprite.Play(animation);
            Add(sprite);
            if (animation == "before_fill")
            {
                sprite.OnChange = (_, currentAnimationId) =>
                {
                    if (currentAnimationId != "fill") return;
                    var sound = new SoundSourceComponentState(engine, new Vector2(0, -20f),
                        PickFillSoundEffect(spriteName)) { RemoveOnOneshotEnd = true };
                    sound.SetVolume(0.3f);
                    Add(sound);
                };
            }

            Depth = 1000;
        }

        public StrawberryJamJar(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data.Position + offset, data.Attr("sprite"), data.Attr("map"),
                data.Attr("returnToLobbyMode"), data.Bool("allowSaving"))
        {
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            ((ISourceScene)scene).SourceScene.AddEntity(new ChapterPanelTrigger(engine, new EntityData
            {
                Position = Position - new Vector2(24f, 32f),
                Width = 48,
                Height = 32,
                Nodes = new[] { Position - new Vector2(0, 32) },
                Values = new Dictionary<string, object>
                {
                    ["map"] = map,
                    ["returnToLobbyMode"] = returnToLobbyMode,
                    ["allowSaving"] = allowSaving
                }
            }, Vector2.Zero));
        }

        private static string PickFillSoundEffect(string spriteName)
        {
            return spriteName switch
            {
                "beginner" => "event:/sj21_jamjar-blue",
                "intermediate" => "event:/sj21_jamjar-red",
                "advanced" => "event:/sj21_jamjar-yellow",
                "expert" => "event:/sj21_jamjar-orange",
                "grandmaster" => "event:/sj21_jamjar-purple",
                _ => "event:/strawberry_jam_2021/game/jars/open_demo"
            };
        }
    }
}
