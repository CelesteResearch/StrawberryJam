using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Collectibles.Refills;
using Celeste.Graphics.Display.Colors;
using Celeste.Mods.StrawberryJam.Entities.Fields.Dashes;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Contacts;
using Celeste.Simulation.Gameplay.Player.Dash;
using Celeste.Simulation.Gameplay.Player.Movement;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Simulation.Presentation.Player.Death;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mods.StrawberryJam.Runtime.Fields.Dashes
{
    internal sealed class DashBoostFieldRuntime : IDisposable
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private readonly string fieldType;
        private readonly ISourceEngineContext engine;
        private readonly DashBoostFieldRuntimeState state;
        private readonly List<IDisposable> hooks = new();

        public DashBoostFieldRuntime(ISourceEngineContext engine)
        {
            this.engine = engine;
            fieldType = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(DashBoostField))
                .RegistryKey;
            state = DashBoostFieldRuntimeState.For(engine);
            try
            {
                hooks.Add(new ILHook(
                    typeof(OriginalLevelSceneRoot).GetMethod("UpdateSourceAssistSpeed", PrivateInstance), il =>
                    {
                        var cursor = new ILCursor(il);
                        if (cursor.TryGotoNext(MoveType.After, i => i.MatchLdcR4(10f), i => i.MatchDiv()))
                        {
                            cursor.Emit(OpCodes.Ldarg_0);
                            cursor.EmitDelegate<Func<float, OriginalLevelSceneRoot, float>>(ModifyTimeRate);
                        }
                    }));
                hooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.Update)),
                    (Action<Action<RoomPlayerSession>, RoomPlayerSession>)OnPlayerUpdate));
                hooks.Add(new Hook(typeof(PlayerContacts).GetMethod(nameof(PlayerContacts.UnderwaterMusicCheck)),
                    (Func<Func<PlayerContacts, bool>, PlayerContacts, bool>)UnderwaterMusicCheck));
                hooks.Add(new Hook(typeof(DashSequence).GetMethod(nameof(DashSequence.Begin)),
                    (Action<Action<DashSequence>, DashSequence>)OnDashBegin));
                var coroutine = typeof(DashSequence).GetMethod(nameof(DashSequence.Coroutine)).GetStateMachineTarget();
                hooks.Add(new ILHook(coroutine, il =>
                {
                    var cursor = new ILCursor(il);
                    if (cursor.TryGotoNext(MoveType.Before,
                            i => i.MatchStfld<NormalMotionState>(nameof(NormalMotionState.Speed))))
                    {
                        cursor.Emit(OpCodes.Ldarg_0);
                        cursor.Emit(OpCodes.Ldfld, coroutine.DeclaringType.GetField("<>4__this"));
                        cursor.EmitDelegate<Func<Vector2, DashSequence, Vector2>>(ModifyDashSpeed);
                    }
                }));
                hooks.Add(new ILHook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.SuperWallJump)),
                    il => ModifyJumpConstant(il, -160f)));
                hooks.Add(new ILHook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.SuperJump)),
                    il => ModifyJumpConstant(il, 260f)));
                hooks.Add(new ILHook(typeof(RoomPlayerSession).GetMethod("DreamDashBegin", PrivateInstance),
                    il => ModifyJumpConstant(il, 240f)));
                hooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.Die)),
                    (Func<Func<RoomPlayerSession, Vector2, bool, bool, PlayerDeathPresentation>, RoomPlayerSession,
                        Vector2, bool, bool, PlayerDeathPresentation>)OnPlayerDeath));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void LoadParticles()
        {
            state.P_RedRefill = new ParticleTypePresentation(((IOriginalRefillProcess)engine).RefillParticles.P_Shatter)
            {
                Color = SourceHexColor.HexToColor("ffb0b0"),
                Color2 = SourceHexColor.HexToColor("ffd8d8")
            };
        }

        private float ModifyTimeRate(float rate, OriginalLevelSceneRoot level) =>
            ReferenceEquals(level.EngineContext, engine) && !level.Paused ? rate * state.CurrentTimeRateMult : rate;

        private static bool IsDashingOrRespawning(RoomPlayerSession player) =>
            player.Machine.State == 2 || player.Machine.State == 14;

        private void OnPlayerUpdate(Action<RoomPlayerSession> original, RoomPlayerSession player)
        {
            if (!ReferenceEquals(player.EngineContext, engine))
            {
                original(player);
                return;
            }

            bool wasDashing = player.Data.DashAttacking(player.Machine.State) || player.Machine.State == 2;
            original(player);
            var field = player.CollideFirst<DashBoostField>(fieldType);
            state.CurrentTimeRateMult = !player.Dead && field != null && field.Active && !IsDashingOrRespawning(player)
                ? field.TargetTimeRateMult
                : 1f;
            if (wasDashing && !(player.Data.DashAttacking(player.Machine.State) || player.Machine.State == 2))
                state.Players.GetValue(player, _ => new DashBoostPlayerState()).DashBoosted = false;
        }

        private bool UnderwaterMusicCheck(Func<PlayerContacts, bool> original, PlayerContacts contacts)
        {
            var player = contacts.ActualOwner as RoomPlayerSession;
            if (player != null && ReferenceEquals(player.EngineContext, engine) && player.CollideCheck(fieldType) &&
                !IsDashingOrRespawning(player)) return true;
            return original(contacts);
        }

        private void OnDashBegin(Action<DashSequence> original, DashSequence dash)
        {
            if (ReferenceEquals(dash.EngineContext, engine) && dash.World is RoomPlayerWorld world)
            {
                var player = world.SourcePlayer;
                var data = state.Players.GetValue(player, _ => new DashBoostPlayerState());
                var field = player.CollideFirst<DashBoostField>(fieldType);
                if (field != null && field.Active)
                {
                    data.DashBoosted = true;
                    data.DashBoostSpeed = field.DashSpeedMult;
                    player.OwnedComponents.Add(new SimulationCoroutine(engine, RefillDashIfSelected(player)));
                }
                else data.DashBoosted = false;
            }

            original(dash);
        }

        private float ModifySpeed(float speed, RoomPlayerSession player) =>
            ReferenceEquals(player.EngineContext, engine) && state.Players.TryGetValue(player, out var data) &&
            data.DashBoosted
                ? speed * data.DashBoostSpeed
                : speed;

        private Vector2 ModifyDashSpeed(Vector2 speed, DashSequence dash) =>
            ReferenceEquals(dash.EngineContext, engine) && dash.World is RoomPlayerWorld world &&
            state.Players.TryGetValue(world.SourcePlayer, out var data) && data.DashBoosted
                ? speed * data.DashBoostSpeed
                : speed;

        private void ModifyJumpConstant(ILContext il, float constant)
        {
            var cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, i => i.MatchLdcR4(constant)))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<float, RoomPlayerSession, float>>(ModifySpeed);
            }
        }

        private IEnumerator RefillDashIfSelected(RoomPlayerSession player)
        {
            var field = player.CollideFirst<DashBoostField>(fieldType);
            if (field?.PreserveDash == true)
            {
                yield return null;
                player.Data.Dashes++;
                ((IOriginalGameplayProcess)engine).AudioSession.Play("event:/game/general/diamond_touch");
                var level = (OriginalLevelSceneRoot)player.Scene;
                float angle = Vector2Geometry.Angle(player.Data.Motion.Normal.Speed);
                level.ParticlesFG.Emit(state.P_RedRefill, 5, player.Position, Vector2.One * 4f,
                    angle - (float)Math.PI / 2f);
                level.ParticlesFG.Emit(state.P_RedRefill, 5, player.Position, Vector2.One * 4f,
                    angle + (float)Math.PI / 2f);
            }
        }

        private PlayerDeathPresentation OnPlayerDeath(
            Func<RoomPlayerSession, Vector2, bool, bool, PlayerDeathPresentation> original, RoomPlayerSession player,
            Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
        {
            if (ReferenceEquals(player.EngineContext, engine)) state.CurrentTimeRateMult = 1f;
            return original(player, direction, evenIfInvincible, registerDeathInStats);
        }

        public void Dispose()
        {
            foreach (var hook in hooks) hook.Dispose();
            hooks.Clear();
        }
    }
}
