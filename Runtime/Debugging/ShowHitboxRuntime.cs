using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection;
using Celeste.Everest.Reflection;
using Celeste.Everest.Reflection.Runtime;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Mods.StrawberryJam.Triggers.Debugging;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.StrawberryJam.Runtime.Debugging
{
    internal sealed partial class ShowHitboxRuntime
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, ShowHitboxRuntime> Instances = new();
        private readonly ISourceEngineContext engine;
        private readonly SceneEntityCatalog catalog;
        private readonly Dictionary<Type, string[]> typeNames = new();
        public readonly HashSet<string> EnabledTypeNames = new();
        private long revision = -1;
        private SourceDrawState draw;
        private bool loaded;

        private ShowHitboxRuntime(ISourceEngineContext engine)
        {
            this.engine = engine;
            catalog = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog;
        }

        internal static ShowHitboxRuntime For(ISourceEngineContext engine) =>
            Instances.GetValue(engine, key => new(key));

        internal void BindDraw() => draw = ((ISourceDrawScope)engine).Draw;

        internal void Clear()
        {
            EnabledTypeNames.Clear();
            revision = -1;
        }

        internal void Refresh()
        {
            Clear();
            var level = (OriginalLevelSceneRoot)engine.Frame.Scenes.CurrentScene;
            foreach (var item in level.Scheduler.Entities.GetTrackedEntities(typeof(ShowHitboxTrigger)))
                if (item is ShowHitboxTrigger trigger && trigger.PlayerIsInside)
                    EnabledTypeNames.UnionWith(trigger.TypeNames);
            RebuildSelection();
        }

        private void RebuildSelection()
        {
            foreach (var registration in catalog.RuntimeRegistrations) AddType(registration.ActualType);
            foreach (var metadata in ((ISourceRuntimeTypeDiscoveryEnvironment)engine).RuntimeTypeDiscovery.GetTypes())
                AddType(metadata.RequireActualType());
            revision = catalog.ActualSourceBindingsRevision;
        }

        private void AddType(Type type)
        {
            if (type == null || typeNames.ContainsKey(type)) return;
            var names = new List<string> { type.FullName, type.Name };
            var members = ((ISourceEverestReflectionEnvironment)engine).Reflection.SourceMembers;
            if (members.ContainsSourceType(type)) AddName(names, members.GetOriginalTypeDefinition(type).Identity);
            foreach (var attribute in type.GetCustomAttributes(
                         typeof(Celeste.Core.Reflection.Runtime.RuntimeTypeAliasAttribute), false))
                AddName(names, ((Celeste.Core.Reflection.Runtime.RuntimeTypeAliasAttribute)attribute).Name);
            typeNames.Add(type, names.ToArray());
        }

        private static void AddName(List<string> names, string identity)
        {
            names.Add(identity);
            names.Add(identity.Substring(Math.Max(identity.LastIndexOf('.'), identity.LastIndexOf('+')) + 1));
        }

        private bool Selected(Type type)
        {
            if (!typeNames.TryGetValue(type, out var names)) return false;
            for (int i = 0; i < names.Length; i++)
                if (EnabledTypeNames.Contains(names[i]))
                    return true;
            return false;
        }

        private void DrawEnabledHitboxes(Celeste.Game.Presentation.Rendering.GameplayRendererState renderer,
            OriginalLevelSceneRoot level)
        {
            if (EnabledTypeNames.Count == 0) return;
            if (revision != catalog.ActualSourceBindingsRevision) RebuildSelection();
            var entities = level.Scheduler.Entities.Entities;
            for (int i = 0; i < entities.Count; i++)
                if (entities[i] is SceneEntityState entity && Selected(entity.GetType()))
                    entity.DebugRender(level.SourceRenderContext, renderer.Camera);
        }
    }
}
