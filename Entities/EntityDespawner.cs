using Celeste.Core.Reflection.Runtime;
using System;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Reflection;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.StrawberryJam.Entities
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.EntityDespawner")]
    [CustomEntity("SJ2021/EntityDespawner")]
    public sealed class EntityDespawner : SceneEntityState
    {
        private readonly Type[] typesToDespawn;
        public readonly bool Invert;
        public readonly string SessionFlagName;

        public EntityDespawner(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            var names = data.Attr("entityTypes");
            if (string.IsNullOrWhiteSpace(names)) throw new ArgumentException("Entity Types cannot be blank.");
            var aggregate = ((ISourceEverestReflectionEnvironment)engine).Reflection.GetFakeEntryAssembly();
            typesToDespawn = names.Split(',').Select(name => aggregate.GetType(name) ??
                                                             throw new ArgumentException(
                                                                 $"\"{name}\" is not a valid entity class name."))
                .ToArray();
            Invert = data.Bool("invert");
            SessionFlagName = data.Attr("flag");
        }

        public override void Awake(object scene)
        {
            var sessionFlag = ((OriginalLevelSceneRoot)Scene).Session.GetFlag(SessionFlagName);
            if (!(sessionFlag ^ Invert)) return;
            var registry = ((ISourceScene)scene).SourceScene.Entities;
            foreach (var type in typesToDespawn)
                if (registry.TryGetTrackedEntities(type, out var entities))
                    foreach (SceneEntityState entity in entities)
                        entity.RemoveSelf();
        }
    }
}
