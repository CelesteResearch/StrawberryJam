using System;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.StrawberryJam.Triggers;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Presentation.Hair;
using Celeste.Simulation.Presentation.Player.Death;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Dashes
{
    public sealed class DashCountColorHooks : IDisposable
    {
        private static readonly Action<PlayerDeathPresentation, PackedColor> WriteInitialHairColor =
            CreateHairColorSetter();

        private readonly ISourceEngineContext engine;
        private readonly string triggerRegistryKey;
        private readonly string playerRegistryKey;
        private Hook hairHook, trailHook, dieHook, deathDrawHook;

        public DashCountColorHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            var catalog = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog;
            triggerRegistryKey = catalog.Find(typeof(DashCountTrigger)).RegistryKey;
            playerRegistryKey = catalog.Find(typeof(RoomPlayerSession)).RegistryKey;
            try
            {
                using (new DetourConfigContext(new DetourConfig("StrawberryJam.DashCount", after: new[] { "*" }))
                           .Use())
                {
                    hairHook = new Hook(typeof(PlayerHair).GetMethod(nameof(PlayerHair.GetHairColor)),
                        (Func<Func<PlayerHair, int, PackedColor>, PlayerHair, int, PackedColor>)HairColor);
                    trailHook = new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.GetTrailColor)),
                        (Func<Func<RoomPlayerSession, bool, PackedColor>, RoomPlayerSession, bool, PackedColor>)
                        TrailColor);
                    dieHook = new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.Die)),
                        (Func<Func<RoomPlayerSession, Vector2, bool, bool, PlayerDeathPresentation>, RoomPlayerSession,
                            Vector2, bool, bool, PlayerDeathPresentation>)Die);
                    deathDrawHook = new Hook(typeof(DeathEffect).GetMethod(nameof(DeathEffect.Draw)),
                        (Action<Action<ISourceRenderContext, Vector2, PackedColor, float, object, ISourceEngineContext>,
                            ISourceRenderContext, Vector2, PackedColor, float, object, ISourceEngineContext>)DrawDeath);
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private bool HasTrigger(object scene) => scene != null &&
                                                 ((ISourceScene)scene).SourceScene.Entities
                                                 .GetTrackedEntity<DashCountTrigger>(triggerRegistryKey) != null;

        private PackedColor HairColor(Func<PlayerHair, int, PackedColor> original, PlayerHair hair, int index)
        {
            if (ReferenceEquals(hair.EngineContext, engine) && hair.Entity is RoomPlayerSession player &&
                HasTrigger(player.Scene))
                return player.Data.Dashes > 0
                    ? PlayerHairAppearance.NormalHairColor
                    : PlayerHairAppearance.UsedHairColor;
            return original(hair, index);
        }

        private PackedColor TrailColor(Func<RoomPlayerSession, bool, PackedColor> original, RoomPlayerSession player,
            bool wasDashB)
        {
            if (ReferenceEquals(player.EngineContext, engine) && player.Data.Dashes > 0 && HasTrigger(player.Scene))
                return PlayerHairAppearance.NormalHairColor;
            return original(player, wasDashB);
        }

        private PlayerDeathPresentation Die(
            Func<RoomPlayerSession, Vector2, bool, bool, PlayerDeathPresentation> original,
            RoomPlayerSession player, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
        {
            if (ReferenceEquals(player.EngineContext, engine) && HasTrigger(player.Scene))
            {
                PlayerDeathPresentation body = original(player, direction, evenIfInvincible, registerDeathInStats);
                if (body != null)
                    WriteInitialHairColor(body,
                        player.Data.Dashes > 0
                            ? PlayerHairAppearance.NormalHairColor
                            : PlayerHairAppearance.UsedHairColor);
                return body;
            }

            return original(player, direction, evenIfInvincible, registerDeathInStats);
        }

        private void DrawDeath(
            Action<ISourceRenderContext, Vector2, PackedColor, float, object, ISourceEngineContext> original,
            ISourceRenderContext context, Vector2 position, PackedColor color, float ease, object entity,
            ISourceEngineContext owner)
        {
            if (ReferenceEquals(owner, engine) && engine.Frame.Scenes.CurrentScene is OriginalLevelSceneRoot level &&
                HasTrigger(level) &&
                level.Scheduler.Entities.GetTrackedEntity<RoomPlayerSession>(playerRegistryKey) is RoomPlayerSession
                    player)
                color = player.Data.Dashes > 0
                    ? PlayerHairAppearance.NormalHairColor
                    : PlayerHairAppearance.UsedHairColor;
            original(context, position, color, ease, entity, owner);
        }

        private static Action<PlayerDeathPresentation, PackedColor> CreateHairColorSetter()
        {
            var field = typeof(PlayerDeathPresentation).GetField("initialHairColor",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var method = new DynamicMethod("WriteDashCountInitialHairColor", typeof(void),
                new[] { typeof(PlayerDeathPresentation), typeof(PackedColor) }, typeof(DashCountColorHooks).Module,
                true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            return (Action<PlayerDeathPresentation, PackedColor>)method.CreateDelegate(
                typeof(Action<PlayerDeathPresentation, PackedColor>));
        }

        public void Dispose()
        {
            deathDrawHook?.Dispose();
            deathDrawHook = null;
            dieHook?.Dispose();
            dieHook = null;
            trailHook?.Dispose();
            trailHook = null;
            hairHook?.Dispose();
            hairHook = null;
        }
    }
}
