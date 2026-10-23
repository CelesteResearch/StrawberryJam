using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Components;
using Celeste.Core.Components.Rendering;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Members;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Reflection;
using Celeste.Everest.Reflection.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Presentation.Lighting;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Display.Colors;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Mods.StrawberryJam.Runtime;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Entities
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.GlowController")]
    [CustomEntity("SJ2021/GlowController")]
    public sealed class GlowController : SceneEntityState
    {
        private const string DeathAnimationId = "death";
        private const string RespawnAnimationId = "respawn";
        private readonly ISourceEngineContext engine;
        private readonly SourceMemberCatalog members;
        private readonly string[] lightWhitelist;
        private readonly string[] lightBlacklist;
        private readonly PackedColor lightColor;
        private readonly float lightAlpha;
        private readonly int lightStartFade;
        private readonly int lightEndFade;
        private readonly Vector2 lightOffset;
        private readonly string[] bloomWhitelist;
        private readonly string[] bloomBlacklist;
        private readonly float bloomAlpha;
        private readonly float bloomRadius;
        private readonly Vector2 bloomOffset;

        public GlowController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            members = ((ISourceEverestReflectionEnvironment)engine).Reflection.SourceMembers;
            lightWhitelist = data.Attr("lightWhitelist").Split(',');
            lightBlacklist = data.Attr("lightBlacklist").Split(',');
            lightColor = SourceHexColor.HexToColor(data.Attr("lightColor", "FFFFFF"));
            lightAlpha = data.Float("lightAlpha", 1f);
            lightStartFade = data.Int("lightStartFade", 24);
            lightEndFade = data.Int("lightEndFade", 48);
            lightOffset = new Vector2(data.Int("lightOffsetX"), data.Int("lightOffsetY", -10));
            bloomWhitelist = data.Attr("bloomWhitelist").Split(',');
            bloomBlacklist = data.Attr("bloomBlacklist").Split(',');
            bloomAlpha = data.Float("bloomAlpha", 1f);
            bloomRadius = data.Float("bloomRadius", 8f);
            bloomOffset = new Vector2(data.Int("bloomOffsetX"), data.Int("bloomOffsetY", -10));
        }

        public static IDisposable InstallHook(ISourceEngineContext engine)
        {
            return new ILHook(typeof(LightingRendererPresentation).GetMethod("BeforeRender"),
                il => BeforeRender(il, engine));
        }

        private static void BeforeRender(ILContext il, ISourceEngineContext owner)
        {
            var cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, typeof(LightingRendererPresentation).GetField("engine",
                BindingFlags.NonPublic | BindingFlags.Instance));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, typeof(LightingRendererPresentation).GetField("lights",
                BindingFlags.NonPublic | BindingFlags.Instance));
            cursor.EmitDelegate<Action<ISourceEngineContext, VertexLightState[]>>((actualEngine, lights) =>
            {
                if (!ReferenceEquals(actualEngine, owner)) return;
                for (var i = 0; i < lights.Length; i++)
                {
                    if (lights[i] == null || lights[i].Entity != null) continue;
                    lights[i].Index = -1;
                    lights[i] = null;
                }
            });
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            var scheduler = ((ISourceScene)scene).SourceScene;
            using var typeNames = new GlowTypeNameCache(typeof(GlowController).GetMethod(
                nameof(GetOriginalTypeFullName), BindingFlags.Instance | BindingFlags.NonPublic));
            Func<Type, string> resolveTypeName = GetOriginalTypeFullName;
            foreach (var entity in scheduler.Entities.Entities.Concat(scheduler.GetToAdd()))
            {
                var fullName = typeNames.Get(entity.GetType(), members, resolveTypeName);
                var components = ((ISourceOwnedComponentList)entity).OwnedComponents;
                var requiresRemovalRoutine = false;
                if (lightWhitelist.Contains(fullName))
                {
                    components.Add(new VertexLightState(engine, lightOffset, lightColor, lightAlpha,
                        lightStartFade, lightEndFade));
                    requiresRemovalRoutine = true;
                }
                else if (lightBlacklist.Contains(fullName))
                {
                    components.Remove(components.GetAll<VertexLightState>().Cast<SourceComponentState>().ToArray());
                }

                if (bloomWhitelist.Contains(fullName))
                {
                    components.Add(new BloomPointState(bloomOffset, bloomAlpha, bloomRadius));
                    requiresRemovalRoutine = true;
                }
                else if (bloomBlacklist.Contains(fullName))
                {
                    components.Remove(components.GetAll<BloomPointState>().Cast<SourceComponentState>().ToArray());
                    components.Remove(components.GetAll<CustomBloomCallbackComponentState>()
                        .Cast<SourceComponentState>().ToArray());
                }

                if (requiresRemovalRoutine &&
                    components.GetAll<Sprite>().FirstOrDefault(sprite => sprite.Has(DeathAnimationId)) is { } sprite)
                    components.Add(new SimulationCoroutine(engine, DeathRemovalRoutine(entity, sprite)));
            }
        }

        private string GetOriginalTypeFullName(Type type)
            => members.ContainsSourceType(type)
                ? members.GetOriginalTypeFullName(type)
                : ((ISourceRuntimeTypeDiscoveryEnvironment)engine).RuntimeTypeDiscovery.TypeAliases
                .GetActiveRuntimeTypeAlias(type) ?? type.FullName;

        private IEnumerator DeathRemovalRoutine(object entity, Sprite sprite)
        {
            void SetAlpha(float alpha)
            {
                var components = ((ISourceOwnedComponentList)entity).OwnedComponents;
                foreach (var light in components.GetAll<VertexLightState>()) light.Alpha = alpha;
                foreach (var bloom in components.GetAll<BloomPointState>()) bloom.Alpha = alpha;
            }

            if (!sprite.Animations.TryGetValue(DeathAnimationId, out var deathAnimation)) yield break;
            var association = (ISourceEntityScene)entity;
            while (association.Scene != null)
            {
                while (association.Scene != null && sprite.CurrentAnimationID != DeathAnimationId) yield return null;
                var fadeTime = deathAnimation.Frames.Length * deathAnimation.Delay;
                var fadeRemaining = fadeTime;
                while (association.Scene != null && sprite.CurrentAnimationID == DeathAnimationId && fadeRemaining > 0)
                {
                    fadeRemaining -= engine.DeltaTime;
                    SetAlpha(Math.Max(fadeRemaining / fadeTime, 0f));
                    yield return null;
                }

                if (!sprite.Animations.TryGetValue(RespawnAnimationId, out var respawnAnimation)) break;
                while (association.Scene != null && sprite.CurrentAnimationID != RespawnAnimationId) yield return null;
                fadeTime = respawnAnimation.Frames.Length * respawnAnimation.Delay;
                fadeRemaining = fadeTime;
                while (association.Scene != null && sprite.CurrentAnimationID == RespawnAnimationId &&
                       fadeRemaining > 0)
                {
                    fadeRemaining -= engine.DeltaTime;
                    SetAlpha(1f - Math.Max(fadeRemaining / fadeTime, 0f));
                    yield return null;
                }
            }
        }
    }
}
