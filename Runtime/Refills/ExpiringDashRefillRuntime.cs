using System;
using System.Collections.Generic;
using MonoMod.RuntimeDetour;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Processes;
using Celeste.Mods.StrawberryJam.Persistence;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Dash;
using Celeste.Simulation.Gameplay.Player.Presentation.Hair;
using Celeste.Simulation.Presentation.Player.Death;

namespace Celeste.Mods.StrawberryJam.Runtime.Refills
{
    internal sealed class ExpiringDashRefillRuntime : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly ExpiringDashRefillState state;
        private readonly List<IDisposable> hooks = new();

        public ExpiringDashRefillRuntime(ISourceEngineContext engine, Func<StrawberryJamSession> getSession)
        {
            this.engine = engine;
            state = ExpiringDashRefillState.For(engine);
            state.GetSession = getSession;
            try
            {
                hooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.Update)),
                    (Action<Action<RoomPlayerSession>, RoomPlayerSession>)Update));
                hooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.Die)),
                    (Func<Func<RoomPlayerSession, System.Numerics.Vector2, bool, bool, PlayerDeathPresentation>,
                        RoomPlayerSession, System.Numerics.Vector2, bool, bool, PlayerDeathPresentation>)
                    OnPlayerDeath));
                hooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.OnTransition)),
                    (Action<Action<RoomPlayerSession>, RoomPlayerSession>)OnTransition));
                hooks.Add(new Hook(typeof(DashSequence).GetMethod(nameof(DashSequence.Begin)),
                    (Action<Action<DashSequence>, DashSequence>)OnDashBegin));
                hooks.Add(new Hook(typeof(PlayerHair).GetMethod(nameof(PlayerHair.GetHairColor)),
                    (Func<Func<PlayerHair, int, PackedColor>, PlayerHair, int, PackedColor>)GetHairColor));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private PackedColor GetHairColor(Func<PlayerHair, int, PackedColor> original, PlayerHair hair, int index) =>
            ReferenceEquals(hair.EngineContext, engine) && state.ExpireFlash
                ? PlayerHairAppearance.FlashHairColor
                : original(hair, index);

        private void OnDashBegin(Action<DashSequence> original, DashSequence dash)
        {
            if (ReferenceEquals(dash.EngineContext, engine))
            {
                state.Session.ExpiringDashRemainingTime = 0;
                state.ExpireFlash = false;
            }

            original(dash);
        }

        private PlayerDeathPresentation OnPlayerDeath(
            Func<RoomPlayerSession, System.Numerics.Vector2, bool, bool, PlayerDeathPresentation> original,
            RoomPlayerSession player, System.Numerics.Vector2 direction, bool evenIfInvincible,
            bool registerDeathInStats)
        {
            if (ReferenceEquals(player.EngineContext, engine) &&
                (evenIfInvincible || !((IOriginalGameplayProcess)engine).SaveContext.CurrentSave.Assists.Invincible))
            {
                state.ExpireFlash = false;
                state.Session.ExpiringDashRemainingTime = 0;
            }

            return original(player, direction, evenIfInvincible, registerDeathInStats);
        }

        private void OnTransition(Action<RoomPlayerSession> original, RoomPlayerSession player)
        {
            if (ReferenceEquals(player.EngineContext, engine) && state.Session.ExpiringDashRemainingTime > 0)
            {
                player.Data.Dashes--;
                state.ExpireFlash = false;
                state.Session.ExpiringDashRemainingTime = 0;
            }

            original(player);
        }

        private void Update(Action<RoomPlayerSession> original, RoomPlayerSession player)
        {
            if (!ReferenceEquals(player.EngineContext, engine))
            {
                original(player);
                return;
            }

            if (!player.Inventory.NoRefills && player.OnGround() &&
                player.Data.Dashes <= player.Motor.BeforeState.MaxDashes)
            {
                state.Session.ExpiringDashRemainingTime = 0;
                state.ExpireFlash = false;
            }

            var session = state.Session;
            if (session.ExpiringDashRemainingTime <= 0)
            {
                original(player);
                return;
            }

            session.ExpiringDashRemainingTime -= engine.DeltaTime;
            if (session.ExpiringDashRemainingTime <= 0)
            {
                player.Data.Dashes--;
                state.ExpireFlash = false;
                original(player);
                return;
            }

            if (session.ExpiringDashRemainingTime <= session.ExpiringDashFlashThreshold &&
                ((ISourceScene)player.Scene).SourceScene.Clock.OnInterval(0.05f, engine))
                state.ExpireFlash = !state.ExpireFlash;
            original(player);
        }

        public void Dispose()
        {
            foreach (var hook in hooks) hook.Dispose();
            hooks.Clear();
        }
    }
}
