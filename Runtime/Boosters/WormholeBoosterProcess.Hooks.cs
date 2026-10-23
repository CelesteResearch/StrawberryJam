using System;
using System.Collections;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Core.Components;
using Celeste.Graphics.Animation;
using Celeste.Game.Entities.Interaction.Boosters;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.StrawberryJam.Entities.Boosters;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Dash;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Boosters
{
    public sealed partial class WormholeBoosterProcess
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private bool Own(OriginalBoosterState booster) =>
            booster is WormholeBooster wormhole && ReferenceEquals(wormhole.Engine, Engine);

        private void InstallHooks()
        {
            if (hooks.Count != 0) return;
            hooks.Add(new Hook(typeof(OriginalBoosterState).GetMethod("AppearParticles", Flags),
                (Action<Action<OriginalBoosterState>, OriginalBoosterState>)AppearParticles));
            hooks.Add(new Hook(typeof(OriginalBoosterState).GetMethod("OnPlayer", Flags),
                (Action<Action<OriginalBoosterState, RoomPlayerSession>, OriginalBoosterState, RoomPlayerSession>)
                OnPlayer));
            hooks.Add(new Hook(typeof(OriginalBoosterState).GetMethod(nameof(OriginalBoosterState.PlayerReleased)),
                (Action<Action<OriginalBoosterState>, OriginalBoosterState>)PlayerReleased));
            hooks.Add(new ILHook(typeof(BoosterBubbleMotion).GetMethod(nameof(BoosterBubbleMotion.BeginRender)),
                RenderHook));
            hooks.Add(new Hook(typeof(PlayerBoostSequence).GetMethod(nameof(PlayerBoostSequence.BoostCoroutine)),
                (Func<Func<PlayerBoostSequence, IEnumerator>, PlayerBoostSequence, IEnumerator>)BoostCoroutine));
        }

        private void AppearParticles(Action<OriginalBoosterState> orig, OriginalBoosterState booster)
        {
            if (!Own(booster))
            {
                orig(booster);
                return;
            }

            for (int i = 0; i < 360; i += 30)
                ((OriginalLevelSceneRoot)booster.Scene).ParticlesBG.Emit(PAppear, 1, booster.Center,
                    System.Numerics.Vector2.One * 2f, (float)i * ((float)Math.PI / 180f));
        }

        private void OnPlayer(Action<OriginalBoosterState, RoomPlayerSession> orig, OriginalBoosterState booster,
            RoomPlayerSession player)
        {
            if (!Own(booster))
            {
                orig(booster, player);
                return;
            }

            var wormhole = (WormholeBooster)booster;
            if (wormhole.CanEnter)
                wormhole.Add(new SimulationCoroutine(Engine,
                    TeleDeath ? wormhole.KillCoroutine(player) : wormhole.TeleportCoroutine(player)));
        }

        private void PlayerReleased(Action<OriginalBoosterState> orig, OriginalBoosterState booster)
        {
            orig(booster);
            if (Own(booster)) CanTeleport = true;
        }

        private void RenderHook(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction =>
                        instruction.MatchLdfld<SourceComponentFields>(nameof(SourceComponentFields.Visible))))
                throw new InvalidOperationException(
                    "The actual BoosterBubbleMotion.BeginRender outline visibility read is missing.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<bool, Sprite, bool>>((original, sprite) =>
                sprite.Entity is OriginalBoosterState booster && Own(booster) ? false : original);
        }

        private IEnumerator BoostCoroutine(Func<PlayerBoostSequence, IEnumerator> orig, PlayerBoostSequence boost)
        {
            if (boost.Player.CurrentBooster is WormholeBooster wormhole && ReferenceEquals(wormhole.Engine, Engine))
            {
                yield return .45f;
                boost.Player.Machine.State = 2;
            }
            else
            {
                IEnumerator original = orig(boost);
                while (original.MoveNext()) yield return original.Current;
            }
        }
    }
}
